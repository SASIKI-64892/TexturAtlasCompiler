﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using net.rs64.TexTransCore;
using net.rs64.TexTransCoreEngineForUnity;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace net.rs64.TexTransTool.Decal
{
    public class MeshData : IDisposable
    {
        internal readonly Renderer ReferenceRenderer;
        internal NativeArray<Vector3> _vertices;
        internal NativeArray<Vector3> Vertices
        {
            get
            {
                _jobHandle.Complete();
                return _vertices;
            }
        }

        internal NativeArray<Vector2> VertexUV;

        internal JobHandle _jobHandle, _destroyJobHandle;
        internal readonly NativeArray<TriangleIndex>[] _triangles;
        internal NativeArray<TriangleIndex>[] TriangleIndex
        {
            get
            {
                _jobHandle.Complete();
                return _triangles;
            }
        }

        internal readonly NativeArray<Triangle>[] _trianglePos;
        internal NativeArray<Triangle>[] Triangles
        {
            get
            {
                _jobHandle.Complete();
                return _trianglePos;
            }
        }

        internal readonly NativeArray<Triangle> _combinedTriangles;
        internal NativeArray<Triangle> CombinedTriangles
        {
            get
            {
                _jobHandle.Complete();
                return _combinedTriangles;
            }
        }

        internal readonly NativeArray<TriangleIndex> _combinedTriangleIndex;
        internal NativeArray<TriangleIndex> CombinedTriangleIndex
        {
            get
            {
                _jobHandle.Complete();
                return _combinedTriangleIndex;
            }
        }

        internal readonly NativeArray<(int, int)> _combinedTriangleToSubmeshIndexAndOffset;
        internal NativeArray<(int, int)> CombinedTriangleToSubmeshIndexAndOffset
        {
            get
            {
                _jobHandle.Complete();
                return _combinedTriangleToSubmeshIndexAndOffset;
            }
        }

        private NativeArray<TexTransUnityAABB> _calcAABBBuffer;
        private JobHandle _calcAABBJobHandle;
        internal TexTransUnityAABB? _axisAlignedBoundingBox;
        internal TexTransUnityAABB AxisAlignedBoundingBox
        {
            get
            {
                if (_axisAlignedBoundingBox is null) { _calcAABBJobHandle.Complete(); _axisAlignedBoundingBox = _calcAABBBuffer[0]; }
                return _axisAlignedBoundingBox.Value;
            }
        }

        public void Dispose()
        {
            _jobHandle.Complete();
            _destroyJobHandle.Complete();
            Vertices.Dispose();
            VertexUV.Dispose();
            _calcAABBBuffer.Dispose();
            foreach (var triangle in _triangles)
            {
                triangle.Dispose();
            }
            foreach (var triangle in _trianglePos)
            {
                triangle.Dispose();
            }
            _combinedTriangles.Dispose();
            _combinedTriangleIndex.Dispose();
            _combinedTriangleToSubmeshIndexAndOffset.Dispose();
        }

        internal MeshData(Renderer renderer, Mesh mesh, Matrix4x4 worldSpaceTransform)
        {
            ReferenceRenderer = renderer;

            var meshDataArray = Mesh.AcquireReadOnlyMeshData(mesh);

            var mainMesh = meshDataArray[0];

            var vertexCount = mainMesh.vertexCount;
            _vertices = new NativeArray<Vector3>(vertexCount, Allocator.TempJob);
            VertexUV = new NativeArray<Vector2>(vertexCount, Allocator.TempJob);

            mainMesh.GetVertices(_vertices);
            mainMesh.GetUVs(0, VertexUV);

            var subMeshCount = mainMesh.subMeshCount;
            _triangles = new NativeArray<TriangleIndex>[subMeshCount];
            _trianglePos = new NativeArray<Triangle>[subMeshCount];

            JobHandle worldSpaceTransformJob = new WorldSpaceTransformJob()
            {
                PositionBuffer = _vertices,
                WorldSpaceTransform = worldSpaceTransform
            }.Schedule(vertexCount, 64);

            _calcAABBBuffer = new NativeArray<TexTransUnityAABB>(1, Allocator.TempJob);
            _calcAABBJobHandle = new CalculateAABB()
            {
                PositionBuffer = _vertices,
                AABB = _calcAABBBuffer,
            }.Schedule(worldSpaceTransformJob);

            int totalTris = 0;
            for (int submesh = 0; submesh < subMeshCount; submesh++)
            {
                var desc = mainMesh.GetSubMesh(submesh);
                totalTris += desc.indexCount / 3;
            }
            _combinedTriangleIndex = new NativeArray<TriangleIndex>(totalTris, Allocator.TempJob);
            _combinedTriangles = new NativeArray<Triangle>(totalTris, Allocator.TempJob);
            _combinedTriangleToSubmeshIndexAndOffset = new NativeArray<(int, int)>(totalTris, Allocator.TempJob);

            JobHandle jobHandle = default;
            int combinedOffset = 0;
            for (int submesh = 0; submesh < subMeshCount; submesh++)
            {
                var desc = mainMesh.GetSubMesh(submesh);
                var indexCount = desc.indexCount;

                _triangles[submesh] = new NativeArray<TriangleIndex>(indexCount, Allocator.TempJob);
                _trianglePos[submesh] = new NativeArray<Triangle>(indexCount / 3, Allocator.TempJob);

                var indexes = new NativeArray<int>(indexCount, Allocator.TempJob);
                mainMesh.GetIndices(indexes, submesh);

                var newHandle = new InitTriangleJob()
                {
                    SubmeshIndexBuffer = indexes,
                    PositionBuffer = _vertices,
                    TriangleIndexBuffer = _triangles[submesh],
                    TrianglePosBuffer = _trianglePos[submesh],
                }.Schedule(indexCount / 3, 64, worldSpaceTransformJob);

                var copyHandle = new PackVerticesJob()
                {
                    srcIndex = _triangles[submesh],
                    srcPos = _trianglePos[submesh],
                    dstIndex = _combinedTriangleIndex.GetSubArray(combinedOffset, indexCount / 3),
                    dstPos = _combinedTriangles.GetSubArray(combinedOffset, indexCount / 3),
                    dstSubmeshIndexAndOffset =
                        _combinedTriangleToSubmeshIndexAndOffset.GetSubArray(combinedOffset, indexCount / 3),
                    submeshIndex = submesh,
                    submeshOffset = combinedOffset
                }.Schedule(indexCount / 3, 64, newHandle);

                jobHandle = JobHandle.CombineDependencies(jobHandle, copyHandle);
                combinedOffset += indexCount / 3;
            }


            _jobHandle = jobHandle;
            _destroyJobHandle = JobHandle.CombineDependencies(jobHandle, _calcAABBJobHandle);

            meshDataArray.Dispose();
        }
        internal MeshData(Renderer renderer) : this(renderer, GetMesh(renderer), GetMatrix(renderer)) { }
        internal static Mesh GetMesh(Renderer target)
        {
            switch (target)
            {
                case SkinnedMeshRenderer smr:
                    {
                        Mesh mesh = new Mesh();
                        smr.BakeMesh(mesh);
                        return mesh;
                    }
                case MeshRenderer mr:
                    {
                        return mr.GetComponent<MeshFilter>().sharedMesh;
                    }
                default:
                    {
                        throw new System.ArgumentException("Rendererが対応したタイプではないか、TargetRendererが存在しません。");
                    }
            }
        }
        internal static Matrix4x4 GetMatrix(Renderer target)
        {
            switch (target)
            {
                case SkinnedMeshRenderer smr:
                    {
                        Matrix4x4 matrix;
                        if (smr.bones.Any())
                        {
                            matrix = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                        }
                        else if (smr.rootBone == null)
                        {
                            matrix = smr.localToWorldMatrix;
                        }
                        else
                        {
                            matrix = smr.rootBone.localToWorldMatrix;
                        }

                        return matrix;
                    }
                case MeshRenderer mr:
                    {
                        return mr.localToWorldMatrix;
                    }
                default:
                    {
                        throw new System.ArgumentException("Rendererが対応したタイプではないか、TargetRendererが存在しません。");
                    }
            }
        }
        public static MeshData GetMeshData(Renderer renderer) => new MeshData(renderer);
        [BurstCompile]
        struct PackVerticesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<TriangleIndex> srcIndex;
            [ReadOnly] public NativeArray<Triangle> srcPos;

            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeSlice<TriangleIndex> dstIndex;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeSlice<Triangle> dstPos;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            [WriteOnly] public NativeSlice<(int, int)> dstSubmeshIndexAndOffset;

            public int submeshIndex;
            public int submeshOffset;

            public void Execute(int index)
            {
                dstIndex[index] = srcIndex[index];
                dstPos[index] = srcPos[index];

                dstSubmeshIndexAndOffset[index] = (submeshIndex, submeshOffset);
            }
        }

        [BurstCompile]
        struct InitTriangleJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion][ReadOnly] public NativeArray<int> SubmeshIndexBuffer;
            [ReadOnly] public NativeArray<Vector3> PositionBuffer;

            [WriteOnly] public NativeArray<TriangleIndex> TriangleIndexBuffer;
            [WriteOnly] public NativeArray<Triangle> TrianglePosBuffer;

            public void Execute(int index)
            {
                var i = index * 3;
                var triIndex = new TriangleIndex(SubmeshIndexBuffer[i], SubmeshIndexBuffer[i + 1], SubmeshIndexBuffer[i + 2]);
                TriangleIndexBuffer[index] = triIndex;

                var triangle = new Triangle();
                triangle.zero = PositionBuffer[triIndex.zero].ToTTCore();
                triangle.one = PositionBuffer[triIndex.one].ToTTCore();
                triangle.two = PositionBuffer[triIndex.two].ToTTCore();
                TrianglePosBuffer[index] = triangle;
            }
        }

        [BurstCompile]
        struct WorldSpaceTransformJob : IJobParallelFor
        {
            public NativeArray<Vector3> PositionBuffer;
            public Matrix4x4 WorldSpaceTransform;

            public void Execute(int index)
            {
                PositionBuffer[index] = WorldSpaceTransform.MultiplyPoint3x4(PositionBuffer[index]);
            }
        }
        [BurstCompile]
        struct CalculateAABB : IJob
        {
            [ReadOnly] public NativeArray<Vector3> PositionBuffer;
            [WriteOnly] public NativeArray<TexTransUnityAABB> AABB;
            public void Execute()
            {
                TexTransUnityAABB aabb = default;
                if (PositionBuffer.Length > 0) { aabb = new(PositionBuffer[0]); }
                for (var i = 0; PositionBuffer.Length > i; i += 1)
                { aabb.AddVertex(PositionBuffer[i]); }
                AABB[0] = aabb;
            }
        }
        internal List<Vector3> VertexList => Vertices.Memo(arr => arr.ToList());
        internal List<Vector2> UVList => VertexUV.Memo(arr => arr.ToList());
        internal List<List<TriangleIndex>> TrianglesSubMeshList
            => TriangleIndex.Memo(arr => arr.Select(subarr => subarr.ToList()).ToList());

        public void AddJobDependency(JobHandle jobHandle)
        {
            _destroyJobHandle = JobHandle.CombineDependencies(_destroyJobHandle, jobHandle);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct TexTransUnityAABB
    {
        public Vector3 Min;
        public Vector3 Max;

        public TexTransUnityAABB(Vector3 init)
        {
            Max = Min = init;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsIntersect(TexTransUnityAABB other)
        {
            var v = Min.x <= other.Max.x && Max.x >= other.Min.x;
            v &= Min.y <= other.Max.y && Max.y >= other.Min.y;
            v &= Min.z <= other.Max.z && Max.z >= other.Min.z;
            return v;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddVertex(Vector3 vector3)
        {
            Min = Vector3.Min(Min, vector3);
            Max = Vector3.Max(Max, vector3);
        }
    }
}
