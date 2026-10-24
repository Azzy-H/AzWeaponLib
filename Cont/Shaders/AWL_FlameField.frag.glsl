#version 450

// 地面火焰速度场。
//   _MainTex.r = 速度 x，g = 速度 z（编码后），b = 热度，a = 该格是否可通行
//   _FlameParams = (时间, 噪声密度, 平流速度, 备用)
//   _MapParams   = (地图宽度格, 地图高度格, 速度解码尺度, 备用)
//
// 火焰不依赖任何贴图或粒子：噪声在「格子空间」里生成，被速度场平流，
// 再乘上该格的热度得到亮度与颜色。整片火是一块连续介质，不会出现精灵叠加。

layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color;       // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST;  // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _FlameParams; // @default = vec4(0.0, 0.55, 5.0, 0.0)
    vec4 _MapParams;   // @default = vec4(250.0, 250.0, 4.0, 0.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

float hash21(vec2 p)
{
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float valueNoise(vec2 p)
{
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p)
{
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += amp * valueNoise(p);
        p = p * 2.03 + 19.7;
        amp *= 0.5;
    }
    return v;
}

void main()
{
    vec4 field = texture(_MainTex, vUv);

    vec2 vel = (field.rg - 0.5) * uMaterial._MapParams.z;
    float heat = field.b;

    // alpha 通道被 CPU 用来标记"该格是否被墙/岩石占满"：
    // 实心格直接丢弃，火焰就不会画进墙里。
    if (field.a < 0.5)
        discard;

    // 冷格直接丢弃，省掉整片无效的噪声计算
    if (heat <= 0.002)
        discard;

    float t = uMaterial._FlameParams.x;
    vec2 cellPos = vUv * uMaterial._MapParams.xy;          // 格子坐标
    // 先按速度场平流，再乘噪声密度：保证平流位移的单位是「格」而不是「噪声格」
    vec2 advected = cellPos - vel * (t * uMaterial._FlameParams.z);
    vec2 p = advected * uMaterial._FlameParams.y;

    float n1 = fbm(p);
    float n2 = fbm(p * 2.4 + 13.1);

    // 形状：热度 × 噪声（只影响"哪里算火"，不影响"多热"）
    float shape = heat * (0.5 + 0.5 * n1) * (0.72 + 0.28 * n2);

    // 温度：直接由热度决定 —— 枪口最热（白），越远越冷（橙 → 暗红）。
    // 噪声只给温度加一点点抖动，避免色带过渡像等高线。
    float temp = clamp(heat * 1.25 + (n2 - 0.5) * 0.10, 0.0, 1.0);

    // 黑体色带：暗红 → 红橙 → 橙 → 黄白 → 白
    vec3 c0 = vec3(0.20, 0.012, 0.0);
    vec3 c1 = vec3(0.85, 0.12, 0.02);
    vec3 c2 = vec3(1.00, 0.45, 0.05);
    vec3 c3 = vec3(1.00, 0.86, 0.38);
    vec3 c4 = vec3(1.00, 0.98, 0.93);

    vec3 col = mix(c0, c1, smoothstep(0.02, 0.26, temp));
    col = mix(col, c2, smoothstep(0.26, 0.52, temp));
    col = mix(col, c3, smoothstep(0.52, 0.78, temp));
    col = mix(col, c4, smoothstep(0.78, 0.97, temp));

    float a = smoothstep(0.02, 0.40, shape);

    outColor = vec4(col * a, a);
}
