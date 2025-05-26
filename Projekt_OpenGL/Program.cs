using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;

namespace Projekt_OpenGL
{
    internal class Program
    {

        //private static CameraDescriptor cameraDescriptor = new();
        private static NewCameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;
        private static ImGuiController imGuiController;

        private static uint program;

        private static GlObject myModel;

        private static GlObject table;

        private static Skybox skybox;
        private static uint skyboxProgram;


        private static bool leftRotationActive = false;
        private static bool rightRotationActive = false;


        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";


        private const string ShinenessVariableName = "uShininess";
        private const string AmbientVariableName = "uAmbientStrength";
        private const string DiffuseVariableName = "uDiffuseStrength";
        private const string SpecularVariableName = "uSpecularStrength";

        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";

        private const string TextureVariableName = "uTexture";
        private const string UseTextureVariableName = "uUseTexture";

        private static float shininess = 50;
        private static float ambient = 0.1f;
        private static float diffuse = 0.3f;
        private static float specular = 0.6f;

        private static float red = 1.0f;
        private static float green = 1.0f;
        private static float blue = 1.0f;

        private static float lightX = 0f;
        private static float lightY = 1.2f;
        private static float lightZ = 0f;

        private static float lightXInput = lightX;
        private static float lightYInput = lightY;
        private static float lightZInput = lightZ;

        private static float modelScale = 1.0f;  
        private static float modelRotX = 0f;
        private static float modelRotY = 0f;
        private static float modelRotZ = 0f;
        private static float modelPosX = 0f;
        private static float modelPosY = 0f;
        private static float modelPosZ = -5f;


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Grafika projekt";
            windowOptions.Size = new Vector2D<int>(800, 800);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }


        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling
            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            foreach (var resourceName in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                Console.WriteLine("Available resource: " + resourceName);
            }


            Gl = window.CreateOpenGL();

            imGuiController = new ImGuiController(Gl, window, inputContext);
            Gl.ClearColor(System.Drawing.Color.DarkGray);

            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);


            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            SetUpObjects();
        }

      
        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string vError = Gl.GetShaderInfoLog(vshader);
                Console.WriteLine($"VERTEX SHADER ERROR: {vError}");
                throw new Exception("Vertex shader failed to compile: " + vError);
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string fError = Gl.GetShaderInfoLog(fshader);
                Console.WriteLine($"FRAGMENT SHADER ERROR: {fError}");
                throw new Exception("Fragment shader failed to compile: " + fError);
            }
            else
            {
                Console.WriteLine("Fragment shader compiled successfully!");
            }

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                string linkError = Gl.GetProgramInfoLog(program);
                Console.WriteLine($"SHADER LINK ERROR: {linkError}");
            }
            else
            {
                Console.WriteLine("Shader program linked successfully!");
            }

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }


        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {

            switch (key)
            {
                // elore, hatra, balra, jobbra, felfele, lefele
                case Key.W:
                    cameraDescriptor.MoveForward();
                    break;
                case Key.S:
                    cameraDescriptor.MoveBackward();
                    break;
                case Key.A:
                    cameraDescriptor.MoveLeft();
                    break;
                case Key.D:
                    cameraDescriptor.MoveRight();
                    break;
                case Key.R:
                    cameraDescriptor.MoveUp();
                    break;
                case Key.F:
                    cameraDescriptor.MoveDown();
                    break;

                //forgatas: yaw (balra-jobbra), pitch (fel-le)
                case Key.Left:
                    cameraDescriptor.YawLeft();
                    break;
                case Key.Right:
                    cameraDescriptor.YawRight();
                    break;
                case Key.Up:
                    cameraDescriptor.PitchUp();
                    break;
                case Key.Down:
                    cameraDescriptor.PitchDown();
                    break;
                case Key.Space:

                    //cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
                    break;
            }
        }


        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            cubeArrangementModel.AdvanceTime(deltaTime);
            imGuiController.Update((float)deltaTime);
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            RenderSkybox();

            Gl.UseProgram(program);

            SetUniform3(LightColorVariableName, new Vector3(red, green, blue));
            SetUniform3(LightPositionVariableName, new Vector3(lightX, lightY, lightZ));
            SetUniform3(ViewPositionVariableName, new Vector3(cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z));

            SetUniform1(ShinenessVariableName, shininess);
            SetUniform1(AmbientVariableName, ambient);
            SetUniform1(DiffuseVariableName, diffuse);
            SetUniform1(SpecularVariableName, specular);


            SetViewMatrix();
            SetProjectionMatrix();

            Gl.Disable(EnableCap.Blend);
            DrawDaeObject();
            Gl.Enable(EnableCap.Blend);

            ImGuiNET.ImGui.Begin("Lighting", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.SliderFloat("Ambient", ref ambient, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Diffuse", ref diffuse, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Specular", ref specular, 0, 1);
            ImGuiNET.ImGui.End();

            ImGuiNET.ImGui.Begin("LightingColor", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize | ImGuiNET.ImGuiWindowFlags.NoCollapse);
            ImGuiNET.ImGui.SliderFloat("Red", ref red, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Green", ref green, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Blue", ref blue, 0, 1);
            ImGuiNET.ImGui.End();

            ImGuiNET.ImGui.Begin("LightPositon", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);

            ImGuiNET.ImGui.InputFloat("Light X", ref lightXInput);
            ImGuiNET.ImGui.InputFloat("Light Y", ref lightYInput);
            ImGuiNET.ImGui.InputFloat("Light Z", ref lightZInput);



            if (ImGuiNET.ImGui.Button("Apply"))
            {
                lightX = lightXInput;
                lightY = lightYInput;
                lightZ = lightZInput;
            }
            ImGuiNET.ImGui.End();


            ImGuiNET.ImGui.Begin("Model Transform", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.SliderFloat("Scale", ref modelScale, 0.1f, 3.0f);
            ImGuiNET.ImGui.SliderFloat("Rotation X", ref modelRotX, -180f, 180f);
            ImGuiNET.ImGui.SliderFloat("Rotation Y", ref modelRotY, -180f, 180f);
            ImGuiNET.ImGui.SliderFloat("Rotation Z", ref modelRotZ, -180f, 180f);
            ImGuiNET.ImGui.SliderFloat("Position X", ref modelPosX, -10f, 10f);
            ImGuiNET.ImGui.SliderFloat("Position Y", ref modelPosY, -10f, 10f);
            ImGuiNET.ImGui.SliderFloat("Position Z", ref modelPosZ, -10f, 10f);
            ImGuiNET.ImGui.End();

            imGuiController.Render();

        }

        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }

            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }
        
        private static unsafe void DrawDaeObject()
        {
            
            Gl.Disable(EnableCap.CullFace);

            
            foreach (var part in wallEParts)
            {
                var scale = Matrix4X4.CreateScale(modelScale);
                var rotX = Matrix4X4.CreateRotationX((float)(modelRotX * Math.PI / 180));
                var rotY = Matrix4X4.CreateRotationY((float)(modelRotY * Math.PI / 180));
                var rotZ = Matrix4X4.CreateRotationZ((float)(modelRotZ * Math.PI / 180));

                
                var baseRotation = Matrix4X4.CreateRotationX((float)(-90 * Math.PI / 180));

                var trans = Matrix4X4.CreateTranslation(modelPosX, modelPosY, modelPosZ);

               
                var modelMatrix = trans * rotX * rotY * rotZ * baseRotation * scale;
                SetModelMatrix(modelMatrix);

               
                var texturedObject = part as ColladaResourceReader.TexturedGlObject;

               
                ResetAllTextureStates();

                if (texturedObject != null)
                {
                    // Handle Albedo texture
                    if (texturedObject.AlbedoTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture0);
                        Gl.BindTexture(TextureTarget.Texture2D, texturedObject.AlbedoTextureId.Value);

                        int useTexLocation = Gl.GetUniformLocation(program, UseTextureVariableName);
                        if (useTexLocation != -1)
                            Gl.Uniform1(useTexLocation, 1);

                        int texLocation = Gl.GetUniformLocation(program, TextureVariableName);
                        if (texLocation != -1)
                            Gl.Uniform1(texLocation, 0);
                    }

                    // Handle AO texture
                    if (texturedObject.AOTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture1);
                        Gl.BindTexture(TextureTarget.Texture2D, texturedObject.AOTextureId.Value);

                        int useAOLocation = Gl.GetUniformLocation(program, "uUseAO");
                        if (useAOLocation != -1)
                            Gl.Uniform1(useAOLocation, 1);

                        int aoTexLocation = Gl.GetUniformLocation(program, "uAOTexture");
                        if (aoTexLocation != -1)
                            Gl.Uniform1(aoTexLocation, 1);
                    }

                    // Handle Metallic texture
                    if (texturedObject.MetallicTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture2);
                        Gl.BindTexture(TextureTarget.Texture2D, texturedObject.MetallicTextureId.Value);

                        int useMetallicLocation = Gl.GetUniformLocation(program, "uUseMetallic");
                        if (useMetallicLocation != -1)
                            Gl.Uniform1(useMetallicLocation, 1);

                        int metallicTexLocation = Gl.GetUniformLocation(program, "uMetallicTexture");
                        if (metallicTexLocation != -1)
                            Gl.Uniform1(metallicTexLocation, 2);
                    }

                    // Handle Normal texture
                    if (texturedObject.NormalTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture3);
                        Gl.BindTexture(TextureTarget.Texture2D, texturedObject.NormalTextureId.Value);

                        int useNormalLocation = Gl.GetUniformLocation(program, "uUseNormal");
                        if (useNormalLocation != -1)
                            Gl.Uniform1(useNormalLocation, 1);

                        int normalTexLocation = Gl.GetUniformLocation(program, "uNormalTexture");
                        if (normalTexLocation != -1)
                            Gl.Uniform1(normalTexLocation, 3);
                    }

                    // Handle Opacity texture - JAVÍTOTT!
                    if (texturedObject.OpacityTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture4);
                        Gl.BindTexture(TextureTarget.Texture2D, texturedObject.OpacityTextureId.Value);

                        int useOpacityLocation = Gl.GetUniformLocation(program, "uUseOpacity");
                        if (useOpacityLocation != -1)
                            Gl.Uniform1(useOpacityLocation, 1);

                        int opacityTexLocation = Gl.GetUniformLocation(program, "uOpacityTexture");
                        if (opacityTexLocation != -1)
                            Gl.Uniform1(opacityTexLocation, 4);
                    }
                }

                
                Gl.BindVertexArray(part.Vao);
                Gl.DrawElements(GLEnum.Triangles, part.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);

                
                UnbindAllTextures();
            }

            
            Gl.Enable(EnableCap.CullFace);

            RenderTable();
        }

      
        private static unsafe void RenderTable()
        {
            Console.WriteLine("Rendering table..."); // Debug

            
            var tableMatrix = Matrix4X4.CreateTranslation(0f, -0.5f, 0f);
            SetModelMatrix(tableMatrix);

           
            ResetAllTextureStates();

          
            Gl.BindVertexArray(table.Vao);

            CheckBlendingState();

            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            Console.WriteLine($"Table rendered with {table.IndexArrayLength} indices"); // Debug
        }

        private static void ResetAllTextureStates()
        {
            // Reset texture usage flags
            int useTexLocation = Gl.GetUniformLocation(program, UseTextureVariableName);
            int useAOLocation = Gl.GetUniformLocation(program, "uUseAO");
            int useMetallicLocation = Gl.GetUniformLocation(program, "uUseMetallic");
            int useNormalLocation = Gl.GetUniformLocation(program, "uUseNormal");
            int useOpacityLocation = Gl.GetUniformLocation(program, "uUseOpacity");
            int useRoughnessLocation = Gl.GetUniformLocation(program, "uUseRoughness");

            if (useTexLocation != -1)
                Gl.Uniform1(useTexLocation, 0);
            if (useAOLocation != -1)
                Gl.Uniform1(useAOLocation, 0);
            if (useMetallicLocation != -1)
                Gl.Uniform1(useMetallicLocation, 0);
            if (useNormalLocation != -1)
                Gl.Uniform1(useNormalLocation, 0);
            if (useOpacityLocation != -1)
                Gl.Uniform1(useOpacityLocation, 0);
            if (useRoughnessLocation != -1)
                Gl.Uniform1(useRoughnessLocation, 0);
        }

       
        private static void UnbindAllTextures()
        {
            // Unbind textures for next object
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            Gl.ActiveTexture(TextureUnit.Texture4);
            Gl.BindTexture(TextureTarget.Texture2D, 0);

            // Reset to texture 0
            Gl.ActiveTexture(TextureUnit.Texture0);
        }

        private static void CheckBlendingState()
        {
           
            Gl.GetInteger(GetPName.BlendSrc, out int srcBlend);
            Gl.GetInteger(GetPName.BlendDst, out int dstBlend);
            bool blendEnabled = Gl.IsEnabled(EnableCap.Blend);

            Console.WriteLine($"Blend enabled: {blendEnabled}, Src: {srcBlend}, Dst: {dstBlend}");

           
            if (!blendEnabled)
            {
                Gl.Enable(EnableCap.Blend);
                Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                Console.WriteLine("Re-enabled blending for table");
            }
        }

      
        private static List<GlObject> wallEParts;
        private static unsafe void SetUpObjects()
        {
            float[] faceColor = new float[] { 1.0f, 0.5f, 0.3f, 1.0f };

            // Load all parts of the Wall-E model
            wallEParts = ColladaResourceReader.CreateMultipleObjectsFromResource(Gl, "model.dae", faceColor);

            Console.WriteLine($"Loaded {wallEParts.Count} parts of Wall-E");

            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                          System.Drawing.Color.Azure.G/256f,
                          System.Drawing.Color.Azure.B/256f,
                          1f];
            table = GlCube.CreateSquare(Gl, tableColor);

            skybox = new Skybox(Gl);
            LinkSkyboxProgram();

        }

        // Skybox shader program link
        private static void LinkSkyboxProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.SkyboxVertex.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string vError = Gl.GetShaderInfoLog(vshader);
                Console.WriteLine($"SKYBOX VERTEX SHADER ERROR: {vError}");
                throw new Exception("Skybox vertex shader failed to compile: " + vError);
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.SkyboxFragment.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string fError = Gl.GetShaderInfoLog(fshader);
                Console.WriteLine($"SKYBOX FRAGMENT SHADER ERROR: {fError}");
                throw new Exception("Skybox fragment shader failed to compile: " + fError);
            }

            skyboxProgram = Gl.CreateProgram();
            Gl.AttachShader(skyboxProgram, vshader);
            Gl.AttachShader(skyboxProgram, fshader);
            Gl.LinkProgram(skyboxProgram);
            Gl.GetProgram(skyboxProgram, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                string linkError = Gl.GetProgramInfoLog(skyboxProgram);
                Console.WriteLine($"SKYBOX SHADER LINK ERROR: {linkError}");
            }
            else
            {
                Console.WriteLine("Skybox shader program linked successfully!");
            }

            Gl.DetachShader(skyboxProgram, vshader);
            Gl.DetachShader(skyboxProgram, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static unsafe void RenderSkybox()
        {
            
            var view = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            var skyboxView = new Matrix4X4<float>(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);

            Gl.UseProgram(skyboxProgram);

            int viewLoc = Gl.GetUniformLocation(skyboxProgram, "view");
            int projLoc = Gl.GetUniformLocation(skyboxProgram, "projection");
            int skyboxLoc = Gl.GetUniformLocation(skyboxProgram, "skybox");

            if (viewLoc != -1)
                Gl.UniformMatrix4(viewLoc, 1, false, (float*)&skyboxView);
            if (projLoc != -1)
                Gl.UniformMatrix4(projLoc, 1, false, (float*)&projectionMatrix);
            if (skyboxLoc != -1)
                Gl.Uniform1(skyboxLoc, 0);

            skybox.Render(skyboxProgram);
        }

        private static void Window_Closing()
        {
            foreach (var part in wallEParts)
            {
                if (part is ColladaResourceReader.TexturedGlObject texturedObject)
                {
                  
                    if (texturedObject.AlbedoTextureId.HasValue)
                    {
                        Gl.DeleteTexture(texturedObject.AlbedoTextureId.Value);
                    }
                
                    if (texturedObject.AOTextureId.HasValue)
                    {
                        Gl.DeleteTexture(texturedObject.AOTextureId.Value);
                    }

                    if (texturedObject.NormalTextureId.HasValue)
                    {
                        Gl.DeleteTexture(texturedObject.NormalTextureId.Value);
                    }
                 
                    if (texturedObject.MetallicTextureId.HasValue)
                    {
                        Gl.DeleteTexture(texturedObject.MetallicTextureId.Value);
                    }

                    
                    if (texturedObject.RoughnessTextureId.HasValue)
                    {
                        Gl.DeleteTexture(texturedObject.RoughnessTextureId.Value);
                    }
                }
                part.ReleaseGlObject();
            }

            table.ReleaseGlObject();

            skybox?.Dispose();
            if (skyboxProgram != 0)
                Gl.DeleteProgram(skyboxProgram);

        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);


            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;

            Console.WriteLine(resourceFullPath);
            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }



    }
}