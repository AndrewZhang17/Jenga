using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using TensorFlowLite;
using Cysharp.Threading.Tasks;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class HandTrackingSample : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string palmModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField, FilePopup("*.tflite")] string landmarkModelFile = "coco_ssd_mobilenet_quant.tflite";

    [SerializeField] RawImage cameraView = null;
    [SerializeField] RawImage debugPalmView = null;
    [SerializeField] bool runBackground;

    [SerializeField] ARCameraManager cameraManager;

    // WebCamTexture webcamTexture;
    PalmDetect palmDetect;
    HandLandmarkDetect landmarkDetect;

    Texture2D m_Texture;

    // just cache for GetWorldCorners
    Vector3[] rtCorners = new Vector3[4];
    Vector3[] worldJoints = new Vector3[HandLandmarkDetect.JOINT_COUNT];
    PrimitiveDraw draw;
    List<PalmDetect.Result> palmResults;
    HandLandmarkDetect.Result landmarkResult;
    UniTask<bool> task;
    CancellationToken cancellationToken;

    [SerializeField] Text text;


    void Start()
    {
        try
        {
            string palmPath = Path.Combine(Application.streamingAssetsPath, palmModelFile);
            palmDetect = new PalmDetect(palmPath);

            string landmarkPath = Path.Combine(Application.streamingAssetsPath, landmarkModelFile);
            landmarkDetect = new HandLandmarkDetect(landmarkPath);
            Debug.Log($"landmark dimension: {landmarkDetect.Dim}");

            string cameraName = WebCamUtil.FindName(new WebCamUtil.PreferSpec()
            {
                isFrontFacing = false,
                kind = WebCamKind.WideAngle,
            });
            // webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
            // cameraView.texture = webcamTexture;
            // webcamTexture.Play();
            Debug.Log($"Starting camera: {cameraName}");
        }
        catch (System.Exception e)
        {
            text.text = e.ToString();
        }
        

        draw = new PrimitiveDraw();
    }

    void OnDestroy()
    {
        // webcamTexture?.Stop();
        palmDetect?.Dispose();
        landmarkDetect?.Dispose();
    }

    void Update()
    {
        if (runBackground)
        {
            if (task.Status.IsCompleted())
            {
                task = InvokeAsync();
            }
        }
        else
        {
            Invoke();
        }

        if (palmResults == null || palmResults.Count <= 0) return;
        DrawFrames(palmResults);

        if (landmarkResult == null || landmarkResult.score < 0.2f) return;
        DrawCropMatrix(landmarkDetect.CropMatrix);
        DrawJoints(landmarkResult.joints);
        DetectGestures();
        cameraView.texture = m_Texture;
    }

    void Invoke()
    {
        palmDetect.Invoke(m_Texture);
        // palmDetect.Invoke(webcamTexture);
        cameraView.material = palmDetect.transformMat;
        cameraView.rectTransform.GetWorldCorners(rtCorners);

        palmResults = palmDetect.GetResults(0.7f, 0.3f);


        if (palmResults.Count <= 0) return;

        // Detect only first palm
        landmarkDetect.Invoke(m_Texture, palmResults[0]);
        // landmarkDetect.Invoke(webcamTexture, palmResults[0]);
        debugPalmView.texture = landmarkDetect.inputTex;

        landmarkResult = landmarkDetect.GetResult();
    }

    async UniTask<bool> InvokeAsync()
    {
        palmResults = await palmDetect.InvokeAsync(m_Texture, cancellationToken);
        // palmResults = await palmDetect.InvokeAsync(webcamTexture, cancellationToken);
        cameraView.material = palmDetect.transformMat;
        cameraView.rectTransform.GetWorldCorners(rtCorners);

        if (palmResults.Count <= 0) return false;

        landmarkResult = await landmarkDetect.InvokeAsync(m_Texture, palmResults[0], cancellationToken);
        // landmarkResult = await landmarkDetect.InvokeAsync(webcamTexture, palmResults[0], cancellationToken);
        debugPalmView.texture = landmarkDetect.inputTex;

        return true;
    }

    void DrawFrames(List<PalmDetect.Result> palms)
    {
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        draw.color = Color.green;
        foreach (var palm in palms)
        {
            draw.Rect(MathTF.Lerp(min, max, palm.rect, true), 0.02f, min.z);

            foreach (var kp in palm.keypoints)
            {
                draw.Point(MathTF.Lerp(min, max, (Vector3)kp, true), 0.05f);
            }
        }
        draw.Apply();
    }

    void DrawCropMatrix(in Matrix4x4 matrix)
    {
        draw.color = Color.red;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        var mtx = new Matrix4x4();

        // var mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored)
        //     * matrix.inverse;
        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    void DrawJoints(Vector3[] joints)
    {
        draw.color = Color.blue;

        // Get World Corners
        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        // Need to apply camera rotation and mirror on mobile
        var mtx = new Matrix4x4();

        // Matrix4x4 mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, false, webcamTexture.videoVerticallyMirrored);

        // Get joint locations in the world space
        float zScale = max.x - min.x;
        for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
        {
            Vector3 p0 = mtx.MultiplyPoint3x4(joints[i]);
            Vector3 p1 = MathTF.Lerp(min, max, p0);
            p1.z += (p0.z - 0.5f) * zScale;
            worldJoints[i] = p1;
        }

        // Cube
        for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
        {
            draw.Cube(worldJoints[i], 0.1f);
        }

        // Connection Lines
        var connections = HandLandmarkDetect.CONNECTIONS;
        for (int i = 0; i < connections.Length; i += 2)
        {
            draw.Line3D(
                worldJoints[connections[i]],
                worldJoints[connections[i + 1]],
                0.05f);
        }

        draw.Apply();
    }

    void DetectGestures() {
        // finger states
        bool thumbIsOpen = false;
        bool firstFingerIsOpen = false;
        bool secondFingerIsOpen = false;
        bool thirdFingerIsOpen = false;
        bool fourthFingerIsOpen = false;
        //

        float pseudoFixKeyPoint = worldJoints[2].x;
        if (worldJoints[3].x < pseudoFixKeyPoint && worldJoints[4].x < pseudoFixKeyPoint)
        {
            thumbIsOpen = true;
        }

        pseudoFixKeyPoint = worldJoints[6].y;
        if (worldJoints[7].y > pseudoFixKeyPoint && worldJoints[8].y > pseudoFixKeyPoint)
        {
            firstFingerIsOpen = true;
        }

        pseudoFixKeyPoint = worldJoints[10].y;
        if (worldJoints[11].y > pseudoFixKeyPoint && worldJoints[12].y > pseudoFixKeyPoint)
        {
            secondFingerIsOpen = true;
        }

        pseudoFixKeyPoint = worldJoints[14].y;
        if (worldJoints[15].y > pseudoFixKeyPoint && worldJoints[16].y > pseudoFixKeyPoint)
        {
            thirdFingerIsOpen = true;
        }

        pseudoFixKeyPoint = worldJoints[18].y;
        if (worldJoints[19].y > pseudoFixKeyPoint && worldJoints[20].y > pseudoFixKeyPoint)
        {
            fourthFingerIsOpen = true;
        }

        // // Hand gesture recognition
        // if (thumbIsOpen && firstFingerIsOpen && secondFingerIsOpen && thirdFingerIsOpen && fourthFingerIsOpen)
        // {
        //     text.text =  "FIVE!";
        // }
        // else if (!thumbIsOpen && firstFingerIsOpen && secondFingerIsOpen && thirdFingerIsOpen && fourthFingerIsOpen)
        // {
        //     text.text =  "FOUR!";
        // }
        // else if (!thumbIsOpen && firstFingerIsOpen && secondFingerIsOpen && thirdFingerIsOpen && !fourthFingerIsOpen)
        // {
        //     text.text =  "THREE!";
        // }
        // else if (!thumbIsOpen && firstFingerIsOpen && secondFingerIsOpen && !thirdFingerIsOpen && !fourthFingerIsOpen)
        // {
        //     text.text =  "TWO!";
        // }
        // else if (!thumbIsOpen && firstFingerIsOpen && !secondFingerIsOpen && !thirdFingerIsOpen && !fourthFingerIsOpen)
        // {
        //     text.text =  "ONE!";
        // }
        // // else if (!thumbIsOpen && firstFingerIsOpen && secondFingerIsOpen && !thirdFingerIsOpen && !fourthFingerIsOpen)
        // // {
        // //     text.text =  "YEAH!";
        // // }
        // else if (!thumbIsOpen && firstFingerIsOpen && !secondFingerIsOpen && !thirdFingerIsOpen && fourthFingerIsOpen)
        // {
        //     text.text =  "ROCK!";
        // }
        // else if (thumbIsOpen && firstFingerIsOpen && !secondFingerIsOpen && !thirdFingerIsOpen && fourthFingerIsOpen)
        // {
        //     text.text =  "SPIDERMAN!";
        // }
        // else if (!thumbIsOpen && !firstFingerIsOpen && !secondFingerIsOpen && !thirdFingerIsOpen && !fourthFingerIsOpen)
        // {
        //     text.text =  "FIST!";
        // }
        // else if (!firstFingerIsOpen && secondFingerIsOpen && thirdFingerIsOpen && fourthFingerIsOpen && areNear(worldJoints[4], worldJoints[8]))
        // {
        //     text.text =  "OK!";
        // }
        // else
        // {
        //     text.text =  "Finger States: " + thumbIsOpen + " " + firstFingerIsOpen + " " + secondFingerIsOpen + " " + thirdFingerIsOpen + " " + fourthFingerIsOpen;
        // }

        if (areNear(worldJoints[4], worldJoints[8])) {
            text.text = "GRAB!";
        }
        else {
            text.text = "RELEASE!";
        }
    }

    bool areNear(Vector3 first, Vector3 second) {
        return Vector3.Distance(first, second) < 1;
    }


    void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            // Get the entire image.
            inputRect = new RectInt(0, 0, image.width, image.height),

            // Downsample by 2.
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),

            // Choose RGBA format.
            outputFormat = TextureFormat.RGBA32,

            // Flip across the vertical axis (mirror image).
            transformation = XRCpuImage.Transformation.MirrorY
        };

        // See how many bytes you need to store the final image.
        int size = image.GetConvertedDataSize(conversionParams);

        // Allocate a buffer to store the image.
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // Extract the image data
        image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

        // The image was converted to RGBA32 format and written into the provided buffer
        // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
        image.Dispose();

        // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // In this example, you apply it to a texture to visualize it.

        // You've got the data; let's put it into a texture so you can visualize it.
        m_Texture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);

        m_Texture.LoadRawTextureData(buffer);
        m_Texture.Apply();

        // Done with your temporary data, so you can dispose it.
        buffer.Dispose();
    }

}
