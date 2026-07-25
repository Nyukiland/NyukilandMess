using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Boids
{
    public class BoidManager : MonoBehaviour
    {
        public Mesh QuadMesh;
        public Material BoidMaterial; // Shader reads Matrix4x4 buffer
        public Transform Leader;
        public int BoidCount = 50000;
        public float PerceptionRadius = 3.0f;

        private NativeArray<Boid> _boids;
        private NativeParallelMultiHashMap<int, Boid> _spatialMap;
        private ComputeBuffer _matrixBuffer;
        private RenderParams _renderParams;
        private Matrix4x4[] _matrices;

        void Start()
        {
            _boids = new NativeArray<Boid>(BoidCount, Allocator.Persistent);
            _spatialMap = new NativeParallelMultiHashMap<int, Boid>(BoidCount, Allocator.Persistent);
            _matrices = new Matrix4x4[BoidCount];
            _matrixBuffer = new ComputeBuffer(BoidCount, sizeof(float) * 16);

            for (int i = 0; i < BoidCount; i++)
            {
                _boids[i] = new Boid
                {
                    Position = UnityEngine.Random.insideUnitSphere * 20f,
                    Velocity = UnityEngine.Random.onUnitSphere * 5f
                };
            }

            _renderParams = new RenderParams(BoidMaterial)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f)
            };

            BoidMaterial.SetBuffer("_Matrices", _matrixBuffer);
        }

        void Update()
        {
            _spatialMap.Clear();

            // 1. Fill Spatial Hash Grid (Parallel)
            var buildHashJob = new BuildSpatialHashJob
            {
                Boids = _boids,
                HashWriter = _spatialMap.AsParallelWriter(),
                CellSize = PerceptionRadius
            };
            JobHandle populateHandle = buildHashJob.Schedule(BoidCount, 64);

            // 2. Schedule Boid Movement Job (Depends on populateHandle)
            var boidJob = new BoidJob
            {
                SpatialMap = _spatialMap,
                Boids = _boids,
                CellSize = PerceptionRadius,
                DeltaTime = Time.deltaTime,
                TargetPosition = Leader != null ? Leader.position : Vector3.zero
            };

            JobHandle jobHandle = boidJob.Schedule(BoidCount, 64, populateHandle);
            
            // Block main thread until worker threads finish
            jobHandle.Complete(); 

            // 3. Prepare Transform Matrices for GPU
            for (int i = 0; i < BoidCount; i++)
            {
                Boid b = _boids[i];
                Quaternion rot = Quaternion.LookRotation(b.Velocity);
                _matrices[i] = Matrix4x4.TRS(b.Position, rot, Vector3.one);
            }

            // 4. Send directly to GPU and Draw
            _matrixBuffer.SetData(_matrices);
            Graphics.RenderMeshInstanced(_renderParams, QuadMesh, 0, _matrices, BoidCount);
        }

        void OnDestroy()
        {
            if (_boids.IsCreated) _boids.Dispose();
            if (_spatialMap.IsCreated) _spatialMap.Dispose();
            _matrixBuffer?.Release();
        }
    }
}