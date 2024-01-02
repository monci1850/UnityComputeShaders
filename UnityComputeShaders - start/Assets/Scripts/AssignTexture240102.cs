using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignTexture240102 : MonoBehaviour
{
    public ComputeShader shader;
    public int texResolution = 256;

    public int TestParam = 0;

    Renderer rend;
    RenderTexture outputTexture;
    int kernelHandle;

    // Start is called before the first frame update
    void Start()
    {
        // RenderTexture constructor does not actually create the hardware texture; it just creates an object that stored in memory. 
        // https://docs.unity3d.com/ScriptReference/RenderTexture.Create.html
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create(); // Calling Create lets you create it up front. Create does nothing if the texture is already created.

        rend = GetComponent<Renderer>();    // To be able render the oject that owns this script. 
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        kernelHandle = shader.FindKernel("CSMain"); // FindKernel returns the index of the kernel with the given name.
        shader.SetTexture(kernelHandle, "Result", outputTexture); // SetTexture sets a named texture parameter.
        rend.material.SetTexture("_MainTex", outputTexture); // SetTexture sets a named texture parameter.
        DispatchShader(texResolution / 16, texResolution / 16); // Dispatch schedules the execution of a compute shader. x, y, z are the number of thread groups to execute in each dimension.
    }

    private void DispatchShader(int x, int y)
    // Dispatch schedules the execution of a compute shader. x, y, z are the number of thread groups to execute in each dimension.
    {
        shader.Dispatch(kernelHandle,x,y,1);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            DispatchShader(texResolution / 8, texResolution / 8);
        }
    }
}
