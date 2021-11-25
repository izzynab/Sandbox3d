using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleRenderer : MonoBehaviour
{
    public enum ParticleType { Point,Mesh};

    // struct
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


    public Vector3 direction;
    [Range(0,20)]
    public float speed = 2;
    public float lifetime = 3;
    public int particleCount = 1000000;

    public ComputeShader particleEmitterShader;

    private Vector3 spawnPosition;
    private int particleKernelID;
    ComputeBuffer particleBuffer;

    /// Number of particle per warp.
    private const int WARP_SIZE = 256;
    /// Number of warp needed.
    private int mWarpCount; 

    // Use this for initialization
    void Start()
    {
        InitComputeShader();
    }

    void InitComputeShader()
    {
        mWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);

        spawnPosition = gameObject.transform.position;

        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            // Initial life value
            //particleArray[i].life = Random.value * lifetime + 1;
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, 11 * sizeof(float));
        particleBuffer.SetData(particleArray);

        // find the id of the kernel
        particleKernelID = particleEmitterShader.FindKernel("CSParticle");

        // bind the compute buffer to the shader and the compute shader
        particleEmitterShader.SetBuffer(particleKernelID, "particleBuffer", particleBuffer);
        particleMaterial.SetBuffer("particleBuffer", particleBuffer);
    }

    void OnRenderObject()
    {
        if(particleType == ParticleType.Point)
        {
            particleMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
        }
        
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {
        if (mesh == null) particleType = ParticleType.Point;
        spawnPosition = gameObject.transform.position;

        float[] spawnPositions = { spawnPosition.x, spawnPosition.y, spawnPosition.z };
        float[] directions = { direction.x, direction.y, direction.z };

        // Send datas to the compute shader
        particleEmitterShader.SetFloat("deltaTime", Time.deltaTime);
        particleEmitterShader.SetFloat("speed", speed);
        particleEmitterShader.SetFloat("lifetime", lifetime);
        particleEmitterShader.SetFloats("spawnPosition", spawnPositions);
        particleEmitterShader.SetFloats("direction", directions);

        // Update the Particles
        mWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);
        particleEmitterShader.Dispatch(particleKernelID, mWarpCount, 1, 1);

        if (particleType == ParticleType.Mesh)
        {
            Graphics.DrawMeshInstancedProcedural(mesh, 0, particleMaterial, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), particleCount);
        }
    }
  
}
