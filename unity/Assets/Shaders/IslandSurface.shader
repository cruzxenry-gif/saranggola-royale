Shader "Saranggola/Island Surface"
{
    Properties
    {
        _BaseColor ("Base colour", Color) = (0.22,0.52,0.55,1)
        _Accent ("Foam or light", Color) = (0.73,0.87,0.72,1)
        _Mode ("Surface type", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _Accent;
            float _Mode;
            CBUFFER_END
            struct In { float4 p : POSITION; half4 color : COLOR; };
            struct Out { float4 p : SV_POSITION; float3 world : TEXCOORD0; half4 color : COLOR; half fog : TEXCOORD1; };
            Out Vert(In input)
            {
                Out output;
                output.world = TransformObjectToWorld(input.p.xyz);
                if (_Mode > 1.5 && _Mode < 2.5)
                    output.world.xz += sin(_Time.y*2.3 + output.world.z*.43)*input.color.a*float2(.12,.085);
                output.p = TransformWorldToHClip(output.world);
                output.color = input.color;
                output.fog = ComputeFogFactor(output.p.z);
                return output;
            }
            half4 Frag(Out input) : SV_Target
            {
                float2 q = input.world.xz;
                float wave = .5 + .5*sin(q.x*.51 + q.y*.82 - _Time.y*1.66);
                float wave2 = .5 + .5*sin(q.x*.37 - q.y*.68 + _Time.y*.73);
                half amount = smoothstep(.71,.97,wave*wave2);
                if (_Mode > .5 && _Mode < 1.5) amount = lerp(.20h,.92h, wave*wave2);
                if (_Mode > 1.5 && _Mode < 2.5) amount = .24h+wave*.38h;
                if (_Mode > 2.5) amount = .25h + .47h*wave;
                half3 color = lerp(_BaseColor.rgb,_Accent.rgb,amount)*lerp(half3(1,1,1),input.color.rgb,.48h);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
