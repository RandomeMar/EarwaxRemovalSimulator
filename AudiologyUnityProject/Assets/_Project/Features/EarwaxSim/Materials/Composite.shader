Shader "Custom/Composite"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "HLSLSupport.cginc"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            struct Attributes
            {
                uint vertexID : SV_VERTEXID;
            };

            struct Varyings
            {
                float4 positionClip : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // Input textures
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_SceneColorTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_EarwaxLitTex);

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_EarwaxDepthTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_SceneDepthTex);


            Varyings vert(Attributes i)
            {
                Varyings o;

                // Fullscreen triangle
                float2 pos;
                if (i.vertexID == 0) pos = float2(-1.0, -1.0);
                else if (i.vertexID == 1) pos = float2(3.0,  -1.0);
                else pos = float2( -1.0, 3.0);

                o.positionClip = float4(pos, 0.0, 1.0);
                o.uv = pos * 0.5 + 0.5;

                // Depending on platform, UVs may be flipped vertically
                #if UNITY_UV_STARTS_AT_TOP
                    o.uv.y = 1.0 - o.uv.y;
                #endif

                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                // Sample color textures
                float4 sceneCol = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SceneColorTex, i.uv);
                float4 waxCol = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_EarwaxLitTex, i.uv);

                // Sample depth textures
                float waxDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_EarwaxDepthTex, i.uv).r;
                float sceneDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SceneDepthTex, i.uv).r;

                #if UNITY_REVERSED_Z
                    if (sceneDepth <= 0.00001) return waxCol;
                #else
                    if (sceneDepth >= 0.99999) return waxCol;
                #endif

                return waxDepth > sceneDepth ? waxCol : sceneCol; // Pick the closer pixel
            }
            ENDHLSL
        }
    }
}
