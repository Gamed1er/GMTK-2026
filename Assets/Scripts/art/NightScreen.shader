Shader "Custom/NightScreen"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}

        _NightColor ("Night Color", Color) = (0.45, 0.55, 0.75, 1)
        _Brightness ("Brightness", Range(0, 1.5)) = 0.85
        _Saturation ("Saturation", Range(0, 1.5)) = 0.8

        _FlickerStrength ("Flicker Strength", Range(0, 0.05)) = 0.005
        _FlickerSpeed ("Flicker Speed", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM

            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;

            fixed4 _NightColor;
            float _Brightness;
            float _Saturation;
            float _FlickerStrength;
            float _FlickerSpeed;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);

                // 低飽和度
                float luminance = dot(
                    color.rgb,
                    float3(0.299, 0.587, 0.114)
                );

                color.rgb = lerp(
                    luminance.xxx,
                    color.rgb,
                    _Saturation
                );

                // 夜晚色調
                color.rgb *= _NightColor.rgb;

                // 月光
                float flicker =
                    sin(_Time.y * _FlickerSpeed) +
                    sin(_Time.y * _FlickerSpeed * 2.37) * 0.4;

                color.rgb *=
                    _Brightness +
                    flicker * _FlickerStrength;

                return color;
            }

            ENDCG
        }
    }

    Fallback Off
}