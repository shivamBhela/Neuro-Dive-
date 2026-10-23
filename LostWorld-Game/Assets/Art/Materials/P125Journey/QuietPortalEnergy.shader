Shader "Lost Worlds/Quiet Portal Energy"
{
    Properties { _MotionTime("Motion time", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _MotionTime;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(input.positionOS.xyz); o.uv=input.uv; return o;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p=input.uv;
                float rim=smoothstep(.70,1.0,length(p));
                float fade=smoothstep(0,.055,p.y);
                float drift=.5+.5*sin(p.y*8+p.x*2-_MotionTime*.22);
                float pulse=.5+.5*sin(_MotionTime*.35);
                float alpha=(.12+.13*rim+.035*drift+.015*pulse)*fade;
                return half4(lerp(float3(.12,.48,.85),float3(.32,.90,1.0),rim*.65+drift*.25),alpha);
            }
            ENDHLSL
        }
    }
}
