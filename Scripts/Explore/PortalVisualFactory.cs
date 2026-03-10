using Godot;

public static class PortalVisualFactory
{
    public static MeshInstance3D Create(Vector2 size)
    {
        return new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = size },
            MaterialOverride = BuildMaterial(),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            ExtraCullMargin = 8f,
        };
    }

    private static ShaderMaterial BuildMaterial()
    {
        return new ShaderMaterial { Shader = new Shader { Code = ShaderCode } };
    }

    private const string ShaderCode = """
shader_type spatial;
render_mode unshaded, cull_disabled, blend_mix, depth_draw_alpha_prepass;

uniform vec4 core_color : source_color = vec4(0.45, 0.62, 0.73, 1.0);
uniform vec4 mist_color : source_color = vec4(0.82, 0.93, 0.98, 1.0);
uniform float flow_speed = 0.55;

float hash(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

float noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(
        mix(hash(i + vec2(0.0, 0.0)), hash(i + vec2(1.0, 0.0)), u.x),
        mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x),
        u.y
    );
}

float fbm(vec2 p) {
    float v = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++) {
        v += amp * noise(p);
        p *= 2.03;
        amp *= 0.5;
    }
    return v;
}

void fragment() {
    vec2 uv = UV * 2.0 - vec2(1.0);
    float t = TIME * flow_speed;
    float swirl = atan(uv.y, uv.x) + t * 0.9;
    vec2 flow = vec2(cos(swirl), sin(swirl)) * (0.3 + length(uv) * 0.5);
    float cloud = fbm((uv + flow) * 3.4 + vec2(t, -t * 0.6));
    float detail = fbm((uv - flow) * 5.8 - vec2(t * 1.2, t * 0.3));
    float density = clamp(cloud * 0.72 + detail * 0.45, 0.0, 1.0);

    float radius = length(uv);
    float softEdge = 1.0 - smoothstep(0.62, 0.98, radius);
    float alpha = clamp(density * softEdge * 1.3, 0.0, 1.0);
    vec3 color = mix(core_color.rgb, mist_color.rgb, smoothstep(0.45, 1.0, density));

    ALBEDO = color;
    EMISSION = color * (0.55 + density * 0.75);
    ALPHA = alpha * 0.95;
}
""";
}
