using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;

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

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;

		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "2 szeminárium";
            windowOptions.Size = new Vector2D<int>(500, 500);

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

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

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

                case Key.Keypad4:
                    if (!cubeArrangementModel.AnimationEnabeld)
                    {
                        cubeArrangementModel.LeftAnimationEnabled = true;
                        cubeArrangementModel.TargetLeftFaceRotationAngle = cubeArrangementModel.LeftFaceRotationAngle + 90;
                        cubeArrangementModel.AnimationEnabeld = true;
                    }
                    break;

                case Key.Keypad6:
                    if (!cubeArrangementModel.AnimationEnabeld)
                    {
                        cubeArrangementModel.RightAnimationEnabled = true;
                        cubeArrangementModel.TargetRightFaceRotationAngle = cubeArrangementModel.RightFaceRotationAngle + 90;
                        cubeArrangementModel.AnimationEnabeld = true;
                    }
                    break;
                case Key.Keypad8:
                    if (!cubeArrangementModel.AnimationEnabeld)
                    {
                        cubeArrangementModel.TopAnimationEnabled = true;
                        cubeArrangementModel.TargetTopFaceRotationAngle = cubeArrangementModel.TopFaceRotationAngle + 90;
                        cubeArrangementModel.AnimationEnabeld = true;
                    }
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
            cubeArrangementModel.AdvanceTime(deltaTime);        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            DrawRubikCenterCube();

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
                Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(1.25f) * trans;
                Matrix4X4<float> scale = Matrix4X4.CreateScale(1.25f);
                Matrix4X4<float> rotationMatrix = Matrix4X4<float>.Identity;


                instance.ChekerPosition = instance.Position;
                if (instance.ChekerPosition.X == -offset )
                {
                    float angle = (float)(cubeArrangementModel.LeftFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationX(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);

                    modelMatrix = scale * trans * rotationMatrix;
                }
                if (instance.ChekerPosition.X == (2.6f - offset))
                {
                    float angle = (float)(cubeArrangementModel.RightFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationX(angle);
                    instance.ChekerPosition = Vector3D.Transform(instance.Position, rotationMatrix);

                    modelMatrix = scale * trans * rotationMatrix;
                }
                if(instance.ChekerPosition.Y == (2.6f - offset))
                {
                    float angle = (float)(cubeArrangementModel.TopFaceRotationAngle * Math.PI / 180f);
                    rotationMatrix *= Matrix4X4.CreateRotationY(angle);
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
        /*
        private static unsafe void SetUpObjects()
        {

            float[] face1Color = [1.0f, 0.0f, 0.0f, 1.0f];
            float[] face2Color = [0.0f, 1.0f, 0.0f, 1.0f];
            float[] face3Color = [0.0f, 0.0f, 1.0f, 1.0f];
            float[] face4Color = [1.0f, 0.0f, 1.0f, 1.0f];
            float[] face5Color = [0.0f, 1.0f, 1.0f, 1.0f];
            float[] face6Color = [1.0f, 1.0f, 0.0f, 1.0f];

            glCubeCentered = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);

            face1Color = [0.5f, 0.0f, 0.0f, 1.0f];
            face2Color = [0.0f, 0.5f, 0.0f, 1.0f];
            face3Color = [0.0f, 0.0f, 0.5f, 1.0f];
            face4Color = [0.5f, 0.0f, 0.5f, 1.0f];
            face5Color = [0.0f, 0.5f, 0.5f, 1.0f];
            face6Color = [0.5f, 0.5f, 0.0f, 1.0f];

            glCubeRotating = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);
        }*/

     
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
            glCubeRubik.ReleaseGlCube();
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
    }
}