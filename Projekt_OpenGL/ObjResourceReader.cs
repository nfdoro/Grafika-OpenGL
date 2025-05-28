using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using StbImageSharp;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class TexturedObjGlObject : GlObject
    {
        public uint? AOTextureId { get; set; }
        public uint? NormalTextureId { get; set; }
        public uint? BaseColorTextureId { get; set; }
        public uint? MetallicTextureId { get; set; }
        public uint? RoughnessTextureId { get; set; }

        public TexturedObjGlObject(uint vao, uint vertices, uint colors, uint indices, uint indexArrayLength, GL gl)
            : base(vao, vertices, colors, indices, indexArrayLength, gl)
        {
        }

        public new void ReleaseGlObject()
        {
            // Delete our textures first
            if (AOTextureId.HasValue)
            {
                Gl.DeleteTexture(AOTextureId.Value);
            }
            if (NormalTextureId.HasValue)
            {
                Gl.DeleteTexture(NormalTextureId.Value);
            }
            if (BaseColorTextureId.HasValue)
            {
                Gl.DeleteTexture(BaseColorTextureId.Value);
            }
            if (MetallicTextureId.HasValue)
            {
                Gl.DeleteTexture(MetallicTextureId.Value);
            }
            if (RoughnessTextureId.HasValue)
            {
                Gl.DeleteTexture(RoughnessTextureId.Value);
            }
            // for clean up
            base.ReleaseGlObject();
        }
    }

    internal class ObjResourceReader
    {
        public static unsafe TexturedObjGlObject CreateObjectFromResourceWithTextures(GL Gl, string resourceName, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<float[]> objTexCoords;

            ReadObjDataFromResource(resourceName, out objVertices, out objFaces, out objNormals, out objTexCoords);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, objNormals, objTexCoords, glVertices, glColors, glIndices);

            var texturedObject = CreateTexturedOpenGlObject(Gl, vao, glVertices, glColors, glIndices);


            LoadTexturesForObject(Gl, resourceName, texturedObject);

            return texturedObject;
        }

        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName, float[] faceColor)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            List<float[]> objVertices;
            List<int[]> objFaces;
            List<float[]> objNormals;
            List<float[]> objTexCoords;

            ReadObjDataFromResource(resourceName, out objVertices, out objFaces, out objNormals, out objTexCoords);

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            List<uint> glIndices = new List<uint>();

            CreateGlArraysFromObjArrays(faceColor, objVertices, objFaces, objNormals, objTexCoords, glVertices, glColors, glIndices);

            return CreateOpenGlObject(Gl, vao, glVertices, glColors, glIndices);
        }

        private static unsafe void LoadTexturesForObject(GL Gl, string objResourceName, TexturedObjGlObject texturedObject)
        {
            string texturePrefix = "";

            if (objResourceName.Contains("can1"))
            {
                texturePrefix = "GameOBJ.textures_can1";
            }
            else if (objResourceName.Contains("can2"))
            {
                texturePrefix = "GameOBJ.textures_can2";
            }
            else if (objResourceName.Contains("terrain"))
            {
                texturePrefix = "terrain.texture";
                LoadTerrainTextures(Gl, texturedObject);
                return;
            }
            else
            {
                Console.WriteLine($"Othe object type for texture loading: {objResourceName}");
                return;
            }

            LoadCanTextures(Gl, texturePrefix, texturedObject);
        }

        private static void LoadTerrainTextures(GL Gl, TexturedObjGlObject texturedObject)
        {
            // AO texture
            try
            {
                texturedObject.AOTextureId = LoadTextureFromResource(Gl, "terrain.texture.ao.png");
                Console.WriteLine("Terrain AO texture loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load terrain AO texture: {ex.Message}");
            }

            // Normal texture
            try
            {
                texturedObject.NormalTextureId = LoadTextureFromResource(Gl, "terrain.texture.normal.png");
                Console.WriteLine("Terrain Normal texture loaded");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load terrain normal texture: {ex.Message}");
            }
        }

        private static void LoadCanTextures(GL Gl, string texturePrefix, TexturedObjGlObject texturedObject)
        {
            var textureTypes = new[]
            {
                new { Name = "AO_2k.png", PropertySetter = new Action<uint>(id => texturedObject.AOTextureId = id), Description = "AO" },
                new { Name = "Normal_2k.png", PropertySetter = new Action<uint>(id => texturedObject.NormalTextureId = id), Description = "Normal" },
                new { Name = "BaseColor_2k.png", PropertySetter = new Action<uint>(id => texturedObject.BaseColorTextureId = id), Description = "BaseColor" },
                new { Name = "Metallic_2k.png", PropertySetter = new Action<uint>(id => texturedObject.MetallicTextureId = id), Description = "Metallic" },
                new { Name = "Roughness_2k.png", PropertySetter = new Action<uint>(id => texturedObject.RoughnessTextureId = id), Description = "Roughness" }
            };

            foreach (var textureType in textureTypes)
            {
                string textureName = $"{texturePrefix}.{textureType.Name}";
                try
                {
                    uint textureId = LoadTextureFromResource(Gl, textureName);
                    textureType.PropertySetter(textureId);
                    Console.WriteLine($"{textureType.Description} texture loaded: {textureName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {textureType.Description} texture {textureName}: {ex.Message}");
                }
            }
        }

        private static unsafe uint LoadTextureFromResource(GL Gl, string textureName)
        {
            string fullResourceName = "Projekt_OpenGL.Resources." + textureName;

            using (Stream textureStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            {
                if (textureStream == null)
                {
                    throw new Exception($"Could not find embedded texture resource: {fullResourceName}");
                }

                StbImage.stbi_set_flip_vertically_on_load(1); // Flip Y axis for OpenGL

                byte[] imageData = new byte[textureStream.Length];
                textureStream.Read(imageData, 0, imageData.Length);

                ImageResult image = ImageResult.FromMemory(imageData, ColorComponents.RedGreenBlueAlpha);

                if (image.Data == null)
                {
                    throw new Exception($"Failed to decode texture: {textureName}");
                }

                uint textureId = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2D, textureId);

                fixed (byte* ptr = image.Data)
                {
                    Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                                 (uint)image.Width, (uint)image.Height, 0,
                                 PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }

                // Set texture parameters
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
                Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);

                Gl.GenerateMipmap(TextureTarget.Texture2D);
                Gl.BindTexture(TextureTarget.Texture2D, 0);

                Console.WriteLine($"Texture loaded successfully: {textureName} ({image.Width}x{image.Height})");

                return textureId;
            }
        }

        private static unsafe TexturedObjGlObject CreateTexturedOpenGlObject(GL Gl, uint vao, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            uint offsetPos = 0;
            uint offsetNormal = offsetPos + (3 * sizeof(float));
            uint offsetTexCoord = offsetNormal + (3 * sizeof(float)); 
            uint vertexSize = offsetTexCoord + (2 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);

            Gl.EnableVertexAttribArray(2);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);

            Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexCoord);
            Gl.EnableVertexAttribArray(3);

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

            return new TexturedObjGlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
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

        private static unsafe void CreateGlArraysFromObjArrays(float[] faceColor, List<float[]> objVertices, List<int[]> objFaces, List<float[]> objNormals, List<float[]> objTexCoords, List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            List<Vertex> uniqueVertices = new List<Vertex>();
            Dictionary<Vertex, uint> vertexToIndex = new Dictionary<Vertex, uint>();

            bool normalsProvided = objNormals.Count > 0;
            bool texCoordsProvided = objTexCoords.Count > 0;

            Console.WriteLine($"Processing OBJ: {objVertices.Count} vertices, {objFaces.Count} faces, {objNormals.Count} normals");

            foreach (var objFace in objFaces)
            {
                if (objFace.Length < 9)
                {
                    Console.WriteLine($"Warning: Invalid face with {objFace.Length} elements, skipping");
                    continue;
                }

                Vector3D<float> computedNormal = Vector3D<float>.Zero;
                if (!normalsProvided)
                {
                    if (objFace[0] >= 0 && objFace[0] < objVertices.Count &&
                        objFace[3] >= 0 && objFace[3] < objVertices.Count &&
                        objFace[6] >= 0 && objFace[6] < objVertices.Count)
                    {
                        var aObjVertex = objVertices[objFace[0]];
                        var bObjVertex = objVertices[objFace[3]];
                        var cObjVertex = objVertices[objFace[6]];

                        var a = new Vector3D<float>(aObjVertex[0], aObjVertex[1], aObjVertex[2]);
                        var b = new Vector3D<float>(bObjVertex[0], bObjVertex[1], bObjVertex[2]);
                        var c = new Vector3D<float>(cObjVertex[0], cObjVertex[1], cObjVertex[2]);

                        var edge1 = b - a;
                        var edge2 = c - a;
                        computedNormal = Vector3D.Normalize(Vector3D.Cross(edge1, edge2));
                    }
                }

                // Process 3 vertices of the triangle
                for (int i = 0; i < 3; ++i)
                {
                    int vertexIdx = objFace[i * 3 + 0];
                    int texIdx = objFace[i * 3 + 1];
                    int normalIdx = objFace[i * 3 + 2];

                    if (vertexIdx < 0 || vertexIdx >= objVertices.Count)
                    {
                        Console.WriteLine($"Warning: Invalid vertex index {vertexIdx}, skipping face");
                        continue;
                    }

                    var vertex = objVertices[vertexIdx];
                    Vector3D<float> normal;

                    if (normalsProvided && normalIdx >= 0 && normalIdx < objNormals.Count)
                    {
                        var n = objNormals[normalIdx];
                        normal = new Vector3D<float>(n[0], n[1], n[2]);
                    }
                    else
                    {
                        normal = computedNormal;
                    }

                    Vector2D<float> texCoord = Vector2D<float>.Zero;
                    if (texCoordsProvided && texIdx >= 0 && texIdx < objTexCoords.Count)
                    {
                        var tc = objTexCoords[texIdx];
                        texCoord = new Vector2D<float>(tc[0], 1.0f - tc[1]); // Y flip OpenGL-hez
                    }


                    var newVertex = new Vertex
                    {
                        Position = new Vector3D<float>(vertex[0], vertex[1], vertex[2]),
                        Normal = normal,
                        TexCoord = texCoord
                    };

                    uint index;
                    if (vertexToIndex.TryGetValue(newVertex, out index))
                    {

                        glIndices.Add(index);
                    }
                    else
                    {

                        index = (uint)uniqueVertices.Count;
                        uniqueVertices.Add(newVertex);
                        vertexToIndex[newVertex] = index;


                        glVertices.Add(newVertex.Position.X);
                        glVertices.Add(newVertex.Position.Y);
                        glVertices.Add(newVertex.Position.Z);
                        glVertices.Add(newVertex.Normal.X);
                        glVertices.Add(newVertex.Normal.Y);
                        glVertices.Add(newVertex.Normal.Z);
                        glVertices.Add(newVertex.TexCoord.X); 
                        glVertices.Add(newVertex.TexCoord.Y);


                        glColors.AddRange(faceColor);

                        glIndices.Add(index);
                    }
                }
            }

            Console.WriteLine($"Created {uniqueVertices.Count} unique vertices, {glIndices.Count} indices");
        }
 
        private struct Vertex : IEquatable<Vertex>
        {
            public Vector3D<float> Position;
            public Vector3D<float> Normal;
            public Vector2D<float> TexCoord;

            public bool Equals(Vertex other)
            {
                const float epsilon = 0.001f;
                return Math.Abs(Position.X - other.Position.X) < epsilon &&
                       Math.Abs(Position.Y - other.Position.Y) < epsilon &&
                       Math.Abs(Position.Z - other.Position.Z) < epsilon &&
                       Math.Abs(Normal.X - other.Normal.X) < epsilon &&
                       Math.Abs(Normal.Y - other.Normal.Y) < epsilon &&
                       Math.Abs(Normal.Z - other.Normal.Z) < epsilon &&
                       Math.Abs(TexCoord.X - other.TexCoord.X) < epsilon &&
                       Math.Abs(TexCoord.Y - other.TexCoord.Y) < epsilon;
            }

        }

        public static unsafe void ReadObjDataFromResource(string resourceName, out List<float[]> objVertices, out List<int[]> objFaces, out List<float[]> objNormals, out List<float[]> objTexCoords)
        {
            objVertices = new List<float[]>();
            objFaces = new List<int[]>();
            objNormals = new List<float[]>();
            objTexCoords = new List<float[]>();

            string fullResourceName = "Projekt_OpenGL.Resources." + resourceName;

            using (Stream objStream = typeof(ObjResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            {
                if (objStream == null)
                {
                    throw new Exception($"Could not find embedded resource: {fullResourceName}");
                }

                using (StreamReader objReader = new StreamReader(objStream))
                {
                    int lineNumber = 0;
                    while (!objReader.EndOfStream)
                    {
                        lineNumber++;
                        var line = objReader.ReadLine()?.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                            continue;

                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 0)
                            continue;

                        var lineClassifier = parts[0];

                        try
                        {
                            switch (lineClassifier)
                            {
                                case "v":
                                    if (parts.Length >= 4)
                                    {
                                        objVertices.Add(new float[]
                                        {
                                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                                        });
                                    }
                                    break;

                                case "vt":
                                    if (parts.Length >= 3)
                                    {
                                        objTexCoords.Add(new float[]
                                        {
                                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                                            float.Parse(parts[2], CultureInfo.InvariantCulture)
                                        });
                                    }
                                    break;

                                case "vn":
                                    if (parts.Length >= 4)
                                    {
                                        objNormals.Add(new float[]
                                        {
                                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                                            float.Parse(parts[3], CultureInfo.InvariantCulture)
                                        });
                                    }
                                    break;

                                case "f":
                                    if (parts.Length >= 4)
                                    {
                                        var faceVertices = new List<int[]>();

                                        for (int i = 1; i < parts.Length; i++)
                                        {
                                            var vertexParts = parts[i].Split('/');
                                            var faceVertex = new int[3] { -1, -1, -1 }; // v, vt, vn

                                            // Vertex index (required)
                                            if (vertexParts.Length > 0 && !string.IsNullOrEmpty(vertexParts[0]))
                                            {
                                                faceVertex[0] = int.Parse(vertexParts[0], CultureInfo.InvariantCulture) - 1;
                                            }

                                            // Texture coordinate index (optional)
                                            if (vertexParts.Length > 1 && !string.IsNullOrEmpty(vertexParts[1]))
                                            {
                                                faceVertex[1] = int.Parse(vertexParts[1], CultureInfo.InvariantCulture) - 1;
                                            }

                                            // Normal index (optional)
                                            if (vertexParts.Length > 2 && !string.IsNullOrEmpty(vertexParts[2]))
                                            {
                                                faceVertex[2] = int.Parse(vertexParts[2], CultureInfo.InvariantCulture) - 1;
                                            }

                                            faceVertices.Add(faceVertex);
                                        }

                                        if (faceVertices.Count == 3)
                                        {
                                            // Triangle
                                            int[] face = new int[9];
                                            for (int i = 0; i < 3; i++)
                                            {
                                                face[i * 3 + 0] = faceVertices[i][0];
                                                face[i * 3 + 1] = faceVertices[i][1];
                                                face[i * 3 + 2] = faceVertices[i][2];
                                            }
                                            objFaces.Add(face);
                                        }
                                        else if (faceVertices.Count == 4)
                                        {
                                            // Triangle 1: 0, 1, 2
                                            int[] face1 = new int[9];
                                            face1[0] = faceVertices[0][0]; face1[1] = faceVertices[0][1]; face1[2] = faceVertices[0][2];
                                            face1[3] = faceVertices[1][0]; face1[4] = faceVertices[1][1]; face1[5] = faceVertices[1][2];
                                            face1[6] = faceVertices[2][0]; face1[7] = faceVertices[2][1]; face1[8] = faceVertices[2][2];
                                            objFaces.Add(face1);

                                            // Triangle 2: 0, 2, 3
                                            int[] face2 = new int[9];
                                            face2[0] = faceVertices[0][0]; face2[1] = faceVertices[0][1]; face2[2] = faceVertices[0][2];
                                            face2[3] = faceVertices[2][0]; face2[4] = faceVertices[2][1]; face2[5] = faceVertices[2][2];
                                            face2[6] = faceVertices[3][0]; face2[7] = faceVertices[3][1]; face2[8] = faceVertices[3][2];
                                            objFaces.Add(face2);
                                        }
                                        else if (faceVertices.Count > 4)
                                        {
                                            // N-gon -> triangle fan
                                            for (int i = 1; i < faceVertices.Count - 1; i++)
                                            {
                                                int[] face = new int[9];
                                                face[0] = faceVertices[0][0]; face[1] = faceVertices[0][1]; face[2] = faceVertices[0][2];
                                                face[3] = faceVertices[i][0]; face[4] = faceVertices[i][1]; face[5] = faceVertices[i][2];
                                                face[6] = faceVertices[i + 1][0]; face[7] = faceVertices[i + 1][1]; face[8] = faceVertices[i + 1][2];
                                                objFaces.Add(face);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Error parsing line {lineNumber}: '{line}' - {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine($"Loaded OBJ: {objVertices.Count} vertices, {objTexCoords.Count} tex coords, {objNormals.Count} normals, {objFaces.Count} faces");
        }
        public static (Vector3 min, Vector3 max) GetLocalBoundingBox(string resourceName)
        {
            ReadObjDataFromResource(resourceName,
                out List<float[]> verts, out _, out _, out _);

            var first = verts[0];
            Vector3 min = new Vector3(first[0], first[1], first[2]);
            Vector3 max = min;

            foreach (var v in verts)
            {
                min.X = MathF.Min(min.X, v[0]);
                min.Y = MathF.Min(min.Y, v[1]);
                min.Z = MathF.Min(min.Z, v[2]);
                max.X = MathF.Max(max.X, v[0]);
                max.Y = MathF.Max(max.Y, v[1]);
                max.Z = MathF.Max(max.Z, v[2]);
            }

            return (min, max);
        }

    }
}