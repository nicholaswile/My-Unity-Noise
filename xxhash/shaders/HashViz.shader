Shader "Hash/HashViz"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        CGPROGRAM
        #pragma surface ConfigureSurface Standard fullforwardshadows addshadow
        #pragma instancing_options assumeuniformscaling procedural:ConfigureProcedural
        #pragma editor_sync_compilation
        #pragma target 4.5

        #include "HashGPU.hlsl"

        struct Input
        {
            float3 worldPos;
        };

        float _Smoothness;

        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            surface.Albedo = GetHashColor().rgb;
            surface.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
