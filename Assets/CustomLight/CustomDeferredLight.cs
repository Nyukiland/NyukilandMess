using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CustomDeferredLight : MonoBehaviour
{
	public Color color = Color.white;
	public float intensity = 1.0f;
	public float range = 5.0f;

	private MaterialPropertyBlock propertyBlock;
	private Matrix4x4 matrix;
	private Vector3 lastPosition;
	private float lastRange;

	private void OnEnable()
	{
		StartCoroutine(WaitForRegister());
	}

	IEnumerator WaitForRegister()
	{
		yield return new WaitForSeconds(0.1f);

		CustomDeferredLightRenderer.Instance.RegisterLight(this);
	}

	private void OnDisable()
	{
		CustomDeferredLightRenderer.Instance.UnregisterLight(this);
	}

	public MaterialPropertyBlock GetPropertyBlock()
	{
		if (propertyBlock == null)
		{
			propertyBlock = new MaterialPropertyBlock();
		}
		else
		{
			propertyBlock.Clear();
		}

		propertyBlock.SetColor("_LightColor", color * intensity);
		propertyBlock.SetFloat("_LightRange", range);

		return propertyBlock;
	}

	public Matrix4x4 GetTransformMatrix()
	{
		if (lastPosition != transform.position || lastRange != range)
		{
			matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * range * 2);
			lastPosition = transform.position;
			lastRange = range;
		}

		return matrix;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = color;
		Gizmos.DrawWireSphere(transform.position, range);
	}
}
