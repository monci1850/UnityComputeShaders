Shader "Custom/GrassBlades240103"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull off

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Scale;
        float _Fade;
        float4x4 _Matrix;
        float3 _Position;

        float4x4 create_matrix(float3 pos, float theta)
        // this function creates a matrix that rotates around the z-axis
        {
            float c = cos(theta);
            float s = sin(theta);
            return float4x4(
                c,-s, 0, pos.x,
                s, c, 0, pos.y,
                0, 0, 1, pos.z,
                0, 0, 0, 1
            );
        }

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct GrassBlade
            {
                float3 position;
                float lean;
                float noise;
                float fade;
            }        
            UNITY_DECLARE_INSTANCE_BUFFER<GrassBlade> bladesBuffer; //replace StructuredBuffer with UNITY_DECLARE_INSTANCE_BUFFER macro
        #endif

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex.xyz *= _Scale;
                float4 rotatedVertex = mul(_Matrix, v.vertex);
                v.vertex.xyz += _Position;
                v.vertex = lerp(v.vertex, rotatedVertex, v.texcoord.y);
            #endif
        }
        
        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassBlade blade = bladesBuffer[unity_InstanceID];
                _Matrix = create_matrix(blade.position, blade.lean);
                _Position = blade.position;
                _Fade = blade.fade;
            #endif
        }
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * _Fade;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
