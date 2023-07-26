Shader"Unlit/MosaicShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlockSize ("Block Size", Range(1, 100)) = 10
        _Intensity ("Intensity", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            uniform float _BlockSize;
            uniform float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
                        
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
                        
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv *= _BlockSize;
                uv = floor(uv) / _BlockSize;
                fixed4 col = tex2D(_MainTex, uv);
                col.rgb *= _Intensity;
                return col;
            }
    
            ENDCG
        }
    }
}