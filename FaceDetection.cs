using System;
using System.Collections;
using System.Collections.Generic;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using UnityEngine;

public class FaceDetection : MonoBehaviour
{
    Mat grayMat;
    Texture2D texture2D;
    CascadeClassifier cascade;
    MatOfRect faces;
    WebCamTextureToMatHelper webCamTextureToMatHelper;
    //cascade検出器（顔検出に使うパーツ）を取得
    protected static readonly string LBP_CASCADE_FILENAME = "lbpcascade_frontalface.xml";

    private void Start()
    {
        webCamTextureToMatHelper = gameObject.GetComponent<WebCamTextureToMatHelper>();
        cascade = new CascadeClassifier();
        cascade.load(Utils.getFilePath(LBP_CASCADE_FILENAME));
        if (cascade.empty())
        {
            Debug.Log("Cascade is not found");
        }
        webCamTextureToMatHelper.Initialize();
    }
    void Update()
    {
        if (webCamTextureToMatHelper.IsPlaying() && webCamTextureToMatHelper.DidUpdateThisFrame())
        {
            Mat rgbaMat = webCamTextureToMatHelper.GetMat();

            Imgproc.cvtColor(rgbaMat, grayMat, Imgproc.COLOR_RGBA2GRAY);
            Imgproc.equalizeHist(grayMat, grayMat);

            if (cascade != null)
                cascade.detectMultiScale(grayMat, faces, 1.1, 2, 2, new Size(grayMat.cols() * 0.2, grayMat.rows() * 0.2), new Size());

            OpenCVForUnity.CoreModule.Rect[] rects = faces.toArray();
            for (int i = 0; i < rects.Length; i++)
            {
                Imgproc.rectangle(rgbaMat, new Point(rects[i].x, rects[i].y), new Point(rects[i].width, rects[i].y + rects[i].height), new Scalar(255, 0, 0, 255), 2);

            }
            Utils.fastMatToTexture2D(rgbaMat, texture2D);
        }
    }
    //called when OnWebCamTextureInitialized (UnityEvent in WebCamTextureToMatHelper)
    public void OnWebCamTextureToMatHelperInitialized()
    {
        Debug.Log("WebCamTextureToMatHelper Initialized");
        //get mat of current frame it is CV_8UC4
        Mat webcamTexturemat = webCamTextureToMatHelper.GetMat();

        texture2D = new Texture2D(webcamTexturemat.cols(), webcamTexturemat.rows(), TextureFormat.RGBA32, false);
        Utils.fastMatToTexture2D(webcamTexturemat, texture2D);

        gameObject.GetComponent<Renderer>().material.mainTexture = texture2D;

        gameObject.transform.localScale = new Vector3(webcamTexturemat.cols(), webcamTexturemat.rows(), 1);
        Debug.Log("Width" + Screen.width + "Height" + Screen.height + "Orientation" + Screen.orientation);
        float width = webcamTexturemat.width();
        float height = webcamTexturemat.height();

        float widthScale = (float)webcamTexturemat.width() / width;
        float heightScale = (float)webcamTexturemat.height() / height;

        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }
        grayMat = new Mat(webcamTexturemat.rows(), webcamTexturemat.cols(), CvType.CV_8UC1);

        faces = new MatOfRect();
    }

    public void OnWebCamTextureHelperDisposed()
    {
        Debug.Log("OnWebCamTextureHelperDisposed");

        if (grayMat != null)
            grayMat.Dispose();
        if (texture2D != null)
        {
            Texture2D.Destroy(texture2D);
            texture2D = null;
        }
        if (faces != null)
            faces.Dispose();
    }

    ///<param name="errorCode">Error code.</param>
    public void OnWebCamTextureToMatHelperErrorOccurred(WebCamTextureToMatHelper.ErrorCode errorCode)
    {
        Debug.Log("OnWebCamTextureToMatHelperErrorOccurred:" + errorCode);
    }

    private void OnDestroy()
    {
        webCamTextureToMatHelper.Dispose();

        if (cascade != null)
            cascade.Dispose();
    }

    public void OnPlay()
    {
        webCamTextureToMatHelper.Play();
    }
    public void OnChangeWebCamButton()
    {
        webCamTextureToMatHelper.requestedIsFrontFacing = !webCamTextureToMatHelper.IsFrontFacing();
    }
}