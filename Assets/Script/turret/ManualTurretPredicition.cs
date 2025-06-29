using UnityEngine;

public class ManualTurretPredicition : MonoBehaviour
{
	[Header("References")]
	public Transform target;
	public Transform baseTransform;
	public Transform gunTransform;
	public Transform muzzle;

	[Header("Projectile Settings")]
	public float projectileSpeed = 10f;
	public float gravity = -9.81f;

	[Header("Rotation Settings")]
	public float maxRotationSpeed = 180f;

	[Header("Prediction")]
	public Vector3 targetVelocity;

	[Header("Debug Settings")]
	public bool showDebugGizmos = true;
	public int debugSteps = 30;
	public float minRange = 1f;

	private Vector3 predictedPosition;

	void Update()
	{
		if (!target || !baseTransform || !gunTransform || !muzzle) return;

		Vector3 muzzlePos = muzzle.position;

		// === 1. Estimation du temps d’interception (distance / vitesse) ===
		float distanceToTarget = Vector3.Distance(muzzlePos, target.position);
		float estimatedTime = distanceToTarget / projectileSpeed;

		// === 2. Calcul de la position prédite ===
		predictedPosition = target.position + targetVelocity * estimatedTime;

		// === 3. Vecteur vers la position prédite ===
		Vector3 toTarget = predictedPosition - muzzlePos;

		// === 4. Yaw (rotation horizontale) ===
		Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
		if (flatToTarget.sqrMagnitude < 0.01f) return;

		float currentYaw = baseTransform.eulerAngles.y;
		float targetYaw = Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
		float newYaw = MoveTowardsAngleCustom(currentYaw, targetYaw, maxRotationSpeed * Time.deltaTime);
		baseTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);

		// === 5. Recalcul du tir après rotation base ===
		muzzlePos = muzzle.position;
		toTarget = predictedPosition - muzzlePos;
		float dx = new Vector2(toTarget.x, toTarget.z).magnitude;
		float dy = toTarget.y;

		float v2 = projectileSpeed * projectileSpeed;
		float g = -gravity;
		float discriminant = v2 * v2 - g * (g * dx * dx + 2 * dy * v2);

		if (discriminant < 0f || dx < minRange)
			return;

		float sqrtDisc = Mathf.Sqrt(discriminant);
		float lowAngle = Mathf.Atan((v2 - sqrtDisc) / (g * dx));
		float pitchDeg = Mathf.Rad2Deg * lowAngle;

		gunTransform.localRotation = Quaternion.Euler(-pitchDeg, 0f, 0f);
	}

	// === CUSTOM ANGLE HELPERS ===

	float DeltaAngleCustom(float current, float target)
	{
		float delta = (target - current + 540f) % 360f - 180f;
		return delta;
	}

	float MoveTowardsAngleCustom(float current, float target, float maxDelta)
	{
		float delta = DeltaAngleCustom(current, target);
		if (Mathf.Abs(delta) <= maxDelta)
			return target;
		return current + Mathf.Sign(delta) * maxDelta;
	}

	// === DEBUG GIZMOS ===

	private void OnDrawGizmos()
	{
		Vector3 pos = muzzle.position;
		Vector3 toPredicted = predictedPosition - pos;
		float dx = new Vector2(toPredicted.x, toPredicted.z).magnitude;
		float dy = toPredicted.y;

		float v2 = projectileSpeed * projectileSpeed;
		float g = -gravity;
		float discriminant = v2 * v2 - g * (g * dx * dx + 2 * dy * v2);

		if (discriminant < 0f || dx < minRange)
			return;

		float sqrtDisc = Mathf.Sqrt(discriminant);
		float lowAngle = Mathf.Atan((v2 - sqrtDisc) / (g * dx));

		// Vector direction
		Vector3 dir = (new Vector3(toPredicted.x, 0f, toPredicted.z)).normalized;
		Vector3 shootDir = Quaternion.Euler(-Mathf.Rad2Deg * lowAngle, 0f, 0f) * dir * projectileSpeed;

		// Trajectory
		Gizmos.color = Color.red;
		float timeStep = 0.1f;
		Vector3 vel = shootDir;
		Vector3 p = pos;
		for (int i = 0; i < debugSteps; i++)
		{
			Vector3 nextPos = p + vel * timeStep + 0.5f * Physics.gravity * timeStep * timeStep;
			Gizmos.DrawLine(p, nextPos);
			vel += Physics.gravity * timeStep;
			p = nextPos;
		}

		// Portée maximale
		float maxRange = (projectileSpeed * projectileSpeed) / -gravity;
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(muzzle.position, maxRange);

		// Zone trop proche
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(muzzle.position, minRange);
	}
}
