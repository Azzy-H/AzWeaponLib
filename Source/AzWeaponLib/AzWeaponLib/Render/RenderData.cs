using UnityEngine;
using Verse;

namespace AzWeaponLib.Render
{
    /// <summary>绘制阶段。世界层 = 物品形态，手持 = 装备形态。由宿主决定。</summary>
    [System.Flags]
    public enum RenderStage : byte
    {
        None = 0,
        World = 1,
        Equipped = 2,
        Both = World | Equipped
    }

    /// <summary>
    /// 层变换：位置（格）、绕 Y 轴角度（度）、平面尺寸倍率。
    /// 组合规则为“父 × 子”：位置先按父的尺寸缩放、再按父的角度旋转，最后叠加父的位置。
    /// </summary>
    public readonly struct Pose
    {
        public readonly Vector3 pos;
        public readonly float rot;
        public readonly Vector2 scale;

        public Pose(Vector3 pos, float rot, Vector2 scale)
        {
            this.pos = pos;
            this.rot = rot;
            this.scale = scale;
        }

        public static readonly Pose Identity = new Pose(Vector3.zero, 0f, Vector2.one);

        public static Pose Compose(in Pose parent, in Pose child)
        {
            Vector3 scaled = new Vector3(child.pos.x * parent.scale.x, child.pos.y, child.pos.z * parent.scale.y);
            Vector3 rotated = Quaternion.AngleAxis(parent.rot, Vector3.up) * scaled;
            return new Pose(
                parent.pos + rotated,
                parent.rot + child.rot,
                new Vector2(parent.scale.x * child.scale.x, parent.scale.y * child.scale.y));
        }

        /// <summary>局部矩阵。Y 轴缩放取 0，与原版枪械绘制矩阵保持一致（平面网格不含 Y 分量）。</summary>
        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(pos, Quaternion.AngleAxis(rot, Vector3.up), new Vector3(scale.x, 0f, scale.y));
        }
    }

    /// <summary>
    /// 一条待提交的绘制：网格、材质、相对根变换的局部姿态、排序信息。
    /// 只在解析期短暂存在于池化列表，烘焙后复制进只读数组。
    /// </summary>
    public readonly struct DrawData
    {
        /// <summary>每个 layer 单位对应的高度（与 PawnRenderUtility.AltitudeForLayer 同系数）。</summary>
        public const float AltitudePerLayer = 0.0003658537f;

        public readonly Mesh mesh;
        public readonly Material mat;
        public readonly Pose local;
        public readonly float layer;
        public readonly int order;
        public readonly int seq;

        public DrawData(Mesh mesh, Material mat, in Pose local, float layer, int order, int seq)
        {
            this.mesh = mesh;
            this.mat = mat;
            this.local = local;
            this.layer = layer;
            this.order = order;
            this.seq = seq;
        }

        /// <summary>最终矩阵 = 根变换 × 局部变换（× 层高偏移）。层高对两个宿主语义一致。</summary>
        public Matrix4x4 Matrix(in Matrix4x4 root)
        {
            Matrix4x4 m = root * local.ToMatrix();
            if (layer != 0f)
            {
                m *= Matrix4x4.Translate(Vector3.up * (layer * AltitudePerLayer));
            }
            return m;
        }
    }

    /// <summary>
    /// 一次绘制的上下文。构造后不可变；根变换只在提交时使用，不参与烘焙。
    /// </summary>
    public readonly struct FrameCtx
    {
        public readonly Thing thing;
        public readonly Graphic graphic;
        public readonly RenderStage stage;
        public readonly Rot4 rot;

        /// <summary>基座尺寸（格）；朝向导致尺寸交换时由宿主传入交换后的值。</summary>
        public readonly Vector2 baseSize;

        /// <summary>基座是否使用翻转网格。</summary>
        public readonly bool baseFlip;

        /// <summary>根变换：位置 + 旋转，缩放为 1。</summary>
        public readonly Matrix4x4 root;

        /// <summary>离散状态摘要，参与烘焙缓存键。</summary>
        public readonly int stateKey;

        public readonly int tick;

        private FrameCtx(Thing thing, Graphic graphic, RenderStage stage, Rot4 rot,
                         Vector2 baseSize, bool baseFlip, Matrix4x4 root, int stateKey, int tick)
        {
            this.thing = thing;
            this.graphic = graphic;
            this.stage = stage;
            this.rot = rot;
            this.baseSize = baseSize;
            this.baseFlip = baseFlip;
            this.root = root;
            this.stateKey = stateKey;
            this.tick = tick;
        }

        public static FrameCtx Create(RenderStage stage, Thing thing, Graphic graphic, Rot4 rot,
                                      Vector2 baseSize, bool baseFlip, Matrix4x4 root, int stateKey = 0)
        {
            return new FrameCtx(thing, graphic, stage, rot, baseSize, baseFlip, root, stateKey, 0);
        }

        public static FrameCtx World(Thing thing, Graphic graphic, Rot4 rot,
                                     Vector2 baseSize, bool baseFlip, Matrix4x4 root, int stateKey = 0)
        {
            return new FrameCtx(thing, graphic, RenderStage.World, rot, baseSize, baseFlip, root, stateKey, 0);
        }

        public static FrameCtx Equipped(Thing thing, Graphic graphic,
                                        Vector2 baseSize, bool baseFlip, Matrix4x4 root, int stateKey = 0)
        {
            return new FrameCtx(thing, graphic, RenderStage.Equipped, Rot4.North, baseSize, baseFlip, root, stateKey, 0);
        }
    }
}
