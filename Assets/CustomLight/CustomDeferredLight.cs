using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CustomDeferredLight : MonoBehaviour
{
	[SerializeField]
	Color _color = Color.white;
	[SerializeField]
	float _intensity = 1.0f;
	[SerializeField]
	float _range = 5.0f;
	[SerializeField]
	Mesh _mesh;
	[SerializeField]
	bool _displayGizmo = true;

	// Use the new "DeferredLightWithEffect" shader for the custom light
	[SerializeField]
	Shader _lightShader;

	MaterialPropertyBlock _propertyBlock;
	Matrix4x4 _matrix;
	Vector3 _lastPosition;
	float _lastRange;

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
		if (_propertyBlock == null)
		{
			_propertyBlock = new MaterialPropertyBlock();
		}
		else
		{
			_propertyBlock.Clear();
		}

		_propertyBlock.SetColor("_LightColor", _color * _intensity);
		_propertyBlock.SetFloat("_LightRange", _range);

		return _propertyBlock;
	}

	public Matrix4x4 GetTransformMatrix()
	{
		if (_lastPosition != transform.position || _lastRange != _range)
		{
			_matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one * _range * 2);
			_lastPosition = transform.position;
			_lastRange = _range;
		}

		return _matrix;
	}

	public Mesh GetMesh()
	{
		return _mesh;
	}

	public Shader GetLightShader()
	{
		return _lightShader;
	}

	private void OnDrawGizmos()
	{
		if (!_displayGizmo) return;

		Gizmos.color = _color;
		Gizmos.DrawWireMesh(_mesh, 0, transform.position, transform.rotation, Vector3.one * _range * 2);
	}
}
