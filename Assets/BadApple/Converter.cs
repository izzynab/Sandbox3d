using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEditor;

[RequireComponent(typeof(VideoPlayer))]
public class Converter : MonoBehaviour
{ 
    public enum VideoConvertType {Threshold,SobelEdgeDetection,Greyscale};

    [Header("Conversion")]
    public VideoConvertType videoConvertType;

    public VideoClip clip;
    //public float resolutionScale = 4.0f;

    public ComputeShader convertShader;

    public Material materialBlitResult;
    private Texture2D resultTexture;

    public float resolutionOfVideo = 1;
    public int resolutionOfParticles = 1;
    public float particleThreshold;

    public float randomizeParticleMultipler = 10;
    public bool invert;

    RenderTexture videoRenderTex;
    VideoPlayer videoPlayer;

    int kernelSobel;
    int kernelThreshold;
    int kernelGreyscale;

    private ComputeBuffer activePositionsBuffer;
    private ComputeBuffer numberOfActivePixelsBuffer;

    private uint numberOfActivePixels;

    void Start()
    {
        if (Screen.width != clip.width || Screen.height != clip.height)
        {
            Screen.SetResolution((int)clip.width, (int)clip.height, false);           
        }

        FindObjectOfType<Camera>().transform.position = new Vector3(clip.width / 2, clip.height / 2, -650);

        resultTexture = new Texture2D((int)clip.width, (int)clip.height);

        videoRenderTex = new RenderTexture((int)clip.width, (int)clip.height, 0);

        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.playbackSpeed = 0;
        videoPlayer.targetTexture = videoRenderTex;
        videoPlayer.clip = clip;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        videoPlayer.Play();

        kernelSobel = convertShader.FindKernel("CSSobel");
        kernelThreshold = convertShader.FindKernel("CSThreshold");
        kernelGreyscale = convertShader.FindKernel("CSGreyscale");

        numberOfActivePixelsBuffer = new ComputeBuffer(1, sizeof(uint));
        uint[] data = { 0 };
        numberOfActivePixelsBuffer.SetData(data);
        activePositionsBuffer = new ComputeBuffer((videoRenderTex.width * videoRenderTex.height), sizeof(int) * 2);

        //UpdateBuffers();
    }


    void UpdateBuffers()
    {
        convertShader.SetFloat("ParticleThreshold", particleThreshold);
        convertShader.SetFloat("Resolution", resolutionOfVideo);
        convertShader.SetFloat("randomizeParticleMultipler", randomizeParticleMultipler);
        convertShader.SetInt("resolutionOfParticles", resolutionOfParticles);
        convertShader.SetBool("Invert", invert);

        convertShader.SetTexture(kernelSobel, "ClipTexture", videoRenderTex);
        convertShader.SetBuffer(kernelSobel, "ActivePositions", activePositionsBuffer);
        convertShader.SetBuffer(kernelSobel, "NumberOfActivePixels", numberOfActivePixelsBuffer);

        convertShader.SetTexture(kernelThreshold, "ClipTexture", videoRenderTex);
        convertShader.SetBuffer(kernelThreshold, "ActivePositions", activePositionsBuffer);
        convertShader.SetBuffer(kernelThreshold, "NumberOfActivePixels", numberOfActivePixelsBuffer);

        convertShader.SetTexture(kernelGreyscale, "ClipTexture", videoRenderTex);
        convertShader.SetBuffer(kernelGreyscale, "ActivePositions", activePositionsBuffer);
        convertShader.SetBuffer(kernelGreyscale, "NumberOfActivePixels", numberOfActivePixelsBuffer);


        switch (videoConvertType)
        {
            case VideoConvertType.Threshold:
                convertShader.Dispatch(kernelThreshold, (videoRenderTex.width ), (videoRenderTex.height), 1);
                break;
            case VideoConvertType.SobelEdgeDetection:
                convertShader.Dispatch(kernelSobel, (videoRenderTex.width), (videoRenderTex.height ), 1);
                break;
            case VideoConvertType.Greyscale:
                convertShader.Dispatch(kernelGreyscale, (videoRenderTex.width), (videoRenderTex.height ), 1);
                break;
        }


        uint[] data = { 0 };
        numberOfActivePixelsBuffer.GetData(data);
        numberOfActivePixels = data[0];


        uint[] clear = { 0 };
        numberOfActivePixelsBuffer.SetData(clear);

        //Debug.Log("numberOfActivePixels after converter dispatch " + numberOfActivePixels);

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
            if (videoPlayer.playbackSpeed == 1) videoPlayer.playbackSpeed = 0;
            else videoPlayer.playbackSpeed = 1;
        }

        UpdateBuffers();

        RenderTexture.active = videoRenderTex;
        resultTexture.ReadPixels(new Rect(0, 0, videoRenderTex.width, videoRenderTex.height), 0, 0);
        resultTexture.Apply();

        materialBlitResult.mainTexture = resultTexture;
    }

    public uint GetNumberOfActivePixels()
    {
        return numberOfActivePixels;
    }

    public ComputeBuffer GetActivePositionsBuffer()
    {
        return activePositionsBuffer;
    }

    void OnDisable()
    {
        if (activePositionsBuffer != null)
            activePositionsBuffer.Release();
        activePositionsBuffer = null;

        if (numberOfActivePixelsBuffer != null)
            numberOfActivePixelsBuffer.Release();
        numberOfActivePixelsBuffer = null;

    }
}
