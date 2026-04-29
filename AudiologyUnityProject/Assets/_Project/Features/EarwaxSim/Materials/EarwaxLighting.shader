Shader "Custom/EarwaxLighting"
{
    Properties
    {
        _WaxColor("Wax Color", Color) = (1, 1, 1, 1)
        _Ambient("Ambient", Range(0, 1)) = 0.5
        _DepthScale("Depth Scale", float) = 1.0
        _NormalStrength("Normal Strength", float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Overlay" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "HLSLSupport.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // AddBlitPass() values
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float4 _WaxColor;
            float _Ambient;
            float _DepthScale;
            float _NormalStrength;


            // Input for Vertex Shader
            struct VertIn
            {
                uint vertexID : SV_VERTEXID;
            };

            // Input for Fragment Shader
            struct FragIn
            {
                float4 positionClip : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            FragIn vert(VertIn input)
            {
                FragIn o;

                // Fullscreen triangle
                float2 pos;
                if (input.vertexID == 0) pos = float2(-1.0, -1.0);
                else if (input.vertexID == 1) pos = float2(-1.0,  3.0);
                else pos = float2( 3.0, -1.0);

                o.positionClip = float4(pos, 0.0, 1.0);
                o.uv = pos * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                    o.uv.y = 1.0 - o.uv.y;
                #endif

                return o;
            }

            float SampleDepth(float2 uv)
            {
                return UNITY_SAMPLE_SCREENSPACE_TEXTURE(_BlitTexture, uv);
            }

            float4 frag(FragIn i) : SV_TARGET
            {
                float2 texel = float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y);

                float dC = SampleDepth(i.uv);

                if (dC <= 0.0001) discard;

                // Sample nearby points to get gradient vector
                float dL = SampleDepth(i.uv - float2(texel.x, 0));
                float dR = SampleDepth(i.uv + float2(texel.x, 0));
                float dD = SampleDepth(i.uv - float2(0, texel.y));
                float dU = SampleDepth(i.uv + float2(0, texel.y));

                float dx = (dR - dL) * _NormalStrength;
                float dy = (dU - dD) * _NormalStrength;

                // Simple screen-space pseudo-normal
                float3 N = normalize(float3(-dx, -dy, _DepthScale));

                float3 worldPos = ComputeWorldSpacePosition(i.uv, dC, UNITY_MATRIX_I_VP);
                float4 shadowCoord = TransformWorldToShadowCoord(worldPos);

                // Light
                Light mainLight = GetMainLight(shadowCoord);
                float3 lightWorld = normalize(mainLight.direction);
                float3 lightView = normalize(mul((float3x3)UNITY_MATRIX_V, lightWorld)); // Convert to view space

                float NdotL = saturate(dot(N, lightView));
                float3 direct = mainLight.color * NdotL * mainLight.shadowAttenuation;
                float3 color = (direct + _Ambient) * _WaxColor.rgb;

                //float3 color = N * .5 + .5; // For testing normals

                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
