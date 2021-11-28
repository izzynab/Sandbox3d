using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Converter))]
public class ParticleFromVideo : MonoBehaviour
{
    public enum ParticleType { Point, Mesh };
    public enum EmitterShape { Sphere, CircleXY, CircleXZ,Cone };
    private int numberOfShapes = 4;

    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
        public Vector4 color;
        public float size;
    }

    [Header("Basic")]
    public ParticleType particleType;
    public Mesh mesh;
    public Material particleMaterial;
    public ComputeShader particleShader;

    [Header("Spawn")]
    [Range(0, 80)]
    public float speed = 20;
    public float lifetime = 1;
    public int maxParticleCount = 1000000;

    [Header("Shape")]
    public EmitterShape emitterShape = EmitterShape.Sphere;
    [Range(0,15)]
    public float shapeDegrees = 60;
    public float randomizeParticleMultipler = 2;

    [Header("Appearance")]
    public float mainParticleSize = 1;
    public float otherParticleSize = 0.2f;

    public Color mainParticleColor;
    public Gradient otherParticleGradient;

    private ComputeBuffer particleBuffer;
    private ComputeBuffer gradientBuffer;

    private Converter converter;

    private int generateKernelID;

    private const int WARP_SIZE = 256;
    private int mWarpCount;

    void Start()
    {
        converter = GetComponent<Converter>();

        //generateKernelID = particleShader.FindKernel("CSGenerate");
        generateKernelID = (int)emitterShape;

        if (mesh == null) particleType = ParticleType.Point;

        if (particleBuffer != null) particleBuffer.Release();

        particleBuffer = new ComputeBuffer(maxParticleCount, sizeof(float) * 12);
        gradientBuffer = new ComputeBuffer(100, sizeof(float)*4);
        Vector4[] data = new Vector4[100];

        //we are filling gradient buffer in reversed order so in compute shader we can acces it faster
        for (int i = 0,j = 99;i<100;i++,j--)
        {
            data[j] = otherParticleGradient.Evaluate(i*0.01f);
            Debug.Log("j: " + j.ToString() + "i: " + i.ToString());
        }
        gradientBuffer.SetData(data);

        for(int i =0;i< numberOfShapes; i++)
        {
            particleShader.SetBuffer(i, "gradientBuffer", gradientBuffer);
        }

    }

    void Update()
    {
        if(generateKernelID != (int)emitterShape)
        {
           generateKernelID = (int)emitterShape;
        }

        converter.UpdateBuffers();
        UpdateShaders();


        if (particleType == ParticleType.Mesh)
        {
            //int count = (int)converter.GetNumberOfActivePixels() * additionalParticlesPerMain;
            int count = maxParticleCount;
            if (count <= 10) return;
            Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)),count);
        }
    }

    void UpdateShaders()
    {     

        particleShader.SetFloat("deltaTime", Time.deltaTime);
        particleShader.SetFloat("speed", speed);
        particleShader.SetFloat("lifetime", lifetime);
        particleShader.SetFloat("mainParticleSize", mainParticleSize);
        particleShader.SetFloat("otherParticleSize", otherParticleSize);
        particleShader.SetFloat("shapeDegrees", shapeDegrees);
        particleShader.SetFloat("randomizeParticleMultipler", randomizeParticleMultipler);
        particleShader.SetInt("NumberOfActivePixels", (int)converter.GetNumberOfActivePixels());

        float[] color = { mainParticleColor.r, mainParticleColor.g, mainParticleColor.b };
        particleShader.SetFloats("mainParticleColor", color);

        particleShader.SetBuffer(generateKernelID, "ActivePositions", converter.GetActivePositionsBuffer());
        particleShader.SetBuffer(generateKernelID, "ParticleBuffer", particleBuffer);

        
        mWarpCount = Mathf.CeilToInt((float)maxParticleCount / WARP_SIZE);
        particleShader.Dispatch(generateKernelID, mWarpCount, 1, 1);
        

        particleMaterial.SetBuffer("particleBuffer", particleBuffer);

    }

    void OnRenderObject()
    {
        if (particleType == ParticleType.Point)
        {
            particleMaterial.SetPass(0);
            int count = maxParticleCount;
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, count);
        }

    }


    void OnDisable()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = null;

        if (gradientBuffer != null)
            gradientBuffer.Release();
        gradientBuffer = null;
        
    }
}
