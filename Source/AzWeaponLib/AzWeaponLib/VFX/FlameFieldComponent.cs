using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AzWeaponLib.VFX
{
    /// <summary>
    /// 地面火焰速度场（整张地图一张 RGBA 场）。
    ///
    /// 数据流：
    ///   射流每 tick 往格子里「撒」一点速度 + 热度（高斯核，方向来自弹体速度）
    ///     → 每 tick 对活跃格做衰减，编码成 RGBA32 贴图（r/g = 速度，b = 热量，a = 是否实心）
    ///     → 每帧上传给 GPU，由 shader 在「格子空间」生成噪声、被速度场平流
    ///     → 一张覆盖地图的 quad 一次画完
    ///
    /// 火焰是连续介质，不是精灵/贴图叠加；也不产生任何 Thing，没有存档开销。
    /// 活跃格归零后停止上传与绘制，成本为 0。
    /// </summary>
    public class FlameFieldComponent : MapComponent
    {
        // ---- 调参 ----
        // 卷积核随飞行距离长大：出膛时只影响所在的一格，射程末端才散开
        private const float MuzzleSplatRadius = 0.5f;  // 出膛：≤1 格
        private const float EndSplatRadius = 2.6f;     // 末端：散开到约 5 格宽

        // 核先按 1/Σw 归一化（总注入量恒定），再乘一个随飞行距离的指数衰减：
        // 枪口一次注入 0.9，末端只剩 0.9 * e^-1.2 ≈ 0.27
        private const float BaseHeat = 0.9f;           // 归一化后的单次注入总热量
        private const float DistanceFalloff = 1.2f;    // 距离衰减指数
        private const float VelocityPerSplat = 0.55f;  // 每次撒点的速度强度
        private const float KernelCutoff = 0.02f;      // 权重低于此值的格子直接丢弃

        // 衰减按 tick 计，与撒点同频，因此火焰的空间范围与游戏倍速无关
        private const float VelDecay = 0.92f;          // 每 tick 速度衰减
        private const float HeatDecay = 0.94f;         // 每 tick 热度衰减
        private const float VelRange = 2f;             // 速度编码范围 ±VelRange
        private const float HeatFullScale = 1.2f;      // 热度编码满刻度
        private const float EpsHeat = 0.004f;
        private const float EpsVel = 0.002f;

        private const float NoiseDensity = 0.55f;      // 噪声密度（越大火舌越碎）
        private const float FlowSpeed = 2.2f;          // 沿速度场平流的速度

        private int width;
        private int height;
        private float[] vx = System.Array.Empty<float>();
        private float[] vz = System.Array.Empty<float>();
        private float[] heat = System.Array.Empty<float>();
        private bool[] tracked = System.Array.Empty<bool>();
        private Color32[] pixels = System.Array.Empty<Color32>();
        private readonly List<int> active = new List<int>();

        // 卷积核的临时缓冲（半径上限 3 → 7×7 = 49 个格子，留点余量）
        private readonly int[] kernelIdx = new int[64];
        private readonly float[] kernelW = new float[64];

        private Texture2D? tex;
        private Material? mat;
        private Mesh? quad;
        private bool inited;
        private bool needsClearUpload;
        private bool dirty;

        public FlameFieldComponent(Map map) : base(map)
        {
        }

        private void EnsureInit()
        {
            if (inited)
            {
                return;
            }
            inited = true;

            width = map.Size.x;
            height = map.Size.z;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            int n = width * height;
            vx = new float[n];
            vz = new float[n];
            heat = new float[n];
            tracked = new bool[n];
            pixels = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                pixels[i] = new Color32(128, 128, 0, 255);   // 速度中性点 = 0
            }

            tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "AWL_FlameField",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(pixels);
            tex.Apply();

            mat = CreateMaterial();
            if (mat != null)
            {
                mat.mainTexture = tex;
                mat.SetVector("_MapParams", new Vector4(width, height, VelRange, 0f));
            }

            quad = BuildQuad(width, height);
        }

        private Material CreateMaterial()
        {
            return new Material(AWL_DefOf.AWL_FlameField.Shader) { color = Color.white };
        }

        private static Mesh BuildQuad(int w, int h)
        {
            Mesh mesh = new Mesh { name = "AWL_FlameFieldQuad" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(w, 0f, 0f),
                new Vector3(0f, 0f, h),
                new Vector3(w, 0f, h),
            });
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
            });
            mesh.SetTriangles(new List<int> { 0, 2, 1, 2, 3, 1 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------
        // 对外接口：由 JetProjectile 每 tick 调用
        // ------------------------------------------------------------------

        /// <summary>
        /// 在弹体所在格附近撒入一点速度与热度。
        /// travelFraction = 0 时核半径只有半格（实际只影响所在的那一格），
        /// 随飞行距离增大到末端约 2.6 格，形成"出膛细、末端散"的锥形。
        /// </summary>
        public void Splat(Vector3 pos, Vector3 dir, float travelFraction = 0f)
        {
            EnsureInit();
            if (!inited || width <= 0)
            {
                return;
            }

            dir = dir.Yto0();
            if (dir.sqrMagnitude < 1e-6f)
            {
                return;
            }
            dir = dir.normalized;

            float t = Mathf.Clamp01(travelFraction);
            float radius = Mathf.Lerp(MuzzleSplatRadius, EndSplatRadius, t);

            // 归一化保证"总注入量"由这里决定，核的形状只决定分布；
            // 距离衰减让射流越远注入越少，于是枪口最热、越远越冷。
            float decay = Mathf.Exp(-DistanceFalloff * t);
            float heatAmount = BaseHeat * decay;

            int cx = Mathf.FloorToInt(pos.x);
            int cz = Mathf.FloorToInt(pos.z);
            int r = Mathf.CeilToInt(radius);
            float sigma = radius * 0.5f;
            float inv = 1f / (2f * sigma * sigma);

            // 第一遍：收集落在场内的格子与权重，同时求权重和
            int n = 0;
            float sumW = 0f;
            for (int dz = -r; dz <= r; dz++)
            {
                int z = cz + dz;
                if (z < 0 || z >= height)
                {
                    continue;
                }
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= width)
                    {
                        continue;
                    }
                    float w = Mathf.Exp(-(dx * dx + dz * dz) * inv);
                    if (w < KernelCutoff)
                    {
                        continue;
                    }
                    if (n >= kernelIdx.Length)
                    {
                        break;
                    }
                    kernelIdx[n] = z * width + x;
                    kernelW[n] = w;
                    n++;
                    sumW += w;
                }
            }

            if (n == 0 || sumW <= 0f)
            {
                return;
            }

            // 第二遍：按 1/Σw 归一化后写入
            // 这样「一次撒点注入的总热量」恒定，扩散只改变分布、不改变总量：
            // 枪口一格拿满，末端摊到 7×7 上、每格只剩下约 1/8.5。
            float norm = 1f / sumW;
            for (int k = 0; k < n; k++)
            {
                int idx = kernelIdx[k];
                float w = kernelW[k] * norm;

                vx[idx] += dir.x * w * VelocityPerSplat * decay;
                vz[idx] += dir.z * w * VelocityPerSplat * decay;
                heat[idx] = Mathf.Min(1.5f, heat[idx] + w * heatAmount);

                if (!tracked[idx])
                {
                    tracked[idx] = true;
                    active.Add(idx);
                }
            }

            dirty = true;
        }

        // ------------------------------------------------------------------
        // 每 tick 衰减，每帧编码上传
        // ------------------------------------------------------------------

        public override void MapComponentTick()
        {
            if (!inited || active.Count == 0)
            {
                return;
            }

            // 衰减与撒点同为每 tick 一次，所以火焰的空间范围与游戏倍速无关。
            for (int i = active.Count - 1; i >= 0; i--)
            {
                int idx = active[i];
                vx[idx] *= VelDecay;
                vz[idx] *= VelDecay;
                heat[idx] *= HeatDecay;

                if (heat[idx] < EpsHeat && Mathf.Abs(vx[idx]) < EpsVel && Mathf.Abs(vz[idx]) < EpsVel)
                {
                    vx[idx] = 0f;
                    vz[idx] = 0f;
                    heat[idx] = 0f;
                    tracked[idx] = false;
                    pixels[PixelIndex(idx)] = new Color32(128, 128, 0, 255);
                    active.RemoveAt(i);
                    needsClearUpload = true;
                }
            }

            dirty = true;
        }

        public override void MapComponentUpdate()
        {
            // 懒初始化：没有射流撒过点的地图永远不分配数组与纹理
            if (!inited || width <= 0)
            {
                return;
            }

            if (active.Count == 0)
            {
                if (needsClearUpload)
                {
                    needsClearUpload = false;
                    tex!.SetPixels32(pixels);
                    tex.Apply();
                }
                return;
            }

            // 只在有变化时上传（高倍速下一帧可能跑了多个 tick，也只传一次）
            if (dirty)
            {
                dirty = false;
                EncodeAndUpload();
            }
        }

        /// <summary>
        /// 场数组按 z 行优先存储，但引擎把纹理第 0 行当作 v = 1（顶行），
        /// 所以写像素时必须把 z 翻过来，否则整张场在 Z 轴上是镜像的。
        /// </summary>
        private int PixelIndex(int idx)
        {
            int x = idx % width;
            int z = idx / width;
            return (height - 1 - z) * width + x;
        }

        /// <summary>
        /// 该格是否被墙体/岩石占满。
        /// 结果写进纹理的 alpha 通道（0 = 实心），shader 在实心格子上直接 discard，
        /// 所以火焰不会出现在墙里，也不会"糊"在墙上。
        /// </summary>
        private byte SolidAt(int idx)
        {
            IntVec3 c = new IntVec3(idx % width, 0, idx / width);
            return (c.InBounds(map) && !c.Filled(map)) ? (byte)255 : (byte)0;
        }

        /// <summary>把当前热度/速度写进像素缓冲并上传（不衰减）。</summary>
        private void EncodeAndUpload()
        {
            for (int i = 0; i < active.Count; i++)
            {
                int idx = active[i];
                float h = Mathf.Clamp01(heat[idx] / HeatFullScale);
                pixels[PixelIndex(idx)] = new Color32(
                    EncodeVel(vx[idx]),
                    EncodeVel(vz[idx]),
                    (byte)Mathf.RoundToInt(h * 255f),
                    SolidAt(idx));
            }
            tex!.SetPixels32(pixels);
            tex.Apply();
            needsClearUpload = true;
        }

        private static byte EncodeVel(float v)
        {
            float t = v / (VelRange * 2f) + 0.5f;   // [-VelRange, VelRange] → [0, 1]
            return (byte)Mathf.Clamp(Mathf.RoundToInt(t * 255f), 0, 255);
        }

        // ------------------------------------------------------------------
        // 绘制
        // ------------------------------------------------------------------

        public override void MapComponentDraw()
        {
            if (!inited || active.Count == 0 || mat == null || quad == null)
            {
                return;
            }

            mat.SetVector("_FlameParams", new Vector4(
                Find.TickManager.TicksGame / 60f,
                NoiseDensity,
                FlowSpeed,
                0f));

            // Texture2D.Apply() 会重建 GPU 纹理资源，所以每帧重新绑定一次句柄
            mat.mainTexture = tex;

            // VisEffects：Pawn / Weather / LightingOverlay 之上，FogOfWar / Darkness 之下
            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(0f, AltitudeLayer.VisEffects.AltitudeFor(), 0f),
                Quaternion.identity,
                Vector3.one);

            Graphics.DrawMesh(quad, matrix, mat, 0);
        }

        public override void MapRemoved()
        {
            active.Clear();
            if (quad != null)
            {
                UnityEngine.Object.Destroy(quad);
                quad = null;
            }
            if (mat != null)
            {
                UnityEngine.Object.Destroy(mat);
                mat = null;
            }
            if (tex != null)
            {
                UnityEngine.Object.Destroy(tex);
                tex = null;
            }
            inited = false;
        }

        public override void ExposeData()
        {
            // 纯表现，不存档
        }
    }
}
