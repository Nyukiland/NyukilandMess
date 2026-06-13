using UnityEngine;

public class GameOfLifeManager : MonoBehaviour
{
    public int width = 50;
    public int height = 50;
    public Mesh cellMesh;
    public Material cellMaterial;

    private bool[,] grid;
    private bool[,] nextGrid;
    private Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
    
    // Instancing arrays for optimized batch rendering
    private Matrix4x4[][] batches;
    private int[] batchCounts;

    void Start()
    {
        grid = new bool[width, height];
        nextGrid = new bool[width, height];
        SetupCamera();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.transform.position = new Vector3(width / 2f, Mathf.Max(width, height), height / 2f);
        cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        cam.orthographic = true;
        cam.orthographicSize = Mathf.Max(width, height) / 2f + 2f;
    }

    void Update()
    {
        HandleInput();
        DrawCells();
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                int x = Mathf.FloorToInt(hitPoint.x);
                int z = Mathf.FloorToInt(hitPoint.z);

                if (x >= 0 && x < width && z >= 0 && z < height)
                {
                    grid[x, z] = Input.GetMouseButton(0);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ComputeNextGeneration();
        }
    }

    void ComputeNextGeneration()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int aliveNeighbors = CountNeighbors(x, y);
                nextGrid[x, y] = grid[x, y] ? (aliveNeighbors == 2 || aliveNeighbors == 3) : (aliveNeighbors == 3);
            }
        }

        (grid, nextGrid) = (nextGrid, grid);
    }

    int CountNeighbors(int x, int y)
    {
        int count = 0;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;
                int nx = x + i;
                int ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && grid[nx, ny])
                {
                    count++;
                }
            }
        }
        return count;
    }

    void DrawCells()
    {
        if (cellMesh == null || cellMaterial == null) return;

        int activeCount = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y]) activeCount++;

        if (activeCount == 0) return;

        int batchCount = Mathf.CeilToInt(activeCount / 1023f);
        if (batches == null || batches.Length < batchCount)
        {
            batches = new Matrix4x4[batchCount][];
            batchCounts = new int[batchCount];
            for (int i = 0; i < batchCount; i++) batches[i] = new Matrix4x4[1023];
        }

        System.Array.Clear(batchCounts, 0, batchCounts.Length);

        int currentBatch = 0;
        int currentIndex = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y])
                {
                    batches[currentBatch][currentIndex] = Matrix4x4.TRS(new Vector3(x + 0.5f, 0, y + 0.5f), Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.9f);                    currentIndex++;
                    batchCounts[currentBatch]++;

                    if (currentIndex == 1023)
                    {
                        currentBatch++;
                        currentIndex = 0;
                    }
                }
            }
        }

        for (int i = 0; i <= currentBatch; i++)
        {
            if (batchCounts[i] > 0)
            {
                Graphics.DrawMeshInstanced(cellMesh, 0, cellMaterial, batches[i], batchCounts[i]);
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        for (int x = 0; x <= width; x++) Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, 0, height));
        for (int y = 0; y <= height; y++) Gizmos.DrawLine(new Vector3(0, 0, y), new Vector3(width, 0, y));
    }
}