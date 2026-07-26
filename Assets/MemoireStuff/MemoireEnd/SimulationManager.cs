using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    [Header("Dependencies")]
    public PlantGenomeSO baseGenome;
    public GameObject plantPrefab; // Prefab contenant le script PlantPhenotype

    [Header("Simulation Params")]
    public int populationSize = 20;
    public int generations = 50;
    public float spawnSpacing = 2f;

    private List<PlantDNA> _populationDNA;
    private List<PlantPhenotype> _activePhenotypes;

    private void Start()
    {
        InitializePopulation();
        StartCoroutine(EvolutionLoop());
    }

    private void InitializePopulation()
    {
        _populationDNA = new List<PlantDNA>();
        for (int i = 0; i < populationSize; i++)
        {
            PlantDNA dna = new PlantDNA(baseGenome);
        
            if (i > 0) 
            {
                dna.Mutate(1.0f);
            }
        
            _populationDNA.Add(dna);
        }
    }

    private System.Collections.IEnumerator EvolutionLoop()
    {
        for (int gen = 0; gen < generations; gen++)
        {
            Debug.Log($"--- Generation {gen} ---");
            _activePhenotypes = new List<PlantPhenotype>();

            // 1. Instanciation & Croissance (Phénotypes)
            for (int i = 0; i < populationSize; i++)
            {
                Vector2 spawnPos = new Vector2(i * spawnSpacing, 0);
                GameObject go = Instantiate(plantPrefab, spawnPos, Quaternion.identity);
                
                Color plantColor;
                if (i == 0)
                {
                    plantColor = new Color(0.1f, 0.8f, 0.2f);
                }
                else
                {
                    // Random HSV: full hue spectrum, high saturation and value for visibility
                    plantColor = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.8f, 1f);
                }
                
                PlantPhenotype plant = go.GetComponent<PlantPhenotype>();
                plant.Initialize(baseGenome, _populationDNA[i], plantColor);
                plant.GrowAndRender();
                
                _activePhenotypes.Add(plant);
            }

            // Attente pour visualiser le résultat (Optionnel)
            yield return new WaitForSeconds(2f);

            // 2. Évaluation (Nécessité Darwinienne)
            // On associe chaque ADN à son score de Fitness
            var evaluatedPopulation = _activePhenotypes
                .Select(p => new { DNA = p.dna, Fitness = p.GetFitness() })
                .OrderByDescending(x => x.Fitness)
                .ToList();

            Debug.Log($"Best Fitness: {evaluatedPopulation.First().Fitness}");

            // 3. Destruction des phénotypes (Garbage Collection)
            foreach (var plant in _activePhenotypes)
            {
                Destroy(plant.gameObject);
            }

            // 4. Sélection & Reproduction (sur les structures RAM uniquement)
            List<PlantDNA> newPopulation = new List<PlantDNA>();

            // Élitisme (Garder le meilleur)
            newPopulation.Add(evaluatedPopulation[0].DNA);

            // Crossover & Mutation
            while (newPopulation.Count < populationSize)
            {
                // Sélection basique (prendre dans le top 50%)
                PlantDNA parentA = evaluatedPopulation[Random.Range(0, populationSize / 2)].DNA;
                PlantDNA parentB = evaluatedPopulation[Random.Range(0, populationSize / 2)].DNA;

                PlantDNA child = parentA.Crossover(parentB);
                child.Mutate(baseGenome.mutationRate);

                newPopulation.Add(child);
            }

            _populationDNA = newPopulation;
        }

        SaveBestGenome();
    }

    private void SaveBestGenome()
    {
        // Extraction du meilleur et sérialisation dans le SO
        PlantDNA bestDNA = _populationDNA[0];
        baseGenome.initialAxiom = bestDNA.axiom;
        baseGenome.productionRules = bestDNA.rules;
        baseGenome.baseAngle = bestDNA.angle;
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(baseGenome);
        UnityEditor.AssetDatabase.SaveAssets();
        Debug.Log("Évolution terminée. Genome sauvegardé sur le disque.");
        #endif
    }
}
