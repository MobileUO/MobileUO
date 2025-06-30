using ClassicUO.Renderer;
using UnityEngine;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class MeshHolder
    {
        public readonly Mesh Mesh;

        private readonly UnityEngine.Vector3[] vertices;
        private readonly UnityEngine.Vector2[] uvs;
        private readonly UnityEngine.Vector3[] normals;

        private int currentQuadIndex;

        public UnityEngine.Color CurrentHue { get; private set; } = UnityEngine.Color.white;

        public MeshHolder(int quadCount)
        {
            Mesh = new Mesh();
            Mesh.MarkDynamic();

            quadCount = Mathf.NextPowerOfTwo(quadCount);
            int vCount = quadCount * 4;

            vertices = new UnityEngine.Vector3[vCount];
            uvs = new UnityEngine.Vector2[vCount];
            normals = new UnityEngine.Vector3[vCount];

            var triangles = new int[quadCount * 6];
            for (var i = 0; i < quadCount; i++)
            {
                /*
                 *  TL    TR
                 *   0----1 0,1,2,3 = index offsets for vertex indices
                 *   |   /| TL,TR,BL,BR are vertex references in SpriteBatchItem.
                 *   |  / |
                 *   | /  |
                 *   |/   |
                 *   2----3
                 *  BL    BR
                 */
                // Triangle 1
                triangles[i * 6] = i * 4;
                triangles[i * 6 + 1] = i * 4 + 1;
                triangles[i * 6 + 2] = i * 4 + 2;
                // Triangle 2
                triangles[i * 6 + 3] = i * 4 + 1;
                triangles[i * 6 + 4] = i * 4 + 3;
                triangles[i * 6 + 5] = i * 4 + 2;
            }

            Mesh.vertices = vertices;
            Mesh.uv = uvs;
            Mesh.triangles = triangles;
            Mesh.normals = normals;
        }

        public void Clear()
        {
            currentQuadIndex = 0;
        }

        public void AddQuad(UltimaBatcher2D.PositionNormalTextureColor4 vertex)
        {
            int i = currentQuadIndex * 4;
            if (i + 3 >= vertices.Length)
                return;

            vertices[i + 0] = vertex.Position0;
            vertices[i + 1] = vertex.Position1;
            vertices[i + 2] = vertex.Position2;
            vertices[i + 3] = vertex.Position3;

            uvs[i + 0] = FlipY(vertex.TextureCoordinate0);
            uvs[i + 1] = FlipY(vertex.TextureCoordinate1);
            uvs[i + 2] = FlipY(vertex.TextureCoordinate2);
            uvs[i + 3] = FlipY(vertex.TextureCoordinate3);

            normals[i + 0] = vertex.Normal0;
            normals[i + 1] = vertex.Normal1;
            normals[i + 2] = vertex.Normal2;
            normals[i + 3] = vertex.Normal3;

            // Assume all 4 hue values are equal — just pick the first
            CurrentHue = new UnityEngine.Color(vertex.Hue0.x, vertex.Hue0.y, vertex.Hue0.z, 1f);

            currentQuadIndex++;
        }

        //public void FinalizeMesh()
        //{
        //    int count = currentQuadIndex * 4;
        //    Mesh.Clear();
        //    Mesh.vertices = vertices;
        //    Mesh.uv = uvs;
        //    Mesh.normals = normals;
        //}

        public void FinalizeMesh()
        {
            int quadCount = currentQuadIndex;
            int vertexCount = quadCount * 4;
            int indexCount = quadCount * 6;

            int[] triangles = new int[indexCount];
            for (int i = 0; i < quadCount; i++)
            {
                int vi = i * 4;
                int ti = i * 6;
                triangles[ti + 0] = vi + 0;
                triangles[ti + 1] = vi + 1;
                triangles[ti + 2] = vi + 2;
                triangles[ti + 3] = vi + 1;
                triangles[ti + 4] = vi + 3;
                triangles[ti + 5] = vi + 2;
            }

            Mesh.Clear();
            Mesh.vertices = vertices;
            Mesh.uv = uvs;
            Mesh.normals = normals;
            Mesh.triangles = triangles;
        }

        private UnityEngine.Vector2 FlipY(UnityEngine.Vector3 texCoord)
        {
            return new UnityEngine.Vector2(texCoord.x, 1f - texCoord.y);
        }

        internal void Populate(UltimaBatcher2D.PositionNormalTextureColor4 vertex)
        {
            vertex.TextureCoordinate0.y = 1 - vertex.TextureCoordinate0.y;
            vertices[0] = vertex.Position0;
            uvs[0] = vertex.TextureCoordinate0;
            normals[0] = vertex.Normal0;

            vertex.TextureCoordinate1.y = 1 - vertex.TextureCoordinate1.y;
            vertices[1] = vertex.Position1;
            uvs[1] = vertex.TextureCoordinate1;
            normals[1] = vertex.Normal1;

            vertex.TextureCoordinate2.y = 1 - vertex.TextureCoordinate2.y;
            vertices[2] = vertex.Position2;
            uvs[2] = vertex.TextureCoordinate2;
            normals[2] = vertex.Normal2;

            vertex.TextureCoordinate3.y = 1 - vertex.TextureCoordinate3.y;
            vertices[3] = vertex.Position3;
            uvs[3] = vertex.TextureCoordinate3;
            normals[3] = vertex.Normal3;

            Mesh.vertices = vertices;
            Mesh.uv = uvs;
            Mesh.normals = normals;
        }
    }
}