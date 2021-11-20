using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class Converter : MonoBehaviour
{ 
    public enum VideoConvertType {Threshold,SobelEdgeDetection,Greyscale};

    [Header("Conversion")]
    public VideoConvertType videoConvertType;
    public float colorThreshold = 0.9f;
    public bool invert = false;

    [Header("Other")]
    public Mesh mesh;
    public Material material;
    public VideoClip clip;
    public float resolutionScale = 4.0f;

    public ComputeShader computeShader;

    //private int currentInstanceCount = 1;
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

        UpdateBuffers();
    }


    void UpdateBuffers()
    {
        int kernelGenerate = computeShader.FindKernel("CSGenerate");
        int kernelSobel = computeShader.FindKernel("CSSobel");
        int kernelThreshold = computeShader.FindKernel("CSThreshold");
        int kernelGreyscale = computeShader.FindKernel("CSGreyscale");

        //instanceCountBuffer = new ComputeBuffer(1, sizeof(int));
        int[] analysisResult = new int[1];

        if (positionBuffer != null)
            positionBuffer.Release();


        positionBuffer = new ComputeBuffer((int)(videoRenderTex.width * videoRenderTex.height), sizeof(float) * 4);

        computeShader.SetFloat("ResolutionScale", resolutionScale);
        computeShader.SetBool("Invert", invert);
        computeShader.SetFloat("ColorThreshold", colorThreshold);
        computeShader.SetFloat("ClipWidth", videoRenderTex.width);
        computeShader.SetFloat("ClipHeight", videoRenderTex.height);

        computeShader.SetTexture(kernelGenerate, "ClipTexture", videoRenderTex);
        computeShader.SetTexture(kernelGenerate, "Result", result);
        computeShader.SetBuffer(kernelGenerate, "Positions", positionBuffer);

        computeShader.SetTexture(kernelSobel, "ClipTexture", videoRenderTex);
        computeShader.SetTexture(kernelSobel, "Result", result);

        computeShader.SetTexture(kernelThreshold, "ClipTexture", videoRenderTex);
        computeShader.SetTexture(kernelThreshold, "Result", result);

        computeShader.SetTexture(kernelGreyscale, "ClipTexture", videoRenderTex);
        computeShader.SetTexture(kernelGreyscale, "Result", result);

        //computeShader.Dispatch(kernelInit, 1, 1, 1);

        switch (videoConvertType)
        {
            case VideoConvertType.Threshold:
                computeShader.Dispatch(kernelThreshold, (int)(videoRenderTex.width ), (int)(videoRenderTex.height), 1);
                computeShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
                break;
            case VideoConvertType.SobelEdgeDetection:
                computeShader.Dispatch(kernelSobel, (int)(videoRenderTex.width), (int)(videoRenderTex.height ), 1);
                computeShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
                break;
            case VideoConvertType.Greyscale:
                computeShader.Dispatch(kernelGreyscale, (int)(videoRenderTex.width), (int)(videoRenderTex.height ), 1);
                computeShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
                break;
        }


        //instanceCountBuffer.GetData(analysisResult);
        //instanceCountBuffer.Release();
        //instanceCountBuffer = null;
        //currentInstanceCount = analysisResult[0];

        material.SetBuffer("positionBuffer", positionBuffer);


        if (mesh != null)
        {
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)(videoRenderTex.width * videoRenderTex.height);
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            videoPlayer.playbackSpeed = 1;
        }

        UpdateBuffers();

        // Render
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);

    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 40;
        //GUI.Label(new Rect(10, 70, 100, 20), "count: " + (currentInstanceCount).ToString(), style);
        style.normal.textColor = Color.green;
        //GUI.Label(new Rect(10, 10, 100, 20), "percent: " + ((float) currentInstanceCount / (clip.width * clip.height)).ToString(), style);
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
