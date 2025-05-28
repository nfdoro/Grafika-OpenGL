using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Globalization;
using System.Xml;
using StbImageSharp;

namespace Projekt_OpenGL
{
    internal class ColladaResourceReader
    {
        public class TexturedGlObject : GlObject
        {
            public uint? AlbedoTextureId { get; }
            public uint? AOTextureId { get; }
            public uint? NormalTextureId { get; }
            public uint? MetallicTextureId { get; }
            public uint? RoughnessTextureId { get; }
            public uint? OpacityTextureId { get; }

            public TexturedGlObject(uint vao, uint vertices, uint colors, uint indices,
                                   uint indexArrayLength, GL gl,
                                   uint? albedoTexture = null,
                                   uint? aoTexture = null,
                                   uint? normalTexture = null,
                                   uint? metallicTexture = null,
                                   uint? roughnessTexture = null,
                                   uint? opacityTexture = null)
                : base(vao, vertices, colors, indices, indexArrayLength, gl)
            {
                AlbedoTextureId = albedoTexture;
                AOTextureId = aoTexture;
                NormalTextureId = normalTexture;
                MetallicTextureId = metallicTexture;
                RoughnessTextureId = roughnessTexture;
                OpacityTextureId = opacityTexture;
            }
        }


        public static unsafe List<GlObject> CreateMultipleObjectsFromResource(GL Gl,
                                                                      string resourceName,
                                                                      float[] faceColor)
        {
            List<GlObject> objects = new List<GlObject>();

            string fullResourceName = "Projekt_OpenGL.Resources.model." + resourceName;

            using (Stream stream = typeof(ColladaResourceReader)
                                    .Assembly.GetManifestResourceStream(fullResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("c", "http://www.collada.org/2005/11/COLLADASchema");

              
                Dictionary<string, Dictionary<string, string>> materialTextureFiles;
                ReadMaterialTextureMaps(doc, nsmgr, out materialTextureFiles);
                var geometryMaterial = BuildGeometryMaterialMap(doc, nsmgr);

                Console.WriteLine( "Material-Texture mapping");
                foreach (var mat in materialTextureFiles)
                {
                    Console.WriteLine($"Material: {mat.Key}");
                    foreach (var tex in mat.Value)
                        Console.WriteLine($"  {tex.Key}: {tex.Value}");
                }

                XmlNodeList geometryNodes =
                    doc.SelectNodes("//c:library_geometries/c:geometry", nsmgr);

                foreach (XmlNode geometryNode in geometryNodes)
                {
                    string geometryId = geometryNode.Attributes["id"]?.Value ?? "unknown";
                    Console.WriteLine($"Loading geometry: {geometryId}");

                    XmlNode meshNode = geometryNode.SelectSingleNode("c:mesh", nsmgr);
                    if (meshNode == null) continue;

                    try
                    {
                    
                        uint vao = Gl.GenVertexArray();
                        Gl.BindVertexArray(vao);

                        List<float[]> colladaVertices;
                        List<int[]> colladaFaces;
                        List<float[]> colladaNormals;
                        List<float[]> colladaTexCoords;
                        string materialId;     

                        ProcessMeshNode(meshNode, nsmgr,
                                        out colladaVertices,
                                        out colladaFaces,
                                        out colladaNormals,
                                        out colladaTexCoords,
                                        out materialId);

                        int meshNumber = -1;
                        if (geometryId.StartsWith("meshId") &&
                            int.TryParse(geometryId.Substring(6), out int num))
                            meshNumber = num;

                        List<float> glVertices = new();
                        List<float> glColors = new();
                        List<uint> glIndices = new();

                        CreateGlArraysFromColladaArrays(faceColor,
                                                        colladaVertices,
                                                        colladaFaces,
                                                        colladaNormals,
                                                        colladaTexCoords,
                                                        glVertices,
                                                        glColors,
                                                        glIndices);

                        string effectiveMaterialId = materialId;

                        if ((effectiveMaterialId == "defaultMaterial"
                             || !materialTextureFiles.ContainsKey(effectiveMaterialId))
                            && geometryMaterial.TryGetValue(geometryId, out var sceneMat))
                        {
                            effectiveMaterialId = sceneMat;
                        }

                        uint? albedoTextureId = LoadAlbedoTexture(Gl, effectiveMaterialId, materialTextureFiles);
                        uint? aoTextureId = LoadAOTexture(Gl, effectiveMaterialId, materialTextureFiles);
                        uint? metallicTextureId = LoadMetallicTexture(Gl, effectiveMaterialId, materialTextureFiles);
                        uint? normalTextureId = LoadNormalTexture(Gl, effectiveMaterialId, materialTextureFiles);
                        //uint? roughnessTextureId = LoadRoughnessTexture(Gl, effectiveMaterialId, materialTextureFiles);
                        uint? opacityTextureId = LoadOpacityTexture(Gl, effectiveMaterialId, materialTextureFiles);

         
                        if (glIndices.Count > 0)
                        {
                            GlObject obj = CreateOpenGlObjectWithMultipleTextures(Gl, vao, glVertices, glColors, glIndices,
                            albedoTextureId, aoTextureId, normalTextureId, metallicTextureId, null, opacityTextureId); 
                            objects.Add(obj);
                            Console.WriteLine($"Created object with {glIndices.Count / 3} triangles, albedo: {(albedoTextureId.HasValue ? "YES" : "NO")}, AO: {(aoTextureId.HasValue ? "YES" : "NO")}, metallic: {(metallicTextureId.HasValue ? "YES" : "NO")}, normal: {(normalTextureId.HasValue ? "YES" : "NO")}, roughness:, opacity: {(opacityTextureId.HasValue ? "YES" : "NO")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load geometry {geometryId}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"Loaded {objects.Count} objects from {resourceName}");
            return objects;
        }



        private static uint? LoadAlbedoTexture(GL gl,
                                       string materialId,
                                       Dictionary<string, Dictionary<string, string>> matTex)
        {
            if (matTex.TryGetValue(materialId, out var texMap) &&
                texMap.TryGetValue("albedo", out string file))
            {
                return TryLoadTextureFromResource(gl, file);        
            }

            Console.WriteLine($"[Warning]: There is no albedo texture for materialID {materialId}");
            return null;                                            
        }


        public static unsafe GlObject CreateObjectFromResource(GL Gl, string resourceName, float[] faceColor)
        {
            var objects = CreateMultipleObjectsFromResource(Gl, resourceName, faceColor);
            return objects.Count > 0 ? objects[0] : null;
        }


        private static void ProcessMeshNode(XmlNode meshNode, XmlNamespaceManager nsmgr,
            out List<float[]> colladaVertices, out List<int[]> colladaFaces,
            out List<float[]> colladaNormals, out List<float[]> colladaTexCoords,
            out string materialId)
        {
            colladaVertices = new List<float[]>();
            colladaFaces = new List<int[]>();
            colladaNormals = new List<float[]>();
            colladaTexCoords = new List<float[]>();
            materialId = "";

            Dictionary<string, List<float>> sources = new Dictionary<string, List<float>>();
            Dictionary<string, string> sourceTypes = new Dictionary<string, string>();

            // Extract sources
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

                    // Determine source type
                    XmlNode techniqueNode = sourceNode.SelectSingleNode(".//c:technique_common", nsmgr);
                    XmlNode paramNode = techniqueNode?.SelectSingleNode(".//c:param", nsmgr);
                    string paramName = paramNode?.Attributes["name"]?.Value ?? "";

                    var accessorNode = techniqueNode?.SelectSingleNode(".//c:accessor", nsmgr);
                    int stride = accessorNode != null
                        ? int.Parse(accessorNode.Attributes["stride"]?.Value ?? "1")
                        : 1;

                    bool looksLikeTex = id.ToLower().Contains("tex") || stride == 2;

                    if (paramName.ToLower().Contains("texcoord") || paramName.ToLower().Contains("uv") ||
                        id.ToLower().Contains("texcoord") || id.ToLower().Contains("uv") ||
                        looksLikeTex)
                        sourceTypes[id] = "TEXCOORD";
                    else if (id.ToLower().Contains("normal"))
                        sourceTypes[id] = "NORMAL";
                    else if (id.ToLower().Contains("position"))
                        sourceTypes[id] = "POSITION";
                }
            }

            // Process triangles/polylist
            XmlNode trianglesNode = meshNode.SelectSingleNode("c:triangles", nsmgr) ??
                                   meshNode.SelectSingleNode("c:polylist", nsmgr);

            if (trianglesNode != null)
            {
                materialId = trianglesNode.Attributes["material"]?.Value ?? "";
                Console.WriteLine($"Material ID for mesh: '{materialId}'");

                var inputNodes = trianglesNode.SelectNodes("c:input", nsmgr);
                Dictionary<string, int> offsets = new Dictionary<string, int>();
                Dictionary<int, string> offsetToSource = new Dictionary<int, string>();
                Dictionary<string, string> semanticToSource = new Dictionary<string, string>();

                foreach (XmlNode inputNode in inputNodes)
                {
                    string semantic = inputNode.Attributes["semantic"].Value;
                    int offset = int.Parse(inputNode.Attributes["offset"].Value);
                    string source = inputNode.Attributes["source"].Value.Substring(1);

                    offsets[semantic] = offset;
                    offsetToSource[offset] = source;
                    semanticToSource[semantic] = source;
                }

                if (semanticToSource.TryGetValue("TEXCOORD", out var texSourceId)
                    && sources.ContainsKey(texSourceId))
                {
                    var flat = sources[texSourceId];
                    for (int i = 0; i < flat.Count; i += 2)
                        colladaTexCoords.Add(new float[] { flat[i], flat[i + 1] });
                }

                int stride = offsets.Count > 0 ? offsets.Values.Max() + 1 : 1;

                XmlNode pNode = trianglesNode.SelectSingleNode("c:p", nsmgr);
                if (pNode != null)
                {
                    string[] parts = pNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    // Handle polylist with vertex counts
                    if (trianglesNode.Name == "polylist")
                    {
                        XmlNode vcountNode = trianglesNode.SelectSingleNode("c:vcount", nsmgr);
                        if (vcountNode != null)
                        {
                            string[] vcounts = vcountNode.InnerText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            int dataIndex = 0;

                            foreach (string vcountStr in vcounts)
                            {
                                int vertexCount = int.Parse(vcountStr);

                                if (vertexCount == 3)
                                {
                                    int[] faceIndices = new int[9];

                                    for (int v = 0; v < 3; v++)
                                    {
                                        faceIndices[v * 3] = -1;
                                        faceIndices[v * 3 + 1] = -1;
                                        faceIndices[v * 3 + 2] = -1;

                                        if (offsets.ContainsKey("VERTEX"))
                                        {
                                            faceIndices[v * 3] = int.Parse(parts[dataIndex + v * stride + offsets["VERTEX"]]);
                                        }

                                        if (offsets.ContainsKey("NORMAL"))
                                        {
                                            faceIndices[v * 3 + 1] = int.Parse(parts[dataIndex + v * stride + offsets["NORMAL"]]);
                                        }

                                        if (offsets.ContainsKey("TEXCOORD"))
                                        {
                                            faceIndices[v * 3 + 2] = int.Parse(parts[dataIndex + v * stride + offsets["TEXCOORD"]]);
                                        }
                                    }

                                    colladaFaces.Add(faceIndices);
                                }

                                dataIndex += vertexCount * stride;
                            }
                        }
                    }
                    else
                    {
                        // Handle regular triangles
                        for (int i = 0; i < parts.Length; i += stride * 3)
                        {
                            int[] faceIndices = new int[9];

                            for (int v = 0; v < 3; v++)
                            {
                                faceIndices[v * 3] = -1;
                                faceIndices[v * 3 + 1] = -1;
                                faceIndices[v * 3 + 2] = -1;

                                if (offsets.ContainsKey("VERTEX"))
                                {
                                    faceIndices[v * 3] = int.Parse(parts[i + v * stride + offsets["VERTEX"]]);
                                }

                                if (offsets.ContainsKey("NORMAL"))
                                {
                                    faceIndices[v * 3 + 1] = int.Parse(parts[i + v * stride + offsets["NORMAL"]]);
                                }

                                if (offsets.ContainsKey("TEXCOORD"))
                                {
                                    faceIndices[v * 3 + 2] = int.Parse(parts[i + v * stride + offsets["TEXCOORD"]]);
                                }
                            }

                            colladaFaces.Add(faceIndices);
                        }
                    }
                }
            }

            // Extract vertex positions
            XmlNode verticesNode = meshNode.SelectSingleNode("c:vertices", nsmgr);
            string positionSourceId = verticesNode?.SelectSingleNode("c:input[@semantic='POSITION']", nsmgr)?.Attributes["source"].Value.Substring(1);

            if (positionSourceId != null && sources.ContainsKey(positionSourceId))
            {
                var verts = sources[positionSourceId];
                for (int i = 0; i < verts.Count; i += 3)
                {
                    colladaVertices.Add(new float[] { verts[i], verts[i + 1], verts[i + 2] });
                }
            }

            // Extract normals and texture coordinates
            foreach (var kv in sources)
            {
                if (sourceTypes.ContainsKey(kv.Key))
                {
                    if (sourceTypes[kv.Key] == "NORMAL")
                    {
                        var norms = kv.Value;
                        for (int i = 0; i < norms.Count; i += 3)
                        {
                            colladaNormals.Add(new float[] { norms[i], norms[i + 1], norms[i + 2] });
                        }
                    }
                    else if (sourceTypes[kv.Key] == "TEXCOORD")
                    {
                        var texCoords = kv.Value;
                        for (int i = 0; i < texCoords.Count; i += 2)
                        {
                            colladaTexCoords.Add(new float[] { texCoords[i], texCoords[i + 1] });
                        }
                    }
                }
            }
        }

        public static unsafe uint? TryLoadTextureFromResource(GL Gl, string textureFileName)
        {
            try
            {
                return LoadTextureFromResource(Gl, textureFileName);
            }
            catch
            {
                return null;
            }
        }

        public static unsafe uint LoadTextureFromResource(GL Gl, string textureFileName)
        {
            uint texture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, texture);

            string[] possiblePaths = new string[]
            {
                $"Projekt_OpenGL.Resources.{textureFileName}",
                $"Projekt_OpenGL.Resources.textures.{textureFileName}",
                $"Projekt_OpenGL.Resources.model.textures.{textureFileName}"
            };

            bool textureLoaded = false;

            foreach (string resourcePath in possiblePaths)
            {
                try
                {
                    using (Stream stream = typeof(ColladaResourceReader).Assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                            fixed (byte* ptr = image.Data)
                            {
                                Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                                    (uint)image.Width, (uint)image.Height, 0,
                                    GLEnum.Rgba, PixelType.UnsignedByte, ptr);
                            }

                            textureLoaded = true;
                            Console.WriteLine($"Loaded texture: {textureFileName} from {resourcePath}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load texture from {resourcePath}: {ex.Message}");
                }
            }

            if (!textureLoaded)
            {
                Console.WriteLine($"Failed to load texture {textureFileName} from any path. Creating default white texture.");
                uint[] whitePixel = { 0xFFFFFFFF };
                fixed (uint* ptr = whitePixel)
                {
                    Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                        1, 1, 0, GLEnum.Rgba, PixelType.UnsignedByte, ptr);
                }
            }

            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            Gl.GenerateMipmap(TextureTarget.Texture2D);

            return texture;
        }

        private static unsafe GlObject CreateOpenGlObject(GL Gl, uint vao, List<float> glVertices,
            List<float> glColors, List<uint> glIndices, uint? textureId = null)
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

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(2);

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

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            if (textureId.HasValue)
            {
                return new TexturedGlObject(vao, vertices, colors, indices, indexArrayLength, Gl, textureId.Value);
            }

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }

        private static unsafe void CreateGlArraysFromColladaArrays(float[] faceColor, List<float[]> colladaVertices,
            List<int[]> colladaFaces, List<float[]> colladaNormals, List<float[]> colladaTexCoords,
            List<float> glVertices, List<float> glColors, List<uint> glIndices)
        {
            Console.WriteLine($"Texture coordinates provided: {colladaTexCoords.Count > 0}");
            if (colladaTexCoords.Count > 0)
            {
                Console.WriteLine($"Sample texture coordinates: ({colladaTexCoords[0][0]}, {colladaTexCoords[0][1]})");
            }

            Dictionary<string, int> glVertexIndices = new Dictionary<string, int>();
            bool normalsProvided = colladaNormals.Count > 0;
            bool texCoordsProvided = colladaTexCoords.Count > 0;

            foreach (var face in colladaFaces)
            {
                Vector3D<float> computedNormal = Vector3D<float>.Zero;

                if (!normalsProvided && face.Length >= 3)
                {
                    int v0 = face[0] >= 0 && face[0] < colladaVertices.Count ? face[0] : 0;
                    int v1 = face[3] >= 0 && face[3] < colladaVertices.Count ? face[3] : 0;
                    int v2 = face[6] >= 0 && face[6] < colladaVertices.Count ? face[6] : 0;

                    var a = new Vector3D<float>(colladaVertices[v0][0], colladaVertices[v0][1], colladaVertices[v0][2]);
                    var b = new Vector3D<float>(colladaVertices[v1][0], colladaVertices[v1][1], colladaVertices[v1][2]);
                    var c = new Vector3D<float>(colladaVertices[v2][0], colladaVertices[v2][1], colladaVertices[v2][2]);

                    computedNormal = Vector3D.Normalize(Vector3D.Cross(b - a, c - a));
                }

                for (int i = 0; i < 3; ++i)
                {
                    int vertexIdx = face[i * 3];
                    int normalIdx = face[i * 3 + 1];
                    int texCoordIdx = face[i * 3 + 2];

                    if (vertexIdx < 0 || vertexIdx >= colladaVertices.Count)
                        continue;

                    var vertex = colladaVertices[vertexIdx];
                    Vector3D<float> normal;

                    if (normalsProvided && normalIdx >= 0 && normalIdx < colladaNormals.Count)
                    {
                        var n = colladaNormals[normalIdx];
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

                    if (texCoordsProvided && texCoordIdx >= 0 && texCoordIdx < colladaTexCoords.Count)
                    {
                        glVertex.Add(colladaTexCoords[texCoordIdx][0]);
                        glVertex.Add(1.0f - colladaTexCoords[texCoordIdx][1]);
                    }
                    else
                    {
                        glVertex.Add(0.0f);
                        glVertex.Add(0.0f);
                    }

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

        private static void ReadMaterialTextureMaps(
            XmlDocument doc,
            XmlNamespaceManager nsmgr,
            out Dictionary<string, Dictionary<string, string>> materialTextureFiles)
        {
            // 1) imageId -> filepath
            var imagePaths = new Dictionary<string, string>();
            foreach (XmlNode img in doc.SelectNodes("//c:library_images/c:image", nsmgr))
            {
                string imgId = img.Attributes["id"]?.Value;
                XmlNode initFromNode = img.SelectSingleNode(".//c:init_from", nsmgr);
                if (imgId != null && initFromNode != null)
                {
                    string initFrom = initFromNode.InnerText;
                    imagePaths[imgId] = Path.GetFileName(initFrom);
                    Console.WriteLine($"Image mapping: {imgId} -> {Path.GetFileName(initFrom)}");
                }
            }

            // 2) samplerSid -> imageId (library_effects/newparam/surface)
            var samplerImage = new Dictionary<string, string>();
            foreach (XmlNode np in doc.SelectNodes("//c:library_effects//c:newparam", nsmgr))
            {
                string sid = np.Attributes["sid"]?.Value;
                XmlNode surf = np.SelectSingleNode("c:surface/c:init_from", nsmgr);
                if (sid != null && surf != null)
                {
                    samplerImage[sid] = surf.InnerText;
                    Console.WriteLine($"Sampler mapping: {sid} -> {surf.InnerText}");
                }
            }

            // 3) effectId -> (semantic -> samplerSid)
            var effectSemanticSampler = new Dictionary<string, Dictionary<string, string>>();
            foreach (XmlNode fx in doc.SelectNodes("//c:library_effects/c:effect", nsmgr))
            {
                string fxId = fx.Attributes["id"]?.Value;
                if (fxId == null) continue;

                var semMap = new Dictionary<string, string>();

                // diffuse → albedo
                XmlNode diff = fx.SelectSingleNode(".//c:diffuse/c:texture", nsmgr);
                if (diff != null)
                {
                    string texAttr = diff.Attributes["texture"]?.Value;
                    if (texAttr != null)
                    {
                        semMap["albedo"] = texAttr;
                        Console.WriteLine($"Effect {fxId}: diffuse -> {texAttr}");
                    }
                }

                // bump → normal
                XmlNode bump = fx.SelectSingleNode(".//c:bump/c:texture", nsmgr);
                if (bump != null)
                {
                    string texAttr = bump.Attributes["texture"]?.Value;
                    if (texAttr != null)
                    {
                        semMap["normal"] = texAttr;
                        Console.WriteLine($"Effect {fxId}: bump -> {texAttr}");
                    }
                }

                // specular → metallic
                XmlNode spec = fx.SelectSingleNode(".//c:specular/c:texture", nsmgr);
                if (spec != null)
                {
                    string texAttr = spec.Attributes["texture"]?.Value;
                    if (texAttr != null)
                    {
                        semMap["metallic"] = texAttr;
                        Console.WriteLine($"Effect {fxId}: specular -> {texAttr}");
                    }
                }

                // További semantic-ek keresése extra részben
                XmlNode extraRough = fx.SelectSingleNode(".//c:extra//c:technique//c:roughness/c:texture", nsmgr);
                if (extraRough != null)
                {
                    string texAttr = extraRough.Attributes["texture"]?.Value;
                    if (texAttr != null)
                    {
                        semMap["roughness"] = texAttr;
                        Console.WriteLine($"Effect {fxId}: roughness -> {texAttr}");
                    }

                }

                XmlNode extraAo = fx.SelectSingleNode(".//c:extra//c:technique//c:ambient_occlusion/c:texture", nsmgr);
                if (extraAo != null)
                {
                    string texAttr = extraAo.Attributes["texture"]?.Value;
                    if (texAttr != null)
                    {
                        semMap["ao"] = texAttr;
                        Console.WriteLine($"Effect {fxId}: ao -> {texAttr}");
                    }
                }

                effectSemanticSampler[fxId] = semMap;
            }

            // 4) materialId -> effectId
            var materialEffect = new Dictionary<string, string>();
            foreach (XmlNode mat in doc.SelectNodes("//c:library_materials/c:material", nsmgr))
            {
                string matId = mat.Attributes["id"]?.Value;
                XmlNode inst = mat.SelectSingleNode("c:instance_effect", nsmgr);
                if (matId != null && inst != null)
                {
                    string effectUrl = inst.Attributes["url"]?.Value;
                    if (effectUrl != null && effectUrl.StartsWith("#"))
                    {
                        materialEffect[matId] = effectUrl.Substring(1);
                        Console.WriteLine($"Material mapping: {matId} -> {effectUrl.Substring(1)}");
                    }
                }
            }

            // 5) materialId -> (semantic -> filename)
            materialTextureFiles = new Dictionary<string, Dictionary<string, string>>();
            foreach (var kv in materialEffect)
            {
                string matId = kv.Key;
                string fxId = kv.Value;

                if (!effectSemanticSampler.ContainsKey(fxId))
                    continue;

                var semMap = effectSemanticSampler[fxId];
                var fileMap = new Dictionary<string, string>();

                foreach (var sem in semMap.Keys)
                {
                    string samplerSid = semMap[sem];
                    if (samplerImage.TryGetValue(samplerSid, out string imgId) &&
                        imagePaths.TryGetValue(imgId, out string fileName))
                    {
                        fileMap[sem] = fileName;
                        Console.WriteLine($"Final mapping: Material {matId} -> {sem} -> {fileName}");
                    }
                }

                if (fileMap.Count > 0)
                {
                    materialTextureFiles[matId] = fileMap;
                }
            }

           
            if (materialTextureFiles.Count == 0)
            {
                Console.WriteLine("No material mappings found, trying fallback approach...");

                foreach (XmlNode mat in doc.SelectNodes("//c:library_materials/c:material", nsmgr))
                {
                    string matId = mat.Attributes["id"]?.Value;
                    if (matId != null)
                    {
                        var fileMap = new Dictionary<string, string>();

                        if (matId.ToLower().Contains("metal"))
                        {
                            if (matId.ToLower().Contains("painted"))
                            {
                                fileMap["albedo"] = "Atlas_Painted_metal_albedo.jpg";
                            }
                            else
                            {
                                fileMap["albedo"] = "Atlas_Metal_albedo.jpg";
                            }
                        }
                        else if (matId.ToLower().Contains("tread"))
                        {
                            fileMap["albedo"] = "Tread_albedo.jpg";
                        }
                        else
                        {
                            // Default
                            fileMap["albedo"] = "Atlas_Metal_albedo.jpg";
                        }

                        materialTextureFiles[matId] = fileMap;
                        Console.WriteLine($"Fallback mapping: Material {matId} -> albedo -> {fileMap["albedo"]}");
                    }
                }
            }
        }

        private static Dictionary<string, string> BuildSymbolMaterialMap(XmlDocument doc,
                                                                XmlNamespaceManager nsmgr)
        {
            var map = new Dictionary<string, string>();

            foreach (XmlNode inst in doc.SelectNodes("//c:instance_material", nsmgr))
            {
                string symbol = inst.Attributes["symbol"]?.Value;
                string target = inst.Attributes["target"]?.Value;   // „#MaterialName”

                if (!string.IsNullOrEmpty(symbol) &&
                    !string.IsNullOrEmpty(target) && target.StartsWith("#"))
                {
                    map[symbol] = target.Substring(1);              
                }
            }
            return map;
        }

        // geometryId  ->  materialId   (
        private static Dictionary<string, string> BuildGeometryMaterialMap(
                                 XmlDocument doc, XmlNamespaceManager nsmgr)
        {
            var map = new Dictionary<string, string>();

            foreach (XmlNode ig in doc.SelectNodes(
                     "//c:library_visual_scenes//c:instance_geometry", nsmgr))
            {
                string geomUrl = ig.Attributes["url"]?.Value;            // "#meshId17"
                if (string.IsNullOrEmpty(geomUrl) || !geomUrl.StartsWith("#")) continue;

                string geomId = geomUrl.Substring(1);                    // "meshId17"

                // az első <instance_material>
                XmlNode im = ig.SelectSingleNode(
                    ".//c:bind_material/c:technique_common/c:instance_material", nsmgr);
                string target = im?.Attributes["target"]?.Value;         // "#Atlas_Painted_metal"

                if (!string.IsNullOrEmpty(target) && target.StartsWith("#"))
                {
                    string matId = target.Substring(1);
                    map[geomId] = matId;
                    Console.WriteLine($"Geometry mapping: {geomId} -> {matId}");
                }
            }
            return map;
        }

        private static uint? LoadAOTexture(GL gl,
                                 string materialId,
                                 Dictionary<string, Dictionary<string, string>> matTex)
        {
            
            if (matTex.TryGetValue(materialId, out var texMap) &&
                texMap.TryGetValue("ao", out string file))
            {
                uint? aoTexture = TryLoadTextureFromResource(gl, file);
                if (aoTexture.HasValue)
                {
                    Console.WriteLine($"Loaded AO texture from COLLADA for {materialId}: {file}");
                    return aoTexture;
                }
            }

            
            Dictionary<string, string> materialToAOMap = new Dictionary<string, string>
            {
                { "Atlas_Metal", "Atlas_Metal_AO.jpg" },
                { "Atlas_Painted_metal", "Atlas_Painted_metal_AO.jpg" },
                { "Tread", "Tread_AO.jpg" }
            };

            if (materialToAOMap.TryGetValue(materialId, out string aoFileName))
            {
                uint? aoTexture = TryLoadTextureFromResource(gl, aoFileName);
                if (aoTexture.HasValue)
                {
                    Console.WriteLine($"Loaded AO texture by convention for {materialId}: {aoFileName}");
                    return aoTexture;
                }
            }

            Console.WriteLine($"[Warning]: There is no AO texture for materialID {materialId}");
            return null;
        }

        private static unsafe GlObject CreateOpenGlObjectWithMultipleTextures(GL Gl, uint vao,
        List<float> glVertices, List<float> glColors, List<uint> glIndices,
        uint? albedoTextureId = null, uint? aoTextureId = null, uint? normalTextureId = null,
        uint? metallicTextureId = null, uint? roughnessTextureId = null, uint? opacityTextureId = null)
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

            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetNormal);
            Gl.EnableVertexAttribArray(2);

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

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            uint indexArrayLength = (uint)glIndices.Count;

            if (albedoTextureId.HasValue || aoTextureId.HasValue || normalTextureId.HasValue ||
            metallicTextureId.HasValue || roughnessTextureId.HasValue || opacityTextureId.HasValue)
            {
                return new TexturedGlObject(vao, vertices, colors, indices, indexArrayLength, Gl,
                            albedoTextureId, aoTextureId, normalTextureId,
                            metallicTextureId, roughnessTextureId, opacityTextureId);
            }

            return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
        }


        private static uint? LoadMetallicTexture(GL gl,
                                       string materialId,
                                       Dictionary<string, Dictionary<string, string>> matTex)
        {
           
            if (matTex.TryGetValue(materialId, out var texMap) &&
                texMap.TryGetValue("metallic", out string file))
            {
                uint? metallicTexture = TryLoadTextureFromResource(gl, file);
                if (metallicTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Metallic texture from COLLADA for {materialId}: {file}");
                    return metallicTexture;
                }
            }

       
            Dictionary<string, string> materialToMetallicMap = new Dictionary<string, string>
            {
                { "Atlas_Metal", "Atlas_Metal_metallic.jpg" },
                { "Atlas_Painted_metal", "Atlas_Painted_metal_metallic.jpg" },
                { "Tread", "Tread_metallic.jpg" }
            };

            if (materialToMetallicMap.TryGetValue(materialId, out string metallicFileName))
            {
                uint? metallicTexture = TryLoadTextureFromResource(gl, metallicFileName);
                if (metallicTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Metallic texture by convention for {materialId}: {metallicFileName}");
                    return metallicTexture;
                }
            }

            Console.WriteLine($"[Warning]: There is no metalic texture for materialID {materialId}");
            return null;
        }


        private static uint? LoadNormalTexture(GL gl,
                                      string materialId,
                                      Dictionary<string, Dictionary<string, string>> matTex)
        {
            if (matTex.TryGetValue(materialId, out var texMap) &&
                texMap.TryGetValue("normal", out string file))
            {
                uint? normalTexture = TryLoadTextureFromResource(gl, file);
                if (normalTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Normal texture from COLLADA for {materialId}: {file}");
                    return normalTexture;
                }
            }

            Dictionary<string, string> materialToNormalMap = new Dictionary<string, string>
            {
                { "Atlas_Metal", "Atlas_Metal_normal.png" },
                { "Atlas_Painted_metal", "Atlas_Painted_metal_normal.png" },
                { "Tread", "Tread_normal.png" }
            };

            if (materialToNormalMap.TryGetValue(materialId, out string normalFileName))
            {
                uint? normalTexture = TryLoadTextureFromResource(gl, normalFileName);
                if (normalTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Normal texture by convention for {materialId}: {normalFileName}");
                    return normalTexture;
                }
            }

            Console.WriteLine($"[Warning]: There is no normal texture for materialID {materialId}");
            return null;
        }

        private static uint? LoadOpacityTexture(GL gl,
                                       string materialId,
                                       Dictionary<string, Dictionary<string, string>> matTex)
        {
            if (matTex.TryGetValue(materialId, out var texMap) &&
                texMap.TryGetValue("opacity", out string file))
            {
                uint? opacityTexture = TryLoadTextureFromResource(gl, file);
                if (opacityTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Opacity texture from COLLADA for {materialId}: {file}");
                    return opacityTexture;
                }
            }

            Dictionary<string, string> materialToOpacityMap = new Dictionary<string, string>
            {
                { "Atlas_Metal", "Atlas_Metal_opacity.jpg" },
                { "Atlas_Painted_metal", "Atlas_Painted_metal_opacity.jpg" },
                { "Tread", "Tread_opacity.jpg" }
            };

            if (materialToOpacityMap.TryGetValue(materialId, out string opacityFileName))
            {
                uint? opacityTexture = TryLoadTextureFromResource(gl, opacityFileName);
                if (opacityTexture.HasValue)
                {
                    Console.WriteLine($"Loaded Opacity texture by convention for {materialId}: {opacityFileName}");
                    return opacityTexture;
                }
            }

            Console.WriteLine($"[Warning]: There is no opacity texture for materialID {materialId}");
            return null;
        }

      

    }
}