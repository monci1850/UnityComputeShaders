using UnityEngine;
using System.Collections;

public class BufferJoy : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;
    int clearHandle;

    struct Circle
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    int count = 10;

    // The CPU -side data for our buffer
    Circle[] circlesData; // array of Circle structs, in other words, array of circles

    // The GPU -side data for our buffer
    ComputeBuffer circleBuffer; // compute buffer to hold array of circles

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    private void InitData()
    {
        circlesHandle = shader.FindKernel("Circles"); // find kernel in compute shader

        uint threadGroupSizeX;
        shader.GetKernelThreadGroupSizes(circlesHandle, out threadGroupSizeX, out _, out _); // use '_' to ignore the other two outputs

        int total = (int)threadGroupSizeX * count;
        circlesData = new Circle[total];    // create array of circles on CPU

        float speed = 100.0f;
        float halfSpeed = speed * 0.5f; // to get both positive and negative values later
        float minRadius = 10.0f;
        float maxRadius = 50.0f;
        float radiusRange = maxRadius - minRadius;

        // initialize circles, fill array of circles on CPU
        for (int i = 0; i < total; i++)
        {
            Circle circle = circlesData[i]; // get circle at index i

            // Random.value returns a random number between 0.0 [inclusive] and 1.0 [inclusive] (Read Only).
            circle.origin.x = Random.value * texResolution;
            circle.origin.y = Random.value * texResolution;
            circle.velocity.x = (Random.value * speed) - halfSpeed; // -50 to 50
            circle.velocity.y = (Random.value * speed) - halfSpeed; // -50 to 50
            circle.radius = Random.value * radiusRange + minRadius; // 10 to 50

            circlesData[i] = circle; // set circle at index i
        }
    }

    private void InitShader()
    {
    	clearHandle = shader.FindKernel("Clear");
    	
        shader.SetVector( "clearColor", clearColor );
        shader.SetVector( "circleColor", circleColor );
        shader.SetInt( "texResolution", texResolution );

        
        int strideOfCircle = (sizeof(float) * 2) + (sizeof(float) * 2) + sizeof(float); // 2 floats for origin, 2 floats for velocity, 1 float for radius 
        circleBuffer = new ComputeBuffer(circlesData.Length, strideOfCircle); // create compute buffer on GPU, stride is the size of each element in the buffer, in bytes
        circleBuffer.SetData(circlesData); // set data in compute buffer on GPU
        shader.SetBuffer(circlesHandle, "circleBuffer", circleBuffer); // set compute buffer in compute shader, this case is "circles"

		shader.SetTexture( clearHandle, "Result", outputTexture );
        shader.SetTexture( circlesHandle, "Result", outputTexture );

        rend.material.SetTexture("_MainTex", outputTexture);
    }
 
    private void DispatchKernels(int count)
    {
    	shader.Dispatch(clearHandle, texResolution/8, texResolution/8, 1);
        shader.SetFloat("time", Time.time);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void Update()
    {
        DispatchKernels(count);
    }

    private void OnDestroy()
    {
        
    }
}

