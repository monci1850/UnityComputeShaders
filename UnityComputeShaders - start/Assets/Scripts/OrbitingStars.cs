using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitingStars : MonoBehaviour
{
    public int starCount = 2;
    public ComputeShader shader;

    public GameObject prefab;

    ComputeBuffer resultBuffer;

    int kernelHandle;
    uint threadGroupSizeX; // The number of threads in a group, this case is 64.
    int groupSizeX;
    
    Vector3[] output; // The output data from the compute shader
    Transform[] stars;
    
    void Start()
    {
        kernelHandle = shader.FindKernel("OrbitingStars");
        shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _); // Get the thread group size from the compute shader, this case is 64.[numthreads(64,1,1)] 
        groupSizeX = (int)((starCount + threadGroupSizeX - 1) / threadGroupSizeX); // Q: Why starCount + threadGroupSizeX - 1 ? A: Because we want to round up the number of groups to the nearest integer.

        // Use a GPU buffer to store the output data from the compute shader
        resultBuffer = new ComputeBuffer(starCount, sizeof(float) * 3); 
        shader.SetBuffer(kernelHandle, "Result", resultBuffer); // Set the compute buffer to the compute shader
        output = new Vector3[starCount]; // Initialize the output data array

        // Create the stars
        stars = new Transform[starCount];
        for (int i = 0; i < starCount; i++)
        {
            stars[i] = Instantiate(prefab, transform).transform; // Instantiate prefab, set parent to this transform, and get the transform component
            
        }
    }

    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        resultBuffer.GetData(output);

        for (int i = 0; i < starCount; i++)
        {
            stars[i].position = output[i];
        }
    }

    void OnDestroy()
    {
        resultBuffer.Dispose();
    }

}
