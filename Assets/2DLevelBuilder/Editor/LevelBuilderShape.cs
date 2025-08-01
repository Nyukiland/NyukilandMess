using UnityEditor;
using UnityEngine;

public class LevelBuilderShape : EditorWindow
{
    Color baseBackgroundColor;

    bool creatingShape = false;

    bool posLock, sizeLock = false;

    float gridPosLock = 1, gridSizeLock = 1;

    bool createCircle, createSquare, createTriangle;

    bool addCollider;

    bool addToFolder;
    string folderName = "Terrain";

    GameObject current = null;

    Vector2 firstpos;

    public static void ShowWindow()
    {
        LevelBuilderShape window = GetWindow<LevelBuilderShape>();
        window.titleContent = new GUIContent("LSBuilder");
        window.Show();
    }

    private void OnGUI()
    {
        GUIStyle TitleStyle = new GUIStyle();
        TitleStyle.normal.textColor = Color.white;
        TitleStyle.fontStyle = FontStyle.Bold;
        TitleStyle.fontSize = 18;

        GUILayout.Space(10);
        GUILayout.Label("Base Settings", TitleStyle);

        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);
        GUI.backgroundColor = Color.green;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Automatically add colliders");
        addCollider = EditorGUILayout.Toggle(addCollider);
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.blue;

        GUILayout.BeginHorizontal();
        GUILayout.Label("Automatically add to a specific folder");
        addToFolder = EditorGUILayout.Toggle(addToFolder);
        GUI.enabled = addToFolder;
        folderName = EditorGUILayout.TextField(folderName);
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        LockGUIProperty();

        GUILayout.Space(20);

        GUILayout.Label("Select shape", TitleStyle);

        BaseShapeButton();
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

    void BaseShapeButton()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        if (!creatingShape) GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("None", GUILayout.Height(100), GUILayout.MinWidth(125)))
        {
            TurnAllOff();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        if (createSquare) GUI.backgroundColor = Color.yellow;
        Texture2D textureS = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png");
        if (GUILayout.Button(textureS, GUILayout.Height(100), GUILayout.MinWidth(125)))
        {
            TurnAllOff();
            creatingShape = true;
            createSquare = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (createCircle) GUI.backgroundColor = Color.yellow;
        Texture2D textureC = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Circle.png");
        if (GUILayout.Button(textureC, GUILayout.Height(100), GUILayout.MinWidth(125)))
        {
            TurnAllOff();
            creatingShape = true;
            createCircle = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        if (createTriangle) GUI.backgroundColor = Color.yellow;
        Texture2D textureT = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Triangle.png");
        if (GUILayout.Button(textureT, GUILayout.Height(100), GUILayout.MinWidth(125)))
        {
            TurnAllOff();   
            creatingShape = true;
            createTriangle = true;
            SceneView.duringSceneGui += OnSceneGUI;
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        if (creatingShape)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Vector2 mousePos = Vector2.zero;

            if (posLock && gridPosLock > 0)
            {
                mousePos = new Vector2(RoundToNum(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.x, gridPosLock), RoundToNum(HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.y, gridPosLock));
            }
            else mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;

            Handles.color = Color.white;
            if (current == null) Handles.DrawSolidDisc(mousePos, Vector3.forward, 0.05f);
            SceneView.RepaintAll();

            if (createCircle) CreateCircle(e, mousePos);
            else if (createSquare) CreateSquare(e, mousePos);
            else if (createTriangle) CreateTriangle(e, mousePos);
        }
    }

    float RoundToNum(float valueToRound, float roundValue)
    {
        float temp = valueToRound / roundValue;
        temp = Mathf.Round(temp);
        return temp * roundValue;
    }

    #region SpriteGeneration

    void CreateSquare(Event e, Vector2 startPos)
    {
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            current = new GameObject();
            current.name = "Square";
            if (addToFolder)
            {
                if (GameObject.Find(folderName)) current.transform.parent = GameObject.Find(folderName).transform;
                else
                {
                    GameObject temp = new GameObject();
                    temp.name = folderName;
                    current.transform.parent = temp.transform;
                }
            }
            current.transform.localScale = Vector3.one * 0.05f;
            current.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png");
            if (addCollider) current.AddComponent<BoxCollider2D>();

            firstpos = startPos;
            current.transform.position = new Vector2(startPos.x + current.transform.localScale.x/2, startPos.y + current.transform.localScale.y / 2);
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            float distx = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.x - firstpos.x;
            float disty = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.y - firstpos.y;

            if (sizeLock && gridSizeLock > 0)
            {
                distx = RoundToNum(distx, gridSizeLock);
                disty = RoundToNum(disty, gridSizeLock);
            }
            current.transform.position = firstpos + (new Vector2(distx, disty)/2);
            current.transform.localScale = new Vector2(Mathf.Abs(distx), Mathf.Abs(disty));
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            Undo.RegisterCreatedObjectUndo(current, "Object creation");
            firstpos = Vector2.zero;
            current = null;
        }
    }

    void CreateCircle(Event e, Vector2 startPos)
    {
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            current = new GameObject();
            current.name = "Circle";
            if (addToFolder)
            {
                if (GameObject.Find(folderName)) current.transform.parent = GameObject.Find(folderName).transform;
                else
                {
                    GameObject temp = new GameObject();
                    temp.name = folderName;
                    current.transform.parent = temp.transform;
                }
            }
            current.transform.position = startPos;
            current.transform.localScale = Vector3.one * 0.05f;
            current.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Circle.png");
            if (addCollider) current.AddComponent<CircleCollider2D>();
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            float cursorObjDist = Mathf.Abs(Vector2.Distance(current.transform.position, HandleUtility.GUIPointToWorldRay(e.mousePosition).origin)) * 2;
            if (sizeLock && gridSizeLock > 0) cursorObjDist = RoundToNum(cursorObjDist, gridSizeLock);
            current.transform.localScale = Vector3.one * cursorObjDist;
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            Undo.RegisterCreatedObjectUndo(current, "Object creation");
            current = null;
        }
    }

    void CreateTriangle(Event e, Vector2 startPos)
    {
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            current = new GameObject();
            current.name = "Triangle";
            if (addToFolder)
            {
                if (GameObject.Find(folderName)) current.transform.parent = GameObject.Find(folderName).transform;
                else
                {
                    GameObject temp = new GameObject();
                    temp.name = folderName;
                    current.transform.parent = temp.transform;
                }
            }
            current.transform.localScale = Vector3.one * 0.05f;
            current.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Triangle.png");
            if (addCollider) current.AddComponent<PolygonCollider2D>();

            firstpos = startPos;
            current.transform.position = new Vector2(startPos.x + current.transform.localScale.x / 2, startPos.y + current.transform.localScale.y / 3.4638f);
        }

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            float distx = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.x - firstpos.x;
            float disty = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin.y - firstpos.y;

            if (sizeLock && gridSizeLock > 0)
            {
                distx = RoundToNum(distx, gridSizeLock);
                disty = RoundToNum(disty, gridSizeLock);
            }
            current.transform.position = firstpos + new Vector2(distx, disty/3.4638f);
            current.transform.localScale = new Vector2(distx*2, disty);
        }

        if (e.type == EventType.MouseUp && e.button == 0)
        {
            Undo.RegisterCreatedObjectUndo(current, "Object creation");
            firstpos = Vector2.zero;
            current = null;
        }
    }

    #endregion

    void TurnAllOff()
    {
        creatingShape = false;
        createCircle = false;
        createSquare = false;
        createTriangle = false;
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDisable()
    {
        TurnAllOff();
    }
}
