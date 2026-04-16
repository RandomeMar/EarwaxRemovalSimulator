Shader "Custom/EarwaxFieldShader"
{
    Properties
    {
        _ParticleSize("Particle Size", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            ColorMask R // Only outputs to red channel
            ZWrite Off
            Cull Off
            Blend One One
            BlendOp Max


            HLSLPROGRAM

            // These lines set the functions vert and frag to be the vertex and fragment shaders respectively
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Particle position buffer
            StructuredBuffer<float3> _Positions;

            float _ParticleSize;

            // Input for Vertex Shader
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // Input for Fragment Shader
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 centerVS : TEXCOORD1;
            };

            // Vertex Shader
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;

                float3 particlePos = _Positions[instanceID];

                float3 local = v.vertex.xyz * _ParticleSize; // Local space
                
                float3 camUp = UNITY_MATRIX_I_V._m01_m11_m21;
                float3 camRight = UNITY_MATRIX_I_V._m00_m10_m20;
                
                float3 world = particlePos + camRight * local.x + camUp * local.y; // Rotates the billboard so it always faces the camera

                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0)); // Puts vertex in clip space for the frag shader
                o.uv = v.uv;
                o.centerVS = mul(UNITY_MATRIX_V, float4(particlePos, 1.0)).xyz;

                return o;
            }

            // Fragment Shader
            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0; // Since UVs usually go from 0 to 1, this makes them go -1 to 1. That means (0, 0) is now the center
                float r2 = dot(p, p);

                if (r2 > 1) discard; // Discard pixels more than 1 unit away from the center uv position. This results in a circle of radius 1

                float z = sqrt(1.0 - r2);
                float3 normalView = float3(p.x, p.y, z); // Normal in view space
                float3 surfaceView = i.centerVS + normalView * _ParticleSize; // Fragment position in view space

                float depth = -surfaceView.z;

                float depthKey = 1.0 / max(depth, 1e-5);
                return float4(depthKey, 0.0, 0.0, 1.0);

                //float centerDepth = -i.centerVS.z;
                //float depthDelta = centerDepth - depth;   // or depth - centerDepth depending on which way you want
                //float debug = saturate(depthDelta); // scale up


                //return float4(debug, 0.0, 0.0, 1.0);

                //return float4(depth, 0.0, 0.0, 1);
            }

            ENDHLSL
        }
    }
}
