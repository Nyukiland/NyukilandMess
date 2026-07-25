using System.Collections.Generic;
using UnityEngine;

public class FranquinTopology : MonoBehaviour
{
    [Header("Topologie de Franquin (Triangle de Pascal)")]
    [Tooltip("Itération temporelle (t). 3 ou 4 est idéal pour un schéma lisible.")]
    [Range(1, 6)]
    public int timeIteration = 3;

    [Header("Paramètres Visuels")]
    public float nodeSize = 0.5f;
    public float branchWidth = 0.1f;
    public float horizontalSpacing = 2f;
    public float verticalSpacing = 2f;
    
    [Header("Paramètres de Capture")]
    public bool autoFrameCamera = true;
    public Color backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);

    // Couleurs distinctes pour identifier les sous-ensembles homologues (k)
    private Color[] synchronousColors = new Color[]
    {
        new Color(0.8f, 0.2f, 0.2f), // Rouge (k=0)
        new Color(0.2f, 0.5f, 0.8f), // Bleu (k=1)
        new Color(0.2f, 0.7f, 0.3f), // Vert (k=2)
        new Color(0.9f, 0.6f, 0.1f), // Orange (k=3)
        new Color(0.6f, 0.3f, 0.8f), // Violet (k=4)
        new Color(0.4f, 0.4f, 0.4f), // Gris (k=5+)
    };

    private List<GameObject> generatedGeometry = new List<GameObject>();

    void Start()
    {
        GenerateTopology();
        if (autoFrameCamera) FrameCamera();
    }

    private void GenerateTopology()
    {
        foreach (GameObject obj in generatedGeometry) Destroy(obj);
        generatedGeometry.Clear();

        // On lance la génération récursive depuis la racine (t=0, k=0)
        Vector3 rootPosition = transform.position;
        GenerateNode(0, 0, rootPosition, horizontalSpacing);
    }

    private void GenerateNode(int currentDepth, int k, Vector3 position, float currentSpacing)
    {
        // 1. Création du Nœud (Méristème)
        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        node.transform.SetParent(this.transform);
        node.transform.position = position;
        node.transform.localScale = Vector3.one * nodeSize;
        Destroy(node.GetComponent<SphereCollider>());

        // Coloration basée sur 'k' (nombre de ramifications d'ordre 2)
        // C'est ce qui prouve le sous-ensemble synchrone de Franquin !
        Material mat = new Material(Shader.Find("Standard"));
        int colorIndex = Mathf.Min(k, synchronousColors.Length - 1);
        mat.color = synchronousColors[colorIndex];
        node.GetComponent<MeshRenderer>().material = mat;
        generatedGeometry.Add(node);

        // Si on a atteint l'itération temporelle t, on arrête la croissance
        if (currentDepth >= timeIteration) return;

        // 2. Ramification (Croissance vers l'itération t+1)
        float nextSpacing = currentSpacing / 2f; // On réduit l'espacement pour éviter les collisions visuelles
        
        // Branche 1 : Maintien de l'axe (k reste identique) -> Analogue à l'ordre principal
        Vector3 leftPos = position + new Vector3(-currentSpacing, -verticalSpacing, 0);
        CreateBranch(position, leftPos);
        GenerateNode(currentDepth + 1, k, leftPos, nextSpacing);

        // Branche 2 : Nouvelle ramification (k augmente de 1) -> Analogue au saut d'ordre de Franquin
        Vector3 rightPos = position + new Vector3(currentSpacing, -verticalSpacing, 0);
        CreateBranch(position, rightPos);
        GenerateNode(currentDepth + 1, k + 1, rightPos, nextSpacing);
    }

    private void CreateBranch(Vector3 start, Vector3 end)
    {
        GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        branch.transform.SetParent(this.transform);
        Destroy(branch.GetComponent<CapsuleCollider>());

        branch.transform.position = (start + end) / 2.0f;
        Vector3 direction = end - start;
        branch.transform.up = direction;
        branch.transform.localScale = new Vector3(branchWidth, direction.magnitude / 2.0f, branchWidth);
        
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.7f, 0.7f, 0.7f); // Branches grises pour faire ressortir les nœuds
        branch.GetComponent<MeshRenderer>().material = mat;
        
        generatedGeometry.Add(branch);
    }

    private void FrameCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || generatedGeometry.Count == 0) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backgroundColor;
        cam.orthographic = true;

        Bounds bounds = new Bounds(generatedGeometry[0].transform.position, Vector3.zero);
        foreach (GameObject obj in generatedGeometry)
        {
            bounds.Encapsulate(obj.GetComponent<Renderer>().bounds);
        }

        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
        cam.orthographicSize = maxExtent * 1.2f; // Marge
        
        cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
        cam.transform.LookAt(bounds.center);
    }
}
