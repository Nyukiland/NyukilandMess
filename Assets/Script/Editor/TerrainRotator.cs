using UnityEngine;
using UnityEditor;

public class TerrainRotator : MonoBehaviour
{
	[MenuItem("Game Tools/Rotate Terrain")]
	static void RotateTerrain()
	{
		float angle = 38f; // Change this to any angle
		Terrain terrain = Terrain.activeTerrain;
		if (terrain == null)
		{
			Debug.LogError("No active terrain found!");
			return;
		}

		TerrainData td = terrain.terrainData;

		// Rotate heightmap
		float[,] heights = td.GetHeights(0, 0, td.heightmapResolution, td.heightmapResolution);
		td.SetHeights(0, 0, RotateHeightmap(heights, angle));

		// Rotate splatmaps
		float[,,] alphaMaps = td.GetAlphamaps(0, 0, td.alphamapResolution, td.alphamapResolution);
		td.SetAlphamaps(0, 0, RotateSplatmap(alphaMaps, angle));

		// Rotate trees
		RotateTrees(td, angle);

		terrain.Flush();
		Debug.Log("Terrain rotated by " + angle + " degrees!");
	}

	static float[,] RotateHeightmap(float[,] heights, float angleDegrees)
	{
		int width = heights.GetLength(0);
		int height = heights.GetLength(1);
		float[,] newHeights = new float[width, height];

		float centerX = width / 2f;
		float centerY = height / 2f;
		float angleRad = Mathf.Deg2Rad * angleDegrees;

		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				// Compute rotated coordinates
				float newX = Mathf.Cos(angleRad) * (x - centerX) - Mathf.Sin(angleRad) * (y - centerY) + centerX;
				float newY = Mathf.Sin(angleRad) * (x - centerX) + Mathf.Cos(angleRad) * (y - centerY) + centerY;

				// Bilinear interpolation for smooth rotation
				newHeights[x, y] = BilinearSample(heights, newX, newY);
			}
		}
		return newHeights;
	}

	static float BilinearSample(float[,] heights, float x, float y)
	{
		int width = heights.GetLength(0);
		int height = heights.GetLength(1);

		int x0 = Mathf.Clamp((int)x, 0, width - 1);
		int x1 = Mathf.Clamp(x0 + 1, 0, width - 1);
		int y0 = Mathf.Clamp((int)y, 0, height - 1);
		int y1 = Mathf.Clamp(y0 + 1, 0, height - 1);

		float sx = x - x0;
		float sy = y - y0;

		float v0 = Mathf.Lerp(heights[x0, y0], heights[x1, y0], sx);
		float v1 = Mathf.Lerp(heights[x0, y1], heights[x1, y1], sx);
		return Mathf.Lerp(v0, v1, sy);
	}

	static float[,,] RotateSplatmap(float[,,] splatmap, float angleDegrees)
	{
		int width = splatmap.GetLength(0);
		int height = splatmap.GetLength(1);
		int layers = splatmap.GetLength(2);
		float[,,] newSplatmap = new float[width, height, layers];

		for (int l = 0; l < layers; l++)
		{
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					float newX = Mathf.Cos(angleDegrees) * x - Mathf.Sin(angleDegrees) * y;
					float newY = Mathf.Sin(angleDegrees) * x + Mathf.Cos(angleDegrees) * y;
					newSplatmap[x, y, l] = BilinearSample(splatmap, newX, newY, l);
				}
			}
		}
		return newSplatmap;
	}

	static float BilinearSample(float[,,] splatmap, float x, float y, int layer)
	{
		int width = splatmap.GetLength(0);
		int height = splatmap.GetLength(1);

		int x0 = Mathf.Clamp((int)x, 0, width - 1);
		int x1 = Mathf.Clamp(x0 + 1, 0, width - 1);
		int y0 = Mathf.Clamp((int)y, 0, height - 1);
		int y1 = Mathf.Clamp(y0 + 1, 0, height - 1);

		float sx = x - x0;
		float sy = y - y0;

		float v0 = Mathf.Lerp(splatmap[x0, y0, layer], splatmap[x1, y0, layer], sx);
		float v1 = Mathf.Lerp(splatmap[x0, y1, layer], splatmap[x1, y1, layer], sx);
		return Mathf.Lerp(v0, v1, sy);
	}

	static void RotateTrees(TerrainData td, float angleDegrees)
	{
		TreeInstance[] trees = td.treeInstances;
		for (int i = 0; i < trees.Length; i++)
		{
			trees[i].position = RotatePoint(trees[i].position, angleDegrees);
		}
		td.treeInstances = trees;
	}

	static Vector3 RotatePoint(Vector3 point, float angleDegrees)
	{
		float angleRad = Mathf.Deg2Rad * angleDegrees;
		float sinA = Mathf.Sin(angleRad);
		float cosA = Mathf.Cos(angleRad);

		float x = cosA * point.x - sinA * point.z;
		float z = sinA * point.x + cosA * point.z;

		return new Vector3(x, point.y, z);
	}
}
