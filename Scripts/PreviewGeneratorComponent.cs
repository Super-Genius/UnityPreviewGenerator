using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

[HelpURL("https://github.com/klhurley/UnityPreviewGenerator/wiki/Editor-Window-&-Preview-Component#PreviewComponent")]
[ExecuteAlways]
public class PreviewGeneratorComponent : MonoBehaviour
{
    public PreviewGenerator PreviewGenerator;

    void OnValidate()
    {
        PreviewGenerator.bRepaintNeeded = true;        
    }
    
    void Start()
    {
        if (PreviewGenerator == null)
        {
            PreviewGenerator = new PreviewGenerator();
        }
        PreviewGenerator.Initialize();
        PreviewGenerator.bRepaintNeeded = true;

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