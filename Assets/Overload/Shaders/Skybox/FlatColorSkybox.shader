Shader "Overload/FlatColorCubemap" {
Properties {
    _Threshold("Threshold", Range(0,1)) = 0.5
    _primaryColor ("Primary Color", Color) = (0, 0, 0, 1)
    _secondaryColor ("Secondary Color", Color) = (0, 0, 0, 1)
    _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
    [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
    _Rotation ("Rotation", Range(0, 360)) = 0
    [NoScaleOffset] _Tex ("Cubemap (HDR)", Cube) = "grey" {}
}

SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off ZWrite Off

    Pass {

        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.0

        #include "UnityCG.cginc"

        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;
        half _Exposure;
        float _Rotation;
        float _Threshold;
        half4 _primaryColor;
        half4 _secondaryColor;

        float3 RotateAroundYInDegrees (float3 vertex, float degrees)
        {
            float alpha = degrees * UNITY_PI / 180.0;
            float sina, cosa;
            sincos(alpha, sina, cosa);
            float2x2 m = float2x2(cosa, -sina, sina, cosa);
            return float3(mul(m, vertex.xz), vertex.y).xzy;
        }

        struct appdata_t {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            float4 screenPos : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // start: dithering from https://github.com/hughsk/glsl-dither
        float luma(float3 color) {
        return dot(color, float3(0.299, 0.587, 0.114));
        }

        float luma(float4 color) {
        return dot(color.rgb, float3(0.299, 0.587, 0.114));
        }

        float dither8x8(float2 position, float brightness) {
        int x = int(fmod(position.x, 8.0));
        int y = int(fmod(position.y, 8.0));
        int index = x + y * 8;
        float limit = 0.0;

        if (x < 8) {
            if (index == 0) limit = 0.015625;
            if (index == 1) limit = 0.515625;
            if (index == 2) limit = 0.140625;
            if (index == 3) limit = 0.640625;
            if (index == 4) limit = 0.046875;
            if (index == 5) limit = 0.546875;
            if (index == 6) limit = 0.171875;
            if (index == 7) limit = 0.671875;
            if (index == 8) limit = 0.765625;
            if (index == 9) limit = 0.265625;
            if (index == 10) limit = 0.890625;
            if (index == 11) limit = 0.390625;
            if (index == 12) limit = 0.796875;
            if (index == 13) limit = 0.296875;
            if (index == 14) limit = 0.921875;
            if (index == 15) limit = 0.421875;
            if (index == 16) limit = 0.203125;
            if (index == 17) limit = 0.703125;
            if (index == 18) limit = 0.078125;
            if (index == 19) limit = 0.578125;
            if (index == 20) limit = 0.234375;
            if (index == 21) limit = 0.734375;
            if (index == 22) limit = 0.109375;
            if (index == 23) limit = 0.609375;
            if (index == 24) limit = 0.953125;
            if (index == 25) limit = 0.453125;
            if (index == 26) limit = 0.828125;
            if (index == 27) limit = 0.328125;
            if (index == 28) limit = 0.984375;
            if (index == 29) limit = 0.484375;
            if (index == 30) limit = 0.859375;
            if (index == 31) limit = 0.359375;
            if (index == 32) limit = 0.0625;
            if (index == 33) limit = 0.5625;
            if (index == 34) limit = 0.1875;
            if (index == 35) limit = 0.6875;
            if (index == 36) limit = 0.03125;
            if (index == 37) limit = 0.53125;
            if (index == 38) limit = 0.15625;
            if (index == 39) limit = 0.65625;
            if (index == 40) limit = 0.8125;
            if (index == 41) limit = 0.3125;
            if (index == 42) limit = 0.9375;
            if (index == 43) limit = 0.4375;
            if (index == 44) limit = 0.78125;
            if (index == 45) limit = 0.28125;
            if (index == 46) limit = 0.90625;
            if (index == 47) limit = 0.40625;
            if (index == 48) limit = 0.25;
            if (index == 49) limit = 0.75;
            if (index == 50) limit = 0.125;
            if (index == 51) limit = 0.625;
            if (index == 52) limit = 0.21875;
            if (index == 53) limit = 0.71875;
            if (index == 54) limit = 0.09375;
            if (index == 55) limit = 0.59375;
            if (index == 56) limit = 1.0;
            if (index == 57) limit = 0.5;
            if (index == 58) limit = 0.875;
            if (index == 59) limit = 0.375;
            if (index == 60) limit = 0.96875;
            if (index == 61) limit = 0.46875;
            if (index == 62) limit = 0.84375;
            if (index == 63) limit = 0.34375;
        }

        return brightness < limit ? 0.03 : 1.0;
        }

        float3 dither8x8(float2 position, float3 color) {
        return color * dither8x8(position, luma(color));
        }

        v2f vert (appdata_t v)
        {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            float3 rotated = RotateAroundYInDegrees(v.vertex, _Rotation);
            o.vertex = UnityObjectToClipPos(rotated);
            o.texcoord = v.vertex.xyz;
            o.screenPos = ComputeScreenPos(o.vertex);
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            half4 tex = texCUBE (_Tex, i.texcoord);
            half3 c = DecodeHDR (tex, _Tex_HDR);
            c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
            c *= _Exposure;

            // Threshold-based coloring
            //float2 ss = i.screenPos / i.screenPos.w;
            //float num = (c.x + c.y + c.z) * 0.33333;
            //c = lerp( _secondaryColor, _primaryColor, num);
            //c = dither8x8(ss*float2(1280, 720), c);
        
            float num = (c.x + c.y + c.z) * 0.33333;
            float s = step((1.0-_Threshold), num);
            c = (1-s)*_primaryColor + (s)*_secondaryColor;

            return half4(c, 1);
        }
        ENDCG
    }
}


Fallback Off

}