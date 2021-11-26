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

    public float mainParticleSize = 1;
    public float otherParticleSize = 0.2f;

    private ComputeBuffer particleBuffer;

    private Converter converter;
    
    private int generateKernelID;

    /// Number of particle per warp.
    private const int WARP_SIZE = 256;
    /// Number of warp needed.
    private int mWarpCount;

    void Start()
    {
        converter = GetComponent<Converter>();

        generateKernelID = particleShader.FindKernel("CSGenerate");

        if (mesh == null) particleType = ParticleType.Point;

        //particleCount = resultTexture.width * resultTexture.height;
    }

    void Update()
    {
        converter.UpdateBuffers();
        UpdateShaders();


        if (particleType == ParticleType.Mesh)
        {
            Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), particleCount);
        }
    }

    void UpdateShaders()
    {     
        if (particleBuffer != null) particleBuffer.Release(); 

        particleBuffer = new ComputeBuffer(particleCount, sizeof(float) * 11);

        particleShader.SetFloat("deltaTime", Time.deltaTime);
        particleShader.SetFloat("speed", speed);
        particleShader.SetFloat("lifetime", lifetime);
        particleShader.SetFloat("mainParticleSize", mainParticleSize);
        particleShader.SetFloat("otherParticleSize", otherParticleSize);

        particleShader.SetInt("NumberOfActivePixels", (int)converter.GetNumberOfActivePixels());
      
        particleShader.SetBuffer(generateKernelID, "ActivePositions", converter.GetActivePositionsBuffer());
        particleShader.SetBuffer(generateKernelID, "ParticleBuffer", particleBuffer);

        
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
        if (particleBuffer != null)
            particleBuffer.Release();
        particleBuffer = null;
    }
}
