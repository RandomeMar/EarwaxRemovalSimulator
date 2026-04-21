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

            // These lines set the functions vert and frag to be the vertex and fragment shaders respectively
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Particle position buffer
            StructuredBuffer<float3> _Positions;

            float4 _BaseColor;
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

                o.pos = mul(UNITY_MATRIX_VP, float4(world, 1.0)); // Puts vertex in view space for the frag shader
                o.uv = v.uv;

                return o;
            }

            // Fragment Shader
            float4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0; // Since UVs usually go from 0 to 1, this makes them go -1 to 1. That means (0, 0) is now the center
                float r2 = dot(p, p);

                if (r2 > 1) discard; // Discard pixels more than 1 unit away from the center uv position. This results in a circle of radius 1

                float z = sqrt(1.0 - r2); // z is negative since in Unity, -z is the forward direction
                float3 normalView = normalize(float3(p.x, p.y, z)); // Fake surface normal in view space
                float3 normalWorld = mul(UNITY_MATRIX_I_V, float4(normalView, 0.0)).xyz; // Converts normal to world space

                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float diffuse = max(0.0, dot(normalWorld, lightDir)); // How aligned the surface normal is with the light direction

                float ambient = .3;

                return float4(_BaseColor.xyz * diffuse, 1.0);
            }

            ENDHLSL
        }
    }
}
