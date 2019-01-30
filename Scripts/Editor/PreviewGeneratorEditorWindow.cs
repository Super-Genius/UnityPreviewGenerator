using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[HelpURL("https://github.com/klhurley/UnityPreviewGenerator/wiki/Editor-Window-&-Preview-Component#EditorWindow")]
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
    private const string _settingsPath = "/ProjectSettings/PreviewGeneratorSettings.json";

    [MenuItem("Window/Preview Generator")]
    static void ShowWindow()
    {
        window = GetWindow<PreviewGeneratorEditorWindow>();
        window.minSize = new Vector2(525.0f, 695.0f);
    }
    
    protected void OnEnable ()
    {
        if (_previewGenerator == null)
        {
            _previewGenerator = new PreviewGenerator();
        }

        _previewGenerator.Initialize();

        string path = Path.GetDirectoryName(Application.dataPath) + _settingsPath;
        if (File.Exists(path))
        {
            string settings = File.ReadAllText(path);
            if (settings != null)
            {
                // Then we apply them to this window
                EditorJsonUtility.FromJsonOverwrite(settings, _previewGenerator);
            }

            _previewGenerator.bRepaintNeeded = true;
        }

    }
 
    protected void OnDisable ()
    {
        string path = Path.GetDirectoryName(Application.dataPath) + _settingsPath;
        File.WriteAllText(path, EditorJsonUtility.ToJson(_previewGenerator, true));
    }

    void OnGUI()
    {
       if (window == null)
        {
            window = GetWindow<PreviewGeneratorEditorWindow>();
            window.minSize = new Vector2(525.0f, 695.0f);
            //OnEnable();
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

        previewGenSO.ApplyModifiedProperties();
                
        if (_previewGenerator.bRepaintNeeded)
        {
            Repaint();
        }

    }

}

// these are for the Preview Generator script
[CustomEditor(typeof(PreviewGeneratorComponent))]
public class PreviewGeneratorEditor : Editor
{
    // this happens when clicked off component or OnDisable/OnEnable with prefab reverts
    void OnEnable()
    {
        PreviewGenerator previewGenerator = ((PreviewGeneratorComponent)serializedObject.targetObject).PreviewGenerator;
        previewGenerator.bRepaintNeeded = true;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        base.OnInspectorGUI();
        serializedObject.ApplyModifiedProperties();

        PreviewGenerator previewGenerator = ((PreviewGeneratorComponent)serializedObject.targetObject).PreviewGenerator;
        if (previewGenerator.bRepaintNeeded)
        {
            Repaint();
        }
    }
 
    public override bool RequiresConstantRepaint()
    {
        PreviewGenerator previewGenerator = ((PreviewGeneratorComponent)serializedObject.targetObject).PreviewGenerator;
        return previewGenerator.bRepaintNeeded;
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
    private const int MinTextureSize = 1;
    private const int MaxTextureSize = 4096;
    
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
        SerializedProperty RenderWidthProperty = property.FindPropertyRelative("RenderWidth");
        SerializedProperty RenderHeightProperty = property.FindPropertyRelative("RenderHeight");

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
                float newZoom = previewGenerator.ZoomLevel - Event.current.delta.y * zoomSpeed;
                
                if (previewGenerator.OrthographicCamera)
                {
                    newZoom = Mathf.Min(.999999f, newZoom);
                }

                ZoomLevelProperty.floatValue =  newZoom;
            }
            else
            {
                Vector3 viewDirection = ViewDirectionProperty.vector3Value;
                viewDirection += ViewRightProperty.vector3Value * (-Event.current.delta.x * rotSpeed * Mathf.Deg2Rad) +
                                ViewUpProperty.vector3Value * (-Event.current.delta.y * rotSpeed * Mathf.Deg2Rad);

                viewDirection.Normalize();
                ViewRightProperty.vector3Value = Vector3.Cross(ViewUpProperty.vector3Value, viewDirection).normalized;
                // and new up Vector
                ViewUpProperty.vector3Value = Vector3.Cross(viewDirection, ViewRightProperty.vector3Value).normalized;
                ViewDirectionProperty.vector3Value = viewDirection;
            }

            if ((previewGenerator != null) && (previewGenerator.GameObjectToRender != null))
            {
                // allows high frame rate in inspector
                previewGenerator.bRepaintNeeded = true;
            }

            return;

        }
             
        if ((Event.current.type == EventType.ValidateCommand) &&
            (Event.current.commandName == "UndoRedoPerformed"))
        {
            if ((previewGenerator != null) && (previewGenerator.GameObjectToRender != null))
            {
                previewGenerator.bRepaintNeeded = true;
            }

            return;
        }
        
        // should we only do this during layout and paint events?
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

            // This can happen when the user is editing the View Direction with keyboard.
            if (ViewUpProperty.vector3Value == Vector3.zero)
            {
                ViewUpProperty.vector3Value = Vector3.up;
            }

            if (previewGenerator.OrthographicCamera)
            {
                ZoomLevelProperty.floatValue = Mathf.Min(.999999f, ZoomLevelProperty.floatValue);
            }

            RenderWidthProperty.intValue = Mathf.Min(Mathf.Max(RenderWidthProperty.intValue, MinTextureSize), MaxTextureSize);
            RenderHeightProperty.intValue = Mathf.Min(Mathf.Max(RenderHeightProperty.intValue, MinTextureSize), MaxTextureSize);

            previewGenerator.bRepaintNeeded = true;

        }
 
        if ((previewGenerator != null) && (previewGenerator.GameObjectToRender != null) && 
            (Event.current.type == EventType.Repaint) && previewGenerator.bRepaintNeeded)
        {
            previewGenerator.RenderPreviewTexture();
            previewGenerator.bRepaintNeeded = false;

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
        AnimationClip animClip = null;
        if (animClipProperty.objectReferenceValue != null)
        {
            if (animClipProperty.objectReferenceValue.GetType() == typeof(AnimationClip))
            {
                animClip = (AnimationClip) animClipProperty.objectReferenceValue;
            }
        }
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


