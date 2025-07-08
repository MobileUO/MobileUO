using ClassicUO.Renderer;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ClassicUO.Renderer.UltimaBatcher2D;

namespace Microsoft.Xna.Framework.Graphics
{
    internal class MeshHolder
    {
        public readonly Mesh Mesh;

        //private UnityEngine.Vector3[] vertices;
        //private UnityEngine.Vector2[] uvs;
        //private UnityEngine.Vector3[] normals;
        //private int[] triangles;

        private readonly List<UnityEngine.Vector3> vertices = new List<UnityEngine.Vector3>();
        private readonly List<UnityEngine.Vector2> uvs = new List<UnityEngine.Vector2>();
        private readonly List<UnityEngine.Vector3> normals = new List<UnityEngine.Vector3>();
        private readonly List<int> tris = new List<int>();

        public MeshHolder(int quadCount)
        {
            Mesh = new Mesh();
            Mesh.bounds = new Bounds(
              new Vector3(0, 0, 0),
              new Vector3(20000f, 20000f, 1f)
            );
            Mesh.MarkDynamic();

            quadCount = Mathf.NextPowerOfTwo(quadCount);
            int vCount = quadCount * 4;

            // optional: pre-size lists
            int tCount = quadCount * 6;
            vertices.Capacity = vCount;
            uvs.Capacity = vCount;
            normals.Capacity = vCount;
            tris.Capacity = tCount;

            //vertices = new UnityEngine.Vector3[vCount];
            //uvs = new UnityEngine.Vector2[vCount];
            //normals = new UnityEngine.Vector3[vCount];

            //var triangles = new int[quadCount * 6];
            //for (var i = 0; i < quadCount; i++)
            //{
            //    /*
            //     *  TL    TR
            //     *   0----1 0,1,2,3 = index offsets for vertex indices
            //     *   |   /| TL,TR,BL,BR are vertex references in SpriteBatchItem.
            //     *   |  / |
            //     *   | /  |
            //     *   |/   |
            //     *   2----3
            //     *  BL    BR
            //     */
            //    // Triangle 1
            //    triangles[i * 6] = i * 4;
            //    triangles[i * 6 + 1] = i * 4 + 1;
            //    triangles[i * 6 + 2] = i * 4 + 2;
            //    // Triangle 2
            //    triangles[i * 6 + 3] = i * 4 + 1;
            //    triangles[i * 6 + 4] = i * 4 + 3;
            //    triangles[i * 6 + 5] = i * 4 + 2;
            //}

            //Mesh.vertices = vertices;
            //Mesh.uv = uvs;
            //Mesh.triangles = triangles;
            //Mesh.normals = normals;
        }

        //internal void Populate(UltimaBatcher2D.PositionNormalTextureColor4 vertex)
        //{
        //    vertex.TextureCoordinate0.y = 1 - vertex.TextureCoordinate0.y;
        //    vertices[0] = vertex.Position0;
        //    uvs[0] = vertex.TextureCoordinate0;
        //    normals[0] = vertex.Normal0;

        //    vertex.TextureCoordinate1.y = 1 - vertex.TextureCoordinate1.y;
        //    vertices[1] = vertex.Position1;
        //    uvs[1] = vertex.TextureCoordinate1;
        //    normals[1] = vertex.Normal1;

        //    vertex.TextureCoordinate2.y = 1 - vertex.TextureCoordinate2.y;
        //    vertices[2] = vertex.Position2;
        //    uvs[2] = vertex.TextureCoordinate2;
        //    normals[2] = vertex.Normal2;

        //    vertex.TextureCoordinate3.y = 1 - vertex.TextureCoordinate3.y;
        //    vertices[3] = vertex.Position3;
        //    uvs[3] = vertex.TextureCoordinate3;
        //    normals[3] = vertex.Normal3;

        //    Mesh.vertices = vertices;
        //    Mesh.uv = uvs;
        //    Mesh.normals = normals;
        //}

        public void Populate(IList<PositionNormalTextureColor4> quads)//, bool isFromTextureAtlas)
        {
            int quadCount = quads.Count;

            // reuse the same internal buffers
            vertices.Clear();
            uvs.Clear();
            normals.Clear();
            tris.Clear();

            for (int q = 0; q < quadCount; q++)
            {
                int vOff = q * 4;
                var quad = quads[q];

                // --- positions
                vertices.Add(quad.Position0); // TL
                vertices.Add(quad.Position1); // TR
                vertices.Add(quad.Position2); // BL
                vertices.Add(quad.Position3); // BR

                // --- UVs (flip Y)
                //if (isFromTextureAtlas)
                //{
                //    uvs.Add(quad.TextureCoordinate2); // old BL -> new TL
                //    uvs.Add(quad.TextureCoordinate3); // old BR -> new TR
                //    uvs.Add(quad.TextureCoordinate0); // old TL -> new BL
                //    uvs.Add(quad.TextureCoordinate1); // old TR -> new BR
                //}
                //else
                //{
                //    var uv0 = quad.TextureCoordinate0; uv0.y = 1f - uv0.y;
                //    var uv1 = quad.TextureCoordinate1; uv1.y = 1f - uv1.y;
                //    var uv2 = quad.TextureCoordinate2; uv2.y = 1f - uv2.y;
                //    var uv3 = quad.TextureCoordinate3; uv3.y = 1f - uv3.y;
                //    uvs.Add(uv0);
                //    uvs.Add(uv1);
                //    uvs.Add(uv2);
                //    uvs.Add(uv3);
                //}

                var uv0 = quad.TextureCoordinate0;
                var uv1 = quad.TextureCoordinate1;
                var uv2 = quad.TextureCoordinate2;
                var uv3 = quad.TextureCoordinate3;
                uvs.Add(uv0);
                uvs.Add(uv1);
                uvs.Add(uv2);
                uvs.Add(uv3);

                // --- normals
                normals.Add(quad.Normal0);
                normals.Add(quad.Normal1);
                normals.Add(quad.Normal2);
                normals.Add(quad.Normal3);

                // --- triangles (XNA winding)
                tris.Add(vOff + 0); // TL
                tris.Add(vOff + 1); // TR
                tris.Add(vOff + 2); // BL

                tris.Add(vOff + 1); // TR
                tris.Add(vOff + 3); // BR
                tris.Add(vOff + 2); // BL
            }

            Mesh.Clear();
            Mesh.SetVertices(vertices);
            Mesh.SetUVs(0, uvs);
            Mesh.SetNormals(normals);
            Mesh.SetTriangles(tris, 0);
            Mesh.RecalculateBounds();
        }


        /// <summary>
        /// Ensures our mesh?buffers are at least as large as needed.
        /// If not, replaces them with new arrays of the exact needed size.
        /// </summary>
        //private void EnsureArraysCapacity(int vertCount, int triCount)
        //{
        //    if (vertices == null || vertices.Length < vertCount)
        //        vertices = new UnityEngine.Vector3[vertCount];

        //    if (uvs == null || uvs.Length < vertCount)
        //        uvs = new UnityEngine.Vector2[vertCount];

        //    if (normals == null || normals.Length < vertCount)
        //        normals = new UnityEngine.Vector3[vertCount];

        //    if (triangles == null || triangles.Length < triCount)
        //        triangles = new int[triCount];
        //}
    }
}