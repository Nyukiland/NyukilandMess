using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boids
{
    [BurstCompile(CompileSynchronously = true)]
    public struct BoidJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeParallelMultiHashMap<int, Boid> SpatialMap;

        public NativeArray<Boid> Boids; // Contiguous in memory (NativeArray = raw C++ pointer)
        public float CellSize;
        public float DeltaTime;
        public float3 TargetPosition;

        public void Execute(int index)
        {
            Boid boid = Boids[index];
            float3 currentPos = boid.Position;
            int3 currentCell = (int3)math.floor(currentPos / CellSize);

            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesionCenter = float3.zero;
            int neighborCount = 0;

            // Query 27 neighboring grid cells
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        int key = SpatialHash(currentCell + new int3(x, y, z));

                        if (SpatialMap.TryGetFirstValue(key, out Boid neighbor, out var it))
                        {
                            do
                            {
                                float3 distVec = currentPos - neighbor.Position;
                                float distSq = math.lengthsq(distVec);

                                if (distSq > 0.0001f && distSq < CellSize * CellSize)
                                {
                                    separation += distVec / distSq;
                                    alignment += neighbor.Velocity;
                                    cohesionCenter += neighbor.Position;
                                    neighborCount++;
                                }
                            } while (SpatialMap.TryGetNextValue(out neighbor, ref it));
                        }
                    }
                }
            }

            // Apply Forces
            float3 accel = (TargetPosition - currentPos) * 0.5f; // Simple leader seek
            if (neighborCount > 0)
            {
                accel += math.normalizesafe(separation) * 1.5f;
                accel += math.normalizesafe(alignment / neighborCount - boid.Velocity) * 1.0f;
                accel += math.normalizesafe((cohesionCenter / neighborCount) - currentPos) * 1.0f;
            }

            // Integrate
            boid.Velocity += accel * DeltaTime;
            boid.Velocity = math.normalizesafe(boid.Velocity) * 5.0f; // Speed = 5
            boid.Position += boid.Velocity * DeltaTime;

            // Write back to contiguous array
            Boids[index] = boid;
        }

        private static int SpatialHash(int3 cell)
        {
            unchecked
            {
                return (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
            }
        }
    }

    public struct Boid
	{
		public float3 Position;
		public float3 Velocity;
	}
}