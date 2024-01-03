using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassClump240102 : MonoBehaviour
{
    struct GrassClump
    {
        public Vector3 position;
        public float lean;
        public float noise;

        public GrassClump(Vector3 pos) // Constructor of GrassClump.
        {
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            lean = 0;
            noise = Random.Range(0.5f, 1);
            if (Random.value < 0.5f) noise = -noise;    // use another random number to decide whether to make the noise negative.
        }
    }

    int SIZE_GRASS_CLUMP = 5 * sizeof(float); 
    public Mesh mesh;  // The mesh that will be drawn.
    public Material material;   // The material that will be used to render the mesh.
    public ComputeShader shader;    // The compute shader that will be used to generate the mesh.
    [Range(0, 1)]
    public float density = 0.8f;    // The density of the grass clumps.
    [Range(0.1f, 3)]
    public float scale = 0.2f;  // The scale of the grass clumps.
    [Range(10, 45)]
    public float maxLean = 25;  // The maximum lean of the grass clumps.

    // ComputeBuffer is a buffer that can be accessed from compute shaders.
    ComputeBuffer clumpsBuffer; // A buffer that stores the grass clumps.
    ComputeBuffer argsBuffer;   // A buffer that stores the arguments for drawing the mesh.
    GrassClump[] clumpsArray;   // An array that stores the grass clumps.
    uint[] argsArray = new uint[] { 0, 0, 0, 0, 0 }; // An array that stores the arguments for drawing the mesh.
    Bounds bounds;  // The bounds of the mesh.
    int timeID; // The ID of the time variable in the compute shader.
    int groupSize;  // The size of the group of threads in the compute shader.
    int kernelLeanGrass;    // The ID of the kernel that generates the grass clumps.

    // Start is called before the first frame update
    void Start()
    {
        // print out the path of shader to console for debugging.
        Debug.Log("Compute Shader path: " + shader.name); // shader.name is the name of the shader.
        
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30)); // Create a new Bounds object.
        InitShader();
    }

    void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;   // Why use sharedMesh instead of mesh? A: https://answers.unity.com/questions/1219661/what-is-the-difference-between-mesh-and-sharedmesh.html
        
        Vector3 clumps = bounds.extents; // extends is the distance from the center to the extents of the bounding box. 
        // vec is used to scale the grass clumps.
        Vector3 vec = transform.localScale / 0.1f * density; // transform.localScale is the scale of the object that owns this script. / 0.1f is to make the grass clumps smaller.
        clumps.x *= vec.x;  
        clumps.z *= vec.z;

        int total = (int)(clumps.x) * (int)(clumps.z); // total is the total number of grass clumps.



        kernelLeanGrass = shader.FindKernel("LeanGrass"); // FindKernel returns the index of the kernel with the given name.

        uint threadGroupSize;  // The size of the group of threads in the compute shader.
        shader.GetKernelThreadGroupSizes(kernelLeanGrass, out threadGroupSize, out _, out _); // GetKernelThreadGroupSizes gets the thread group sizes of a compute shader kernel.
        groupSize = Mathf.CeilToInt(total / (float)threadGroupSize); // Mathf.CeilToInt returns the smallest integer greater to or equal to f.
        int count = groupSize * (int)threadGroupSize; // count is the number of grass clumps that will be generated.

        clumpsArray = new GrassClump[count]; // Create a new array of GrassClump.

        for(int i=0; i<count; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-clumps.x, clumps.x), 
                                      0,
                                      Random.Range(-clumps.z, clumps.z)); 
            clumpsArray[i] = new GrassClump(pos); // Create a new GrassClump and store the pos in the array.
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP); // Create a new ComputeBuffer.
        clumpsBuffer.SetData(clumpsArray); // SetData sets the values of the ComputeBuffer.

        shader.SetBuffer(kernelLeanGrass, "clumpsBuffer", clumpsBuffer); // SetBuffer sets a named compute buffer.
        shader.SetFloat("maxLean", maxLean*Mathf.PI/180); // maxLean*Mathf.PI/180 is the maximum lean in radians.
        timeID = Shader.PropertyToID("time"); // PropertyToID gets the unique identifier for a shader property name.

        argsArray[0] = mesh.GetIndexCount(0); // GetIndexCount returns the number of indices in the submesh.
        argsArray[1] = (uint)count; // The number of instances to draw.
        argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments); // Create a new ComputeBuffer.
        argsBuffer.SetData(argsArray); // SetData sets the values of the ComputeBuffer.

        material.SetBuffer("clumpsBuffer", clumpsBuffer); // SetBuffer sets a named compute buffer.
        material.SetFloat("_scale", scale); // SetFloat sets a named float value.
    }

    // Update is called once per frame
    void Update()
    {
        shader.SetFloat(timeID, Time.time); // SetFloat sets a named float value.
        shader.Dispatch(kernelLeanGrass, groupSize, 1, 1); // Dispatch schedules the execution of a compute shader. x, y, z are the number of thread groups to execute in each dimension.
        
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer); //Q: DrawMeshInstancedIndirect use CPU or not? 
    }

    private void OnDestroy()
    {
        // Release the buffers when the object is destroyed.
        clumpsBuffer.Release();
        argsBuffer.Release();
    }
}
