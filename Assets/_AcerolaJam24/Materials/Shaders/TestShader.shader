//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

Shader "Custom/TestShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
        {
            Pass
            {
                Tags {"LightMode" = "ForwardBase"}
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"
                #include "Lighting.cginc"

                // compile shader into multiple variants, with and without shadows
                // (we don't care about any lightmaps yet, so skip these variants)
            //    #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

                #pragma multi_compile_fwdbase  novertexlight

                // shadow helper functions and macros
                #include "AutoLight.cginc"

                struct appdata
                {
                    float4 pos : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent: TANGENT;
                    float2 uv0 : TEXCOORD0; // Palette UVs 
                    float2 uv1 : TEXCOORD1; // Lightmap UVs 
                    float2 uv2 : TEXCOORD2; // Main UVs
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 uv0 : TEXCOORD0; // Main Texture UVs
                    SHADOW_COORDS(1)
                    float3 uv2 : TEXCOORD2; // Detail map UVs
                    float3 worldPos : TEXCOORD3;

                    half3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z

                    float2 uv1 : TEXCOORD7; // Lightmap UVs

                    float4 pos : SV_POSITION;
                    fixed4 colour : COLOR0;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.pos);
                    o.uv0 = v.uv2;
                    o.uv1 = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;


                    // compute shadows data
                    TRANSFER_SHADOW(o)
                    return o;
                }

                sampler2D _MainTex;

                fixed4 frag(v2f i) : SV_Target
                {
                    fixed4 col = tex2D(_MainTex, i.uv0);
                    // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)

                    half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1.xy);
                    half3 bakedColor = DecodeLightmap(bakedColorTex);

                    fixed shadow = SHADOW_ATTENUATION(i);
                    // darken light's illumination with shadow, keep ambient intact
                   // fixed3 lighting = i.diff * shadow + i.ambient;

                    col.rgb = shadow * bakedColor;
                    return col;
                }
            ENDCG
        }

        // shadow casting support
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
