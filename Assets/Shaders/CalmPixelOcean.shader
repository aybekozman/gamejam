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
        _CrestMotion("Crest pixels, speed, shimmer, FPS", Vector) = (0,1.1,.14,6)
        [HideInInspector] _UseSpriteUV("Use animated sprite atlas", Float) = 0
        [HideInInspector] _SourceRect("Sprite atlas rect", Vector) = (0,0,1,1)
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _OceanPixelSize;
                float4 _Tint, _RepeatSize, _Flow;
                float4 _CrestMotion, _SourceRect;
                float _OceanTime, _UseSpriteUV;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 world : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(world);
                output.world = world.xy;
                output.uv = input.uv;
                return output;
            }
            half3 OceanSample(float2 pixel)
            {
                // Wrap inside the current sprite frame, never into a neighbouring atlas frame.
                float2 frameSize = _OceanPixelSize.zw * _SourceRect.zw;
                float2 local = floor(frac(pixel / frameSize) * frameSize) + .5;
                float2 uv = _SourceRect.xy + local * _OceanPixelSize.xy;
                if (_UseSpriteUV > .5) return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb;
                else return SAMPLE_TEXTURE2D_LOD(_OceanTex, sampler_OceanTex, uv, 0).rgb;
            }
            half3 CrestDetail(float2 pixel, half3 original)
            {
                // A narrow bright line is separated from the broader blue water underneath.
                // Removing its old location before adding the moved detail avoids double crests.
                half3 surround = (OceanSample(pixel + float2(0, 3)) + OceanSample(pixel - float2(0, 3))) * .5;
                half3 difference = max(original - surround, 0);
                half contrast = min(difference.g, difference.b);
                half bright = min(original.g, original.b);
                half mask = smoothstep(.22, .42, bright) * smoothstep(.015, .065, contrast);
                return difference * mask;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 pixel = input.world / _RepeatSize.xy * _OceanPixelSize.zw;
                if (_UseSpriteUV > .5) pixel = (input.uv - _SourceRect.xy) * _OceanPixelSize.zw;
                // Broad travelling swells, quantized in time and space for pixel art.
                // The base texture remains rigid: wave fronts modulate light, never bend sprites.
                float waveTime = floor(_OceanTime * 8) / 8;
                float2 waveWorld = floor(input.world * 6.4) / 6.4;
                float phase = dot(waveWorld, float2(.22, .48)) - waveTime * _Flow.w;
                float swell = sin(phase) * .7 + sin(dot(waveWorld, float2(-.34,.17)) - waveTime * _Flow.w * .7) * .3;
                float crest = pow(saturate(sin(phase + sin(waveWorld.x * .16) * .4)), 12);
                float2 driftPixels = floor((_Flow.xy * _OceanTime / _RepeatSize.xy) * _OceanPixelSize.zw);
                if (_UseSpriteUV < .5) pixel -= driftPixels;
                pixel = floor(pixel);
                half3 color = OceanSample(pixel);
                if (_CrestMotion.x > 0)
                {
                    float t = floor(_OceanTime * max(1, _CrestMotion.w)) / max(1, _CrestMotion.w);
                    // Continuous world phases keep neighbouring areas out of sync and stay stable
                    // as the camera moves. Integer offsets retain crisp, unfiltered source pixels.
                    float p = dot(waveWorld, float2(.47, .29)) - t * _CrestMotion.y;
                    float q = dot(waveWorld, float2(-.21, .53)) - t * _CrestMotion.y * .73;
                    float2 offset = round(float2(sin(p) * .8 + sin(q) * .2, sin(q) * .65) * _CrestMotion.x);
                    half3 restingDetail = CrestDetail(pixel, color);
                    half3 movingDetail = CrestDetail(pixel + offset, OceanSample(pixel + offset));
                    color = color - restingDetail + movingDetail * (1 + _CrestMotion.z * sin(p + q * .4));
                }
                color *= _Tint.rgb * (1 + _Flow.z * (.10 * swell + .06 * crest));
                color += half3(.035,.065,.06) * crest * _Flow.z;
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
