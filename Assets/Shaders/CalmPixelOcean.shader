Shader "Ship/Calm Pixel Ocean"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _OceanTex("Ocean (standalone, repeat)", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _RepeatSize("World repeat size", Vector) = (40,30,0,0)
        _Flow("Flow XY, wave strength Z, wave speed W", Vector) = (.12,.03,0,.3)
        _OceanTime("Scaled time", Float) = 0
        _OceanPixelSize("Inverse size XY, pixel dimensions ZW", Vector) = (.001953125,.0026041667,512,384)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest LEqual
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_OceanTex); SAMPLER(sampler_OceanTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _OceanPixelSize;
                float4 _Tint, _RepeatSize, _Flow;
                float _OceanTime;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.world / _RepeatSize.xy;
                // Broad travelling swells, quantized in time and space for pixel art.
                // The base texture remains rigid: wave fronts modulate light, never bend sprites.
                float waveTime = floor(_OceanTime * 8) / 8;
                float2 waveWorld = floor(input.world * 6.4) / 6.4;
                float phase = dot(waveWorld, float2(.22, .48)) - waveTime * _Flow.w;
                float swell = sin(phase) * .7 + sin(dot(waveWorld, float2(-.34,.17)) - waveTime * _Flow.w * .7) * .3;
                float crest = pow(saturate(sin(phase + sin(waveWorld.x * .16) * .4)), 12);
                float2 driftPixels = floor((_Flow.xy * _OceanTime / _RepeatSize.xy) * _OceanPixelSize.zw);
                uv -= driftPixels * _OceanPixelSize.xy;
                uv = (floor(uv * _OceanPixelSize.zw) + .5) * _OceanPixelSize.xy;
                half4 color = SAMPLE_TEXTURE2D(_OceanTex, sampler_OceanTex, uv);
                color.rgb *= _Tint.rgb * (1 + _Flow.z * (.10 * swell + .06 * crest));
                color.rgb += half3(.035,.065,.06) * crest * _Flow.z;
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
