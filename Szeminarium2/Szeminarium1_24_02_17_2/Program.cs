using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;
using System.Dynamic;
using System.Numerics;
using System.Reflection;
using System.Transactions;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        class CubeInstance
        {
            public GlCube Cube { get; set; }
            public Vector3D<float> Position { get; set; }
            public Vector3D<float> ChekerPosition { get; set; }
        }

        private static List<CubeInstance> rubikCubes = new List<CubeInstance>();


        //private static CameraDescriptor cameraDescriptor = new();
        private static NewCameraDescriptor cameraDescriptor = new();
        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;
        private static ImGuiController imGuiController;
        private static float faceToRotate;

        private static uint program;

        //private static GlCube glCubeCentered;
        //private static GlCube glCubeRotating;

        private static GlCube glCubeRubik;

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


        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
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

            Gl = window.CreateOpenGL();

            imGuiController = new ImGuiController(Gl, window, inputContext);
            Gl.ClearColor(System.Drawing.Color.White);

            LinkProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            InitRubikCube();
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

            DrawRubikCenterCube();

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

            ImGuiNET.ImGui.Begin("Rubik Cube Rotation", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize);

            if (ImGuiNET.ImGui.Button("Left"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.LeftAnimationEnabled = true;
                    cubeArrangementModel.TargetLeftFaceRotationAngle = cubeArrangementModel.LeftFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
            }

            if (ImGuiNET.ImGui.Button("Right"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.RightAnimationEnabled = true;
                    cubeArrangementModel.TargetRightFaceRotationAngle = cubeArrangementModel.RightFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
            }

            if (ImGuiNET.ImGui.Button("Top"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.TopAnimationEnabled = true;
                    cubeArrangementModel.TargetTopFaceRotationAngle = cubeArrangementModel.TopFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
            }

            if (ImGuiNET.ImGui.Button("Bottom"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.BottomAnimationEnabled = true;
                    cubeArrangementModel.TargetBottomFaceRotationAngle = cubeArrangementModel.BottomFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
            }

            if (ImGuiNET.ImGui.Button("Front"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.FrontAnimationEnabled = true;
                    cubeArrangementModel.TargetFrontFaceRotationAngle = cubeArrangementModel.FrontFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
            }

            if (ImGuiNET.ImGui.Button("Back"))
            {
                if (!cubeArrangementModel.AnimationEnabeld)
                {
                    cubeArrangementModel.BackAnimationEnabled = true;
                    cubeArrangementModel.TargetBackFaceRotationAngle = cubeArrangementModel.BackFaceRotationAngle + 90;
                    cubeArrangementModel.AnimationEnabeld = true;
                }
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


        public static unsafe void InitRubikCube()
        {

            rubikCubes.Clear();
            float offset = 1.3f;
            GlCube cube;
            for (float i = 0; i <= 2.6f; i += 1.3f)
            {
                for (float j = 0; j <= 2.6f; j += 1.3f)
                {
                    for (float k = 0; k <= 2.6f; k += 1.3f)
                    {

                        //sarkak

                        //teteje
                        if (j == 2.6f && k == 2.6f && i == 2.6f)
                        {
                            cube =SetUpObject(true, true, false, true, false, false);
                        }
                        else if (j == 2.6f && k == 2.6f && i == 0.0f)
                        {
                            cube =SetUpObject(true, true, true, false, false, false);
                        }
                        else if (j == 2.6f && k == 0.0f && i == 0.0f)
                        {
                            cube = SetUpObject(true, false, true, false, true, false);
                        }
                        else if (j == 2.6f && k == 0.0f && i == 2.6f)
                        {
                            cube = SetUpObject(true, false, false, true, true, false);
                        }
                        //alja
                        else if (j == 0.0f && k == 2.6f && i == 2.6f)
                        {
                            cube = SetUpObject(false, true, false, true, false, true);
                        }
                        else if (j == 0.0f && k == 2.6f && i == 0.0f)
                        {
                            cube = SetUpObject(false, true, true, false, false, true);
                        }
                        else if (j == 0.0f && k == 0.0f && i == 0.0f)
                        {
                            cube = SetUpObject(false, false, true, false, true, true);
                        }
                        else if (j == 0.0f && k == 0.0f && i == 2.6f)
                        {
                            cube = SetUpObject(false, false, false, true, true, true);
                        }
                        //Szelso oldalak
                        else if (k == 2.6f && i == 0.0f)
                        {
                            cube = SetUpObject(false, true, true, false, false, false);
                        }
                        else if (k == 2.6f && i == 2.6f)
                        {
                            cube = SetUpObject(false, true, false, true, false, false);
                        }
                        else if (k == 0.0f && i == 2.6f)
                        {
                            cube = SetUpObject(false, false, false, true, true, false);
                        }
                        else if (k == 0.0f && i == 0.0f)
                        {
                            cube = SetUpObject(false, false, true, false, true, false);
                        }
                        else if (j == 2.6f && i == 0.0f)
                        {
                            cube = SetUpObject(true, false, true, false, false, false);
                        }
                        else if (j == 2.6f && i == 2.6f)
                        {
                            cube = SetUpObject(true, false, false, true, false, false);
                        }
                        else if (j == 2.6f && k == 2.6f)
                        {
                            cube = SetUpObject(true, true, false, false, false, false);
                        }
                        else if (j == 2.6f && k == 0.0f)
                        {
                            cube = SetUpObject(true, false, false, false, true, false);
                        }
                        else if (j == 0.0f && i == 0.0f)
                        {
                            cube = SetUpObject(false, false, true, false, false, true);
                        }
                        
                        else if (j == 0.0f && i == 2.6f)
                        {
                            cube = SetUpObject(false, false, false, true, false, true);
                        }
                        
                        else if (j == 0.0f && k == 2.6f)
                        {
                            cube = SetUpObject(false, true, false, false, false, true);
                        }
                        else if (j == 0.0f && k == 0.0f)
                        {
                            cube = SetUpObject(false, false, false, false, true, true);
                        }
                        
                        //kozepso reszek
                        else if (k == 2.6f)
                        {
                            cube = SetUpObject(false, true, false, false, false, false);
                        }
                        else if (k == 0f)
                        {
                            cube = SetUpObject(false, false, false, false, true, false);
                        }
                        else if (i == 0.0f)
                        {
                            cube = SetUpObject(false, false, true, false, false, false);
                        }
                        else if (i == 2.6f)
                        {
                            cube = SetUpObject(false, false, false, true, false, false);
                        }
                        else if (j == 2.6f)
                        {
                            cube = SetUpObject(true, false, false, false, false, false);
                        }
                        else if (j == 0)
                        {
                            cube = SetUpObject(false, false, false, false, false, true);
                        }
                        else
                        {
                            cube = SetUpObject(false, false, false, false, false, false);
                        }


                        rubikCubes.Add(new CubeInstance { Cube = cube, Position = new Vector3D<float>(i - offset, j - offset, k - offset)});
                        
                    }
                }
            }
        }
        private static unsafe void DrawRubikCenterCube()
        {
            float offset = 1.3f;
         
            foreach (var instance in rubikCubes)
            {
                Matrix4X4<float> trans = Matrix4X4.CreateTranslation<float>(new Vector3D<float>(instance.Position.X, instance.Position.Y, instance.Position.Z));

                float baseScale = 1.25f;
                float pulsationScale = baseScale * cubeArrangementModel.PulseScale;
                Matrix4X4<float> scale = Matrix4X4.CreateScale(pulsationScale);
                Matrix4X4<float> modelMatrix = scale * trans;
                Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;

                instance.ChekerPosition = instance.Position;
                double eps = 0.0001;
                if (Math.Abs(instance.ChekerPosition.X - (-offset))<eps )
                {
                    float angle = (float)(cubeArrangementModel.LeftFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationX(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);

                    modelMatrix = scale * trans * rotationMatrix;
                }
                if (Math.Abs(instance.ChekerPosition.X - (2.6f - offset)) < eps)
                {
                    float angle = (float)(cubeArrangementModel.RightFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationX(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);

                    modelMatrix = scale * trans * rotationMatrix;
                }
                if (Math.Abs(instance.ChekerPosition.Y - (2.6f - offset)) < eps)
                {
                    float angle = (float)(cubeArrangementModel.TopFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationY(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);

                    modelMatrix = scale * trans * rotationMatrix;
                }

                if  (Math.Abs(instance.ChekerPosition.Y -(-offset)) < eps) 
                {
                    float angle = (float)(cubeArrangementModel.BottomFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationY(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);
                    modelMatrix = scale * trans * rotationMatrix;
                }


                if (Math.Abs(instance.ChekerPosition.Z - (2.6f - offset)) < eps)
                {
                    float angle = (float)(cubeArrangementModel.FrontFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationZ(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);
                    modelMatrix = scale * trans * rotationMatrix;
                }
                if (Math.Abs(instance.ChekerPosition.Z -(-offset))<eps)
                {
                    float angle = (float)(cubeArrangementModel.BackFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationZ(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);
                    modelMatrix = scale * trans * rotationMatrix;
                }


                SetModelMatrix(modelMatrix);
                Gl.BindVertexArray(instance.Cube.Vao);
                Gl.DrawElements(GLEnum.Triangles, instance.Cube.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);

               
            }
           
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
        }
     
        private static unsafe GlCube SetUpObject(bool blue,bool green,bool red,bool cian, bool yellow, bool magenta)
        {

            float[] face1Color = [0f, 0f, 0f, 1f];
            float[] face2Color = [0f, 0f, 0f, 1f];
            float[] face3Color = [0f, 0f, 0f, 1f];
            float[] face4Color = [0f, 0f, 0f, 1f];
            float[] face5Color = [0f, 0f, 0f, 1f];
            float[] face6Color = [0f, 0f, 0f, 1f];
            if (blue)
            {
                 face1Color = [0.0f, 0.0f, 1.0f, 1.0f];
            }
            if (green)
            {
                 face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            }

            if (red)
            {
                 face3Color = [1.0f, 0.0f, 0.0f, 1.0f];
            }

            if (magenta)
            {
                 face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            }

            if (yellow)
            {
                 face5Color = [1.0f, 1.0f, 0.0f, 1.0f];
            }

            if (cian)
            {
                face6Color = [0.0f, 1.0f, 1.0f, 1.0f];
            }

            return glCubeRubik = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);

        }

        

        private static void Window_Closing()
        {
            //glCubeCentered.ReleaseGlCube();
            //glCubeRotating.ReleaseGlCube();
            glCubeRubik.Dispose();
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

            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                var text = resStreamReader.ReadToEnd();
                return text;
            }
        }
    }
}