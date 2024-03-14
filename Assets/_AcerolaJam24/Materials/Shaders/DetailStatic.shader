//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

Shader "Custom/DetailStatic"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("NormalMap", 2D) = "white" {}
        _MatSmoothAO("MatSmoothAO", 2D) = "white" {}

        _BaseReflectAmount("Base Reflect Amount", Range(0,0.2)) = 0.05
        _RefMinAdjust("Reflect Min Adjust", Vector) = (0,0,0,1)
        _RefMaxAdjust("Reflect Max Adjust", Vector) = (0,0,0,1)

        _ReflectAmount("Reflect Amount", Range(0,3)) = 1
        _MetallicAmount("Metallic Amount", Range(0,2)) = 0
        _NormAmount("Normal Amount", Range(0,5)) = 0.1
        _LightmapFactor("Lightmap Factor", Range(0,1.5)) = 1
        _RoughnessAmount("Roughness Amount", float) = 80

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

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 pos : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent: TANGENT;
                    float2 uv0 : TEXCOORD0; // Main UVs
                    float2 uv1 : TEXCOORD1; // Lightmap UVs 
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 uv0 : TEXCOORD0; // Main Texture UVs
                    float2 uv1 : TEXCOORD1; // Lightmap UVs
                    float3 worldPos : TEXCOORD2;

                    half3 tspace0 : TEXCOORD3; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD4; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD5; // tangent.z, bitangent.z, normal.z

                    float4 vertex : SV_POSITION;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float4 _MainTex_ST;

                sampler2D _NormalTex;
                sampler2D _MatSmoothAO;

                float _ReflectAmount;
                float _MetallicAmount;
                float _BaseReflectAmount;
                float4 _RefMinAdjust;
                float4 _RefMaxAdjust;

                float _NormAmount;
                float _LightmapFactor;
                float _RoughnessAmount;

                uniform fixed4 _LightmapTint;


                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = UnityObjectToClipPos(v.pos);

                    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                    o.uv1 = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                    // For reflection
                    o.worldPos = mul(unity_ObjectToWorld, v.pos).xyz;

                    // For bump normals
                    half3 wNormal = UnityObjectToWorldNormal(v.normal);
                    half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 wBitangent = cross(wNormal, wTangent) * tangentSign;

                    // output the tangent space matrix
                    o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
                    o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
                    o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);

                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                float3 BoxProjection(float3 direction, float3 position, float3 cubemapPosition, float3 boxMin, float3 boxMax)
                {
                    boxMin *= _RefMinAdjust.w;
                    boxMax *= _RefMaxAdjust.w;

                    boxMin += _RefMinAdjust.xyz;
                    boxMax += _RefMaxAdjust.xyz;

                    float3 factors = ((direction > 0 ? boxMax : boxMin) - position) / direction;
                    float scalar = min(min(factors.x, factors.y), factors.z);
                    return direction * scalar + (position - cubemapPosition);
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    // sample the textures
                    fixed4 baseCol, col;
                    float refAmount;

                    // Fetch main texture & colour
                    baseCol = tex2D(_MainTex, i.uv0.xy);
                    fixed4 msao = tex2D(_MatSmoothAO, i.uv0);

                    // Fetch detailmap 
                    // R = Metallic, G = Smoothness, B = Ambient Occlusion
                    float metallic = msao.r;      // Amount of reflection
                    float smoothness = msao.g;    // LOD mip level of reflection
                    float ao = msao.b;

                    // Apply reflection
                    half3 originalWorldNormal = float3(i.tspace0.z, i.tspace1.z, i.tspace2.z); // Need to normalise the normal again to get the per-pixel interpolation values

                    // Sample the normal map, and decode from the Unity encoding
                    half3 tnormal = UnpackNormal(tex2D(_NormalTex, i.uv0));
                    half3 worldNormal;

                    // transform normal from tangent to world space
                    worldNormal.x = dot(i.tspace0, tnormal);
                    worldNormal.y = dot(i.tspace1, tnormal);
                    worldNormal.z = dot(i.tspace2, tnormal);

                    half3 scaledWorldNormal = lerp(originalWorldNormal, worldNormal, _NormAmount);

                    // Fetch lightmap
                    half4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv1.xy);
                    half3 bakedColor = DecodeLightmap(bakedColorTex);

                    // Apply the directional lightmap
                    fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, i.uv1.xy);
                    bakedColor = DecodeDirectionalLightmap(bakedColor, bakedDirTex, scaledWorldNormal);
                    bakedColor = pow(bakedColor, _LightmapFactor);

                    // Get the cubemap reflection
                    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                    float3 refDirection = reflect(-worldViewDir, scaledWorldNormal);
                    half3 worldRefl = BoxProjection(refDirection, i.worldPos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);

                    smoothness = lerp(smoothness, smoothness * 0.5, metallic);
                    half4 refData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, worldRefl, smoothness * _RoughnessAmount);
                    half3 baseRefColor = DecodeHDR(refData, unity_SpecCube0_HDR) *_LightmapTint;

                    half3 refColor = baseRefColor * _ReflectAmount;
                    refColor *= baseCol.rgb * bakedColor;

                    half3 litColor = baseCol * bakedColor;
                    col.rgb = lerp(litColor, refColor, metallic + _MetallicAmount);
                    col.rgb += (baseRefColor * _BaseReflectAmount);

                    // Apply if you want an overall lightmap tint colour
                     col.rgb *= _LightmapTint;

                    // Apply ambient occlusion
                //    col.rgb += (unity_AmbientSky * baseCol) * (1.0 - metallic);
                //    col.rgb += unity_AmbientSky;
                    col.rgb *= ao;


                    
                    col.a = 1;

                    return col;

                }
                ENDCG
            }
        }
}
