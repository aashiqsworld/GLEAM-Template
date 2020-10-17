using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using TMPro;


public class ARFoundationCameraWrapper : MonoBehaviour
{
    public GameObject probePrefab;
    public GLEAMBehaviour mGLEAM;
    [HideInInspector]
    public RenderTexture cameraImage; // full color camera image
    public Camera sampleCamera; // reference to the camera that
    private ARCameraBackground m_ARCameraBackground; // component that holds the camera image
    Vector3 screenProbePosition; // screen position of the probe
    Vector3 imageAnchorPosition; // position of the image target
    float secondCounter = 0.0f;
    public GameObject probe;
    public GameObject origin;

    // GLEAMBehaviour public properties
    [Header("GLEAMBehaviour Settings")]
    public RawImage sampleViewer; // displays the sample on the canvas
    public TextMeshProUGUI debugDisplay; // debug display
    public int probeSampleSize;
    public RawImage imageFacePosX,
                    imageFacePosY,
                    imageFacePosZ,
                    imageFaceNegX,
                    imageFaceNegY,
                    imageFaceNegZ;
    public RawImage debugViewer;
    public Vector3 probeOffset; // offset of the probe relative to the image anchor
    public Quaternion probeRotation;
    public Material skyMaterial; // Material storing cubemap texture
    public bool debug; // enable debug mode
    public float intensity;
    public bool THREADING;

    void Start()
    {
        if (sampleCamera == null)
            sampleCamera = gameObject.GetComponent<Camera>();

        // Set the ARCameraBackground property
        m_ARCameraBackground = sampleCamera.GetComponent<ARCameraBackground>();

        // Set the texture to sample from for the probe.
        cameraImage = new RenderTexture(Screen.width, Screen.height, 24);

        // check if the probe has a mesh collider; otherwise, add one
        if(probe.GetComponent<MeshCollider>() == null)
        {
            probe.AddComponent<MeshCollider>();
        }

        // set the probe tag
        probe.tag = "Probe";
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (secondCounter >= 0.3f && probe != null)
        {
            Graphics.Blit(null, cameraImage, m_ARCameraBackground.material);
            mGLEAM.GLEAM_Update(sampleCamera, cameraImage);
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

                mGLEAM = probe.GetComponent<GLEAMBehaviour>();

                mGLEAM.sampleViewer = sampleViewer;
                mGLEAM.debugDisplay = debugDisplay;
                mGLEAM.probe = probe;
                mGLEAM.probeSampleSize = probeSampleSize;
                mGLEAM.imageFaceNegX = imageFaceNegX;
                mGLEAM.imageFaceNegY = imageFaceNegY;
                mGLEAM.imageFaceNegZ = imageFaceNegZ;
                mGLEAM.imageFacePosX = imageFacePosX;
                mGLEAM.imageFacePosY = imageFacePosY;
                mGLEAM.imageFacePosZ = imageFacePosZ;
                mGLEAM.probeOffset = probeOffset;
                mGLEAM.skyMaterial = skyMaterial;
                mGLEAM.debug = debug;
                mGLEAM.intensity = intensity;
                mGLEAM.debugViewer = debugViewer;
                mGLEAM.THREADING = THREADING;

                mGLEAM.Init();
            }
        }
        secondCounter += Time.deltaTime;
    }
}
