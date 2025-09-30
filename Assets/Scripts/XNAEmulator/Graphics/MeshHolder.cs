using ClassicUO.Renderer;
using MobileUO.Profiling;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static ClassicUO.Renderer.UltimaBatcher2D;
using UnityVector2 = UnityEngine.Vector2;
using UnityVector3 = UnityEngine.Vector3;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class MeshHolder
    {
        private struct MeshVertex
        {
            public UnityVector3 Position;
            public UnityVector3 Normal;
            public UnityVector2 TexCoord;
        }

        public readonly Mesh Mesh;

        private MeshVertex[] _vertexBuffer = System.Array.Empty<MeshVertex>();
        private ushort[] _indexBuffer = System.Array.Empty<ushort>();
        private int _quadCapacity;
        private readonly Bounds _bounds;
        private readonly VertexAttributeDescriptor[] _vertexLayout;
        private IndexFormat _indexFormat = IndexFormat.UInt16;

        public MeshHolder(int quadCount)
        {
            Mesh = new Mesh();
            Mesh.MarkDynamic();

            _bounds = new Bounds(new UnityVector3(0f, 0f, 0f), new UnityVector3(20000f, 20000f, 1f));
            Mesh.bounds = _bounds;

            _vertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            };

            EnsureCapacity(Mathf.Max(1, quadCount));
        }

        // TODO: deprecated
        public void Populate(IList<PositionNormalTextureColor4> quads)
        {
            using (UnityProfiler.Auto(UnityProfiler.Mk_MeshPopulate))
            {
                int quadCount = quads.Count;
                if (quadCount == 0)
                {
                    Mesh.subMeshCount = 0;
                    return;
                }

                EnsureCapacity(quadCount);

                int vertexCount = quadCount * 4;
                int indexCount = quadCount * 6;

                for (int q = 0, v = 0; q < quadCount; q++, v += 4)
                {
                    var quad = quads[q];

                    WriteVertex(ref _vertexBuffer[v + 0], quad.Position0, quad.Normal0, quad.TextureCoordinate0);
                    WriteVertex(ref _vertexBuffer[v + 1], quad.Position1, quad.Normal1, quad.TextureCoordinate1);
                    WriteVertex(ref _vertexBuffer[v + 2], quad.Position2, quad.Normal2, quad.TextureCoordinate2);
                    WriteVertex(ref _vertexBuffer[v + 3], quad.Position3, quad.Normal3, quad.TextureCoordinate3);
                }

                using (UnityProfiler.Auto(UnityProfiler.Mk_SetVB))
                {
                    Mesh.SetVertexBufferData(_vertexBuffer, 0, 0, vertexCount, 0,
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
                }

                using (UnityProfiler.Auto(UnityProfiler.Mk_SetSM))
                {
                    var subMesh = new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles)
                    {
                        vertexCount = vertexCount
                    };

                    Mesh.subMeshCount = 1;
                    Mesh.SetSubMesh(0, subMesh, MeshUpdateFlags.DontRecalculateBounds);
                }

                Mesh.bounds = _bounds;
            }
        }

        public void Populate(PositionNormalTextureColor4[] quads, int start, int count)
        {
            using (UnityProfiler.Auto(UnityProfiler.Mk_MeshPopulate))
            {
                if (count <= 0)
                {
                    Mesh.subMeshCount = 0;
                    return;
                }

                EnsureCapacity(count);

                int vertexCount = count * 4;
                int indexCount = count * 6;

                for (int i = 0, v = 0; i < count; i++, v += 4)
                {
                    ref readonly var quad = ref quads[start + i];

                    WriteVertex(ref _vertexBuffer[v + 0], quad.Position0, quad.Normal0, quad.TextureCoordinate0);
                    WriteVertex(ref _vertexBuffer[v + 1], quad.Position1, quad.Normal1, quad.TextureCoordinate1);
                    WriteVertex(ref _vertexBuffer[v + 2], quad.Position2, quad.Normal2, quad.TextureCoordinate2);
                    WriteVertex(ref _vertexBuffer[v + 3], quad.Position3, quad.Normal3, quad.TextureCoordinate3);
                }

                using (UnityProfiler.Auto(UnityProfiler.Mk_SetVB))
                {
                    Mesh.SetVertexBufferData(_vertexBuffer, 0, 0, vertexCount, 0,
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
                }

                using (UnityProfiler.Auto(UnityProfiler.Mk_SetSM))
                {
                    var subMesh = new SubMeshDescriptor(0, indexCount, MeshTopology.Triangles)
                    {
                        vertexCount = vertexCount
                    };

                    Mesh.subMeshCount = 1;
                    Mesh.SetSubMesh(0, subMesh, MeshUpdateFlags.DontRecalculateBounds);
                }

                Mesh.bounds = _bounds;
            }
        }

        private void EnsureCapacity(int quadCount)
        {
            using (UnityProfiler.Auto(UnityProfiler.Mk_EnsureCap))
            {
                quadCount = Mathf.NextPowerOfTwo(Mathf.Max(1, quadCount));

                if (quadCount <= _quadCapacity)
                {
                    return;
                }

                _quadCapacity = quadCount;

                int vertexCapacity = quadCount * 4;
                int indexCapacity = quadCount * 6;

                _vertexBuffer = new MeshVertex[vertexCapacity];
                _indexBuffer = new ushort[indexCapacity];

                for (int q = 0, v = 0, i = 0; q < quadCount; q++, v += 4, i += 6)
                {
                    _indexBuffer[i + 0] = (ushort)(v + 0);
                    _indexBuffer[i + 1] = (ushort)(v + 1);
                    _indexBuffer[i + 2] = (ushort)(v + 2);
                    _indexBuffer[i + 3] = (ushort)(v + 1);
                    _indexBuffer[i + 4] = (ushort)(v + 3);
                    _indexBuffer[i + 5] = (ushort)(v + 2);
                }

                Mesh.SetVertexBufferParams(vertexCapacity, _vertexLayout);
                Mesh.SetIndexBufferParams(indexCapacity, _indexFormat);

                using (UnityProfiler.Auto(UnityProfiler.Mk_SetIB))
                {
                    Mesh.SetIndexBufferData(_indexBuffer, 0, 0, indexCapacity,
                    MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
                }

                Mesh.subMeshCount = 1;
            }
        }

        private static void WriteVertex(ref MeshVertex dst, UnityVector3 position, UnityVector3 normal, UnityVector3 texCoord)
        {
            dst.Position.x = Mathf.Round(position.x);
            dst.Position.y = Mathf.Round(position.y);
            dst.Position.z = position.z;
            dst.Normal = normal;
            dst.TexCoord.x = texCoord.x;
            dst.TexCoord.y = texCoord.y;
        }
    }
}
