using Silk.NET.Core.Attributes;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Xml;

namespace Projekt_OpenGL
{
    internal class ColladaResourceReader
    {
        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> colladaVertices;
            List<int[]> colladaFaces;
            List<float[]> colladaNormals;
            List<float[]> colladaTexcoords;

            ReadColladaDataFromResource(resourceName, out colladaVertices, out colladaFaces, out colladaNormals, out colladaTexcoords);

            List<float> glVertices = new();
            List<float> glColors = new();
            List<uint> glIndices = new();

            CreateGlArraysFromColladaArrays(faceColor, colladaVertices, colladaFaces, colladaNormals, colladaTexcoords, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetUv = offsetPos + (3 * sizeof(float));
            uint offsetNormal = offsetUv + (2 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vbo);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, glVertices.ToArray(), GLEnum.StaticDraw);

            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetUv);
            Gl.EnableVertexAttribArray(3);

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(2);

            uint cbo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, cbo);
            Gl.BufferData<float>(GLEnum.ArrayBuffer, glColors.ToArray(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint ebo = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, ebo);
           // Gl.BufferData<float>(GLEnum.ArrayBuffer, glColors.ToArray(), GLEnum.StaticDraw);
            Gl.BufferData<uint>(GLEnum.ElementArrayBuffer, glIndices.ToArray(), GLEnum.StaticDraw);


            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexCount = (uint)glIndices.Count;

            return new GlObject(vao, vbo, cbo, ebo, indexCount, Gl);
        }

        private static unsafe void CreateGlArraysFromColladaArrays(float[] faceColor, List<float[]> vertices, List<int[]> faces, List<float[]> normals, List<float[]> uvs, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> indexMap = new();
            bool hasNormals = normals.Count > 0;
            bool hasUvs = uvs.Count > 0;

            foreach (var face in faces)
            {
                Vector3D<float> computedNormal = Vector3D<float>.Zero;

                if (!hasNormals)
                {
                    var a = new Vector3D<float>(vertices[face[0]][0], vertices[face[0]][1], vertices[face[0]][2]);
                    var b = new Vector3D<float>(vertices[face[1]][0], vertices[face[1]][1], vertices[face[1]][2]);
                    var c = new Vector3D<float>(vertices[face[2]][0], vertices[face[2]][1], vertices[face[2]][2]);
                    computedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                for (int i = 0; i < 3; i++)
                {
                    int vIdx = face[i];
                    var pos = vertices[vIdx];
                    var uv = hasUvs && vIdx < uvs.Count ? uvs[vIdx] : new float[] { 0f, 0f };
                    var nor = hasNormals && vIdx < normals.Count ? normals[vIdx] : new float[] { computedNormal.X, computedNormal.Y, computedNormal.Z };

                    string key = string.Join(",", pos.Concat(uv).Concat(nor));

                    if (!indexMap.ContainsKey(key))
                    {
                        glVertices.AddRange(pos);     // 3
                        glVertices.AddRange(uv);      // 2
                        glVertices.AddRange(nor);     // 3
                        glColors.AddRange(faceColor); // 4
                        indexMap[key] = indexMap.Count;
                    }

                    glIndices.Add((uint)indexMap[key]);
                }
            }
        }

        private static unsafe void ReadColladaDataFromResource(string resourceName, out List<float[]> vertices, out List<int[]> faces, out List<float[]> normals, out List<float[]> texcoords)
        {
            vertices = new();
            faces = new();
            normals = new();
            texcoords = new();

            string fullResourceName = "Projekt_OpenGL.Resources." + resourceName;

            using var stream = typeof(ColladaResourceReader).Assembly.GetManifestResourceStream(fullResourceName);
            using var reader = new StreamReader(stream);

            XmlDocument doc = new();
            doc.Load(reader);
            XmlNamespaceManager nsmgr = new(doc.NameTable);
            nsmgr.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

            XmlNode meshNode = doc.SelectSingleNode("//c:library_geometries//c:mesh", nsmgr);
            if (meshNode == null)
                throw new Exception("Mesh not found in COLLADA file.");

            Dictionary<string, List<float>> sources = new();

            foreach (XmlNode src in meshNode.SelectNodes("c:source", nsmgr))
            {
                string id = src.Attributes["id"].Value;
                XmlNode floatArray = src.SelectSingleNode("c:float_array", nsmgr);
                if (floatArray == null) continue;

                List<float> data = floatArray.InnerText
                    .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToList();

                sources[id] = data;
            }

            XmlNode verticesNode = meshNode.SelectSingleNode("c:vertices", nsmgr);
            string positionSourceId = verticesNode?.SelectSingleNode("c:input", nsmgr)?.Attributes["source"].Value.Substring(1);

            XmlNode polylist = meshNode.SelectSingleNode("c:polylist", nsmgr);
            if (polylist != null)
            {
                int inputCount = polylist.SelectNodes("c:input", nsmgr).Count;
                int vertexOffset = 0;

                XmlNode pNode = polylist.SelectSingleNode("c:p", nsmgr);
                if (pNode != null)
                {
                    var parts = pNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i + inputCount * 3 <= parts.Length; i += inputCount * 3)
                    {
                        int[] triangle = new int[3];
                        for (int j = 0; j < 3; j++)
                        {
                            int index = i + j * inputCount + vertexOffset;
                            if (index >= parts.Length)
                                continue; // vagy break; ha nem akarod hozzáadni ezt a triangle-t

                            triangle[j] = int.Parse(parts[index]);
                        }

                        faces.Add(triangle);
                    }

                }
            }

            if (positionSourceId != null && sources.TryGetValue(positionSourceId, out var vertData))
            {
                for (int i = 0; i < vertData.Count; i += 3)
                    vertices.Add(new[] { vertData[i], vertData[i + 1], vertData[i + 2] });
            }

            foreach (var kv in sources)
            {
                if (kv.Key.ToLower().Contains("normal"))
                {
                    for (int i = 0; i < kv.Value.Count; i += 3)
                        normals.Add(new[] { kv.Value[i], kv.Value[i + 1], kv.Value[i + 2] });
                }

                if (kv.Key.ToLower().Contains("map") || kv.Key.ToLower().Contains("texcoord"))
                {
                    for (int i = 0; i < kv.Value.Count; i += 2)
                        texcoords.Add(new[] { kv.Value[i], kv.Value[i + 1] });
                }
            }
        }
    }
}
