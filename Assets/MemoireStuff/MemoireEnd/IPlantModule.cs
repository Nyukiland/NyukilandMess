using UnityEngine;

public interface IPlantModule
{
    void Evaluate(PlantPhenotype plant, Vector2 position, ref Quaternion rotation);
}

// Scripts/Modules/LightModule.cs
public class LightModule : IPlantModule
{
    private LayerMask obstacleLayer;

    public LightModule(LayerMask layer)
    {
        obstacleLayer = layer;
    }

    public void Evaluate(PlantPhenotype plant, Vector2 position, ref Quaternion rotation)
    {
        // Raycast vers le haut (Soleil)
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.up, 10f, obstacleLayer);
        
        if (hit.collider == null)
        {
            // Pleine lumière : on ajoute de l'énergie produite
            plant.energyProduced += plant.genomeRef.leafEnergyProduction;
        }
        else
        {
            // Occlusion : pénalité ou production réduite
            plant.energyProduced += (plant.genomeRef.leafEnergyProduction * 0.1f);
        }
    }
}

// Scripts/Modules/EnergyModule.cs
public class EnergyModule : IPlantModule
{
    public void Evaluate(PlantPhenotype plant, Vector2 position, ref Quaternion rotation)
    {
        // Check métabolique: A-t-on l'énergie pour croître ici ?
        if (plant.currentEnergy < plant.genomeRef.segmentCost)
        {
            // Déclenche un auto-élagage forcé (mécanique interne)
            plant.TriggerPruning(); 
        }
        else
        {
            plant.currentEnergy -= plant.genomeRef.segmentCost;
            plant.biomassCost += plant.genomeRef.segmentCost;
        }
    }
}

public class AuxinModule : IPlantModule
{
    private LayerMask _obstacleLayer;

    public AuxinModule(LayerMask layer)
    {
        _obstacleLayer = layer;
    }

    public void Evaluate(PlantPhenotype plant, Vector2 position, ref Quaternion rotation)
    {
        // Calcule des vecteurs directeurs : Face, Gauche, Droite
        Vector2 forward = rotation * Vector3.up;
        Vector2 left = rotation * Quaternion.Euler(0, 0, 15f) * Vector3.up;
        Vector2 right = rotation * Quaternion.Euler(0, 0, -15f) * Vector3.up;

        // Raycasts très courts pour "sentir" l'espace immédiat
        bool hitLeft = Physics2D.Raycast(position, left, 1.5f, _obstacleLayer);
        bool hitRight = Physics2D.Raycast(position, right, 1.5f, _obstacleLayer);

        // Si bloqué à gauche mais libre à droite -> Torsion vers la droite
        if (hitLeft && !hitRight)
        {
            rotation *= Quaternion.Euler(0, 0, -15f);
        }
        // Si bloqué à droite mais libre à gauche -> Torsion vers la gauche
        else if (hitRight && !hitLeft)
        {
            rotation *= Quaternion.Euler(0, 0, 15f);
        }
        // Si totalement libre -> Phototropisme pur (redressement vers le haut absolu)
        else if (!hitLeft && !hitRight)
        {
            float angleToUp = Vector2.SignedAngle(forward, Vector2.up);
            // La plante se redresse doucement de 20% vers le zénith
            rotation *= Quaternion.Euler(0, 0, angleToUp * 0.2f); 
        }
    }
}
