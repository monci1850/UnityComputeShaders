Shader "Custom/VisualizeNoise240103"
{
    Properties
    {
    
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass // Pass in terms of Shader means a draw call
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "noiseSimplex.cginc" // to be able to use the perlin function

            #include "UnityCG.cginc" // to be able to use the UnityObjectToClipPos function

            struct appdata // the struct that will be used to pass data from the CPU to the GPU
            {
                float4 vertex : POSITION; // Q: float4? A: 4 floats, x, y, z, w, w is used for perspective division
            };

            struct v2f // the struct that will be used to pass data from the vertex shader to the fragment shader
            {
                float4 vertex : SV_POSITION; //SV_POSITION is a semantic, it tells the GPU that this is the vertex position
                float3 position : TEXCOORD1; // TEXCOORD1 is a semantic, it tells the GPU that this is the second texture coordinate
            };

            float4 wind; //Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windStrength)

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // this function transforms the vertex position from object space to clip space
                o.position = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // offset the position by the wind
                float2 offset = (i.position.xz + wind.xz * _Time.y * wind.z) * wind.w;  
                float noise = perlin(offset.x, offset.y);
                return fixed4(noise, noise, noise, 1);
            }
            ENDCG
        }
    }
}
