using UnityEngine;

[RequireComponent(typeof(Renderer))]
[ExecuteInEditMode]
public class WaterLightEffect : MonoBehaviour
{
	[SerializeField]
	private Transform lightObject;  // The object whose position will act as the light position

	// Update is called once per frame
	void Update()
	{
		GetComponent<Renderer>().material.SetVector("_LightPosition", new Vector4(-lightObject.transform.localPosition.x+5, -lightObject.transform.localPosition.z+ 5, 0, 0));
	}
}
