using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SpriteRenderTexture : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private PreviewGeneratorComponent _previewGeneratorComponent;
    private Texture2D _curTexture = null;
    private GameObject _curGameObject;
    
    // Start is called before the first frame update
    void Start()
    {

        _spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        _previewGeneratorComponent = gameObject.GetComponent<PreviewGeneratorComponent>();
        if (_previewGeneratorComponent != null)
        {
            SetSprite(true);
            _curGameObject = _previewGeneratorComponent.PreviewGenerator.GameObjectToRender;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_previewGeneratorComponent != null)
        {
            // only gets new renderTexture if object changes or texture changes
            SetSprite((_curGameObject != _previewGeneratorComponent.PreviewGenerator.GameObjectToRender) || _previewGeneratorComponent.renderTextureChanged);
        }
    }

    void SetSprite(bool doRender)
    {
        if (_previewGeneratorComponent != null)
        {
            if (_spriteRenderer != null)
            {
                Texture2D spriteTexture = _previewGeneratorComponent.GetRenderTexture(doRender);
                if ((spriteTexture != null) && (spriteTexture != _curTexture))
                {
                    _curTexture = spriteTexture;
                    Sprite sprite = Sprite.Create(spriteTexture,
                        new Rect(0.0f, 0.0f, spriteTexture.width, spriteTexture.height), new Vector2(.5f, .5f), 100.0f);
                    _spriteRenderer.sprite = sprite;
                }
            }
        }
        
    }
}
