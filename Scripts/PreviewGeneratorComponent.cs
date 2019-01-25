using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

[HelpURL("https://github.com/klhurley/UnityPreviewGenerator/blob/master/README.md")]
[ExecuteAlways]
public class PreviewGeneratorComponent : MonoBehaviour
{
    public PreviewGenerator PreviewGenerator;

    void Start()
    {
        PreviewGenerator.Initialize();
    }
    
    public Texture2D GetRenderTexture(bool doRender = false)
    {
        if (PreviewGenerator != null)
        {
            PreviewGenerator.Initialize();
            if (doRender)
            {
                PreviewGenerator.RenderPreviewTexture();
            }

            return PreviewGenerator.PreviewTexture;
        }

        return null;
    }
}