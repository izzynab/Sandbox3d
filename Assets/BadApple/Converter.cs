using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Converter : MonoBehaviour
{ 
    public Mesh mesh;
    public Material material;
    public VideoClip clip;
    public float resolutionScale = 4.0f;

    public ComputeShader computeShader;

    private int currentInstanceCount = 1;
    private ComputeBuffer instanceCountBuffer;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };


    RenderTexture result;
    RenderTexture videoRenderTex;
    VideoPlayer videoPlayer;




    // Start is called before the first frame update
    void Start()
    {
        result = new RenderTexture(Screen.width, Screen.height, 0);
        result.enableRandomWrite = true;
        result.Create();

        if (Screen.width != clip.width || Screen.height != clip.height)
        {
            Screen.SetResolution((int)clip.width, (int)clip.height,false);
        }
        videoRenderTex = new RenderTexture((int)Screen.width, (int)Screen.height, 0);

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playbackSpeed = 0;
        videoPlayer.targetTexture = videoRenderTex;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        videoPlayer.Play();

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);


    }


    void UpdateBuffers()
    {
       
        int kernelMain = computeShader.FindKernel("CSMain");
        int kernelInit = computeShader.FindKernel("CSInit");
        int kernelGenerate = computeShader.FindKernel("CSGenerate");

        instanceCountBuffer = new ComputeBuffer(1, sizeof(int));
        int[] analysisResult = new int[1];

        computeShader.SetTexture(kernelMain, "ClipTexture", videoRenderTex);
        computeShader.SetTexture(kernelMain, "Result", result);
        computeShader.SetFloat("ResolutionScale", resolutionScale);

        computeShader.SetBuffer(kernelMain, "ResultBuffer", instanceCountBuffer);
        computeShader.SetBuffer(kernelInit, "ResultBuffer", instanceCountBuffer);

        computeShader.Dispatch(kernelInit, 1, 1, 1);
        computeShader.Dispatch(kernelInit, (int)(videoRenderTex.width / (8.0f* resolutionScale)), (int)(videoRenderTex.height / (8.0f * resolutionScale)), 1);

        //Debug.Log((int)(videoRenderTex.width / (8.0f * resolutionScale)));

        instanceCountBuffer.GetData(analysisResult);

        instanceCountBuffer.Release();
        instanceCountBuffer = null;

        currentInstanceCount = analysisResult[0];


        if (positionBuffer != null)
            positionBuffer.Release();
        if (currentInstanceCount == 0) return;
        positionBuffer = new ComputeBuffer((int)currentInstanceCount, sizeof(float) * 4);

        computeShader.SetBuffer(kernelGenerate, "Positions", positionBuffer);
        //computeShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f * resolutionScale)), (int)(videoRenderTex.height / (8.0f * resolutionScale)), 1);

        material.SetBuffer("positionBuffer", positionBuffer);


        if (mesh != null)
        {
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)currentInstanceCount;
            args[2] = mesh.GetIndexStart(0);
            args[3] = mesh.GetBaseVertex(0);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);


    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.RightArrow))
        {
            videoPlayer.frame++;
        }

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            videoPlayer.frame--;
        }

        UpdateBuffers();

        // Render
        //Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);

    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 40;
        GUI.Label(new Rect(10, 70, 100, 20), "count: " + (currentInstanceCount).ToString(), style);
        style.normal.textColor = Color.green;
        GUI.Label(new Rect(10, 10, 100, 20), "percent: " + ((float) currentInstanceCount / (clip.width * clip.height)).ToString(), style);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }

    void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;

        if (instanceCountBuffer != null)
            instanceCountBuffer.Release();
        instanceCountBuffer = null;
        
    }

}
