using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;

namespace Szeminarium1_24_02_17_2
{
    internal class ObjResourceReader
    {
        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;

            ReadObjDataFromResource(resourceName,out objVertices, out objFaces, out objNormals);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces,objNormals, glVertices, glColors, glIndices);

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

            // release array buffer
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float[]> objNormals, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();
            bool normalsProvided = objNormals.Count > 0;
            foreach (var objFace in objFaces)
            {

                Vector3D<float> computedNormal = Vector3D<float>.Zero;
                if (!normalsProvided)
                {
                    var aObjVertex = objVertices[objFace[0]];
                    var bObjVertex = objVertices[objFace[3]];
                    var cObjVertex = objVertices[objFace[6]];

                    var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                    var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                    var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);
                    computedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }



                // process 3 vertices
                for (int i = 0; i < 3; ++i)
                {
                    int vertexIdx = objFace[i * 3 + 0];
                    int normalIdx = objFace[i * 3 + 2];

                    var vertex = objVertices[vertexIdx];
                    Vector3D<float> normal;

                    if (normalsProvided && normalIdx >= 0)
                    {
                        var n = objNormals[normalIdx];
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

        private static unsafe void ReadObjDataFromResource(string resourceName, out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objNormals = new List<float[]>();

            string fullResourceName = "Szeminarium1_24_02_17_2.Resources." + resourceName;

            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0)
                        continue;

                    var lineClassifier = parts[0];

                    switch (lineClassifier)
                    {
                        case "v":
                            objVertices.Add(new float[]
                            {
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                            });
                            break;

                        case "vn":
                            objNormals.Add(new float[]
                            {
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                            });
                            break;

                        case "f":
                            int[] face = new int[9]; // v/vt/vn vagy v//vn vagy csak v

                            for (int i = 0; i < 3; ++i)
                            {
                                var vertexParts = parts[i + 1].Split('/');

                                // vertex index
                                face[i * 3 + 0] = int.Parse(vertexParts[0], CultureInfo.InvariantCulture) - 1;

                                // texture index (ignored)
                                if (vertexParts.Length == 2 && vertexParts[1] != "")
                                {
                                    // v/t
                                    face[i * 3 + 1] = int.Parse(vertexParts[1], CultureInfo.InvariantCulture) - 1;
                                    face[i * 3 + 2] = -1;
                                }
                                else if (vertexParts.Length == 3)
                                {
                                    if (string.IsNullOrEmpty(vertexParts[1]))
                                    {
                                        // v//n 
                                        face[i * 3 + 1] = -1;
                                        face[i * 3 + 2] = int.Parse(vertexParts[2], CultureInfo.InvariantCulture) - 1;
                                    }
                                    else
                                    {
                                        // v/t/n 
                                        face[i * 3 + 1] = int.Parse(vertexParts[1], CultureInfo.InvariantCulture) - 1;
                                        face[i * 3 + 2] = int.Parse(vertexParts[2], CultureInfo.InvariantCulture) - 1;
                                    }
                                }
                                else
                                {
                                    // csak vertex
                                    face[i * 3 + 1] = -1;
                                    face[i * 3 + 2] = -1;
                                }
                            }
                            objFaces.Add(face);
                            break;
                    }
                }
            }
        }
    }
}
