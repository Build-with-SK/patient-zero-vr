// Claude-owned integration (E-28): ArmsPreview's pictures only. Never used by the game (Editor folder: not in builds).
// Shades by a fixed direction in the shader itself, so the preview needs no lights (lights, fog and ambient belong to
// Atmosphere and Environment) and still shows the shape of the hands.
Shader "Hidden/PatientZero/ArmsPreviewShade"
{
    Properties
    {
        _BaseMap ("Base map", 2D) = "white" {}
        _BaseColor ("Base colour", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            fixed4 _BaseColor;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _BaseMap);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // From above, behind and to the right of a viewer looking along +z, plus a dim fill from the left.
                float3 key = normalize(float3(.45, .8, -.4)), fill = normalize(float3(-.7, .1, -.3));
                float3 n = normalize(i.normal);
                float shade = .22 + .7 * saturate(dot(n, key)) + .18 * saturate(dot(n, fill));
                fixed4 c = tex2D(_BaseMap, i.uv) * _BaseColor;
                c.rgb *= shade;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
}
