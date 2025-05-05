using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Xml;

namespace Szeminarium1_24_02_17_2
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

            ReadColladaDataFromResource(resourceName, out colladaVertices, out colladaFaces, out colladaNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromColladaArrays(faceColor, colladaVertices, colladaFaces, colladaNormals, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint vertexSize = offsetNormal + (3 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndices.ToArray().AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromColladaArrays(float[] faceColor, List<float[]> colladaVertices, List<int[]> colladaFaces, List<float[]> colladaNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();
            bool normalsProvided = colladaNormals.Count > 0;

            foreach (var face in colladaFaces)
            {
                Vector3D<float> computedNormal = Vector3D<float>.Zero;

                if (!normalsProvided)
                {
                    var a = new Vector3D<float>(colladaVertices[face[0]][0], colladaVertices[face[0]][1], colladaVertices[face[0]][2]);
                    var b = new Vector3D<float>(colladaVertices[face[1]][0], colladaVertices[face[1]][1], colladaVertices[face[1]][2]);
                    var c = new Vector3D<float>(colladaVertices[face[2]][0], colladaVertices[face[2]][1], colladaVertices[face[2]][2]);

                    computedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                for (int i = 0; i < 3; ++i)
                {
                    int vertexIdx = face[i];
                    var vertex = colladaVertices[vertexIdx];
                    Vector3D<float> normal;

                    if (normalsProvided && vertexIdx < colladaNormals.Count)
                    {
                        var n = colladaNormals[vertexIdx];
                        normal = new Vector3D<float>(n[0], n[1], n[2]);
                    }
                    else
                    {
                        normal = computedNormal;
                    }

                    List<float> glVertex = new List<float>();
                    glVertex.AddRange(vertex);
                    glVertex.Add(normal.X);
                    glVertex.Add(normal.Y);
                    glVertex.Add(normal.Z);

                    var glVertexStringKey = string.Join(" ", glVertex);
                    if (!glVertexIndices.ContainsKey(glVertexStringKey))
                    {
                        glVertices.AddRange(glVertex);
                        glColors.AddRange(faceColor);
                        glVertexIndices.Add(glVertexStringKey, glVertexIndices.Count);
                    }

                    glIndices.Add((uint)glVertexIndices[glVertexStringKey]);
                }
            }
        }

        private static unsafe void ReadColladaDataFromResource(string resourceName, out List<float[]> colladaVertices, out List<int[]> colladaFaces, out List<float[]> colladaNormals)
        {
            colladaVertices = new List<float[]>();
            colladaFaces = new List<int[]>();
            colladaNormals = new List<float[]>();

            string fullResourceName = "Szeminarium1_24_02_17_2.Resources." + resourceName;

            using (Stream stream = typeof(ColladaResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("c", "http://www.collada.org/2008/03/COLLADASchema");

                XmlNode meshNode = doc.SelectSingleNode("//c:library_geometries//c:mesh", nsmgr);

                if (meshNode == null)
                    throw new Exception("Mesh not found in COLLADA file.");

                Dictionary<string, List<float>> sources = new Dictionary<string, List<float>>();

                foreach (XmlNode sourceNode in meshNode.SelectNodes("c:source", nsmgr))
                {
                    string id = sourceNode.Attributes["id"].Value;
                    XmlNode floatArrayNode = sourceNode.SelectSingleNode("c:float_array", nsmgr);
                    if (floatArrayNode != null)
                    {
                        string[] parts = floatArrayNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        List<float> values = new List<float>();
                        foreach (var part in parts)
                            values.Add(float.Parse(part, CultureInfo.InvariantCulture));

                        sources[id] = values;
                    }
                }

                XmlNode verticesNode = meshNode.SelectSingleNode("c:vertices", nsmgr);
                string positionSourceId = verticesNode?.SelectSingleNode("c:input", nsmgr)?.Attributes["source"].Value.Substring(1);

                XmlNode trianglesNode = meshNode.SelectSingleNode("c:polylist", nsmgr);
                if (trianglesNode != null)
                {
                    var inputNodes = trianglesNode.SelectNodes("c:input", nsmgr);
                    var inputs = new List<string>();
                    foreach (XmlNode inputNode in inputNodes)
                    {
                        inputs.Add(inputNode.Attributes["semantic"].Value); // pl. VERTEX, NORMAL, TEXCOORD
                    }

                    int inputCount = inputs.Count;

                    int vertexOffset = -1;
                    for (int i = 0; i < inputs.Count; i++)
                    {
                        if (inputs[i] == "VERTEX")
                        {
                            vertexOffset = i;
                            break;
                        }
                    }
                    if (vertexOffset == -1)
                        throw new Exception("VERTEX input not found!");

                    XmlNode pNode = trianglesNode.SelectSingleNode("c:p", nsmgr);
                    if (pNode != null)
                    {
                        string[] parts = pNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < parts.Length; i += inputCount * 3)
                        {
                            int[] triangle = new int[3];

                            for (int j = 0; j < 3; ++j)
                            {
                                triangle[j] = int.Parse(parts[i + j * inputCount + vertexOffset]);
                            }

                            colladaFaces.Add(triangle);
                        }
                    }
                }

                if (positionSourceId != null && sources.ContainsKey(positionSourceId))
                {
                    var verts = sources[positionSourceId];
                    for (int i = 0; i < verts.Count; i += 3)
                    {
                        colladaVertices.Add(new float[] { verts[i], verts[i + 1], verts[i + 2] });
                    }
                }

                foreach (var kv in sources)
                {
                    if (kv.Key.ToLower().Contains("normal"))
                    {
                        var norms = kv.Value;
                        for (int i = 0; i < norms.Count; i += 3)
                        {
                            colladaNormals.Add(new float[] { norms[i], norms[i + 1], norms[i + 2] });
                        }
                        break;
                    }
                }

            }
        }
    }
}
