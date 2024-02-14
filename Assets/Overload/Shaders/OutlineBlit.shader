Shader "OutlineBlit"
{
    Properties
    {
        _depthThreshold     ("Depth Threshold", Range(0, 10))   = 1
        _normalThreshold    ("Normal Threshold", Range(0, 10))  = 1
        _surfaceColor       ("Surface Color", Color)            = (0,0,0,1)
        _outlineColor       ("Outline Color", Color)            = (1,1,1,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "OutlineBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Assets/Overload/Shaders/Utils/Outline.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D_X(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            float4 _CameraDepthTexture_TexelSize;

            TEXTURE2D_X(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);
            float4 _CameraNormalsTexture_TexelSize;

            float _depthThreshold;
            float _normalThreshold;

            float4 _surfaceColor;
            float4 _outlineColor;

            inline float getLinearEyeDepth(float2 texCoord) 
            {
                float z = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, texCoord).r;
                z = 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
                
                return z;
            }

            bool checkOutlineByDepth(float2 texCoord, float depthThreshold)
            {
                const float depth = getLinearEyeDepth(texCoord);
                const float depthTR = getLinearEyeDepth(texCoord + float2(_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y));
                const float depthTL = getLinearEyeDepth(texCoord + float2(-_CameraDepthTexture_TexelSize.x, _CameraDepthTexture_TexelSize.y));
                const float depthBR = getLinearEyeDepth(texCoord + float2(_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y));
                const float depthBL = getLinearEyeDepth(texCoord + float2(-_CameraDepthTexture_TexelSize.x, -_CameraDepthTexture_TexelSize.y));

                float TR_BL_2 = depthTR - depthBL; TR_BL_2 *= TR_BL_2;
                float TL_BR_2 = depthTL - depthBR; TL_BR_2 *= TL_BR_2;

                const float krnl = sqrt(TR_BL_2 + TL_BR_2);
                return krnl > depthThreshold * depth;
            }

            bool checkOutlineByNormal(float2 texCoord, float normalThreshold)
            {
                const float3 normalTR = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, texCoord + float2(_CameraNormalsTexture_TexelSize.x, _CameraNormalsTexture_TexelSize.y));
                const float3 normalTL = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, texCoord + float2(-_CameraNormalsTexture_TexelSize.x, _CameraNormalsTexture_TexelSize.y));
                const float3 normalBR = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, texCoord + float2(_CameraNormalsTexture_TexelSize.x, -_CameraNormalsTexture_TexelSize.y));
                const float3 normalBL = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, texCoord + float2(-_CameraNormalsTexture_TexelSize.x, -_CameraNormalsTexture_TexelSize.y));

                const float3 TR_BL = normalTR - normalBL; 
                const float3 TL_BR = normalTL - normalBR;

                const float d_TR_BL = dot(TR_BL, TR_BL);
                const float d_TL_BR = dot(TL_BR, TL_BR);

                const float krnl = sqrt(d_TR_BL + d_TL_BR);
                return krnl > normalThreshold;
            }

            float4 calculateFragColor(/*float3 color,*/ float2 texCoord) {
                // check outline calculate flag
                // bool isCalcualteOutline = isOutlineCalculateStencil(color);
                // if(!isCalcualteOutline) return float4(color,1);

                bool isOutlineDepth     = checkOutlineByDepth(texCoord, _depthThreshold);
                bool isOutlineNormal    = checkOutlineByNormal(texCoord, _normalThreshold);
                return isOutlineDepth || isOutlineNormal ? _outlineColor : _surfaceColor;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // float3 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.texcoord);

                return calculateFragColor(/*color,*/ input.texcoord);
            }
            ENDHLSL
        }
    }
}