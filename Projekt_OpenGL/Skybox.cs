using Silk.NET.OpenGL;
using StbImageSharp;
using System.Reflection;

namespace Projekt_OpenGL
{
    internal class Skybox
    {
        private uint vao;
        private uint vbo;
        private uint cubemapTexture;
        private GL gl;


        private static readonly float[] skyboxVertices = {
            // positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
             1.0f,  1.0f, -1.0f,
             1.0f,  1.0f,  1.0f,
             1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f, -1.0f,
             1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
             1.0f, -1.0f,  1.0f
        };

        public Skybox(GL gl)
        {
            this.gl = gl;
            SetupSkybox();
            LoadCubemap();
        }

        private unsafe void SetupSkybox()
        {
            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();

            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);

            fixed (float* vertices = skyboxVertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer,
                    (nuint)(skyboxVertices.Length * sizeof(float)),
                    vertices, BufferUsageARB.StaticDraw);
            }

            gl.EnableVertexAttribArray(0);
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), null);

            gl.BindVertexArray(0);
        }

        private unsafe uint LoadCubemap()
        {
            cubemapTexture = gl.GenTexture();
            gl.BindTexture(TextureTarget.TextureCubeMap, cubemapTexture);

            string resourcePath = "Projekt_OpenGL.Resources.skybox.Cubemap_Desert_up.png";

            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        StbImage.stbi_set_flip_vertically_on_load(0);
                        ImageResult fullImage = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

                        Console.WriteLine($"Loaded cubemap image: {fullImage.Width}x{fullImage.Height} ({fullImage.Comp})");


                        int faceSize = fullImage.Width / 4;

                        Console.WriteLine($"Using FULL QUALITY face size: {faceSize}x{faceSize}");


                        var facePositions = new (int x, int y, int target, string name)[]
                        {
                            (2 * faceSize, 1 * faceSize, 0, "Right (+X)"),  // +X (right)
                            (0 * faceSize, 1 * faceSize, 1, "Left (-X)"),   // -X (left)
                            (1 * faceSize, 0 * faceSize, 2, "Top (+Y)"),    // +Y (top)
                            (1 * faceSize, 2 * faceSize, 3, "Bottom (-Y)"), // -Y (bottom)
                            (1 * faceSize, 1 * faceSize, 4, "Front (+Z)"),  // +Z (front)
                            (3 * faceSize, 1 * faceSize, 5, "Back (-Z)")    // -Z (back)
                        };


                        int bytesPerPixel = 4; //RGBA
                        var format = PixelFormat.Rgba;
                        var internalFormat = InternalFormat.Rgba;

                        for (int i = 0; i < 6; i++)
                        {
                            var (x, y, target, name) = facePositions[i];


                            if (x + faceSize > fullImage.Width || y + faceSize > fullImage.Height)
                            {
                                Console.WriteLine($"Warning: Face {name} extends beyond image bounds.");
                                CreateSingleFallbackFace(target);
                                continue;
                            }


                            byte[] faceData = ExtractFaceHighQuality(fullImage.Data, fullImage.Width, fullImage.Height,
                                                                    x, y, faceSize, fullImage.Comp);

                            if (faceData.Length > 0)
                            {
                                fixed (byte* ptr = faceData)
                                {
                                    gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + target, 0,
                                        internalFormat, (uint)faceSize, (uint)faceSize,
                                        0, format, PixelType.UnsignedByte, ptr);
                                }

                                Console.WriteLine($"✓ HIGH QUALITY {name}: {faceSize}x{faceSize}, position ({x}, {y})");
                            }
                            else
                            {
                                Console.WriteLine($"✗ Failed to extract {name}, using fallback");
                                CreateSingleFallbackFace(target);
                            }
                        }

                        StbImage.stbi_set_flip_vertically_on_load(1);
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load cubemap texture from resources: {resourcePath}");
                        ListAvailableResources();
                        CreateFallbackCubemap();
                        return cubemapTexture;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cubemap: {ex.Message}");
                CreateFallbackCubemap();
                StbImage.stbi_set_flip_vertically_on_load(1);
                return cubemapTexture;
            }


            SetHighQualityCubemapParameters();

            Console.WriteLine("HIGH QUALITY Cubemap loaded successfully!");
            return cubemapTexture;
        }


        private byte[] ExtractFaceHighQuality(byte[] sourceData, int sourceWidth, int sourceHeight,
                                             int faceX, int faceY, int faceSize, ColorComponents sourceFormat)
        {
            int sourceBytesPerPixel = sourceFormat == ColorComponents.RedGreenBlueAlpha ? 4 : 3;
            int targetBytesPerPixel = 4;

            byte[] faceData = new byte[faceSize * faceSize * targetBytesPerPixel];

            for (int y = 0; y < faceSize; y++)
            {
                for (int x = 0; x < faceSize; x++)
                {
                    int sourceIndex = ((faceY + y) * sourceWidth + (faceX + x)) * sourceBytesPerPixel;
                    int faceIndex = (y * faceSize + x) * targetBytesPerPixel;


                    if (sourceIndex + sourceBytesPerPixel <= sourceData.Length &&
                        faceIndex + targetBytesPerPixel <= faceData.Length)
                    {

                        faceData[faceIndex + 0] = sourceData[sourceIndex + 0]; // R
                        faceData[faceIndex + 1] = sourceData[sourceIndex + 1]; // G
                        faceData[faceIndex + 2] = sourceData[sourceIndex + 2]; // B


                        if (sourceBytesPerPixel == 4)
                        {
                            faceData[faceIndex + 3] = sourceData[sourceIndex + 3]; // A
                        }
                        else
                        {
                            faceData[faceIndex + 3] = 255;
                        }
                    }
                }
            }

            return faceData;
        }


        private void SetHighQualityCubemapParameters()
        {

            gl.GenerateMipmap(TextureTarget.TextureCubeMap);


            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter,
                            (int)TextureMinFilter.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter,
                            (int)TextureMagFilter.Linear);


            try
            {
                gl.TexParameter(TextureTarget.TextureCubeMap, (TextureParameterName)0x84FE, 16.0f); // GL_TEXTURE_MAX_ANISOTROPY_EXT
                Console.WriteLine("Anisotropic filtering enabled (16x)");
            }
            catch
            {
                Console.WriteLine("Anisotropic filtering not supported");
            }


            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            gl.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToEdge);
        }
        private unsafe void CreateSingleFallbackFace(int faceIndex)
        {

            var colors = new byte[][]
            {
                new byte[] { 139, 117, 81 },   // desert brown +X
                new byte[] { 139, 117, 81 },   // desert brown -X
                new byte[] { 135, 206, 235 },  // sky blue +Y  
                new byte[] { 160, 82, 45 },    // saddle brown -Y
                new byte[] { 139, 117, 81 },   // desert brown +Z
                new byte[] { 139, 117, 81 }    // desert brown -Z
            };

            if (faceIndex >= 0 && faceIndex < colors.Length)
            {
                fixed (byte* ptr = colors[faceIndex])
                {
                    gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + faceIndex, 0,
                        InternalFormat.Rgb, 1, 1, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
                }
            }
        }

        private void ListAvailableResources()
        {
            Console.WriteLine("Available embedded resources:");
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.Contains("skybox") || name.Contains("Cubemap"))
                {
                    Console.WriteLine($"  ★ {name}");
                }
                else
                {
                    Console.WriteLine($"    {name}");
                }
            }
        }

        private unsafe void CreateFallbackCubemap()
        {

            var colors = new byte[][]
            {
                new byte[] { 139, 117, 81 },   // desert brown +X
                new byte[] { 139, 117, 81 },   // desert brown -X
                new byte[] { 135, 206, 235 },  // sky blue +Y  
                new byte[] { 160, 82, 45 },    // saddle brown -Y
                new byte[] { 139, 117, 81 },   // desert brown +Z
                new byte[] { 139, 117, 81 }    // desert brown -Z
            };

            for (int i = 0; i < 6; i++)
            {
                fixed (byte* ptr = colors[i])
                {
                    gl.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0,
                        InternalFormat.Rgb, 1, 1, 0, PixelFormat.Rgb, PixelType.UnsignedByte, ptr);
                }
            }

            Console.WriteLine("Created fallback desert cubemap");
        }

        public void Render(uint shaderProgram)
        {

            gl.DepthFunc(DepthFunction.Lequal);

            gl.UseProgram(shaderProgram);
            gl.BindVertexArray(vao);
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.TextureCubeMap, cubemapTexture);

            gl.DrawArrays(PrimitiveType.Triangles, 0, 36);

            gl.BindVertexArray(0);
            gl.DepthFunc(DepthFunction.Less);
        }

        public void Dispose()
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteTexture(cubemapTexture);
        }
    }
}