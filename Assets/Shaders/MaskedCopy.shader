Shader "Custom/MaskedCopy"
{
    SubShader
    {
        Tags { "RenderType"="Transparent" }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off
            Blend Off

            Stencil {
                Ref 1
                Comp Equal
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_SourceTex);
            SAMPLER(sampler_SourceTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            [earlydepthstencil]
            half4 frag (v2f i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, i.uv);
            }
            ENDHLSL
        }
    }
}
