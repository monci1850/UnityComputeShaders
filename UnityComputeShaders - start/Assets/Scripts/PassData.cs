using UnityEngine;
using System.Collections;

public class PassData : MonoBehaviour
{

    public ComputeShader shader;
    public int texResolution = 1024;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;
    int clearHandle;

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitShader();
    }

    private void InitShader()
    {
        circlesHandle = shader.FindKernel("Circles");
        clearHandle = shader.FindKernel("Clear");

        shader.SetInt( "texResolution", texResolution);
        shader.SetVector("clearColor", clearColor);  // set means pass data to shader
        shader.SetVector("circleColor", circleColor);
        
        // setTexture are per kernel 
        shader.SetTexture( circlesHandle, "Result", outputTexture);
        shader.SetTexture(clearHandle, "Result", outputTexture);

        rend.material.SetTexture("_MainTex", outputTexture);
    }
 
    private void DispatchKernels(int count)
    {
        shader.Dispatch(clearHandle, texResolution/8, texResolution/8, 1);
        shader.Dispatch(circlesHandle, count, 1, 1); 
        //shader.SetFloat("time", Time.time); 
    }

    void Update()
    {
        DispatchKernels(15);
    }
}

