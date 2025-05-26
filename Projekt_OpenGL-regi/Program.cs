using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using StbImageSharp;


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

        private static uint textureId;

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
            Gl.ClearColor(System.Drawing.Color.White);

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            SetUpObjects();
        }

        private static unsafe uint LoadTexture(GL gl, string resourceName)
        {
            string fullPath = "Projekt_OpenGL.Resources." + resourceName; // fontos: pontos namespace és mappa
            using Stream stream = typeof(Program).Assembly.GetManifestResourceStream(fullPath)!;
            var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

            uint texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);

            fixed (byte* dataPtr = image.Data)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
                    (uint)image.Width, (uint)image.Height, 0,
                    PixelFormat.Rgba, PixelType.UnsignedByte, dataPtr);
            }

            gl.GenerateMipmap(TextureTarget.Texture2D);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.LinearMipmapLinear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);

            return texture;
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);


            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader));


            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
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


            DrawDaeObject();


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
            Console.WriteLine("Draw: " + myModel.IndexArrayLength);


            Gl.UseProgram(program);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, textureId);
            Gl.Uniform1(Gl.GetUniformLocation(program, "uTexture"), 0);


            var scale = Matrix4X4.CreateScale(0.1f);
            var rotX = Matrix4X4.CreateRotationX((float)(Math.PI / 2));
            var rot180X = Matrix4X4.CreateRotationX((float)Math.PI);
            var trans = Matrix4X4.CreateTranslation(0f, 0f, -5f);

            var modelMatrixForCat = trans * rotX * rot180X * scale;
            SetModelMatrix(modelMatrixForCat);

            Gl.BindVertexArray(myModel.Vao);
            Gl.DrawElements(GLEnum.Triangles, myModel.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            var tableMatrix = Matrix4X4.CreateTranslation(0f, -0.5f, 0f);
            SetModelMatrix(tableMatrix);
            Gl.BindVertexArray(table.Vao);
            Gl.DrawElements(GLEnum.Triangles, table.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);


        }

        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [1f, 0f, 0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            textureId = LoadTexture(Gl, "model.textures.Atlas_Metal_albedo.jpg");

            //teapot = ObjResourceReader.CreateObjectFromResource(Gl, "wall3.obj", face1Color);

            float[] faceColor = new float[] { 1.0f, 0.5f, 0.3f, 1.0f };
            myModel = ColladaResourceReader.CreateObjectFromResource(Gl, "model.model.dae", faceColor);


            float[] tableColor = [System.Drawing.Color.Azure.R/256f,
                                  System.Drawing.Color.Azure.G/256f,
                                  System.Drawing.Color.Azure.B/256f,
                                  1f];
            table = GlCube.CreateSquare(Gl, tableColor);


        }


        private static void Window_Closing()
        {
            //teapot.ReleaseGlObject();
            myModel.ReleaseGlObject();
            //glCubeRotating.ReleaseGlObject();
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
