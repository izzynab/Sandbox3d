using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;

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

    public ComputeShader convertShader;
    public ComputeShader particleShader;

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
        if (Screen.width != clip.width || Screen.height != clip.height)
        {
            Debug.Log("Sizes of clip and screen do not match, plese change the resolution of the screen to: " +clip.width + " " + clip.height);
            Screen.SetResolution((int)clip.width, (int)clip.height, false);
            
        }

        FindObjectOfType<Camera>().transform.position = new Vector3(clip.width / 2, clip.height / 2, -650);

        result = new RenderTexture(Screen.width, Screen.height, 0);
        result.enableRandomWrite = true;
        result.Create();

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
        int kernelGenerate = particleShader.FindKernel("CSGenerate");

        int kernelSobel = convertShader.FindKernel("CSSobel");
        int kernelThreshold = convertShader.FindKernel("CSThreshold");
        int kernelGreyscale = convertShader.FindKernel("CSGreyscale");

        //instanceCountBuffer = new ComputeBuffer(1, sizeof(int));
        int[] analysisResult = new int[1];

        if (positionBuffer != null)
            positionBuffer.Release();


        positionBuffer = new ComputeBuffer((int)(videoRenderTex.width * videoRenderTex.height), sizeof(float) * 4);

        //convertShader.SetFloat("ResolutionScale", resolutionScale);
        //convertShader.SetBool("Invert", invert);
        convertShader.SetFloat("ColorThreshold", colorThreshold);
        //convertShader.SetFloat("ClipWidth", videoRenderTex.width);
        //convertShader.SetFloat("ClipHeight", videoRenderTex.height);

        convertShader.SetTexture(kernelGenerate, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelGenerate, "Result", result);
        convertShader.SetBuffer(kernelGenerate, "Positions", positionBuffer);

        convertShader.SetTexture(kernelSobel, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelSobel, "Result", result);

        convertShader.SetTexture(kernelThreshold, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelThreshold, "Result", result);

        convertShader.SetTexture(kernelGreyscale, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelGreyscale, "Result", result);

        particleShader.SetBool("Invert", invert);
        particleShader.SetFloat("ResolutionScale", resolutionScale);
        particleShader.SetFloat("ColorThreshold", colorThreshold);
        particleShader.SetFloat("ClipWidth", videoRenderTex.width);
        particleShader.SetFloat("ClipHeight", videoRenderTex.height);

        particleShader.SetTexture(kernelGenerate, "ResultTexture", result);
        particleShader.SetBuffer(kernelGenerate, "Positions", positionBuffer);

        //convertShader.Dispatch(kernelInit, 1, 1, 1);

        switch (videoConvertType)
        {
            case VideoConvertType.Threshold:
                convertShader.Dispatch(kernelThreshold, (int)(videoRenderTex.width ), (int)(videoRenderTex.height), 1);
                particleShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
                break;
            case VideoConvertType.SobelEdgeDetection:
                convertShader.Dispatch(kernelSobel, (int)(videoRenderTex.width), (int)(videoRenderTex.height ), 1);
                particleShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
                break;
            case VideoConvertType.Greyscale:
                convertShader.Dispatch(kernelGreyscale, (int)(videoRenderTex.width), (int)(videoRenderTex.height ), 1);
                particleShader.Dispatch(kernelGenerate, (int)(videoRenderTex.width / (8.0f)), (int)(videoRenderTex.height / (8.0f)), 1);
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
        //Graphics.DrawMeshInstancedIndirect(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), videoRenderTex.width * videoRenderTex.height);
    }


    //private void OnRenderImage(RenderTexture source, RenderTexture destination)
   // {
        //Graphics.Blit(result, destination);
    //}

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
