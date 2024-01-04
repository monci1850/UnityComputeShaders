using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.SocialPlatforms;

public class GrassBlades240103 : MonoBehaviour
{
    struct GrassBlade
    {
        public Vector3 position;
        public float bend;
        public float noise;
        public float fade;

        public GrassBlade(Vector3 pos)
        {
            //position = pos;
            position.x = pos.x;
            position.y = pos.y;
            position.z = pos.z;
            bend = 0.0f;
            noise = Random.Range(0.5f, 1.0f) * 2.0f - 1.0f; // 0.0f ~ 1.0f
            fade = Random.Range(0.5f, 1.0f); // 0.5f ~ 1.0f
        }
    }
    int SIZE_GRASS_BLADE = 6*sizeof(float); // Vector3 + float + float + float

    // public variables
    public Material grassMaterial;
    public ComputeShader computeShader;
    public Material visualizeNoiseMaterial;
    public bool viewNoise = false;
    [Range(0,1.5f)]
    public float density=0.5f;
    [Range(0.1f,3)]
    public float scale;
    [Range(10,45)]
    public float maxBend;
    [Range(0,2)]
    public float windSpeed;
    [Range(0,360)]
    public float windDirectionAngle;
    [Range(10,1000)]
    public float windPatternScale; // wind scale

    
    ComputeBuffer grassBladesBuffer;
    ComputeBuffer argsBuffer; // for indirect draw
    GrassBlade[] grassBladesArray; // for CPU data
    uint[] argsArray = new uint[5] { 0, 0, 0, 0, 0 }; // for indirect draw
    Bounds bounds;
    int timeID;
    int groupSize;
    int kernelBendGrass;
    Mesh grassBladeMesh;
    Material groundMaterial;
    
    Mesh BladeMeshData // Defining a mesh in code
    {
        get
        { 
            Mesh mesh;
            if(grassBladeMesh != null)
            {
                mesh = grassBladeMesh;
            }
            else
            {
                mesh = new Mesh();  // create a new mesh, no vertex data yet
                float height = 0.2f;
                float rowHeight = height / 4; // each segment height
                float halfWidth = height / 10; // half width of the blade at the bottom

                // TODO: use individual parameters to control the shape of the blade: height, rowHeight, halfWidth

                Vector3[] vertices = // 9 vertices for each blade
                {
                    new Vector3(-halfWidth, 0, 0),
                    new Vector3( halfWidth, 0, 0),
                    new Vector3(-halfWidth, rowHeight, 0),
                    new Vector3( halfWidth, rowHeight, 0),
                    new Vector3(-halfWidth*0.9f, rowHeight * 2, 0),
                    new Vector3( halfWidth*0.9f, rowHeight * 2, 0),
                    new Vector3(-halfWidth*0.8f, rowHeight * 3, 0),
                    new Vector3( halfWidth*0.8f, rowHeight * 3, 0),
                    new Vector3(-halfWidth, height, 0)
                };

                Vector3 normal = new Vector3(0, 0, -1); // tpwards the non-rotated camera
                Vector3[] normals = 
                {
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal,
                    normal
                };
                
                Vector2[] uvs = 
                {
                    new Vector2(0,0),
                    new Vector2(1,0),
                    new Vector2(0,0.25f),
                    new Vector2(1,0.25f),
                    new Vector2(0,0.5f),
                    new Vector2(1,0.5f),
                    new Vector2(0,0.75f),
                    new Vector2(1,0.75f),
                    new Vector2(0.5f,1) // not use (0,1) to avoid the seam
                };
                int[] indices =
                {
                    // both in CCW order to be able to see the front face and sew the seam
                    // Unity CCW order is forward facing
                    0,1,2,1,3,2,//row 1
                    2,3,4,3,5,4,//row 2
                    4,5,6,5,7,6,//row 3
                    6,7,8//row 4
                };

                mesh.vertices = vertices;
                mesh.normals = normals;
                mesh.uv = uvs;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0); // 0 is the order of submeshes. 
            }
            return mesh;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        bounds = new Bounds(Vector3.zero, new Vector3(30, 30, 30));
        grassBladeMesh = BladeMeshData;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        groundMaterial = renderer.material;

        InitShader();

        // print out the material name that applied to the mesh for debugging which holds this script
        // get the mesh and print out its material name
        Debug.Log("mesh material name: " + GetComponent<MeshRenderer>().material.name);


        
    }

    private void OnValidate() // OnValidate is called when the script is loaded or a value is changed in the inspector (Called in the editor only) 
    {
        if(groundMaterial != null)
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.material = (viewNoise) ? visualizeNoiseMaterial : grassMaterial;

            float theta = windDirectionAngle * Mathf.PI / 180.0f; // to radian
            Vector4 wind = new Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windPatternScale); // x is cos(theta), y is sin(theta).x and y are the direction of the wind, z is the speed, w is the strength
            computeShader.SetVector("wind", wind); // pass the wind to the compute shader
            visualizeNoiseMaterial.SetVector("wind", wind); // pass the wind to the visualizeNoiseMaterial
        }

    }
    void InitShader()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Bounds bounds = mf.mesh.bounds;

        Vector3 blades = bounds.extents;
        Vector3 vec = transform.localScale / 0.1f * density;
        blades.x *= vec.x;
        blades.y *= vec.y;

        // print out blades.x, blades.y, blades.z for debugging
        Debug.Log("blades.x: " + blades.x + " blades.y: " + blades.y + " blades.z: " + blades.z);
        int total = (int)(blades.x * blades.z) * 20;

        kernelBendGrass = computeShader.FindKernel("BendGrassBladesKernel");

        uint threadGroupSizeX;
        computeShader.GetKernelThreadGroupSizes(kernelBendGrass, out threadGroupSizeX, out _, out _);
        groupSize = Mathf.CeilToInt((float)total / (float)threadGroupSizeX);
        // print out groupSize and threadGroupSizeX for debugging
        Debug.Log("groupSize: " + groupSize + " threadGroupSizeX: " + threadGroupSizeX);
        int count = groupSize * (int)threadGroupSizeX;

        grassBladesArray = new GrassBlade[count];

        for(int i=0; i<count; i++)
        {
            //Vector3 pos = new Vector3(Random.Range(-bounds.extents.x, bounds.extents.x), 0, Random.Range(-bounds.extents.z, bounds.extents.z));
            Vector3 pos = new Vector3(Random.value * bounds.extents.x * 2 - bounds.extents.x + bounds.center.x,
                                      0,
                                      Random.value * bounds.extents.z * 2 - bounds.extents.z + bounds.center.z);
            pos = transform.TransformPoint(pos);
            grassBladesArray[i] = new GrassBlade(pos);
        }

        // print out count and SIZE_GRASS_BLADE for debugging
        Debug.Log("count: " + count + " SIZE_GRASS_BLADE: " + SIZE_GRASS_BLADE);

        grassBladesBuffer = new ComputeBuffer(count, SIZE_GRASS_BLADE);
        grassBladesBuffer.SetData(grassBladesArray);

        computeShader.SetBuffer(kernelBendGrass, "grassBladesBuffer", grassBladesBuffer);
        computeShader.SetFloat("maxBend", maxBend * Mathf.PI / 180.0f); 
        float theta = windDirectionAngle * Mathf.PI / 180.0f; // to radian
        Vector4 wind = new Vector4(Mathf.Cos(theta), Mathf.Sin(theta), windSpeed, windPatternScale);
        computeShader.SetVector("wind", wind);
        timeID = Shader.PropertyToID("time");

        argsArray[0] = grassBladeMesh.GetIndexCount(0);
        argsArray[1] = (uint)count;
        argsBuffer = new ComputeBuffer(1, argsArray.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(argsArray);

        grassMaterial.SetBuffer("grassBladesBuffer", grassBladesBuffer);
        grassMaterial.SetFloat("_Scale", scale);
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.SetFloat(timeID, Time.time);
        computeShader.Dispatch(kernelBendGrass, groupSize, 1, 1);  

        if (!viewNoise)
        {
            Graphics.DrawMeshInstancedIndirect(grassBladeMesh, 0, grassMaterial, bounds, argsBuffer);
        }      
    }

    private void OnDestroy()
    {
        grassBladesBuffer.Release();
        argsBuffer.Release();
    }
}
