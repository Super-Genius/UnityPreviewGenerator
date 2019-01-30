using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public class AnimationClipInfo
{
    public float PositionInClip;
    public AnimationClip AnimationClip;
}

[System.Serializable]
public enum BackgroundTextureTypes
{
    Transparent,  Color, Texture 
}

[System.Serializable]
public class BackgroundColorOrTextureInfo
{
    public BackgroundTextureTypes backType = BackgroundTextureTypes.Transparent;
    public Color BackgroundColor;
    public Texture2D BackgroundTexture;
    public bool UseBackgroundAlpha = true;
}

[System.Serializable]
public class PreviewGenerator
{

    public GameObject GameObjectToRender;
    public GameObject PreviewCamera;
    public bool OrthographicCamera = true;
    public Vector3 ViewDirection = new Vector3( -1.0f, -1.0f, -1.0f ).normalized;

    [HideInInspector] public Vector3 ViewRightDirection;

    public Vector3 ViewUpDirection = Vector3.up;
    public Vector2 PanOffset;
    public float ZoomLevel;
    
    [Delayed]
    public int RenderWidth = 256;
    [Delayed]
    public int RenderHeight = 256;
    public BackgroundColorOrTextureInfo BackgroundColorOrTextureInfo;
    public AnimationClipInfo AnimationClipInfo;
    private const int PREVIEW_LAYER = 22;
    private bool _isInited;
    private PostProcessMergeComponent _postProcessMergeComponent;
    private Material _combinerMaterial;

    [NonSerialized]
    public bool bRepaintNeeded = false;
    
    private GameObject _lastCameraObject;
    private GameObject _defaultCameraObject;
    
    private Texture2D _renderedPreview;
    public Texture2D PreviewTexture
    {
        get
        {
            if (GameObjectToRender == null)
            {
                _renderedPreview = null;
            }
            return _renderedPreview;
        }    
    }

    private GameObject m_CloneObject;

    private RuntimeAnimatorController tempRuntimeAnimatorController;

    private string _lastPNGPathName = "default.png";
    public string LastPNGPathName
    {
        get { return _lastPNGPathName; }
    }

    // Camera class to save transparency that is blasted away in post processing
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, ExecuteAlways]
    public class PostProcessMergeComponent : MonoBehaviour
    {
        public Texture2D AlphaStorage { get; set; }        

        void OnPostRender()
        {
            RenderTexture activeRenderTexture = RenderTexture.active;
            if (activeRenderTexture != null)
            {
                AlphaStorage = new Texture2D(activeRenderTexture.width, activeRenderTexture.height,
                    TextureFormat.RGBA32, false);
                AlphaStorage.ReadPixels(new Rect(0, 0, activeRenderTexture.width, activeRenderTexture.height), 0, 0,
                    false);
                AlphaStorage.Apply(false, true);
            }
        }
    }

    public void Initialize()
    {
        if (!_isInited)
        {
            _isInited = true;
            _defaultCameraObject = Resources.Load<GameObject>("PreviewGeneratorDefaultCamera");
            if (_defaultCameraObject != null)
            {
                PreviewCamera = _defaultCameraObject;
            }
            else
            {
                Debug.LogError("Cannot find the PreviewGeneratorDefaultCamera object to load!");
            }

            tempRuntimeAnimatorController =
                Resources.Load<RuntimeAnimatorController>("PreviewGeneratorDummyController");
            if (tempRuntimeAnimatorController == null)
            {
                Debug.LogError("Cannot find the PreviewGeneratorDummyController controller to load!");
            }

            _combinerMaterial = Resources.Load<Material>("CombinerMaterial");
            if (_combinerMaterial == null)
            {
                Debug.LogError("Cannot find the CombinerMaterial to load!");
            }

        }
        // mostly up cross product, to get mostly right
        ViewRightDirection = Vector3.Cross(Vector3.up, -ViewDirection);

    }
    
    public Texture2D MergeAlphaAndBackground(Texture2D srcTexture, Texture2D backgroundTexture, Texture2D alpha)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(srcTexture.width, srcTexture.height, 16);
        Texture2D retTexture = new Texture2D(srcTexture.width, srcTexture.height, TextureFormat.RGBA32, false);
        Material tempMaterial = new Material(_combinerMaterial);
        
        if (tempRT != null)
        {
            if (alpha == null)
            {
                alpha = Texture2D.whiteTexture;
            }

            if (backgroundTexture == null)
            {
                backgroundTexture = Texture2D.blackTexture;
            }

            if (BackgroundColorOrTextureInfo.backType == BackgroundTextureTypes.Texture)
            {
                tempMaterial.SetInt("_UseBackAlpha", BackgroundColorOrTextureInfo.UseBackgroundAlpha ? 1 : 0);                
            }
            else
            {
                tempMaterial.SetInt("_UseBackAlpha", 1);
            }

            backgroundTexture.wrapMode = TextureWrapMode.Repeat;
            srcTexture.wrapMode = TextureWrapMode.Clamp;
            alpha.wrapMode = TextureWrapMode.Clamp;
            
            if (tempMaterial != null)
            {
                tempMaterial.SetTexture("_AlphaTex", alpha);
                tempMaterial.SetTexture("_BackgroundTex", backgroundTexture);
            }
            RenderTexture oldRT = RenderTexture.active;
            Graphics.Blit(srcTexture, tempRT, tempMaterial);

            RenderTexture.active = tempRT;
            retTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0,
                false);
            retTexture.Apply(false, false);
            RenderTexture.active = oldRT;
            RenderTexture.ReleaseTemporary(tempRT);
        }

        return retTexture;
    }
    
    // save the render texture to png
    public void SavePNG(string path)
    {
        if (path.Length != 0)
        {

            _lastPNGPathName = path;
            byte[] pngData = PreviewTexture.EncodeToPNG();

            if (pngData != null)
            {
                try
                {
                    File.WriteAllBytes(path, pngData);
                }
                catch (Exception ex)
                {
                    Debug.Log(ex);            
                }
            }

        }        
    }
    
    // this function can only be called after all components have been through Awake and Start because this
    // class isn't a Monobehaviour class but it does instantiate Monobehaviour classes
    public GameObject GetInternalCamera()
    {

        GameObject _internalCameraObject = null;

        _postProcessMergeComponent = null;
        
        if (PreviewCamera != null)
        {
            
            _internalCameraObject = GameObject.Instantiate(PreviewCamera.gameObject);

            Camera cameraComponent = _internalCameraObject.GetComponent<Camera>();
            if (cameraComponent == null)
            {
                Debug.LogWarning("No camera component for " + _internalCameraObject.name + ". Default camera will be used.");
                _internalCameraObject = GameObject.Instantiate(_defaultCameraObject.gameObject);
                cameraComponent = _internalCameraObject.GetComponent<Camera>();
                PreviewCamera = _defaultCameraObject;
            }
            
            _internalCameraObject.name = "Preview Camera";
            if (cameraComponent != null)
            {
                cameraComponent.cullingMask = 1 << PREVIEW_LAYER;
                cameraComponent.enabled = false;
                cameraComponent.orthographic = OrthographicCamera;
                cameraComponent.backgroundColor = BackgroundColorOrTextureInfo.BackgroundColor;
            }
            
            MonoBehaviour postProcessComponent = (MonoBehaviour)_internalCameraObject.GetComponent("PostProcessingBehaviour");
            // only post processing system we support right now
            if (postProcessComponent != null)
            {
                _postProcessMergeComponent = _internalCameraObject.AddComponent<PostProcessMergeComponent>();
            }

            _internalCameraObject.gameObject.hideFlags = HideFlags.HideAndDontSave;
            RuntimePreviewGenerator.PreviewRenderCamera = cameraComponent;

            return _internalCameraObject;

        }

        return _internalCameraObject;

    }
    
    public void RenderPreviewTexture()
    {

        GameObject _internalCameraObject = null;
        Texture2D backgroundTexture = null;

        // make sure we are Initialiazed
        Initialize();
        
        if (BackgroundColorOrTextureInfo.backType == BackgroundTextureTypes.Texture)
        {
            RuntimePreviewGenerator.TransparentBackground = true;
            backgroundTexture = BackgroundColorOrTextureInfo.BackgroundTexture;
        }
        else
        {
            RuntimePreviewGenerator.TransparentBackground = (BackgroundColorOrTextureInfo.backType == BackgroundTextureTypes.Transparent);
            RuntimePreviewGenerator.BackgroundColor = BackgroundColorOrTextureInfo.BackgroundColor;
        }

        RuntimePreviewGenerator.OrthographicMode = OrthographicCamera;
        
        if (GameObjectToRender != null)
        {
            GameObject tempGameObject = GameObject.Instantiate(GameObjectToRender.gameObject, null, false);
            tempGameObject.hideFlags = HideFlags.HideAndDontSave;

            _internalCameraObject = GetInternalCamera();

            // There is a bug in AnimationMode.SampleAnimationClip which crashes
            // Unity if there is no valid controller attached
            bool doAnimClip = (AnimationClipInfo.AnimationClip != null);
            Animator animator = tempGameObject.GetComponent<Animator>();
            if ((animator != null) && (animator.runtimeAnimatorController == null))
            {
                if (tempRuntimeAnimatorController == null)
                {
                    doAnimClip = false;
                    Debug.LogError("Cannot load a Dummy Runtime Animator, disabling clip which can cause crashes");
                }
                animator.runtimeAnimatorController = tempRuntimeAnimatorController;
            }

            if (doAnimClip)
            {
                AnimationClipInfo.AnimationClip.SampleAnimation(tempGameObject, AnimationClipInfo.PositionInClip);
            }
            
            // set camera rotation/preview rotation
            RuntimePreviewGenerator.PreviewDirection = ViewDirection;
            RuntimePreviewGenerator.UpDirection = ViewUpDirection;
            RuntimePreviewGenerator.PanOffset = PanOffset;
            RuntimePreviewGenerator.ZoomLevel = ZoomLevel;
            Texture2D alphaTexture = null;


            Texture2D tempTexture =
                RuntimePreviewGenerator.GenerateModelPreview(tempGameObject.transform, RenderWidth, RenderHeight);

            if ((_postProcessMergeComponent != null) && (BackgroundColorOrTextureInfo.backType != BackgroundTextureTypes.Color))
            {
                alphaTexture = _postProcessMergeComponent.AlphaStorage;
            }

            _renderedPreview = MergeAlphaAndBackground(tempTexture, backgroundTexture, alphaTexture);
                           
            GameObject.DestroyImmediate(tempGameObject);
        }

        if (_internalCameraObject != null)
        {
            GameObject.DestroyImmediate(_internalCameraObject);
        }
    }

}



