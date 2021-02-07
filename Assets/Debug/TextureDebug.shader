Shader "Unlit/TextureDebug"
{
    Properties
    {
        [MainTexture] _DebugTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 pos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.pos = TransformObjectToWorld(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            TEXTURE2D(_DebugTex);
            SAMPLER(sampler_DebugTex);

            float4 frag (v2f i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_DebugTex, sampler_DebugTex, i.uv);
            }
            ENDHLSL
        }
    }
}
