using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boids
{
    [BurstCompile(CompileSynchronously = true)]
    public struct BuildSpatialHashJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Boid> Boids;
        public NativeParallelMultiHashMap<int, Boid>.ParallelWriter HashWriter;
        public float CellSize;

        public void Execute(int index)
        {
            Boid b = Boids[index];
            int3 cell = (int3)math.floor(b.Position / CellSize);
            
            // Generate unique 1D key from 3D grid coordinate
            int key = (cell.x * 73856093) ^ (cell.y * 19349663) ^ (cell.z * 83492791);
            
            HashWriter.Add(key, b);
        }
    }
}
