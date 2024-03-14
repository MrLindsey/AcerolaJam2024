//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

Shader "Custom/CommonDynamic"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("NormalMap", 2D) = "white" {}
        _MatSmoothAO("MatSmoothAO", 2D) = "white" {}

        _GlowAmount("GlowAmount", Range(-1,1)) = 0

        _NormAmount("Normal Amount", Range(0,2)) = 1
        _MetallicAmount("Metallic Amount", Range(0,2)) = 1
        _ViewAngleAmount("View Angle Amount", Range(0,2)) = 0.5
        _BaseReflectAmount("Base Reflect Amount", Range(0,1)) = 0.05

        _RoughnessAmount("Roughness Amount", float) = 80
        _BumpPow("Bump Power", float) = 0.5
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ForwardBase" }
            LOD 100


            Pass
            {
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"
                #include "Lighting.cginc"

                #pragma multi_compile_fwdbase nodirlightmap nodynlightmap novertexlight
                #include "AutoLight.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float4 tangent: TANGENT;
                    float2 uv : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 pos : SV_POSITION;
                    SHADOW_COORDS(2)

                    float3 worldPos : TEXCOORD3;
                    float3 worldNormal : TEXCOORD4;

                    float4 lightColour: COLOR;      // Alpha has the edge angle
                    float3 lightDir : TEXCOORD1;

                    half3 tspace0 : TEXCOORD5; // tangent.x, bitangent.x, normal.x
                    half3 tspace1 : TEXCOORD6; // tangent.y, bitangent.y, normal.y
                    half3 tspace2 : TEXCOORD7; // tangent.z, bitangent.z, normal.z

                    UNITY_VERTEX_OUTPUT_STEREO
                };


                sampler2D _MainTex;
                float4 _MainTex_ST;
                sampler2D _NormalTex;
                float _NormAmount;
                float _GlowAmount;
                sampler2D _MatSmoothAO;
                float _MetallicAmount;
                float _ViewAngleAmount;
                float _BaseReflectAmount;
                float _RoughnessAmount;
                float _BumpPow;

                uniform fixed4 _LightmapTint;

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    float3 worldNormal = UnityObjectToWorldNormal(v.normal);

                    o.worldPos = worldPos;
                    o.worldNormal = worldNormal;

                    // Do vertex lighting on the lights
                    float3 vertexLighting = float3(0.0, 0.0, 0.0);
                    float3 avgLightDirection = float3(0.0, 0.0, 0.0);
                    for (int index = 0; index < 2; index++)
                    {
                        float3 lightPosition = float3(unity_4LightPosX0[index], unity_4LightPosY0[index], unity_4LightPosZ0[index]);

                        float3 vertexToLightSource = lightPosition - worldPos;
                        float3 lightDirection = normalize(vertexToLightSource);
                        float squaredDistance = dot(vertexToLightSource, vertexToLightSource);
                        float attenuation = 1.0 / (1.0 + unity_4LightAtten0[index] * squaredDistance);

                        // Had to take a fraction off as it flickers off otherwise
                        attenuation = max(0.0, attenuation - 0.05);

                        float3 diffuseReflection = (attenuation * unity_LightColor[index]) * max(0.0, dot(worldNormal, lightDirection));
                        vertexLighting = vertexLighting + diffuseReflection;

                        if (index == 0)
                            avgLightDirection = lightDirection;
                    }

                    o.lightDir = normalize(avgLightDirection);
                    o.lightColour = float4(vertexLighting, 1.0f);
                    o.lightColour += unity_AmbientSky;

                    // For bump normals
                    half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    // compute bitangent from cross product of normal and tangent
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 wBitangent = cross(worldNormal, wTangent) * tangentSign;

                    // View angle
                    float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                    float diff = 1.0 - dot(worldNormal, worldViewDir);
                    o.lightColour.a = _ViewAngleAmount * pow(diff, 3);

                    // output the tangent space matrix
                    o.tspace0 = half3(wTangent.x, wBitangent.x, worldNormal.x);
                    o.tspace1 = half3(wTangent.y, wBitangent.y, worldNormal.y);
                    o.tspace2 = half3(wTangent.z, wBitangent.z, worldNormal.z);

                    TRANSFER_SHADOW(o);
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                    // Main texture
                    fixed4 texCol = tex2D(_MainTex, i.uv);
                    fixed4 col = texCol;
                    fixed3 baseCol = col;

                    // Metallic, smooth and AO texture
                    fixed4 msao = tex2D(_MatSmoothAO, i.uv);

                    float metallic = msao.r * _MetallicAmount;      // Amount of reflection
                    float smoothness = msao.g;  // LOD mip level of reflection
                    float ao = msao.b;
                    float viewAngle = i.lightColour.a;

                    // Sample the normal map, and decode from the Unity encoding
                    half3 tnormal = UnpackNormal(tex2D(_NormalTex, i.uv));

                    // transform normal from tangent to world space
                    half3 worldNormal;
                    worldNormal.x = dot(i.tspace0, tnormal);
                    worldNormal.y = dot(i.tspace1, tnormal);
                    worldNormal.z = dot(i.tspace2, tnormal);
                    worldNormal = lerp(i.worldNormal, worldNormal, _NormAmount);

                    float bumpFactor = abs(dot(worldNormal, i.lightDir));

                    // Bump contrast
              //      bumpFactor = 1.0;// 1.0 - _BumpPow + (_BumpPow * bumpFactor);
                    baseCol = baseCol * (_BumpPow * bumpFactor);

                    half3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

                    // Temp... can use the box project later
                    half3 worldRefl = reflect(-worldViewDir, worldNormal);

                    smoothness = lerp(smoothness, smoothness * 0.5, metallic);
                    half4 refData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, worldRefl, smoothness * _RoughnessAmount);
                    half3 baseRefColor = DecodeHDR(refData, unity_SpecCube0_HDR);

                    half3 refColor = baseRefColor;
                    refColor *= texCol.rgb * bumpFactor;
                    refColor *= i.lightColour.xyz;

                    // Add reflection
                    half3 litColor = baseCol * i.lightColour.xyz;
                    col.rgb = lerp(litColor, refColor, metallic);

                    col.rgb += (baseRefColor * _BaseReflectAmount);

                    // Add rim light
                    float rimFactor = 1.0 - dot(worldNormal, worldViewDir);
                    rimFactor = _ViewAngleAmount * pow(rimFactor, (1 - metallic) * 4);
                    rimFactor = (rimFactor - 0.5)  + 0.5;
                    col.rgb += (viewAngle * baseRefColor) * rimFactor;
                   
                    // Apply realtime shadow map
                    fixed shadow = SHADOW_ATTENUATION(i);
                    col.rgb *= shadow;

                    // Apply ambient occlusion
                    col.rgb += (unity_AmbientSky * texCol) * (1.0 - metallic);
                    col.rgb *= ao;

                    // Apply the overall  tint
                    col.rgb *= _LightmapTint;

                    // Add the selection brightness
                    col.rgb += _GlowAmount;
                    
                    return col;

                }
                ENDCG
            }

            // shadow casting support
            UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
        }
}
