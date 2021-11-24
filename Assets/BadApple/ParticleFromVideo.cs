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
        public Vector3 color;
        public float size;
    }

    public ParticleType particleType;
    public Mesh mesh;
    public Material particleMaterial;
    public ComputeShader particleShader;


    //public Vector3 direction;
    [Range(0, 20)]
    public float speed = 2;
    public float lifetime = 1;
    public int particleCount = 1000000;

    public float resolutionOfVideo = 1;
    public int resolutionOfParticles = 1;

    public bool invert;
    public float particleThreshold;

    public float mainParticleSize = 1;
    public float otherParticleSize = 0.2f;

    public float randomizeParticleMultipler = 10;

    private ComputeBuffer activePositionsBuffer;
    private ComputeBuffer numberOfActivePixelsBuffer;
    private ComputeBuffer counter;
    private ComputeBuffer particleBuffer;

    private Converter converter;
    private RenderTexture resultTexture;

    private int activePixelsKernelID;
    private int activatePixelsKernelIDInvert;
    private int generateKernelID;

    /// Number of particle per warp.
    private const int WARP_SIZE = 64;
    /// Number of warp needed.
    private int mWarpCount;

    void Start()
    {
        converter = GetComponent<Converter>();
        resultTexture = converter.GetResultTexture();

        activePixelsKernelID = particleShader.FindKernel("CSActivePixels");
        activatePixelsKernelIDInvert = particleShader.FindKernel("CSActivePixelsInvert");
        generateKernelID = particleShader.FindKernel("CSGenerate");

        if (mesh == null) particleType = ParticleType.Point;

        particleCount = resultTexture.width * resultTexture.height;

        FindObjectOfType<Camera>().transform.position = new Vector3(resultTexture.width / 2, resultTexture.height / 2, -650);
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
        if (counter != null) counter.Release();

        resultTexture = converter.GetResultTexture();

        activePositionsBuffer = new ComputeBuffer((resultTexture.width * resultTexture.height), sizeof(int) * 2);//this should be continous values of coords
        numberOfActivePixelsBuffer = new ComputeBuffer(1, sizeof(uint));
        counter = new ComputeBuffer(1, sizeof(int));
        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 11);

        int[] datatoset = { 0 };
        numberOfActivePixelsBuffer.SetData(datatoset);
        //counter.SetData(datatoset);

        particleShader.SetFloat("ParticleThreshold", particleThreshold);
        particleShader.SetFloat("Resolution", resolutionOfVideo);
        particleShader.SetFloat("ResultTextureWidth", resultTexture.width);
        particleShader.SetInt("resolutionOfParticles", resolutionOfParticles); 
        particleShader.SetFloat("randomizeParticleMultipler", randomizeParticleMultipler);

        particleShader.SetBuffer(activePixelsKernelID, "ActivePositions", activePositionsBuffer);
        particleShader.SetBuffer(activePixelsKernelID, "NumberOfActivePixels", numberOfActivePixelsBuffer);
        particleShader.SetTexture(activePixelsKernelID, "ResultTexture", resultTexture);

        particleShader.SetBuffer(activatePixelsKernelIDInvert, "ActivePositions", activePositionsBuffer);
        particleShader.SetBuffer(activatePixelsKernelIDInvert, "NumberOfActivePixels", numberOfActivePixelsBuffer);
        particleShader.SetTexture(activatePixelsKernelIDInvert, "ResultTexture", resultTexture);

        if (invert) particleShader.Dispatch(activatePixelsKernelIDInvert, Mathf.CeilToInt(resultTexture.width/resolutionOfParticles), Mathf.CeilToInt(resultTexture.height/ resolutionOfParticles), 1);
        else particleShader.Dispatch(activePixelsKernelID, Mathf.CeilToInt(resultTexture.width / resolutionOfParticles), Mathf.CeilToInt(resultTexture.height / resolutionOfParticles), 1);


        uint[] numberOfActivePixels = { 0 };
        numberOfActivePixelsBuffer.GetData(numberOfActivePixels);      

        //particleShader.SetInt("CountOfActivePixels", numberOfActivePixels[0]);
        particleShader.SetBuffer(generateKernelID, "NumberOfActivePixels", numberOfActivePixelsBuffer);
        particleShader.SetFloat("deltaTime", Time.deltaTime);
        particleShader.SetFloat("speed", speed);
        particleShader.SetFloat("lifetime", lifetime);
        particleShader.SetFloat("mainParticleSize", mainParticleSize);
        particleShader.SetFloat("otherParticleSize", otherParticleSize);

        particleShader.SetBuffer(generateKernelID, "ParticleBuffer", particleBuffer);
        particleShader.SetBuffer(generateKernelID, "ActivePositions", activePositionsBuffer);
        particleShader.SetBuffer(generateKernelID, "counter", counter);

        mWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);
        particleShader.Dispatch(generateKernelID, mWarpCount, 1, 1);

        particleMaterial.SetBuffer("particleBuffer", particleBuffer);

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

        if (counter != null)
            counter.Release();
        counter = null;
        
    }
}