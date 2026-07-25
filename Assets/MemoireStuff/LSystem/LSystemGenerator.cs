using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LSystemGenerator : MonoBehaviour
{
	[System.Serializable]
	public struct LSystemRule
	{
		public string symbol;
		public string replacement;

		[Tooltip("Probabilité d'application de la règle (Stochastic L-System)")]
		[Range(0f, 1f)]
		public float probability;
	}

	[Header("Paramètres du L-System")]
	[Tooltip("L'axiome de départ (ex: F ou FX)")]
	public string axiom = "FX";

	[Tooltip("Les règles de production")]
	public LSystemRule[] rules;

	[Tooltip("Nombre d'itérations temporelles")]
	[Range(1, 7)]
	public int iterations = 3;

	[Header("Open L-System (Environnement)")]
	[Tooltip("Activer les requêtes environnementales (symbole ? dans les règles)")]
	public bool simulateEnvironment = true;

	[Tooltip("Probabilité (0 à 1) qu'une branche manque de lumière lors d'un test ? et s'élague (%)")]
	[Range(0f, 1f)]
	public float environmentalStress = 0.2f;

	[Header("Paramètres de la Tortue Graphique")]
	[Tooltip("Angle de rotation en degrés (ex: 90 ou 30)")]
	public float angle = 30f;

	[Tooltip("Longueur d'un segment de croissance")]
	public float segmentLength = 1f;

	[Tooltip("Épaisseur visuelle de la branche")]
	public float branchWidth = 0.1f;

	[Header("Paramètres de Capture")]
	[Tooltip("Cadrer automatiquement la caméra sur l'arbre")]
	public bool autoFrameCamera = true;

	[Tooltip("Couleur de fond de la capture")]
	public Color backgroundColor = new Color(0.9f, 0.9f, 0.9f, 1f);

	[Header("Matériel (Optionnel)")]
	public Material branchMaterial;

	// État interne pour sauvegarder la position et rotation (symbole [ et ])
	private class TransformInfo
	{
		public Vector3 position;
		public Quaternion rotation;
		public bool isPruned; // Permet de savoir si la branche a été coupée par l'environnement
	}

	private string currentSentence;
	private List<GameObject> generatedGeometry = new List<GameObject>();

	void Start()
	{
		// Si aucune règle n'est définie par défaut, on charge ton exemple Bracketed
		if (rules == null || rules.Length == 0)
		{
			SetupDefaultBracketedExample();
		}

		GenerateLSystem();
		DrawLSystem();
	}

	private void SetupDefaultBracketedExample()
	{
		axiom = "FX";
		rules = new LSystemRule[3];

		// Règle 1 : Croissance basique
		rules[0] = new LSystemRule
		{
			symbol = "F", replacement = "F+X-", probability = 1f
		};

		// Règle 2 : Ramification principale (Stochastique 70%)
		rules[1] = new LSystemRule
		{
			symbol = "X", replacement = "F[F+?X]-F", probability = 0.7f
		};

		// Règle 3 : Ramification alternative avec mutation (Stochastique 30%)
		rules[2] = new LSystemRule
		{
			symbol = "X", replacement = "F[-?X]+F", probability = 0.3f
		};

		iterations = 4;
		angle = 25f;
	}

	private void GenerateLSystem()
	{
		currentSentence = axiom;

		for (int i = 0; i < iterations; i++)
		{
			StringBuilder nextSentence = new StringBuilder();

			foreach (char c in currentSentence)
			{
				string symbolStr = c.ToString();
				List<LSystemRule> matchingRules = new List<LSystemRule>();

				// Récupération de toutes les règles applicables à ce symbole
				foreach (LSystemRule rule in rules)
				{
					if (rule.symbol == symbolStr)
					{
						matchingRules.Add(rule);
					}
				}

				if (matchingRules.Count > 0)
				{
					// Calcul stochastique (Loterie pondérée par les probabilités)
					float totalWeight = 0f;
					foreach (var r in matchingRules) totalWeight += r.probability;

					float randomVal = Random.Range(0f, totalWeight);
					float currentWeight = 0f;
					string selectedReplacement = matchingRules[0].replacement; // Securité

					foreach (var r in matchingRules)
					{
						currentWeight += r.probability;
						if (randomVal <= currentWeight)
						{
							selectedReplacement = r.replacement;
							break;
						}
					}

					nextSentence.Append(selectedReplacement);
				}
				else
				{
					// Si aucune règle, on garde le symbole d'origine (+, -, [, ], %, ?)
					nextSentence.Append(c);
				}
			}

			currentSentence = nextSentence.ToString();
		}

		Debug.Log($"Génération terminée : Itération {iterations}. Longueur du mot : {currentSentence.Length}");
	}

	private void DrawLSystem()
	{
		UnityEngine.Debug.Log(currentSentence);

		// Nettoyage des anciennes géométries si on relance
		foreach (GameObject obj in generatedGeometry)
		{
			Destroy(obj);
		}

		generatedGeometry.Clear();

		// Pile pour gérer les embranchements (Bracketed)
		Stack<TransformInfo> transformStack = new Stack<TransformInfo>();

		// Point de départ
		Vector3 currentPosition = transform.position;
		Quaternion currentRotation = transform.rotation;
		bool branchPruned = false; // Flag pour l'élagage Open L-System

		foreach (char c in currentSentence)
		{
			// Si la branche est morte (élaguée), on ignore la géométrie jusqu'à ce qu'on ressorte du crochet ]
			if (branchPruned && c != ']' && c != '[')
				continue;

			switch (c)
			{
				case 'F':
					// Croissance : on calcule la fin, on dessine, on avance
					Vector3 newPosition = currentPosition + currentRotation * Vector3.up * segmentLength;
					CreateSegment(currentPosition, newPosition);
					currentPosition = newPosition;
					break;

				case '+':
					currentRotation *= Quaternion.Euler(0, 0, -angle);
					break;

				case '-':
					currentRotation *= Quaternion.Euler(0, 0, angle);
					break;

				case '[':
					transformStack.Push(new TransformInfo
					{
						position = currentPosition,
						rotation = currentRotation,
						isPruned = branchPruned // Sauvegarde de l'état vital
					});
					break;

				case ']':
					if (transformStack.Count > 0)
					{
						TransformInfo ti = transformStack.Pop();
						currentPosition = ti.position;
						currentRotation = ti.rotation;
						branchPruned = ti.isPruned; // Restauration de l'état vital
					}

					break;

				case '%':
					// Open L-System : Symbole de coupe. Arrêt de la croissance sur cette branche.
					branchPruned = true;
					break;

				case '?':
					// Open L-System : Module de requête environnementale
					if (simulateEnvironment)
					{
						// Simulation d'une perte d'énergie/ombre
						if (Random.value <= environmentalStress)
						{
							branchPruned = true; // Déclenche un auto-élagage (%)
						}
					}

					break;

				case 'X':
					break;
			}
		}

		if (autoFrameCamera)
		{
			FrameCamera();
		}
	}

	private void CreateSegment(Vector3 start, Vector3 end)
	{
		// Création d'un cylindre primitif natif de Unity
		GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		segment.transform.SetParent(this.transform);

		// Retrait du collider pour ne pas surcharger la physique inutilement
		Destroy(segment.GetComponent<CapsuleCollider>());

		// Position au centre des deux points
		segment.transform.position = (start + end) / 2.0f;

		// Orientation alignée sur l'axe de croissance
		Vector3 direction = end - start;
		segment.transform.up = direction;

		// Mise à l'échelle (Unity Cylinder height = 2, donc on divise la longueur par 2)
		segment.transform.localScale = new Vector3(branchWidth, direction.magnitude / 2.0f, branchWidth);

		// Application du matériel si défini
		if (branchMaterial != null)
		{
			segment.GetComponent<MeshRenderer>().material = branchMaterial;
		}

		generatedGeometry.Add(segment);
	}

	private void FrameCamera()
	{
		Camera cam = Camera.main;
		if (cam == null || generatedGeometry.Count == 0) return;

		// Nettoyage visuel pour le schéma (fond uni à la place du ciel)
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = backgroundColor;

		// Calcul de la boîte de collision globale (Bounding Box)
		Bounds bounds = new Bounds(generatedGeometry[0].transform.position, Vector3.zero);
		foreach (GameObject obj in generatedGeometry)
		{
			Renderer renderer = obj.GetComponent<Renderer>();
			if (renderer != null)
			{
				bounds.Encapsulate(renderer.bounds);
			}
		}

		// Calcul trigonométrique du recul de la caméra selon son angle de vision (FOV)
		float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
		float distance = (maxExtent * 1.3f) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

		// Déplacement de la caméra sur l'axe Z et ciblage
		cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z - distance);
		cam.transform.LookAt(bounds.center);
	}
}