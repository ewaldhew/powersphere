Shader "Custom/Grass"
{
    Properties
    {
        _Radius("Cluster Radius", Float) = 1
        _Height("Grass Blade Maximum Height", Float) = 1
        _HeightJitter("Amount Of Random Height Variation", Float) = 0.1
        _Width("Grass Blade Base Width", Float) = 1
        _Lean("Grass Blade Lean Amount", Float) = 0.4
        _Color("Grass Color", Color) = (0.1, 1, 0.1, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.0
            #pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "Grass.hlsli"

            ENDHLSL
        }
    }
}
