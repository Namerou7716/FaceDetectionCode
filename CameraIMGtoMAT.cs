using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.UnityUtils;

public class CameraIMGtoMAT : MonoBehaviour
{
    WebCamTexture webCamTexture;
    WebCamDevice webCamDevice;
    Mat rgbaMat;
    Texture2D texture2D;
    [SerializeField]
    string deviceName;
    [SerializeField]
    int width;
    [SerializeField]
    int height;
    Color32[] colors;
    bool isInitWaiting = false;
    bool hasInitialized = false;
    public bool requestedIsFrontFacing = false;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (hasInitialized && webCamTexture.isPlaying && webCamTexture.didUpdateThisFrame)
        {
            Utils.webCamTextureToMat(webCamTexture, rgbaMat, colors);
            // Imgproc.putText(rgbaMat, "W:" + rgbaMat.width() + "H:" + rgbaMat.height() + "SO:" + Screen.orientation, new Point(5, rgbaMat.rows() - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(255, 255, 255, 255), 2, Imgproc.LINE_AA, false);
            Utils.matToTexture2D(rgbaMat, texture2D, colors);
        }
    }

    private void Initialize()
    {
        if (isInitWaiting)
            return;
        StartCoroutine(_Initialize());
    }
    private IEnumerator _Initialize()
    {
        if (hasInitialized)
        {
            Dispose();
        }
        isInitWaiting = true;
        //Create Camera
        var devices = WebCamTexture.devices;
        if (!string.IsNullOrEmpty(deviceName))
        {
            int requestedDeviceIndex = -1;
            if (Int32.TryParse(deviceName, out requestedDeviceIndex))
            {
                if (requestedDeviceIndex >= 0 && requestedDeviceIndex < devices.Length)
                {
                    webCamDevice = devices[requestedDeviceIndex];
                    webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
                }
            }
            else
            {
                for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                {
                    if (devices[cameraIndex].name == deviceName)
                    {
                        webCamDevice = devices[cameraIndex];
                        webCamTexture = new WebCamTexture(webCamTexture.name, width, height);
                        break;
                    }
                }
                if (webCamTexture == null)
                    Debug.Log("Cannot find camera device");
            }
            if (webCamTexture == null)
            {
                //Checks how many and which cameras are available on the device
                for (int cameraIndex = 0; cameraIndex < devices.Length; cameraIndex++)
                {
                    if (devices[cameraIndex].kind != WebCamKind.ColorAndDepth && devices[cameraIndex].isFrontFacing == requestedIsFrontFacing)
                    {
                        webCamDevice = devices[cameraIndex];
                        webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
                        break;
                    }
                }
            }
            if (webCamTexture == null)
            {
                if (devices.Length > 0)
                {
                    webCamDevice = devices[0];
                    webCamTexture = new WebCamTexture(webCamDevice.name, width, height);
                }
                else
                {
                    Debug.LogError("Camera device does not exit");
                    isInitWaiting = false;
                    yield break;
                }
            }
            webCamTexture.Play();
            while (true)
            {
                if (webCamTexture.didUpdateThisFrame)
                {
                    Debug.Log("name:" + webCamTexture.deviceName + "width:" + webCamTexture.width + "height" + webCamTexture.height + "fps:" + webCamTexture.requestedFPS);
                    Debug.Log("videoRotationAngle:" + webCamTexture.videoRotationAngle + "videoVerticallyMirroed:" + webCamTexture.videoVerticallyMirrored);
                    isInitWaiting = false;
                    hasInitialized = true;

                    OnInited();
                    break;

                }
                else
                {
                    yield return null;
                }
            }
        }
    }
    private void Dispose()
    {
        isInitWaiting = false;
        hasInitialized = false;
        if (webCamTexture != null)
        {
            webCamTexture.Stop();
            WebCamTexture.Destroy(webCamTexture);
            webCamTexture = null;
        }
        if (rgbaMat != null)
        {
            rgbaMat.Dispose();
            rgbaMat = null;
        }
        if (texture2D != null)
        {
            Texture2D.Destroy(texture2D);
            texture2D = null;
        }
    }
    //raise the webcam texture initialized event

    private void OnInited()
    {
        if (colors == null || colors.Length != webCamTexture.width * webCamTexture.height)
            colors = new Color32[webCamTexture.width * webCamTexture.height];
        if (texture2D == null || texture2D.width != webCamTexture.width || texture2D.height != webCamTexture.height)
            texture2D = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        rgbaMat = new Mat(webCamTexture.height, webCamTexture.width, CvType.CV_8UC4, new Scalar(0, 0, 0, 255));
        Utils.matToTexture2D(rgbaMat, texture2D, colors);

        gameObject.GetComponent<Renderer>().material.mainTexture = texture2D;
        gameObject.transform.localScale = new Vector3(webCamTexture.width, webCamTexture.height, 1);
        Debug.Log("Height:" + webCamTexture.height + "Width" + webCamTexture.width + "Screen Orientation" + Screen.orientation);
        float width = rgbaMat.width();
        float height = rgbaMat.height();
        float widthScale = (float)Screen.width / width;
        float heightScale = (float)Screen.height / height;
        if (widthScale < heightScale)
        {
            Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
        }
        else
        {
            Camera.main.orthographicSize = height / 2;
        }
    }
    void OnDestroy()
    {
        Dispose();
    }
    public void OnChangeCameraButton()
    {
        if (hasInitialized)
        {
            deviceName = null;
            requestedIsFrontFacing = !requestedIsFrontFacing;
            Initialize();
        }
    }
}
