//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

Shader "Custom/ProtoypeBox"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Scale("Scale", float) = 1
        _LightMapped("Lightmapped", float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Scale;
            float _LightMapped;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);

                // Box project the UVs in world space
                half3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                half3 worldNormal = v.normal;
                worldNormal = abs(worldNormal);

                half3 invNormal = 1 - worldNormal;
                float u0 = worldPos.x * invNormal.x;
                u0 += worldPos.y * invNormal.y * invNormal.z;

                float v0 = worldPos.z * invNormal.z;
                v0 += worldPos.y * invNormal.y * invNormal.x;

                o.uv = float2(u0 * _Scale, v0 * _Scale);

                // Lightmap UVs
                o.uv1 = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Sample the main texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // Fetch lightmap
                half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1.xy);
                col.rgb *= lerp(1, DecodeLightmap(bakedColorTex), _LightMapped);

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
