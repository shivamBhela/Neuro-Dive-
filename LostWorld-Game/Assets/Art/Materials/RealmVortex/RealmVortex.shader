Shader "Lost Worlds/Realm Vortex"
{
    Properties
    {
        _ColorA("Primary energy", Color) = (1, .55, .08, 1)
        _ColorB("Secondary energy", Color) = (.6, .12, .015, 1)
        _Core("Core glow", Color) = (1, .92, .65, 1)
        _MotionTime("Motion time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Vortex"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _Core;
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
                float2 p=(input.uv-.5)*2;
                float radius=length(p);
                float angle=atan2(p.y,p.x);
                // Integer angular frequencies keep the atan seam continuous. No global flashing/pulse.
                float spiral=angle*3+radius*13-_MotionTime*.38;
                float ribbon=pow(.5+.5*sin(spiral),5);
                float veil=.5+.5*sin(angle*5-radius*21+_MotionTime*.23);
                float filament=pow(.5+.5*sin(angle*7+radius*32-_MotionTime*.31),12);
                float core=exp(-radius*radius*38);
                float halo=exp(-radius*radius*5);
                half3 energy=lerp(_ColorB.rgb,_ColorA.rgb,saturate(.2+veil*.35+ribbon*.5));
                energy*=.55+ribbon*.8+filament*.16;
                energy+=_Core.rgb*(core*1.5+halo*.22);
                // Full rectangular coverage; the original gateway naturally occludes the edges.
                return half4(energy,.96);
            }
            ENDHLSL
        }
    }
}
