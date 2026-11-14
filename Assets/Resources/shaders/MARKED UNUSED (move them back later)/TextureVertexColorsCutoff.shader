Shader "Griptonite/TextureVertColorsCutoff"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ClipHeight ("Clip Height", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Tags { "RenderType"="Opaque" }
            Fog { Mode Off }
            CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members unknown)

            #pragma vertex vert
            #pragma fragment frag
            struct appdata_t
            {
                float4 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
                float3 vertex : POSITION;
            };
            struct v2f
            {
                float3 objectPosition : TEXCOORD1;
                float2 texcoord0 : TEXCOORD0;
                float4 color : COLOR;
                float4 vertex : SV_POSITION;
            };
            float4 _MainTex_ST;
            float4 _glesMultiTexCoord0;
            float4 _glesColor;
            float4 _glesVertex;
            sampler2D _MainTex;
            float _ClipHeight;
            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord0 = ((v.texcoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
                o.objectPosition = v.vertex.xyz;
                return o;
            }
            float4 frag(v2f i) : SV_TARGET
            {
                float4 c;
                float x;
                x = (i.objectPosition.y - _ClipHeight);
                clip(x);
                float4 tmpvar_1;
                tmpvar_1 = tex2D (_MainTex, i.texcoord0);
                c = tmpvar_1;
                c.xyz = (c.xyz * i.color.xyz);
                return c;
            }
            ENDCG
        }
    }
}
