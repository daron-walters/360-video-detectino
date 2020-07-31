using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Rendering;

using UnityEngine.Experimental.Rendering;

using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

using AForge;
using AForge.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;


public class ExtractFramesVideoPlayer : MonoBehaviour
{

    public VideoPlayer videoPlayer;
    private bool isCubeMapGeneated = false;

    //this is how much the image should be close the 2:1 proportion, or 0.5-proportionThreshold.
    float proportionThreshold = 0.01f;


    //in case of the image is not a panorama, this variable determines the maximum size it could be projected
    float maxFill = 0.65f;

    //what is the amount of the view the pamorama should view:
    int scaleX = 1;
    int scaleY = 1;



    void Start()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.Stop();
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.prepareCompleted += Prepared;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.frameReady += FrameReady;
        videoPlayer.Prepare();
    }

    void Prepared(VideoPlayer vp) => vp.Pause();

    void FrameReady(VideoPlayer vp, long frameIndex)
    {
        Debug.Log("FrameReady " + frameIndex + " at time " + vp.time);
        var textureToCopy = vp.texture;
        // Perform texture copy here ...
        AsyncGPUReadback.Request(textureToCopy, 0, TextureFormat.RGBA32, OnCompleteReadback);
        vp.frame = frameIndex + 30;
    }

    void OnCompleteReadback(AsyncGPUReadbackRequest request)
    {
        if (request.hasError)
        {
            Debug.Log("GPU readback error detected.");
            return;
        }
        var currentFrameTexture = new Texture2D((int)videoPlayer.clip.width, (int)videoPlayer.clip.height, TextureFormat.RGBA32, false);
        Debug.Log("width is " + videoPlayer.clip.width + "height is " + videoPlayer.clip.height);
        currentFrameTexture.LoadRawTextureData(request.GetData<Unity.Color>());
        currentFrameTexture.Apply();


        if (!isCubeMapGeneated)
        {
            PanoramaToCubemap converter = new PanoramaToCubemap();
            converter.ConvertPanoramaToCubemap(currentFrameTexture);
            File.WriteAllBytes("Assets/output_images/daron-test-" + Time.time.ToString("f6") + ".png", ImageConversion.EncodeArrayToPNG(currentFrameTexture.GetRawTextureData(),
     GraphicsFormat.R8G8B8A8_UNorm, videoPlayer.width, videoPlayer.height, 0));

            isCubeMapGeneated = true;
        }
        Destroy(currentFrameTexture);

    }

    /*


        //where the panorama should be positioned in the view:
        posX = 0;


        if(isFullPanorama()){
            Log("Full Panorama");
            //the variables are already set for that
        }else if(isPartialPanorama()){
            Log("Partial Panorama");
            scale = currentHeight/currentWidth * 2f;
            scaleX = 1;
            scaleY = scale;
            posX = 0;
            posY = 0.5-scale/2;
        }else{
            Debug.Log("Not Panorama");
            proportion = currentHeight/currentWidth;
            w = currentWidth > minimumWidth*maxFill ? minimumWidth*maxFill : currentWidth;
            scaleX = w / minimumWidth / 2;
            scaleY = scaleX * proportion * 2;
            if(scaleY>1) {
                h = currentHeight > minimumHeight*maxFill ? minimumHeight*maxFill : currentHeight;
                scaleY = h / minimumHeight / 2;
                scaleX = scaleY * proportion / 2;
            }
            posX = 0.5-scaleX/2;
            posY = 0.5-scaleY/2;
        }
    */
    bool isFullPanorama(Texture2D srcTexture)
    {
        float proportion = srcTexture.height / srcTexture.width;
        return proportion >= 0.5 - proportionThreshold &&
        proportion <= 0.5 + proportionThreshold;
    }

    bool isPartialPanorama(Texture2D srcTexture)
    {
                return srcTexture.width / srcTexture.height <= 0.5;
    }

    int calculateNumberOfLines(Texture2D sourceImage)
    {
        HoughLineTransformation lineTransform = new HoughLineTransformation();
        // apply Hough line transofrm
        Bitmap tmpBitmap = new Bitmap(sourceImage.width, sourceImage.height, 4, PixelFormat.Format32bppArgb, sourceImage.GetRawTextureData());
        lineTransform.ProcessImage(tmpBitmap);
        Bitmap houghLineImage = lineTransform.ToBitmap();
        // get lines using relative intensity
        HoughLine[] lines = lineTransform.GetLinesByRelativeIntensity(0.5);

        return lines.Length;
    }


}