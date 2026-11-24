Shader "Saranggola/Toon"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Flash ("Hit flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Flash;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 world : TEXCOORD0; half3 normal : TEXCOORD1; half4 color : COLOR; half fog : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes input)
            {
                Varyings v;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v);
                v.world = TransformObjectToWorld(input.positionOS.xyz);
                v.positionCS = TransformWorldToHClip(v.world);
                v.normal = TransformObjectToWorldNormal(input.normalOS);
                v.color = input.color;
                v.fog = ComputeFogFactor(v.positionCS.z);
                return v;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.world));
                half facing = saturate(dot(normalize(input.normal), sun.direction));
                half band = facing > .68h ? 1.0h : facing > .24h ? .78h : .53h;
                half3 tint = input.color.rgb * _BaseColor.rgb;
                half3 lit = tint * (.42h + band * sun.color * (.35h + .38h * sun.shadowAttenuation));
                lit = lerp(lit, half3(1.0h,.76h,.52h), _Flash);
                return half4(MixFog(lit, input.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Flash;
            CBUFFER_END
            struct ShadowIn { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 ShadowVertex(ShadowIn input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 position = TransformObjectToWorld(input.vertex.xyz);
                float3 normal = TransformObjectToWorldNormal(input.normal);
                float3 direction = _LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                direction = normalize(_LightPosition - position);
                #endif
                float4 clip = TransformWorldToHClip(ApplyShadowBias(position, normal, direction));
                #if UNITY_REVERSED_Z
                clip.z = min(clip.z, UNITY_NEAR_CLIP_VALUE * clip.w);
                #else
                clip.z = max(clip.z, UNITY_NEAR_CLIP_VALUE * clip.w);
                #endif
                return clip;
            }
            half4 ShadowFragment() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback "Universal Render Pipeline/Lit"
}
