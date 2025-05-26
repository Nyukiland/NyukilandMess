using UnityEngine;

public class SimplifiedTurret : MonoBehaviour
{
    public Transform target;
    public Transform baseTransform;   // pivote sur Y
    public Transform gunTransform;    // pivote sur X (parent: baseTransform)
    public Transform muzzle;          // point de sortie

    public float projectileSpeed = 10f;
    public float rotationSpeed = 180f;
    public bool usePrediction = false;
    public Vector3 targetVelocity;

    void Update()
    {
        if (!target || !baseTransform || !gunTransform || !muzzle)
            return;

        // === 1. Prédiction simple (distance / vitesse) ===
        Vector3 predictedPosition = target.position;
        if (usePrediction)
        {
            float distance = Vector3.Distance(muzzle.position, target.position);
            float estimatedTime = distance / projectileSpeed;
            predictedPosition += targetVelocity * estimatedTime;
        }

        // === 2. Direction vers la cible ou la position prédite ===
        Vector3 direction = predictedPosition - muzzle.position;

        // === 3. Rotation horizontale : uniquement Y (base) ===
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetYaw = Quaternion.LookRotation(flatDirection);
            baseTransform.rotation = Quaternion.RotateTowards(
                baseTransform.rotation,
                targetYaw,
                rotationSpeed * Time.deltaTime
            );
        }

        // === 4. Rotation verticale : gun regarde directement la cible ===
        Quaternion targetPitch = Quaternion.LookRotation(direction);
        gunTransform.rotation = Quaternion.RotateTowards(
            gunTransform.rotation,
            targetPitch,
            rotationSpeed * Time.deltaTime
        );
    }
}