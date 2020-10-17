using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public struct sample
{
    public float u;
    public float v;
    public float r;
    public float g;
    public float b;
    public float a;
    public int cubemapFace;
}

public class GLEAMComputeCommunicator : MonoBehaviour
{
    public ComputeShader GLEAMCompute;

    public Camera sampleCamera;
    public GameObject probe;
    public GameObject probePrefab;
    public GameObject origin;
    public Vector3 probeOffset;
    public int probeSampleSize;
    public RawImage shaderOutput;
    private ARCameraBackground m_ARCameraBackground; // component that holds the camera image
    private float secondCounter = 0.0f;

    private RenderTexture cubemapRT;
    private RenderTexture cameraImage;
    private RenderTexture outputFrame;
    private RenderTexture sampleTextureRT;
    private RenderTexture activeRT;
    private RenderTexture sumsListRT, weightsListRT;
    private Texture2D sampleTexture;
    private sample[] sampleArray;
    private Rect ScreenRect;

    private Vector2Int size;

    private int RayCastKernelIndex;


    // debugging
    public TextMeshProUGUI debugDisplay; // debug display
    public RawImage debugViewer;
 

    // Start is called before the first frame update
    void Start()
    {
        RayCastKernelIndex = GLEAMCompute.FindKernel("RayCast");

        if (sampleCamera == null)
            sampleCamera = gameObject.GetComponent<Camera>();
        m_ARCameraBackground = sampleCamera.GetComponent<ARCameraBackground>();

        cameraImage = new RenderTexture(Screen.width, Screen.height, 24);
        cameraImage.enableRandomWrite = true;
        cameraImage.Create();

        sampleTexture = new Texture2D(probeSampleSize, probeSampleSize, TextureFormat.RGBA32, false);

        outputFrame = new RenderTexture(Screen.width, Screen.height, 24);
        outputFrame.enableRandomWrite = true;
        outputFrame.Create();
        GLEAMCompute.SetTexture(0, "_DebugTexture", outputFrame);
        shaderOutput.texture = outputFrame;

        sampleTextureRT = new RenderTexture(probeSampleSize, probeSampleSize, 24);
        sampleTextureRT.enableRandomWrite = true;
        sampleTextureRT.Create();
        GLEAMCompute.SetTexture(0, "_SampleTexture", sampleTextureRT);

        // create _SumsList and _WeightsList RenderTextures
        sumsListRT = new RenderTexture(probeSampleSize, probeSampleSize, 0, RenderTextureFormat.ARGB32);
        {
            sumsListRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            sumsListRT.volumeDepth = 6;
            sumsListRT.wrapMode = TextureWrapMode.Clamp;
            sumsListRT.filterMode = FilterMode.Trilinear;
            sumsListRT.enableRandomWrite = true;
            sumsListRT.Create();
        }
        GLEAMCompute.SetTexture(RayCastKernelIndex, "_SumsList", sumsListRT);

        weightsListRT = new RenderTexture(probeSampleSize, probeSampleSize, 0, RenderTextureFormat.RHalf);
        {
            weightsListRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            weightsListRT.volumeDepth = 6;
            weightsListRT.wrapMode = TextureWrapMode.Clamp;
            weightsListRT.filterMode = FilterMode.Trilinear;
            weightsListRT.enableRandomWrite = true;
            weightsListRT.Create();
        }
        GLEAMCompute.SetTexture(RayCastKernelIndex, "_WeightsList", weightsListRT);
         
        // create cubemap 3D texture
        cubemapRT = new RenderTexture(probeSampleSize, probeSampleSize, 0, RenderTextureFormat.ARGB32);
        {
            cubemapRT.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            cubemapRT.volumeDepth = 6;
            cubemapRT.wrapMode = TextureWrapMode.Clamp;
            cubemapRT.filterMode = FilterMode.Trilinear;
            cubemapRT.enableRandomWrite = true;
            cubemapRT.Create();
        }
        GLEAMCompute.SetTexture(RayCastKernelIndex, "_Cubemap", cubemapRT);


        GLEAMCompute.SetVector("_CameraDimensions", new Vector2(Screen.width, Screen.height));
        // print("width: " + Screen.width + " height: " + Screen.height);

        sampleArray = new sample[40000];
        int stride = 4 * 7;
        ComputeBuffer sampleBuffer = new ComputeBuffer(40000, stride);
        sampleBuffer.SetData(sampleArray);
        GLEAMCompute.SetBuffer(0, "samples", sampleBuffer);

        ScreenRect = new Rect(0, 0, Screen.width, Screen.height);
        size = new Vector2Int(6, probeSampleSize);

        
        GLEAMCompute.SetInt("_ProbeSampleSize", probeSampleSize);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(secondCounter >= 0.2f && probe != null)
        {
            // blit the cameraImage
            Graphics.Blit(null, cameraImage, m_ARCameraBackground.material);
            
            // send data to shader
            GLEAMCompute.SetMatrix("_CameraToWorld", sampleCamera.cameraToWorldMatrix);
            GLEAMCompute.SetMatrix("_CameraInverseProjection", sampleCamera.projectionMatrix.inverse);
            GLEAMCompute.SetVector("_CameraPosition", new Vector3(sampleCamera.transform.position.x, sampleCamera.transform.position.y, sampleCamera.transform.position.z));
            GLEAMCompute.SetInt("_SampleCount", 0);


            // calculate probe screen position and send to shader
            Vector3 probeScreenPosition = sampleCamera.WorldToScreenPoint(probe.transform.position);

            // print(probeScreenPosition);
            GLEAMCompute.SetFloats("_ProbeScreenPosition", new float[] {probeScreenPosition.x, probeScreenPosition.y, probeScreenPosition.z});

            // store the active render texture and replace it
            activeRT = RenderTexture.active;
            RenderTexture.active = cameraImage;

            int samplePosX = ((int)probeScreenPosition.x - probeSampleSize / 2);
            int samplePosY = ((int)probeScreenPosition.y - probeSampleSize / 2);


            UpdateDebugDisplay("probeDistance: " + probeScreenPosition.z + "\nprobeSampleSize: " + probeSampleSize);

            // initialize sampleTexture and cut the sample out of the active RenderTexture
            if(sampleTexture == null)
            {
                print("ERROR: sampleTexture not assigned.");
            }
            else if(
                (samplePosX >= 0 && (samplePosX + probeSampleSize) < Screen.width) && 
                (samplePosY >= 0 && (samplePosY + probeSampleSize) < Screen.height))
            {
                sampleTexture.ReadPixels(new Rect(samplePosX, samplePosY, probeSampleSize, probeSampleSize), 0, 0, false);
            }


            // applies the ReadPixels call to the sampleTexture, and displays it in the sampleViewer canvas image
            sampleTexture.Apply();
            debugViewer.texture = sampleTexture;
            shaderOutput.texture = outputFrame;
            RenderTexture.active = activeRT;

            // Sets the sampleTexture in the shader

            float sampleTextureScale = ((float)probeSampleSize)/200f;
            // float sampleTextureScale = ((float)probeSampleSize)/512f;

            Graphics.Blit(sampleTexture, sampleTextureRT, new Vector2(sampleTextureScale + 1f, sampleTextureScale + 1f), new Vector2(0f,0f));

            // Dispatch the Compute Shader
            int threadGroupsX = probeSampleSize / 4;
            int threadGroupsY = probeSampleSize / 4;
            GLEAMCompute.Dispatch(RayCastKernelIndex, threadGroupsX, threadGroupsY, 1);

            secondCounter = 0.0f;
        }
        else if (probe == null)
        {
            origin = GameObject.FindWithTag("Origin");

            if (origin != null)
            {
                probe = Instantiate(probePrefab);
                probe.transform.parent = origin.transform;
                probe.transform.localPosition = probeOffset;

                GLEAMCompute.SetVector("_ProbePosition", new Vector3(probe.transform.position.x, probe.transform.position.y, probe.transform.position.z));
                GLEAMCompute.SetFloat("_ProbeRadius", 0.028f);
            }
        }
        secondCounter += Time.deltaTime;
    }

    public void UpdateDebugDisplay(string debugString)
    {
        debugDisplay.text = debugString;
    }

    private void CalculatePixelValues()
    {
        
    }
}
