using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Converter : MonoBehaviour
{
    public ComputeShader shader;
    public VideoClip clip;

    public int instanceCount = 100000;

    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public Mesh mesh;
    public Material material;

    RenderTexture result;
    RenderTexture videoRenderTex;
    VideoPlayer videoPlayer;


    // Start is called before the first frame update
    void Start()
    {
        /*result = new RenderTexture(Screen.width, Screen.height, 0);
        result.enableRandomWrite = true;
        result.Create();

        shader.SetTexture(0, "Result", result);

        if (Screen.width != clip.width || Screen.height != clip.height)
        {
            Screen.SetResolution((int)clip.width, (int)clip.height,false);
        }
        videoRenderTex = new RenderTexture((int)Screen.width, (int)Screen.height, 0);

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.targetTexture = videoRenderTex;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        videoPlayer.Play();*/

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateBuffers();


    }

    void UpdateBuffers()
    {
        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 4);

        shader.SetBuffer(0, "_Positions", positionBuffer);
        
        //make this in compute shader instead c#
        /*Vector4[] positions = new Vector4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Random.Range(20.0f, 100.0f);
            float height = Random.Range(-2.0f, 2.0f);
            float size = Random.Range(1f, 1.25f);
            positions[i] = new Vector4(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance, size);
        }
        positionBuffer.SetData(positions);*/

        material.SetBuffer("positionBuffer", positionBuffer);

        // Indirect args
        if (mesh != null)
        {
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)instanceCount;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        //cachedInstanceCount = instanceCount;
        //cachedSubMeshIndex = 0;
    }

    void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }

    private void Update()
    {
        /*shader.SetTexture(0, "_ClipTexture", videoRenderTex);

        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);*/


        // Update starting position buffer
       // if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != 0)
         //   UpdateBuffers();

        // Render
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);

    }

    /*private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }*/
}
