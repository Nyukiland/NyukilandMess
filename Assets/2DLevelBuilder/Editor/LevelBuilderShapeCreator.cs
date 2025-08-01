using UnityEditor;
using UnityEngine;

namespace SLBuilder
{
    public class LevelBuilderShapeCreator : EditorWindow
    {
        string presetPath = "Assets/2DLevelBuilder/Preset";

        bool posLock = false, sizeLock = false;

        bool quickLock, quickAllAxis;

        float gridPosLock = 1, gridSizeLock = 1;

        Scriptable2DLBuilderPreset selected;

        GameObject current;

        Vector2 scroll;

        Vector2 firstPos;

        [MenuItem("SLBuilder2D/ShapeConstructor")]
        public static void ShowWindow()
        {
            LevelBuilderShapeCreator window = GetWindow<LevelBuilderShapeCreator>();
            window.titleContent = new GUIContent("LSBuilder");
            window.Show();
        }

        private void OnGUI()
        {
            LockGUIProperty();

            GUILayout.Space(10);

            GetPresetGUI();
        }

        void LockGUIProperty()
        {
            GUILayout.BeginHorizontal();

            //grid lock position bool and float
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Snap position to grid");
            posLock = EditorGUILayout.Toggle(posLock);
            GUILayout.EndHorizontal();
            GUI.enabled = posLock;
            gridPosLock = EditorGUILayout.FloatField(gridPosLock);
            GUI.enabled = true;

            GUILayout.EndVertical();

            // splitter
            GUILayout.Space(10);

            //grid lock size bool and float
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Snap size to number");
            sizeLock = EditorGUILayout.Toggle(sizeLock);
            GUILayout.EndHorizontal();
            GUI.enabled = sizeLock;
            gridSizeLock = EditorGUILayout.FloatField(gridSizeLock);
            GUI.enabled = true;

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        void GetPresetGUI()
        {
            Color tempBack = GUI.backgroundColor;
            if (AssetDatabase.FindAssets("t:" + typeof(Scriptable2DLBuilderPreset).Name, new[] { presetPath }).Length == 0)
            {
                GUILayout.Label("No Preset Found");
                SceneView.duringSceneGui -= OnSceneGUI;
                return;
            }

            if (selected == null) GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("None"))
            {
                selected = null;
                SceneView.duringSceneGui -= OnSceneGUI;
            }

            GUI.backgroundColor = tempBack;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(Scriptable2DLBuilderPreset).Name, new[] { presetPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Scriptable2DLBuilderPreset scriptable = AssetDatabase.LoadAssetAtPath<Scriptable2DLBuilderPreset>(path);
                GUIContent content = new GUIContent(scriptable.presetName, AssetPreview.GetAssetPreview(scriptable.imageToWorkWith));


                if (scriptable == selected) GUI.backgroundColor = Color.yellow;

                if (GUILayout.Button(content))
                {
                    if (selected == null) SceneView.duringSceneGui += OnSceneGUI;
                    selected = scriptable;
                }

                GUI.backgroundColor = tempBack;
            }

            EditorGUILayout.EndScrollView();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (selected != null)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                Vector2 mousePos = Vector2.zero;

                mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

                if ((posLock || quickLock) && gridPosLock > 0)
                {
                    mousePos = new Vector2(RoundToNum(mousePos.x, gridPosLock), RoundToNum(mousePos.y, gridPosLock));
                }

                Handles.color = Color.white;
                if (current == null) Handles.DrawSolidDisc(mousePos, Vector3.forward, 0.05f);

                Handles.BeginGUI();
                Rect overlayRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height);
                GUI.color = GUI.color = new Color(0.980f, 0.855f, 0.867f, 0.15f);
                GUI.DrawTexture(overlayRect, Texture2D.whiteTexture);
                GUI.color = Color.white; // Reset color
                Handles.EndGUI();

                CreateShape(e);

                quickAllAxis = e.shift;
                quickLock = e.control;
                
            }
        }

        void CreateShape(Event e)
        {
            Vector2 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                current = new GameObject();
                current.name = selected.presetName;
                if (selected.folderName != "")
                {
                    if (GameObject.Find(selected.folderName)) current.transform.parent = GameObject.Find(selected.folderName).transform;
                    else
                    {
                        GameObject temp = new GameObject();
                        temp.name = selected.folderName;
                        current.transform.parent = temp.transform;
                    }
                }
                current.transform.localScale = Vector3.one * 0.05f;
                current.AddComponent<SpriteRenderer>().sprite = selected.imageToWorkWith;
                if (selected.colType == Scriptable2DLBuilderPreset.ColliderType.CircleCollider) current.AddComponent<CircleCollider2D>();
                if (selected.colType == Scriptable2DLBuilderPreset.ColliderType.BoxCollider) current.AddComponent<BoxCollider2D>();
                if (selected.colType == Scriptable2DLBuilderPreset.ColliderType.PolygonCollider) current.AddComponent<PolygonCollider2D>();

                if (posLock && gridPosLock > 0) firstPos = new Vector2(RoundToNum(mousePos.x, gridSizeLock), RoundToNum(mousePos.y, gridSizeLock));
                else firstPos = mousePos;
                current.transform.position = firstPos;

                //safety 
                selected.SetImageFactor();
            }

            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                float distx = mousePos.x - firstPos.x;
                float disty = mousePos.y - firstPos.y;

                if ((sizeLock || quickLock) && gridSizeLock > 0)
                {
                    distx = RoundToNum(distx, gridSizeLock);
                    disty = RoundToNum(disty, gridSizeLock);
                }


                if (selected.scaleAllAxis || quickAllAxis)
                {
                    float dist = Vector2.Distance(mousePos, firstPos);

                    Vector2 offsetMath = new Vector2(selected.offsetX * dist * selected.globalScaleFact, selected.offsetY * dist * selected.globalScaleFact);
                    current.transform.position = firstPos + offsetMath;

                    current.transform.localScale = new Vector2(dist * selected.globalScaleFact, dist * selected.globalScaleFact);
                }
                else
                {
                    Vector2 offsetMath = new Vector2(selected.offsetX * distx * selected.scaleFactX, selected.offsetY * disty * selected.scaleFactY);
                    current.transform.position = firstPos + offsetMath;

                    current.transform.localScale = new Vector2(distx * selected.scaleFactX, disty * selected.scaleFactY);
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (current != null) Undo.RegisterCreatedObjectUndo(current, "Object creation");
                firstPos = Vector2.zero;
                current = null;
            }
        }

        float RoundToNum(float valueToRound, float roundValue)
        {
            float temp = valueToRound / roundValue;
            temp = Mathf.Round(temp);
            return temp * roundValue;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnInspectorUpdate()
        {
            if (selected != null && EditorWindow.focusedWindow != this && EditorWindow.focusedWindow != SceneView.lastActiveSceneView)
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                selected = null;
            }
        }
    }
}