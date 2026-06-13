using UnityEngine;

public class MassiveLangtonsAnt : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("A 1024x1024 grid handles millions of steps. 2048 is safe but takes a second to load.")]
    public int size = 1024;

    [Header("Simulation Settings")]
    public int stepsPerFrame = 2000;
    public bool isPaused = false;

    private Texture2D texture;
    private Color32[] pixels;     // For the GPU
    private bool[] isBlack;       // For the CPU math (much faster than reading pixel colors)

    private int antX;
    private int antY;
    private int antDir; // 0=Up, 1=Right, 2=Down, 3=Left

    private readonly Color32 colorWhite = new Color32(255, 255, 255, 255);
    private readonly Color32 colorBlack = new Color32(0, 0, 0, 255);
    private readonly Color32 colorRed = new Color32(255, 0, 0, 255);

    private void Start()
    {
        SetupEnvironment();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) isPaused = !isPaused;
        if (isPaused) return;

        // 1. Erase the red ant pixel from its previous position
        int oldIndex = antY * size + antX;
        pixels[oldIndex] = isBlack[oldIndex] ? colorBlack : colorWhite;

        // 2. Run the math loop as fast as possible
        for (int i = 0; i < stepsPerFrame; i++)
        {
            StepSimulation();
        }

        // 3. Draw the ant at its new position
        int newIndex = antY * size + antX;
        pixels[newIndex] = colorRed;

        // 4. Send to GPU
        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    private void SetupEnvironment()
    {
        // Setup static camera to frame the entire texture
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera").AddComponent<Camera>();
        
        cam.transform.position = new Vector3(size / 2f, size / 2f, -10f);
        cam.transform.rotation = Quaternion.identity;
        cam.orthographic = true;
        cam.orthographicSize = size / 2f;

        // Setup display quad
        GameObject displayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        displayQuad.name = "Simulation Display";
        displayQuad.transform.position = new Vector3(size / 2f, size / 2f, 0);
        displayQuad.transform.localScale = new Vector3(size, size, 1);
        Destroy(displayQuad.GetComponent<Collider>());

        // Initialize Data
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Material mat = new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture };
        displayQuad.GetComponent<MeshRenderer>().material = mat;

        int totalPixels = size * size;
        pixels = new Color32[totalPixels];
        isBlack = new bool[totalPixels];

        for (int i = 0; i < totalPixels; i++)
        {
            pixels[i] = colorWhite;
            isBlack[i] = false;
        }

        // Start center
        antX = size / 2;
        antY = size / 2;
        antDir = 0;
    }

    private void StepSimulation()
    {
        // 1. Calculate current 1D array index
        int index = antY * size + antX;

        // 2. Apply rules
        if (isBlack[index])
        {
            antDir = (antDir + 3) % 4; // Turn Left
            isBlack[index] = false;
            pixels[index] = colorWhite;
        }
        else
        {
            antDir = (antDir + 1) % 4; // Turn Right
            isBlack[index] = true;
            pixels[index] = colorBlack;
        }

        // 3. Move forward
        switch (antDir)
        {
            case 0: antY++; break;
            case 1: antX++; break;
            case 2: antY--; break;
            case 3: antX--; break;
        }

        // 4. Wrap-Around Logic (Teleport to opposite side)
        // If it goes off the left edge (-1), teleport to the right edge (size - 1)
        if (antX < 0) antX = size - 1;
        // If it goes off the right edge, teleport to the left edge (0)
        else if (antX >= size) antX = 0;

        // If it goes off the bottom edge (-1), teleport to the top edge (size - 1)
        if (antY < 0) antY = size - 1;
        // If it goes off the top edge, teleport to the bottom edge (0)
        else if (antY >= size) antY = 0;
    }
}