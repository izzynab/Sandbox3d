using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Converter))]
public class ParticleFromVideo : MonoBehaviour
{
    public enum ParticleType { Point, Mesh };

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    public ParticleType particleType;
    public Mesh mesh;
    public Material particleMaterial;
    public ComputeShader particleShader;


    public Vector3 direction;
    [Range(0, 20)]
    public float speed = 2;
    public float lifetime = 1;
    public int particleCount = 1000000;

    public bool invert;
    public float particleThreshold;


    private ComputeBuffer activePositionsBuffer;
    private ComputeBuffer numberOfActivePixelsBuffer;
    private ComputeBuffer particleBuffer;

    private Converter converter;
    private RenderTexture resultTexture;

    private int activePixelsKernelID;
    private int generateKernelID;

    /// Number of particle per warp.
    private const int WARP_SIZE = 256;
    /// Number of warp needed.
    private int mWarpCount;

    void Start()
    {
        converter = GetComponent<Converter>();
        resultTexture = converter.GetResultTexture();

        activePixelsKernelID = particleShader.FindKernel("CSActivePixels");
        generateKernelID = particleShader.FindKernel("CSGenerate");

        if (mesh == null) particleType = ParticleType.Point;

        particleMaterial.SetBuffer("particleBuffer", particleBuffer);



    }

    void Update()
    {
        resultTexture = converter.GetResultTexture();
        if (resultTexture == null)
        {
            Debug.Log("ResultTexture wasnt found");
            return;
        }
        //Debug.Log(resultTexture.width + resultTexture.height);
        UpdateShaders();

        if (particleType == ParticleType.Mesh)
        {
            Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), particleCount);
        }
    }

    void UpdateShaders()
    {     
        if(activePositionsBuffer!=null) activePositionsBuffer.Release();
        if (particleBuffer != null) particleBuffer.Release();
        if (numberOfActivePixelsBuffer != null) numberOfActivePixelsBuffer.Release();

        resultTexture = converter.GetResultTexture();

        activePositionsBuffer = new ComputeBuffer((resultTexture.width * resultTexture.height), sizeof(int) * 2);//make this rather a render texture
        numberOfActivePixelsBuffer = new ComputeBuffer(1, sizeof(int));
        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 7);

        int[] datatoset = { 0 };
        numberOfActivePixelsBuffer.SetData(datatoset);


        particleShader.SetBool("Invert", invert);
        particleShader.SetFloat("ParticleThreshold", particleThreshold);
        particleShader.SetFloat("ResultTextureWidth", resultTexture.width);
        particleShader.SetFloat("NumberOfPixelsActive", resultTexture.width * resultTexture.height);

        particleShader.SetBuffer(activePixelsKernelID, "ActivePositions", activePositionsBuffer);
        particleShader.SetBuffer(activePixelsKernelID, "NumberOfActivePixels", numberOfActivePixelsBuffer);
        particleShader.SetTexture(activePixelsKernelID, "ResultTexture", resultTexture);

        particleShader.Dispatch(activePixelsKernelID, resultTexture.width, resultTexture.height, 1);


        int[] numberOfActivePixels = { 0 };
        numberOfActivePixelsBuffer.GetData(numberOfActivePixels);      
        if (numberOfActivePixels[0] > 1) Debug.Log("numberOfActivePixels " + numberOfActivePixels[0]);

        Vector2Int[] data = new Vector2Int[1];
        activePositionsBuffer.GetData(data);//this data have size of 1 milion
        if (data.Length > 1) Debug.Log("activePositionsBuffer " + data.Length);

        //foreach (var d in data)
        {
            //Debug.Log(d.ToString());
        }


        /*particleShader.SetInt("CountOfActivePixels", numberOfActivePixels[0]);
        particleShader.SetFloat("deltaTime", Time.deltaTime);
        particleShader.SetFloat("speed", speed);
        particleShader.SetFloat("lifetime", lifetime);

        particleShader.SetBuffer(generateKernelID, "ParticleBuffer", particleBuffer);
        particleShader.SetBuffer(generateKernelID, "ActivePositions", activePositionsBuffer);

        mWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);
        particleShader.Dispatch(generateKernelID, mWarpCount, 1, 1);*/
    }

    void OnRenderObject()
    {
        if (particleType == ParticleType.Point)
        {
            particleMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
        }

    }


    void OnDisable()
    {
        if (activePositionsBuffer != null)
            activePositionsBuffer.Release();
        activePositionsBuffer = null;

        if (numberOfActivePixelsBuffer != null)
            numberOfActivePixelsBuffer.Release();
        numberOfActivePixelsBuffer = null;

        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = null;
    }
}
