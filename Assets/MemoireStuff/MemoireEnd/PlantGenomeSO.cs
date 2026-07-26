using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlantGenome", menuName = "Procedural/PlantGenome")]
public class PlantGenomeSO : ScriptableObject
{
    [Header("L-System Base")]
    public string initialAxiom = "X";
    // Format attendu: "X:F[+X]-X" (clé:valeur)
    public string[] productionRules; 
    
    [Header("Phenotype Base")]
    public float baseAngle = 25f;
    public float segmentLength = 0.5f;
    public int maxIterations = 4;

    [Header("Metabolism")]
    public float startingEnergy = 100f;
    public float segmentCost = 2f;
    public float leafEnergyProduction = 5f;

    [Header("Evolution")]
    public float mutationRate = 0.05f;
    public float structuralPenalty = 50f;
}

// Scripts/Data/PlantDNA.cs
[Serializable]
public struct PlantDNA
{
    public string axiom;
    public string[] rules;
    public float angle;
    public float segmentLength;

    // Constructeur d'initialisation depuis le SO
    public PlantDNA(PlantGenomeSO blueprint)
    {
        axiom = blueprint.initialAxiom;
        rules = (string[])blueprint.productionRules.Clone();
        angle = blueprint.baseAngle;
        segmentLength = blueprint.segmentLength;
    }

    // Crossover : Recombinaison génétique
    public PlantDNA Crossover(PlantDNA partner)
    {
        PlantDNA child = new PlantDNA
        {
            axiom = UnityEngine.Random.value > 0.5f ? this.axiom : partner.axiom,
            angle = Mathf.Lerp(this.angle, partner.angle, 0.5f),
            segmentLength = Mathf.Lerp(this.segmentLength, partner.segmentLength, 0.5f),
            rules = new string[this.rules.Length]
        };

        for (int i = 0; i < rules.Length; i++)
        {
            child.rules[i] = UnityEngine.Random.value > 0.5f ? this.rules[i] : partner.rules[i];
        }
        return child;
    }

    // Mutation stochastique
    public void Mutate(float mutationRate)
    {
        if (UnityEngine.Random.value < mutationRate)
        {
            // Bruit stochastique sur l'angle
            angle += UnityEngine.Random.Range(-5f, 5f);
        }
        // Des mutations plus complexes sur les chaînes de règles peuvent être ajoutées ici
    }
}