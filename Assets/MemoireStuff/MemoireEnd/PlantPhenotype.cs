using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlantPhenotype : MonoBehaviour
{
	public GameObject segmentPrefab;
	public GameObject leafPrefab;
	public PlantGenomeSO genomeRef; // Réf pour les constantes (coûts, etc)
	public PlantDNA dna; // La donnée active mutée
	public LayerMask _obstacleLayer;

	// Tracking pour la Fitness
	[HideInInspector]
	public float currentEnergy;

	[HideInInspector]
	public float energyProduced;

	[HideInInspector]
	public float biomassCost;

	[HideInInspector]
	public int structuralFailures;
	
	[HideInInspector] 
	public Color plantColor = Color.green;

	private List<IPlantModule> _modules;
	private bool _isPruned = false;

	private struct TransformState
	{
		public Vector2 position;
		public Quaternion rotation;
	}

	public void Initialize(PlantGenomeSO blueprint, PlantDNA generatedDNA, Color assignedColor)
	{
		genomeRef = blueprint;
		dna = generatedDNA;
		currentEnergy = blueprint.startingEnergy;
		plantColor = assignedColor;

		energyProduced = 0;
		biomassCost = 0;
		structuralFailures = 0;

		// Injection des dépendances (Modules)
		_modules = new List<IPlantModule>
		{
			new EnergyModule(), 
			new LightModule(_obstacleLayer),
			new AuxinModule(_obstacleLayer)
			// Ajouter AuxinModule, StructuralModule ici...
		};
	}

	public void GrowAndRender()
	{
		string currentWord = GenerateLSystemWord();
		ExecuteTurtle(currentWord);
	}

	// Moteur de réécriture
	private string GenerateLSystemWord()
    {
       string word = dna.axiom;
       StringBuilder nextWord = new StringBuilder();

       // Dictionnaire groupant les règles par clé
       Dictionary<char, List<RuleOption>> ruleDict = new Dictionary<char, List<RuleOption>>();
       
       foreach (string rule in dna.rules)
       {
          string[] split = rule.Split(':');
          if (split.Length >= 2)
          {
             char key = split[0][0];
             string result = split[1];
             
             // Poids par défaut à 1 si non précisé. 
             // Format attendu: "X:F[+X]:0.5"
             float weight = 1.0f;
             if (split.Length == 3)
             {
                 float.TryParse(split[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight);
             }

             if (!ruleDict.ContainsKey(key))
             {
                ruleDict[key] = new List<RuleOption>();
             }
             ruleDict[key].Add(new RuleOption { result = result, weight = weight });
          }
       }

       for (int i = 0; i < genomeRef.maxIterations; i++)
       {
          nextWord.Clear();
          foreach (char c in word)
          {
             if (ruleDict.ContainsKey(c))
             {
                // Sélection stochastique (Roulette Wheel)
                List<RuleOption> options = ruleDict[c];
                float totalWeight = 0;
                
                // Optimisation: Pour une liste très courte, une boucle foreach est plus rapide que LINQ
                for (int j = 0; j < options.Count; j++) 
                {
                    totalWeight += options[j].weight;
                }

                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float current = 0;
                string selectedResult = options[options.Count - 1].result; // Fallback de sécurité

                for (int j = 0; j < options.Count; j++)
                {
                   current += options[j].weight;
                   if (roll <= current)
                   {
                      selectedResult = options[j].result;
                      break;
                   }
                }
                
                nextWord.Append(selectedResult);
             }
             else
             {
                nextWord.Append(c);
             }
          }

          word = nextWord.ToString();
       }

       return word;
    }

	// Évaluateur physique (Turtle Graphics)
	private void ExecuteTurtle(string initialWord)
	{
		// On utilise un StringBuilder pour pouvoir modifier le mot en cours de lecture
		StringBuilder word = new StringBuilder(initialWord);
		Stack<TransformState> stack = new Stack<TransformState>();

		Vector2 currentPosition = transform.position;
		Quaternion currentRotation = transform.rotation;

		for (int i = 0; i < word.Length; i++)
		{
			// 1. GESTION DE LA COUPE ET REPOUSSE
			if (_isPruned)
			{
				if (stack.Count > 0)
				{
					// Nous sommes dans une branche. On extrait le potentiel de croissance restant.
					int depth = 1;
					int j = i;
					StringBuilder skippedString = new StringBuilder();

					// On scanne jusqu'à trouver le crochet fermant de cette branche
					while (j < word.Length && depth > 0)
					{
						if (word[j] == '[') depth++;
						else if (word[j] == ']') depth--;

						if (depth > 0) skippedString.Append(word[j]);
						j++;
					}

					// On injecte ce potentiel à la FIN du mot global, encadré de crochets.
					if (skippedString.Length > 0)
					{
						// Détermine aléatoirement une nouvelle direction de pousse agressive
						// '++' ou '--' force un angle double pour s'éloigner du tronc principal
						string newDirection = UnityEngine.Random.value > 0.5f ? "++" : "--";
         
						word.Append("[")
							.Append(newDirection)
							.Append(skippedString)
							.Append("]");
					}

					// On avance l'index de lecture pour sauter la branche morte
					i = j - 1;
				}
				else
				{
					// Le tronc principal est coupé, la plante meurt
					break;
				}

				_isPruned = false;
				continue; // Passe au caractère suivant
			}

			// 2. ÉVALUATION STANDARD
			char c = word[i];
			switch (c)
			{
				case 'F':
					Vector2 nextPosition =
						currentPosition + (Vector2)(currentRotation * Vector3.up * dna.segmentLength);
					RaycastHit2D hit = Physics2D.Linecast(currentPosition, nextPosition, _obstacleLayer);

					if (hit.collider != null)
					{
						DrawSegment(currentPosition, hit.point);
						currentPosition = hit.point;
						_isPruned = true; // Activera le bloc de repousse à la prochaine itération
						structuralFailures++;
					}
					else
					{
						DrawSegment(currentPosition, nextPosition);
						currentPosition = nextPosition;
					}

					break;
				case '+':
					currentRotation *= Quaternion.Euler(0, 0, -dna.angle);
					break;
				case '-':
					currentRotation *= Quaternion.Euler(0, 0, dna.angle);
					break;
				case '[':
					stack.Push(new TransformState
					{
						position = currentPosition, rotation = currentRotation
					});
					break;
				case ']':
					if (stack.Count > 0)
					{
						TransformState state = stack.Pop();
						currentPosition = state.position;
						currentRotation = state.rotation;
					}

					_isPruned = false; // Sécurité
					break;
				case 'E':
					// Le module Auxine peut maintenant altérer currentRotation
					RunModules(currentPosition, ref currentRotation);
					SpawnLeaf(currentPosition, currentRotation);
					break;
				case '%':
					structuralFailures++;
					_isPruned = true;
					break;
			}
		}
	}

	private void RunModules(Vector2 pos, ref Quaternion rot)
	{
		foreach (var module in _modules)
		{
			module.Evaluate(this, pos, ref rot);
		}
	}

	private void SpawnLeaf(Vector2 position, Quaternion rotation)
	{
		if (leafPrefab != null)
		{
			// Instancie la feuille à la position du capteur, orientée selon la direction de la branche
			GameObject leaf = Instantiate(leafPrefab, position, rotation, this.transform);
        
			// Ajuste l'échelle (à adapter selon la taille de ton sprite de base)
			leaf.transform.localScale = new Vector3(0.2f, 0.2f, 1f);

			// Optionnel : teinter légèrement la feuille selon la hauteur ou l'énergie
			SpriteRenderer sr = leaf.GetComponent<SpriteRenderer>();
			if (sr != null)
			{
				sr.color = plantColor;
			}
		}
	}
	
	public void TriggerPruning()
	{
		_isPruned = true;
	}

	private void DrawSegment(Vector2 start, Vector2 end)
	{
		// Instantiate the segment
		GameObject segment = Instantiate(segmentPrefab, start, Quaternion.identity, this.transform);

		// Calculate rotation and scale
		Vector2 direction = end - start;
		float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		segment.transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90 assumes sprite faces up

		// Scale the sprite to match the segment length and give it a stem thickness
		segment.transform.localScale = new Vector3(0.1f, direction.magnitude, 1f);

		// Optional: Color gradient based on energy or height
		SpriteRenderer sr = segment.GetComponent<SpriteRenderer>();
		sr.color = plantColor;
	}

	// Calcul de la fonction objectif de Monod/Goldberg
	public float GetFitness()
	{
		float totalCost = biomassCost + (structuralFailures * genomeRef.structuralPenalty);
		if (totalCost == 0) return 0;

		return energyProduced / totalCost;
	}
	
	private struct RuleOption
	{
		public string result;
		public float weight;
	}
}