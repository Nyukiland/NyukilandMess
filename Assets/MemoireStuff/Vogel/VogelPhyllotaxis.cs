using System.Collections.Generic;
using UnityEngine;

public class VogelPhyllotaxis : MonoBehaviour
{
	[Header("Modèle de Vogel (Phyllotaxie)")]
	[Tooltip("Nombre de primordia (graines/feuilles) à générer")]
	public int seedCount = 500;

	[Tooltip("L'angle de divergence (Or = 137.5). Modifiez cette valeur pour prouver l'occlusion !")]
	public float divergenceAngle = 137.5f;

	[Tooltip("Facteur d'échelle 'c' (distance entre les graines)")]
	public float scalingFactor = 0.5f;

	[Tooltip("Taille visuelle d'une graine")]
	public float seedSize = 0.4f;

	[Header("Paramètres de Capture")]
	public bool autoFrameCamera = true;

	public Color backgroundColor = new Color(0.95f, 0.95f, 0.95f, 1f);
	public Color centerColor = new Color(0.1f, 0.6f, 0.2f); // Vert foncé
	public Color edgeColor = new Color(0.6f, 0.9f, 0.3f); // Vert clair

	private List<GameObject> generatedSeeds = new List<GameObject>();

	void Start()
	{
		GenerateVogelModel();
		if (autoFrameCamera) FrameCamera();
	}

	private void GenerateVogelModel()
	{
		// Nettoyage au cas où on relance
		foreach (GameObject obj in generatedSeeds) Destroy(obj);
		generatedSeeds.Clear();

		// Le modèle de Vogel (1979) : r = c * sqrt(n) et theta = n * angle
		for (int n = 1; n <= seedCount; n++)
		{
			// 1. Calcul du rayon (r)
			float r = scalingFactor * Mathf.Sqrt(n);

			// 2. Calcul de l'angle (theta) en radians
			float theta = n * divergenceAngle * Mathf.Deg2Rad;

			// 3. Conversion Polaire vers Cartésien (x, y)
			float x = r * Mathf.Cos(theta);
			float y = r * Mathf.Sin(theta);

			// 4. Création de la géométrie (Graine)
			GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			seed.transform.SetParent(this.transform);
			seed.transform.position = new Vector3(x, y, 0); // On dessine sur le plan XY
			seed.transform.localScale = Vector3.one * seedSize;

			// Retrait de la physique
			Destroy(seed.GetComponent<SphereCollider>());

			// 5. Coloration didactique (dégradé du centre vers l'extérieur)
			Material mat = new Material(Shader.Find("Standard"));
			float normalizedDistance = (float)n / seedCount;
			mat.color = Color.Lerp(centerColor, edgeColor, normalizedDistance);
			seed.GetComponent<MeshRenderer>().material = mat;

			generatedSeeds.Add(seed);
		}
	}

	private void FrameCamera()
	{
		Camera cam = Camera.main;
		if (cam == null || generatedSeeds.Count == 0) return;

		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = backgroundColor;

		// La caméra regarde le modèle de face (orthographique pour éviter la perspective mathématique)
		cam.orthographic = true;

		// La taille orthographique est basée sur la graine la plus éloignée (la dernière)
		float maxDistance = scalingFactor * Mathf.Sqrt(seedCount);
		cam.orthographicSize = maxDistance + (seedSize * 2);

		cam.transform.position = new Vector3(0, 0, -10f);
		cam.transform.LookAt(Vector3.zero);
	}
}