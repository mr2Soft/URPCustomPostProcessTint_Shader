Shader "Custom/URPCustomPostProcessTintShader"
{
    Properties
    {
        _TintIntensity("Tint Intensity", Range(0, 5)) = 1
        _TintColor("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float _TintIntensity;
            float4 _TintColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                color.rgb += _TintColor.rgb * _TintIntensity;
                return color;
            }
            ENDHLSL
        }
    }
}