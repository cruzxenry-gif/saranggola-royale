Shader "Saranggola/Outline"
{
    Properties { _Width ("Hull width", Range(0,0.1)) = 0.032 _Ink ("Ink", Color) = (.105,.083,.119,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" "RenderType"="Opaque" }
        Pass
        {
            Name "Hull"
            Tags { "LightMode"="UniversalForward" }
            Cull Front ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Width;
            half4 _Ink;
            CBUFFER_END
            struct In { float4 p : POSITION; float3 n : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 Vert(In input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.p.xyz + input.n * _Width);
            }
            half4 Frag() : SV_Target { return _Ink; }
            ENDHLSL
        }
    }
}
