using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class CustomDeferredLightRenderer : MonoBehaviour
{
	public static CustomDeferredLightRenderer Instance { get; private set; }

	private Dictionary<Camera, CommandBuffer> commandBuffers = new Dictionary<Camera, CommandBuffer>();
	private HashSet<CustomDeferredLight> lights = new HashSet<CustomDeferredLight>();

	private void OnEnable()
	{
		if (Instance != null)
		{
			Destroy(this);
			return;
		}

		Instance = this;
		Camera.onPreCull += UpdateCommandBuffer;
	}

	private void OnDisable()
	{
		Camera.onPreCull -= UpdateCommandBuffer;
		CleanupCommandBuffers();
	}

	public void RegisterLight(CustomDeferredLight light)
	{
		lights.Add(light);
	}

	public void UnregisterLight(CustomDeferredLight light)
	{
		lights.Remove(light);
	}

	private void UpdateCommandBuffer(Camera cam)
	{
		if (!commandBuffers.TryGetValue(cam, out CommandBuffer commandBuffer))
		{
			commandBuffer = new CommandBuffer { name = "CustomDeferredLights" };
			cam.AddCommandBuffer(CameraEvent.AfterLighting, commandBuffer);
			commandBuffers[cam] = commandBuffer;
		}

		commandBuffer.Clear();
		foreach (var light in lights)
		{
			var materialPropertyBlock = light.GetPropertyBlock();
			Shader lightShader = light.GetLightShader(); // Get the shader assigned to this light

			Material lightMaterial = new Material(lightShader);
			commandBuffer.DrawMesh(light.GetMesh(), light.GetTransformMatrix(), lightMaterial, 0, -1, materialPropertyBlock);
		}
	}

	private void CleanupCommandBuffers()
	{
		foreach (var camBufferPair in commandBuffers)
		{
			if (camBufferPair.Key != null)
			{
				camBufferPair.Key.RemoveCommandBuffer(CameraEvent.AfterLighting, camBufferPair.Value);
			}
		}

		commandBuffers.Clear();
	}
}
