using System;
using System.Reflection;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[HelpURL("https://github.com/klhurley/UnityPreviewGenerator/blob/master/README.md")]
[Serializable]
public class PreviewGeneratorEditorWindow : EditorWindow
{
    static PreviewGeneratorEditorWindow window;
    [SerializeField]
    private PreviewGenerator _previewGenerator;

    SerializedObject previewGenSO;
    private SerializedProperty previewGenSP;
    private Editor _previewGeneratorEditor;

    private Vector2 curScrollPosition = new Vector2(0.0f, 0.0f);
     
    [MenuItem("Window/Preview Generator")]
    static void ShowWindow()
    {
        window = GetWindow<PreviewGeneratorEditorWindow>();
        window.minSize = new Vector2(525.0f, 695.0f);
        Debug.Log("In Show Window");
    }
    
    protected void OnEnable ()
    {
        Debug.Log("In OnEnable loading window layout and settings");
        if (_previewGenerator == null)
        {
            _previewGenerator = new PreviewGenerator();
        }

        _previewGenerator.Initialize();
        //wantsMouseMove = true;
        // Here we retrieve the data if it exists or we save the default field initializers we set above
        string data = EditorPrefs.GetString("PreviewGeneratorWindow", JsonUtility.ToJson(this, false));
        // Then we apply them to this window
        JsonUtility.FromJsonOverwrite(data, this);

    }
 
    protected void OnDisable ()
    {
        Debug.Log("In OnDisable saving window layout and settings");
        // We get the Json data
        string data = JsonUtility.ToJson(this, false);
        // And we save it
        EditorPrefs.SetString("PreviewGeneratorWindow", data);
    }

    void OnGUI()
    {
       if (window == null)
        {
            window = GetWindow<PreviewGeneratorEditorWindow>();
            window.minSize = new Vector2(525.0f, 695.0f);
            Debug.Log("In OnGUI and Created new Window");
            OnEnable();
        }

        bool oldWideMode = EditorGUIUtility.wideMode;
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        // about 42% for labelWidth
        EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth / 2.4f;
        EditorGUIUtility.wideMode = true;
        
        previewGenSO = new SerializedObject(this);

        previewGenSO.Update();

        previewGenSP = previewGenSO.FindProperty("_previewGenerator");
        if (previewGenSP == null)
        {
            Debug.LogError("Misnamed _preview Generator in this Window class");
            return;
        }

        EditorGUILayout.InspectorTitlebar(true, this);
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            curScrollPosition = EditorGUILayout.BeginScrollView(curScrollPosition);
            {
                EditorGUILayout.PropertyField(previewGenSP, true);
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.EndHorizontal();

        // about 42% for labelWidth
        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUIUtility.wideMode = oldWideMode;

        if (_previewGenerator.bRepaintNeeded)
        {
            Repaint();
            _previewGenerator.bRepaintNeeded = false;

        }
        previewGenSO.ApplyModifiedProperties();

    }

}

// these are for the Preview Generator script
[CustomEditor(typeof(PreviewGeneratorComponent))]
public class PreviewGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        
        SerializedProperty prop = serializedObject.FindProperty("PreviewGenerator");

        if (prop != null)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(prop, true);
            serializedObject.ApplyModifiedProperties();     
            FieldInfo field = prop.serializedObject.targetObject.GetType().GetField(prop.propertyPath);
            if (field != null)
            {
                PreviewGenerator previewGenerator =
                    (PreviewGenerator) field.GetValue(prop.serializedObject.targetObject);
                if (previewGenerator.bRepaintNeeded)
                {
                    Repaint();
                    previewGenerator.bRepaintNeeded = false;
                }
            }

        }

    }
}

[CustomPropertyDrawer(typeof(PreviewGenerator), true)]
public class PreviewGeneratorDrawer : PropertyDrawer
{
    private const float rotSpeed = 1.0f;
    private const float panSpeed = .0025f;
    private const float zoomSpeed = .01f; 
    private bool mouseOverTexture = false;
    private Rect texRect = new Rect(0.0f, 0.0f, 0.0f, 0.0f);
    
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {

        PreviewGenerator previewGenerator =
            (PreviewGenerator) fieldInfo.GetValue(property.serializedObject.targetObject);

        // need to updated values with Serialization for Undo and Changes
        SerializedProperty PanOffsetProperty = property.FindPropertyRelative("PanOffset");
        SerializedProperty ZoomLevelProperty = property.FindPropertyRelative("ZoomLevel");
        SerializedProperty ViewDirectionProperty = property.FindPropertyRelative("ViewDirection");
        SerializedProperty ViewRightProperty = property.FindPropertyRelative("ViewRightDirection");
        SerializedProperty ViewUpProperty = property.FindPropertyRelative("ViewUpDirection");

        mouseOverTexture = texRect.Contains(Event.current.mousePosition);
        if (mouseOverTexture && (Event.current.type == EventType.MouseDrag))
        {
            
            // do pan if the control key is pressed
            if (Event.current.control)
            {
                Vector2 panOffset = PanOffsetProperty.vector2Value;
                panOffset.x += Event.current.delta.x * panSpeed;
                panOffset.y += Event.current.delta.y * panSpeed;
                PanOffsetProperty.vector2Value = panOffset;
            }
            else if (Event.current.shift)
            {
                float newZoom = ZoomLevelProperty.floatValue - Event.current.delta.y * zoomSpeed;
                
                if (previewGenerator.OrthographicCamera)
                {
                    newZoom = Mathf.Min(.999999f, newZoom);
                }

                ZoomLevelProperty.floatValue = newZoom;
            }
            else
            {

                Vector3 newDirection = previewGenerator.ViewDirection.normalized;
                newDirection += previewGenerator.ViewRightDirection.normalized * (Event.current.delta.x * rotSpeed * Mathf.Deg2Rad) +
                                previewGenerator.ViewUpDirection.normalized * (-Event.current.delta.y * rotSpeed * Mathf.Deg2Rad);

                newDirection.Normalize();
                ViewRightProperty.vector3Value = Vector3.Cross(previewGenerator.ViewUpDirection.normalized, newDirection);
                // and new up Vector
                ViewUpProperty.vector3Value = Vector3.Cross(newDirection, ViewRightProperty.vector3Value);
            
                ViewDirectionProperty.vector3Value = newDirection;
               
            }

            previewGenerator.bRepaintNeeded = true;
        }

        if (previewGenerator != null)
        {
            if ((previewGenerator.GameObjectToRender != null) && (previewGenerator.RenderHeight > 0) && (previewGenerator.RenderWidth > 0))
            {
                previewGenerator.RenderPreviewTexture();
            }
        }

        EditorGUI.BeginChangeCheck();
        {
            if (property.hasVisibleChildren)
            {
                property.NextVisible(true);
                do
                {
                    EditorGUILayout.PropertyField(property, true);
                } while (property.NextVisible(false));
            }
        }
        if (EditorGUI.EndChangeCheck())
        {
            // do these outside the drag, since the user may have added new entries manually
            ViewDirectionProperty.vector3Value.Normalize();
            // get new right Vector
            ViewRightProperty.vector3Value =
                Vector3.Cross(ViewUpProperty.vector3Value.normalized, ViewDirectionProperty.vector3Value);
            // and new up Vector
            ViewUpProperty.vector3Value =
                Vector3.Cross(ViewDirectionProperty.vector3Value, ViewRightProperty.vector3Value.normalized);
 
        }

        Texture2D previewTexture = previewGenerator.PreviewTexture;
        
        EditorGUILayout.Space();
        if (GUILayout.Button(" Save PNG... ", GUILayout.ExpandWidth(false)))
        {
            string path = Path.GetDirectoryName(previewGenerator.LastPNGPathName);
            string filename = Path.GetFileName(previewGenerator.LastPNGPathName);
            path = EditorUtility.SaveFilePanel("Save Texture as PNG", path, filename, "png");
            if (path.Length != 0)
            {
                previewGenerator.SavePNG(path);                
            }
        }
        
        EditorGUILayout.Space();
        float boxHeight = Mathf.Min(EditorGUIUtility.currentViewWidth - 36, previewGenerator.RenderHeight);
        GUILayout.Box(previewTexture,
            GUILayout.Height(boxHeight), GUILayout.Width(EditorGUIUtility.currentViewWidth - 36));
        if (Event.current.type == EventType.Repaint)
        {
            texRect = GUILayoutUtility.GetLastRect();
        }


        EditorGUILayout.Space();

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 0.0f;
    }

} 

[CustomPropertyDrawer(typeof(AnimationClipInfo), true)]
public class AnimationClipInfoDrawer : PropertyDrawer
{
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
 
        SerializedProperty animClipProperty = property.FindPropertyRelative("AnimationClip");
        SerializedProperty positionProperty = property.FindPropertyRelative("PositionInClip");
        AnimationClip animClip = (AnimationClip) animClipProperty.objectReferenceValue;
        float positionInClip = positionProperty.floatValue;

        EditorGUILayout.Space();        
        EditorGUILayout.PropertyField(animClipProperty);

        if (animClip != null)
        {
            FontStyle origFontStyle = EditorStyles.label.fontStyle;
            if (positionProperty.prefabOverride)
            {
                EditorStyles.label.fontStyle = FontStyle.Bold;
            }
            // render slider for clip animation length
            positionInClip = EditorGUILayout.Slider("Animation Position", positionInClip, 0.0f, animClip.length);
            EditorStyles.label.fontStyle = origFontStyle;
            positionProperty.floatValue = positionInClip;
            GUILayout.BeginHorizontal();
            var defaultAlignment = GUI.skin.label.alignment;
            EditorGUILayout.PrefixLabel(" ");
            GUILayout.Label("0");
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            GUILayout.Label(animClip.length.ToString("F3") + "                ");
            GUI.skin.label.alignment = defaultAlignment;
            GUILayout.EndHorizontal();
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 0.0f;
    }


}

[CustomPropertyDrawer(typeof(BackgroundColorOrTextureInfo), true)]
public class BackgroundColorOrTextureInfoDrawer : PropertyDrawer
{
    private bool backgroundFoldOut = true;
    
    // Draw the property inside the given rect
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty BackgroundTypeProperty = property.FindPropertyRelative("backType");
        SerializedProperty BackgroundColorProperty = property.FindPropertyRelative("BackgroundColor");
        SerializedProperty BackgroundTextureProperty = property.FindPropertyRelative("BackgroundTexture");
        SerializedProperty UseBackgroundAlphaProperty = property.FindPropertyRelative("UseBackgroundAlpha");

        backgroundFoldOut = EditorGUILayout.Foldout(backgroundFoldOut, "Background Type");
        if (backgroundFoldOut)
        {
            EditorGUI.indentLevel++;
            // make selection transparent background
            BackgroundTextureTypes selGridInt = (BackgroundTextureTypes) BackgroundTypeProperty.enumValueIndex;

            FontStyle origFontStyle = EditorStyles.label.fontStyle;
            if (BackgroundTypeProperty.prefabOverride)
            {
                EditorStyles.label.fontStyle = FontStyle.Bold;
            }

            if (EditorGUILayout.Toggle("Use Transparent Background",
                (selGridInt == BackgroundTextureTypes.Transparent)))
            {
                selGridInt = BackgroundTextureTypes.Transparent;
            }

            if (EditorGUILayout.Toggle("Use Color Background", (selGridInt == BackgroundTextureTypes.Color)))
            {
                selGridInt = BackgroundTextureTypes.Color;
            }

            if (EditorGUILayout.Toggle("Use Texture Background", (selGridInt == BackgroundTextureTypes.Texture)))
            {
                selGridInt = BackgroundTextureTypes.Texture;
            }

            EditorStyles.label.fontStyle = origFontStyle;
            BackgroundTypeProperty.enumValueIndex = (int) selGridInt;

            EditorGUI.BeginDisabledGroup(selGridInt != BackgroundTextureTypes.Color);
            EditorGUILayout.PropertyField(BackgroundColorProperty);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(selGridInt != BackgroundTextureTypes.Texture);
            EditorGUILayout.PropertyField(BackgroundTextureProperty);
            EditorGUILayout.PropertyField(UseBackgroundAlphaProperty);
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return 0.0f;
    }

}


