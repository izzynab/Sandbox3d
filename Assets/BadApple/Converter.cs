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

    RenderTexture result;
    RenderTexture videoRenderTex;
    VideoPlayer videoPlayer;

    int kernelSobel;
    int kernelThreshold;
    int kernelGreyscale;


    public RenderTexture GetResultTexture()
    {
        return result;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (Screen.width != clip.width || Screen.height != clip.height)
        {
            //Debug.Log("Sizes of clip and screen do not match, plese change the resolution of the screen to: " +clip.width + " " + clip.height);
            Screen.SetResolution((int)clip.width, (int)clip.height, false);           
        }

        //FindObjectOfType<Camera>().transform.position = new Vector3(clip.width / 2, clip.height / 2, -650);

        result = new RenderTexture((int)clip.width, (int)clip.height, 0);
        result.enableRandomWrite = true;
        result.Create();

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


        UpdateBuffers();
    }


    void UpdateBuffers()
    {
        convertShader.SetTexture(kernelSobel, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelSobel, "Result", result);

        convertShader.SetTexture(kernelThreshold, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelThreshold, "Result", result);

        convertShader.SetTexture(kernelGreyscale, "ClipTexture", videoRenderTex);
        convertShader.SetTexture(kernelGreyscale, "Result", result);


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

        RenderTexture.active = result;
        resultTexture.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0);
        resultTexture.Apply();

        materialBlitResult.mainTexture = resultTexture;
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }


}
