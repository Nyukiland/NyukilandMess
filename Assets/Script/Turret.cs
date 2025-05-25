using UnityEngine;

public class Turret : MonoBehaviour
{
	public Transform target;
	public Transform muzzle;
	public float rotationSpeed = 180f; // degrees per second
	public float projectileSpeed = 10f;
	public float gravity = -9.81f;

	[Header("Prediction")]
	public bool usePrediction = true;
	public Vector3 targetVelocity;

	void Update()
	{
		if (!target) return;

		Vector3 aimPoint = usePrediction ?
			PredictInterceptPoint(target.position, targetVelocity, muzzle.position, projectileSpeed) :
			target.position;

		Vector3 direction = aimPoint - transform.position;
		Quaternion targetRot = Quaternion.LookRotation(direction);
		transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
	}

	// Basic 3D interception (not accounting for gravity)
	Vector3 PredictInterceptPoint(Vector3 targetPos, Vector3 targetVel, Vector3 origin, float projSpeed)
	{
		Vector3 toTarget = targetPos - origin;
		float a = Vector3.Dot(targetVel, targetVel) - projSpeed * projSpeed;
		float b = 2 * Vector3.Dot(toTarget, targetVel);
		float c = Vector3.Dot(toTarget, toTarget);
		float discriminant = b * b - 4 * a * c;

		if (discriminant < 0 || Mathf.Abs(a) < 0.001f)
			return targetPos;

		float t = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
		t = Mathf.Max(0, t);
		return targetPos + targetVel * t;
	}
}