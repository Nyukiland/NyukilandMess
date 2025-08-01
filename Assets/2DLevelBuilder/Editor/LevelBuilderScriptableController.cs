using UnityEditor;
using UnityEngine;

namespace SLBuilder
{
    public class LevelBuilderScriptableController : EditorWindow
    {
        string presetPath = "Assets/2DLevelBuilder/Preset";

        bool createState = true;

        Vector2 scroll;
        Vector2 scroll2;

        Scriptable2DLBuilderPreset selectedScriptable;
        Scriptable2DLBuilderPreset selectedScriptableCopy;

        Scriptable2DLBuilderPreset newScriptablePreset;

        [MenuItem("SLBuilder2D/Preset Constructor")]
        public static void ShowWindow()
        {
            LevelBuilderScriptableController window = GetWindow<LevelBuilderScriptableController>();
            window.titleContent = new GUIContent("SLBuilder Preset");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();

            Color tempBColor = GUI.backgroundColor;

            if (createState) GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Create")) createState = true;
            GUI.backgroundColor = tempBColor;

            if (!createState) GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Modify")) createState = false;
            GUI.backgroundColor = tempBColor;

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (createState)
            {
                if (newScriptablePreset == null) newScriptablePreset = ScriptableObject.CreateInstance<Scriptable2DLBuilderPreset>();
                PresetEditionGUI(newScriptablePreset, false);
            }
            else
            {
                if (selectedScriptable == null) SelectModifiedPresetGUI();
                else PresetEditionGUI(selectedScriptableCopy, true);
            }
        }

        void SelectModifiedPresetGUI()
        {
            if (AssetDatabase.FindAssets("t:" + typeof(Scriptable2DLBuilderPreset).Name, new[] { presetPath }).Length == 0)
            {
                GUILayout.Label("No Preset Found");
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(Scriptable2DLBuilderPreset).Name, new[] { presetPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scriptable2DLBuilderPreset scriptable = AssetDatabase.LoadAssetAtPath<Scriptable2DLBuilderPreset>(path);
                GUIContent content = new GUIContent(scriptable.presetName, AssetPreview.GetAssetPreview(scriptable.imageToWorkWith));

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(content, GUILayout.ExpandWidth(true), GUILayout.MaxWidth(400)))
                {
                    selectedScriptable = scriptable;
                    selectedScriptableCopy = ScriptableObject.CreateInstance<Scriptable2DLBuilderPreset>();
                    selectedScriptableCopy = Instantiate(scriptable);
                }

                Color tempBC = GUI.backgroundColor;

                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("Destroy", GUILayout.MinWidth(80), GUILayout.MaxWidth(200), GUILayout.Height(50)))
                {
                    AssetDatabase.DeleteAsset(path);
                }

                GUI.backgroundColor = tempBC;

                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        void PresetEditionGUI(Scriptable2DLBuilderPreset preset, bool isModifying)
        {
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            if (isModifying)
            {
                if (GUILayout.Button("Back"))
                {
                    selectedScriptable = null;
                    selectedScriptableCopy = null;
                    GUILayout.EndHorizontal();
                    return;
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            scroll2 = EditorGUILayout.BeginScrollView(scroll2);

            preset.presetName = EditorGUILayout.TextField("Name of the preset", preset.presetName);
            preset.folderName = EditorGUILayout.TextField("Name of the folder", preset.folderName);
            if (preset.presetName == "") preset.presetName = "name";

            GUILayout.Space(10);

            preset.scaleAllAxis = EditorGUILayout.Toggle("Uniform Scale", preset.scaleAllAxis);

            GUILayout.Space(10);
            preset.imageToWorkWith = (Sprite)EditorGUILayout.ObjectField(preset.imageToWorkWith, typeof(Sprite), false);

            if (preset.imageToWorkWith != null) GUI.enabled = true;
            else GUI.enabled = false;

            GUILayout.Space(10);

            PlacementAnchorPreview(preset);

            GUILayout.Space(10);

            preset.colType = (Scriptable2DLBuilderPreset.ColliderType)EditorGUILayout.EnumPopup("Collider type", preset.colType);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (isModifying)
            {
                if (GUILayout.Button("Save"))
                {
                    string path = presetPath + "/" + preset.presetName + ".asset";
                    if (AssetDatabase.LoadAssetAtPath(path, typeof(Scriptable2DLBuilderPreset)) != null)
                    {
                        if (EditorUtility.DisplayDialog("Warning", "The name you chose already exist \n continue will overwrite existing file", "continue", "cancel"))
                        {
                            AssetDatabase.DeleteAsset(presetPath + "/" + selectedScriptable.presetName + ".asset");
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.CreateAsset(preset, path);

                            preset.SetImageFactor();

                            selectedScriptable = null;
                            selectedScriptableCopy = null;

                            GUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                            return;
                        }
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(presetPath + "/" + selectedScriptable.presetName + ".asset");
                        AssetDatabase.CreateAsset(preset, path);

                        preset.SetImageFactor();

                        selectedScriptable = null;
                        selectedScriptableCopy = null;

                        GUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                        return;
                    }
                }
            }
            else
            {
                if (GUILayout.Button("Create"))
                {
                    string path = presetPath + "/" + preset.presetName + ".asset";

                    if (AssetDatabase.LoadAssetAtPath(path, typeof(Scriptable2DLBuilderPreset)) != null)
                    {
                        if (EditorUtility.DisplayDialog("Warning", "The name you chose already exist \n continue will overwrite existing file", "continue", "cancel"))
                        {
                            AssetDatabase.DeleteAsset(path);
                            AssetDatabase.CreateAsset(preset, path);

                            preset.SetImageFactor();


                            newScriptablePreset = null;

                            GUILayout.EndHorizontal();
                            EditorGUILayout.EndScrollView();
                            return;
                        }
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(preset, path);

                        preset.SetImageFactor();

                        newScriptablePreset = null;

                        GUILayout.EndHorizontal();
                        EditorGUILayout.EndScrollView();
                        return;
                    }
                }
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        void PlacementAnchorPreview(Scriptable2DLBuilderPreset preset)
        {
            if (preset.imageToWorkWith == null) return;

            Color tempC = GUI.backgroundColor;

            GUILayout.Box(AssetPreview.GetAssetPreview(preset.imageToWorkWith));
            Rect boxAbove = GUILayoutUtility.GetLastRect();

            Handles.BeginGUI();
            Handles.color = Color.blue;
            Handles.DrawWireDisc(boxAbove.center, Vector3.forward, 10f);

            Handles.color = Color.green;
            Vector2 posStartV = Vector2Lerp(boxAbove.position, boxAbove.position + boxAbove.size, preset.startAnchor);
            Handles.DrawSolidDisc(posStartV, Vector3.forward, 5f);
            Handles.color = Color.black;
            Handles.DrawWireDisc(posStartV, Vector3.forward, 4f);

            Handles.color = Color.magenta;
            Vector2 posEndV = Vector2Lerp(boxAbove.position, boxAbove.position + boxAbove.size, preset.endAnchor);
            Handles.DrawSolidDisc(posEndV, Vector3.forward, 5f);
            Handles.color = Color.black;
            Handles.DrawWireDisc(posEndV, Vector3.forward, 4f);
            Handles.EndGUI();

            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            GUILayout.Label("Start Anchor Position");
            preset.startAnchor.x = EditorGUILayout.Slider(preset.startAnchor.x, 0, 1);
            preset.startAnchor.y = EditorGUILayout.Slider(preset.startAnchor.y, 0, 1);

            GUI.backgroundColor = Color.magenta;
            GUILayout.Label("End Anchor Position");
            preset.endAnchor.x = EditorGUILayout.Slider(preset.endAnchor.x, 0, 1);
            preset.endAnchor.y = EditorGUILayout.Slider(preset.endAnchor.y, 0, 1);

            if (!preset.scaleAllAxis)
            {
                if (preset.startAnchor.x == preset.endAnchor.x) preset.endAnchor.x += 0.01f; 
                if (preset.startAnchor.y == preset.endAnchor.y) preset.endAnchor.y += 0.01f; 
            }
            else
            {
                if (preset.startAnchor == preset.endAnchor) preset.endAnchor += Vector2.one * 0.01f;
            }

            GUI.backgroundColor = tempC;
        }

        Vector2 Vector2Lerp(Vector2 start, Vector2 end, Vector2 lerpIndex)
        {
            return new Vector2(Mathf.Lerp(start.x, end.x, lerpIndex.x), Mathf.Lerp(end.y, start.y, lerpIndex.y)); //not a mistake
        }
    }
}