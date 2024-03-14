Shader "Custom/StaticCommonOld"
{
    Properties
    {
        _SubTextures("SubTextures", float) = 4
        _PaletteTex("Palette", 2D) = "white" {}
        _MainTex("Texture", 2D) = "white" {}
        _MainTexAmount("Main Tex Amount", Range(0,1)) = 1
        _DetailTex("Detail Textures", 2DArray) = "white" {}
        _DetailScale("Detail Scale", float) = 10
        _DetailNormals("Detail Normal Maps", 2DArray) = "white" {}
        _DetailAmount("Detail Amount", Range(0,1)) = 1
        _Roughness("Roughness", Range(0,6)) = 2
        _RefAmount("Reflect Amount", Range(0,10)) = 0.5
        _RefMinAdjust("Reflect Min Adjust", Vector) = (0,0,0,1)
        _RefMaxAdjust("Reflect Max Adjust", Vector) = (0,0,0,1)
        _NormAmount("Normal Amount", Range(0,1)) = 0.1
        _LightmapFactor("Lightmap Factor", Range(0,1.5)) = 1
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase"}
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma require 2darray

                #include "UnityCG.cginc"
                #include "Lighting.cginc"

                #pragma multi_compile_fwdbase nodirlightmap nodynlightmap novertexlight
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
                    float2 uv1 : TEXCOORD1; // Lightmap UVs
                    float3 uv2 : TEXCOORD2; // Detail map UVs
                    float3 worldPos : TEXCOORD3;

                    half3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z

                    SHADOW_COORDS(7)

                    float4 pos : SV_POSITION;
                    fixed4 colour : COLOR0;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                float _SubTextures;
                sampler2D _PaletteTex;
                sampler2D _MainTex;
                float _MainTexAmount;
                float4 _MainTex_ST;

                UNITY_DECLARE_TEX2DARRAY(_DetailTex);
                float _DetailScale;
                float _DetailAmount;

                UNITY_DECLARE_TEX2DARRAY(_DetailNormals);

                float _RefAmount;
                float4 _RefMinAdjust;
                float4 _RefMaxAdjust;
                float _Roughness;
                float _NormAmount;
                float _LightmapFactor;
                float _RefPower;

                half GetSubtextureIndex(float2 uv, float numSubTexAcross)
                {
                    half detailIndexX = floor(uv.x * numSubTexAcross);
                    half detailIndexY = floor((1.0 - uv.y) * numSubTexAcross);
                    half detailIndex = (detailIndexY * numSubTexAcross) + detailIndexX;
                    return detailIndex;
                }

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = UnityObjectToClipPos(v.pos);

                    // Get the colour from the palette texture
                    o.colour = tex2Dlod(_PaletteTex, float4(v.uv0.xy, 0.0, 0.0));

                    half detailIndex = GetSubtextureIndex(v.uv2, _SubTextures);
                    o.uv2 = float3(v.uv2 * _DetailScale, detailIndex);

                    o.uv0 = TRANSFORM_TEX(v.uv2, _MainTex);
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

                    TRANSFER_SHADOW(o);
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
                    fixed4 col, tex, detailTex;
                    float refAmount;

                    // Fetch main texture & colour
                    tex = tex2D(_MainTex, i.uv0.xy);
                    half3 baseColour = i.colour * tex.rgb;

                    // Fetch detailmap 
                    // R = Greyscale, G = AO, B = Metallic
                    detailTex = UNITY_SAMPLE_TEX2DARRAY(_DetailTex, i.uv2);
                    half3 detailColour = (-0.5 + detailTex.r) * (_DetailAmount * 2);
                    col.rgb = baseColour * (1.0 + detailColour);
                    float ao = lerp(1, detailTex.g, _DetailAmount);

                    float metallic = lerp(0, detailTex.b, _DetailAmount);

                    baseColour = col.rgb;
                    refAmount = 1.0 - tex.a;

                    // Apply reflection
                    half3 originalWorldNormal = float3(i.tspace0.z, i.tspace1.z, i.tspace2.z); // Need to normalise the normal again to get the per-pixel interpolation values

                    // Sample the normal map, and decode from the Unity encoding
                    float3 tempUV = float3(i.uv2.x, i.uv2.y, 0);
                    half3 tnormal = UnpackNormal(UNITY_SAMPLE_TEX2DARRAY(_DetailNormals, i.uv2));
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
                    bakedColor = DecodeDirectionalLightmap(bakedColor, bakedDirTex, worldNormal);
                    bakedColor = pow(bakedColor, _LightmapFactor);

                    float maxRoughness = 4;
                    float scaleRoughness = metallic * maxRoughness;

                    // Get the cubemap reflection
                    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
                    float3 refDirection = reflect(-worldViewDir, scaledWorldNormal);
                    half3 worldRefl = BoxProjection(refDirection, i.worldPos, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                    half4 refData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, worldRefl, maxRoughness - (scaleRoughness * _Roughness));
                    half3 refColor = DecodeHDR(refData, unity_SpecCube0_HDR);// *_LightmapTint;

                    float lightMapRef = 0.95;
                    refColor *= (1 - lightMapRef) + (bakedColor * lightMapRef);
                    refColor *= tex.a;

                    col.rgb += (refColor * _RefAmount) * ao;

                    col.rgb *= bakedColor;
                    col.rgb *= ao;

                    // Apply if you want an overall lightmap tint colour
                    // col.rgb *= bakedColor * _LightmapTint;

                    float emissive = 0;// i.colour.a;
                    col.rgb += i.colour * emissive;
                    col.a = 1;

                    // Apply realtime shadow map
                    fixed shadow = SHADOW_ATTENUATION(i);
                    col.rgb *= shadow;

                    return col;

                }
                ENDCG
            }

            // shadow casting support
            UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
        }
}
