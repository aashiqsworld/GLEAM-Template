using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DebugScript : MonoBehaviour
{

    public RenderTexture sumsListRT;
    public RawImage textureDisplay;


    public ComputeShader debugShader;

    void Start()
    {
        sumsListRT = new RenderTexture(200, 200, 0, RenderTextureFormat.ARGB32);
        {
            sumsListRT.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            sumsListRT.volumeDepth = 6;
            sumsListRT.wrapMode = TextureWrapMode.Clamp;
            sumsListRT.filterMode = FilterMode.Trilinear;
            sumsListRT.enableRandomWrite = true;
            sumsListRT.Create();
        }
        debugShader.SetTexture(0, "Result", sumsListRT);
    }

    void Update()
    {
        print("dispatching shader");
        debugShader.Dispatch(0, 20, 20, 1);
        // textureDisplay.texture = sumsListRT;

    }
}
