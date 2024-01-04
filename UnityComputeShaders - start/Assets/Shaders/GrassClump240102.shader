Shader "Custom/GrassClump240102" // change the name of the shader
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200 // LOD 200 means? 
        Cull off

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types    
        #pragma surface surf Standard vertex:vert addshadow fullforwardshadows
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;
        fixed4 _Color;
        float _Glossiness;
        float _Metallic;

        struct Input
        {
            float2 uv_MainTex;
        };

        float _Scale;
        float4x4 _Matrix;
        float3 _Position;

        float4x4 create_matrix(float3 pos, float theta)
        // Create a matrix that rotates the grass clump around the z-axis, in other words, rotate in xy-plane
        {
            float c = cos(theta);
            float s = sin(theta);
            return float4x4(
                c, -s, 0, pos.x,
                s,  c, 0, pos.y,
                0,  0, 1, pos.z,
                0,  0, 0, 1
            );
        }

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct GrassClump
            {
                float3 position;
                float lean;
                float noise;
            };
            StructuredBuffer<GrassClump> clumpsBuffer;
        #endif

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex.xyz *= _Scale; // scale the grass clump model
                float4 rotatedVertex = mul(_Matrix, v.vertex); // rotate the grass clump around the z-axis in vertex shader
                v.vertex.xyz += _Position; // move the grass clump to the position specified in the buffer
                v.vertex = lerp(v.vertex, rotatedVertex,v.texcoord.y);
            #endif
        }

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassClump clump = clumpsBuffer[unity_InstanceID]; // grab the buffer from the GPU
                _Position = clump.position; // set the position of the grass clump
                _Matrix = create_matrix(clump.position, clump.lean); // set the rotation of the grass clump
            #endif
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            clip(c.a - 0.4); // Subtract 0.4 from the alpha value of the grass clump texture because the grass clump texture has a lot of empty space
        }

        ENDCG
    }
}
