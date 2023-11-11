Shader "Custom/GameOfLifeShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            static const float Step = 1.0 / 255.0;

            sampler2D _MainTex;
            float4 _MainTexSize;
            fixed4 _ForegroundColor;
            float _B[9];
            float _S[9];
            float _N[9];
            float _Decoy;

            int getCount(float2 uv)
            {
                int result = 0;

                for (int x = -1, i = 0; x <= 1; x++)
                    for (int y = -1; y <= 1; y++, i++)
                        if (tex2D(_MainTex, uv + float2(x * _MainTexSize.x, y * _MainTexSize.y)).a * _N[i] > Step)
                            result++;

                return result;
            }

            fixed4 getColor(fixed4 color, int count) 
            {
                if (color.a > 0)
                {
                    if (_S[count] > 0.5)
                        return color - fixed4(0, 0, 0, Step * _Decoy);
                }
                else
                {
                    if (_B[count] > 0.5)
                        return _ForegroundColor;
                }
                return fixed4(0, 0, 0, 0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                int count = getCount(i.uv);
                return getColor(color, count);
            }
            ENDCG
        }
    }
}
