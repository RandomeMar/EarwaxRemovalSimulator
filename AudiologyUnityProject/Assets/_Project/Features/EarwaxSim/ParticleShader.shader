Shader "Custom/ParticleShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _ParticleSize("Particle Size", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float3> _Positions;

            float4 _BaseColor;
            float _ParticleSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                float3 particlePos = _Positions[instanceID];

                float3 local = v.vertex.xyz * _ParticleSize;
                float3 world = local + particlePos;

                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0));
                o.uv = v.uv;

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 centerCoord = i.uv - float2(0.5, 0.5);
                float dist = length(centerCoord);

                float radius = 0.45;
                float alpha = 1.0 - step(radius, dist);

                return float4(_BaseColor.rgb, _BaseColor.a * alpha);
            }

            ENDHLSL
        }
    }
}
