using System;
using UnityEngine;

[HelpURL("https://github.com/klhurley/UnityPreviewGenerator/wiki/Editor-Window-&-Preview-Component#PreviewComponent")]
[ExecuteAlways]
public class PreviewGeneratorComponent : MonoBehaviour
{
    public PreviewGenerator PreviewGenerator;
    [NonSerialized]
    public bool renderTextureChanged = false;

    void OnValidate()
    {
        if (PreviewGenerator != null)
        {
            PreviewGenerator.bRepaintNeeded = true;
            renderTextureChanged = true;
        }
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
                renderTextureChanged = false;
            }

            return PreviewGenerator.PreviewTexture;
        }

        return null;
    }
}