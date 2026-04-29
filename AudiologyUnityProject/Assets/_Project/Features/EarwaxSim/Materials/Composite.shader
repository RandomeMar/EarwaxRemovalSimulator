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

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_SceneColorTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_EarwaxLitTex);

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_EarwaxDepthTex);
            UNITY_DECLARE_SCREENSPACE_TEXTURE(_SceneDepthTex);

            // Input textures
            //TEXTURE2D(_SceneColorTex); // Scene's color texture
            //SAMPLER(sampler_SceneColorTex);

            //TEXTURE2D(_EarwaxLitTex);
            //SAMPLER(sampler_EarwaxLitTex);

            //TEXTURE2D(_SceneDepthTex);
            //SAMPLER(sampler_SceneDepthTex);

            //TEXTURE2D(_EarwaxDepthTex);
            //SAMPLER(sampler_EarwaxDepthTex);



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
                float4 sceneCol = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SceneColorTex, i.uv);
                float4 waxCol = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_EarwaxLitTex, i.uv);

                float waxDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_EarwaxDepthTex, i.uv);
                float sceneDepth = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_SceneDepthTex, i.uv);

                //float4 sceneCol = SAMPLE_TEXTURE2D(_SceneColorTex, sampler_SceneColorTex, i.uv);
                //float4 waxCol = SAMPLE_TEXTURE2D(_EarwaxLitTex, sampler_EarwaxLitTex, i.uv);
                
                //float sceneDepth = SAMPLE_TEXTURE2D(_SceneDepthTex, sampler_SceneDepthTex, i.uv); 
                //float waxDepth = SAMPLE_TEXTURE2D(_EarwaxDepthTex, sampler_EarwaxDepthTex, i.uv);

                return waxDepth > sceneDepth ? waxCol : sceneCol; // Pick the closer pixel
            }
            ENDHLSL
        }
    }
}
