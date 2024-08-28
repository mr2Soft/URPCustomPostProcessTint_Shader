Shader "Custom/URPCustomPostProcessTintShader"
{
    Properties
    {
        _TintIntensity("Tint Intensity", Range(0, 5)) = 1
        _TintColor("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType" = "PostProcess" }
        Pass
        {
            Tags { "LightMode" = "PostProcessing" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _TintIntensity;
            float4 _TintColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            Varyings vert(Attributes v)
            {
                Varyings o;
                // Transform position from object space to clip space using URP macros
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                // Apply tint with intensity
                color.rgb += _TintColor.rgb * _TintIntensity;
                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
