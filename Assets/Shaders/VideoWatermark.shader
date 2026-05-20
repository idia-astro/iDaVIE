Shader "IDIA/VideoWatermark"
{
    Properties
    {
        _MainTex ("Camera Texture", 2D) = "white" {}
        _LogoTex ("Logo Texture", 2D) = "white" {}
        _LogoBounds ("Logo Bounds (left bottom right top)", Vector) = (0.8, 0, 1, 0.2)
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            // struct v2f
            // {
            //     float2 uv : TEXCOORD0;
            //     float4 vertex : SV_POSITION;
            // };

            sampler2D _MainTex;
            sampler2D _LogoTex;
            float4 _LogoBounds;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                if (i.uv.x < _LogoBounds.x || i.uv.y < _LogoBounds.y || i.uv.x > _LogoBounds.z || i.uv.y > _LogoBounds.w)
                {
                    return col;
                }

                float2 logo_uv =  (i.uv - _LogoBounds.xy) / (_LogoBounds.zw - _LogoBounds.xy);

                fixed4 logo = tex2D(_LogoTex, logo_uv);
                
                return lerp(col,logo, logo.a);
            }
            ENDCG
        }
    }
}
