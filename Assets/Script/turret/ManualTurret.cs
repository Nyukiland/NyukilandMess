using UnityEngine;

public class ManualTurret : MonoBehaviour
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

	[Header("Debug Settings")]
	public bool showDebugGizmos = true;
	public int debugSteps = 30;
	public float minRange = 1f;

	private void Update()
	{
		if (!target || !baseTransform || !gunTransform || !muzzle)
			return;

		Vector3 targetPos = target.position;
		Vector3 muzzlePos = muzzle.position;
		Vector3 toTarget = targetPos - muzzlePos;

		// === 1. Horizontal rotation (Yaw) ===
		Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
		if (flatToTarget.sqrMagnitude < 0.01f) return;

		float currentYaw = baseTransform.eulerAngles.y;
		float targetYaw = Mathf.Atan2(flatToTarget.x, flatToTarget.z) * Mathf.Rad2Deg;
		float newYaw = MoveTowardsAngleCustom(currentYaw, targetYaw, maxRotationSpeed * Time.deltaTime);
		baseTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);

		// === 2. Recalculate target vector after yaw ===
		muzzlePos = muzzle.position;
		toTarget = targetPos - muzzlePos;
		float dx = new Vector2(toTarget.x, toTarget.z).magnitude;
		float dy = toTarget.y;

		float v2 = projectileSpeed * projectileSpeed;
		float g = -gravity;
		float discriminant = v2 * v2 - g * (g * dx * dx + 2 * dy * v2);

		if (discriminant < 0f || dx < minRange)
		{
			// Target unreachable or too close
			return;
		}

		float sqrtDisc = Mathf.Sqrt(discriminant);
		float lowAngle = Mathf.Atan((v2 - sqrtDisc) / (g * dx));
		float pitchDeg = Mathf.Rad2Deg * lowAngle;

		gunTransform.localRotation = Quaternion.Euler(-pitchDeg, 0f, 0f);
	}

	// === Angle Helpers ===
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

	// === Gizmo Debug ===
	private void OnDrawGizmos()
	{
		if (!showDebugGizmos || !muzzle) return;

		// Trajectory preview
		Vector3 pos = muzzle.position;
		Vector3 forward = muzzle.forward;
		float v = projectileSpeed;
		Vector3 vel = forward * v + Vector3.up * 0f;

		Gizmos.color = Color.red;
		float timeStep = 0.1f;
		for (int i = 0; i < debugSteps; i++)
		{
			Vector3 nextPos = pos + vel * timeStep + 0.5f * Physics.gravity * timeStep * timeStep;
			Gizmos.DrawLine(pos, nextPos);
			vel += Physics.gravity * timeStep;
			pos = nextPos;
		}

		// Portée maximale (parabole horizontale parfaite à 45°)
		float maxRange = (v * v) / -gravity;
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(muzzle.position, maxRange);

		// Zone trop proche
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(muzzle.position, minRange);
	}
}