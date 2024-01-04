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

        public GrassClump(Vector3 pos) // Constructor that takes a Vector3 argument.
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
        // access the bound obj
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.sharedMesh.bounds;   // Why use sharedMesh instead of mesh? A: https://answers.unity.com/questions/1219661/what-is-the-difference-between-mesh-and-sharedmesh.html
        
        // calculate the number of grass clumps
        Vector3 clumps = bounds.extents; // extends is the distance from the center to the extents of the bounding box. 
        // 0.1f is a magic number. It is used to make the grass clumps smaller.
        Vector3 vec = transform.localScale / 0.1f * density; // https://docs.unity3d.com/ScriptReference/Transform-localScale.html
        clumps.x *= vec.x;  
        clumps.z *= vec.z;

        int total = (int)(clumps.x) * (int)(clumps.z); // total is the total number of grass clumps.


        // Use the compute shader to set the lean angle for each clump.
        kernelLeanGrass = shader.FindKernel("LeanGrass"); // FindKernel returns the index of the kernel with the given name.

        uint threadGroupSize;  // The size of the group of threads in the compute shader.
        shader.GetKernelThreadGroupSizes(kernelLeanGrass, out threadGroupSize, out _, out _); // GetKernelThreadGroupSizes gets the thread group sizes of a compute shader kernel.
        groupSize = Mathf.CeilToInt((float)total / (float)threadGroupSize); // Mathf.CeilToInt returns the smallest integer greater to or equal to f.
        int count = groupSize * (int)threadGroupSize; // count is the number of grass clumps that will be generated.

        clumpsArray = new GrassClump[count]; // Create a array of GrassClump struct,the number of struct is count.

        for(int i=0; i<count; i++)
        {
            // Returns a random position inside the bounding box xz plane in world space(add center of bounds, and transformed).
            // Random.Range returns 0~1, 
            Vector3 pos = new Vector3(Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x,
                                      0,
                                      Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z); // Random.value returns a random number between 0.0 [inclusive] and 1.0 [inclusive] (Read Only).
            pos = transform.TransformPoint(pos); // TransformPoint transforms a position from model space to world space.
            clumpsArray[i] = new GrassClump(pos); // Create a new GrassClump and store the pos in the array.
        }

        clumpsBuffer = new ComputeBuffer(count, SIZE_GRASS_CLUMP); // Create a new ComputeBuffer.
        clumpsBuffer.SetData(clumpsArray); // Copy the data from the array to the ComputeBuffer.


        // Pass the data to the compute shader 

        shader.SetBuffer(kernelLeanGrass, "clumpsBuffer", clumpsBuffer); // pass to the compute shader the buffer that stores the grass clumps.
        shader.SetFloat("maxLean", maxLean*Mathf.PI/180); //in radians.
        timeID = Shader.PropertyToID("time"); // PropertyToID gets the unique identifier for a shader property name. ID is more efficient than strings. 

        // Use the compute shader to generate the mesh.

        argsArray[0] = mesh.GetIndexCount(0); // GetIndexCount returns the vertex count in the first submesh of the mesh object. 
        argsArray[1] = (uint)count; // The number of instances to draw.
        argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments); // Create a new ComputeBuffer.
        argsBuffer.SetData(argsArray); // SetData sets the values of the ComputeBuffer.


        // Pass the data to the material

        material.SetBuffer("clumpsBuffer", clumpsBuffer); // SetBuffer sets a named compute buffer.
        material.SetFloat("_Scale", scale); // SetFloat sets a named float value.
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
