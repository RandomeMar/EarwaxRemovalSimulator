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
            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off


            HLSLPROGRAM

            // These lines set the functions vert and frag to be the vertex and fragment shaders respectively
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Particle position buffer
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<int> _Actives;

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
                uint isActive : TEXCOORD2;
            };

            struct fragOut
            {
                float4 color : SV_TARGET;
                float depth : SV_DEPTH;
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
                o.isActive = _Actives[instanceID];

                return o;
            }

            // Fragment Shader
            fragOut frag(v2f i)
            {
                if (i.isActive == 0) discard;

                fragOut o;

                float2 p = i.uv * 2.0 - 1.0; // Since UVs usually go from 0 to 1, this makes them go -1 to 1. That means (0, 0) is now the center
                float r2 = dot(p, p);

                if (r2 > 1) discard; // Discard pixels more than 1 unit away from the center uv position. This results in a circle of radius 1

                float z = sqrt(1.0 - r2);
                float3 normalView = normalize(float3(p.x, p.y, z)); // Normal in view space

                float4 surfaceView = float4(i.centerVS + (normalView * _ParticleSize), 1.0); // Frag position in view space
                float4 surfaceClip = mul(UNITY_MATRIX_P, surfaceView); // Frag position in clip space

                o.depth = surfaceClip.z / surfaceClip.w; // Normalized device coordinate

                o.color = float4(surfaceView.z, 0.0, 0.0, 1.0); // Linear eye depth

                return o;
            }

            ENDHLSL
        }
    }
}
