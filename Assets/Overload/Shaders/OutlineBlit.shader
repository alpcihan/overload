Shader "OutlineBlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "OutlineBlitPass"

            CGPROGRAM
            #include "UnityCG.cginc"
            ENDCG

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraDepthTexture);
            TEXTURE2D_X(_CameraDepthNormalsTexture);

            SAMPLER(sampler_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthNormalsTexture);

            float4 _CameraDepthTexture_TexelSize;

            float3 DecodeViewNormalStereo(float4 enc4)
            {
                float kScale = 1.7777;
                float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
                float g = 2.0 / dot(nn.xyz, nn.xyz);
                float3 n;
                n.xy = g * nn.xy;
                n.z = g - 1;
                return n;
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).x;
                //depth = Linear01Depth(depth); // TODO: make clipping plane independent

                // Define outline parameters
                float depthThreshold = 0.005; // Adjust as needed

                // Sample neighboring pixels to detect depth differences
                float depthLeft = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(-_CameraDepthTexture_TexelSize.x, 0)).r;
                float depthRight = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(_CameraDepthTexture_TexelSize.x, 0)).r;
                float depthUp = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(0, _CameraDepthTexture_TexelSize.y)).r;
                float depthDown = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord + float2(0, -_CameraDepthTexture_TexelSize.y)).r;

                // Calculate depth differences
                float depthDiffX = abs(depth - depthLeft) + abs(depth - depthRight);
                float depthDiffY = abs(depth - depthUp) + abs(depth - depthDown);

                // Check if the depth differences exceed the threshold
                bool outline = (depthDiffX > depthThreshold || depthDiffY > depthThreshold);

                float4 normalDepth = SAMPLE_TEXTURE2D_X(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, input.texcoord);
                float3 normal = DecodeViewNormalStereo(normalDepth);
                
                return half4(normal * 0.5 + 0.5, 1);

                return outline ? float4(1, 1, 1, 1) : float4(0, 0, 0, 1);
            }
            ENDHLSL
        }
    }
}