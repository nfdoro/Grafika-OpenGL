﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1_24_02_17_2
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint program;

        //private static GlCube glCubeCentered;
        //private static GlCube glCubeRotating;

        private static GlCube glCubeRubic;

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
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                    ;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = !cubeArrangementModel.AnimationEnabeld;
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
        }

        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            //DrawPulsingCenterCube();
            //DrawRevolvingCube();

            DrawRubicCenterCube();
           
        }
        /*
        private static unsafe void DrawRevolvingCube()
        {
            Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(0.25f);
            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
            Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(1f, 1f, 0f);
            Matrix4X4<float> rotGlobY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
            Matrix4X4<float> modelMatrix = diamondScale * rotx * rotz * rotLocY * trans * rotGlobY;

            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(glCubeRotating.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRotating.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
        }
        */


        public static unsafe void DrawRubicCenterCube()
        {

            
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
                            SetUpObject(true, true, false, true, false, false);
                        }
                        else if (j == 2.6f && k == 2.6f && i == 0.0f)
                        {
                            SetUpObject(true, true, true, false, false, false);
                        }
                        else if (j == 2.6f && k == 0.0f && i == 0.0f)
                        {
                            SetUpObject(true, false, true, false, true, false);
                        }
                        else if (j == 2.6f && k == 0.0f && i == 2.6f)
                        {
                            SetUpObject(true, false, false, true, true, false);
                        }
                        //alja
                        else if (j == 0.0f && k == 2.6f && i == 2.6f)
                        {
                            SetUpObject(false, true, false, true, false, true);
                        }
                        else if (j == 0.0f && k == 2.6f && i == 0.0f)
                        {
                            SetUpObject(false, true, true, false, false, true);
                        }
                        else if (j == 0.0f && k == 0.0f && i == 0.0f)
                        {
                            SetUpObject(false, false, true, false, true, true);
                        }
                        else if (j == 0.0f && k == 0.0f && i == 2.6f)
                        {
                            SetUpObject(false, false, false, true, true, true);
                        }
                        //Szelso oldalak
                        else if (k == 2.6f && i == 0.0f)
                        {
                            SetUpObject(false, true, true, false, false, false);
                        }
                        else if (k == 2.6f && i == 2.6f)
                        {
                            SetUpObject(false, true, false, true, false, false);
                        }
                        else if (k == 0.0f && i == 2.6f)
                        {
                            SetUpObject(false, false, false, true, true, false);
                        }
                        else if (k == 0.0f && i == 0.0f)
                        {
                            SetUpObject(false, false, true, false, true, false);
                        }
                        else if (j == 2.6f && i == 0.0f)
                        {
                            SetUpObject(true, false, true, false, false, false);
                        }
                        else if (j == 2.6f && i == 2.6f)
                        {
                            SetUpObject(true, false, false, true, false, false);
                        }
                        else if (j == 2.6f && k == 2.6f)
                        {
                            SetUpObject(true, true, false, false, false, false);
                        }
                        else if (j == 2.6f && k == 0.0f)
                        {
                            SetUpObject(true, false, false, false, true, false);
                        }
                        else if (j == 0.0f && i == 0.0f)
                        {
                            SetUpObject(false, false, true, false, false, true);
                        }
                        
                        else if (j == 0.0f && i == 2.6f)
                        {
                            SetUpObject(false, false, false, true, false, true);
                        }
                        
                        else if (j == 0.0f && k == 2.6f)
                        {
                            SetUpObject(false, true, false, false, false, true);
                        }
                        else if (j == 0.0f && k == 0.0f)
                        {
                            SetUpObject(false, false, false, false, true, true);
                        }
                        
                        //kozepso reszek
                        else if (k == 2.6f)
                        {
                            SetUpObject(false, true, false, false, false, false);
                        }
                        else if (k == 0f)
                        {
                            SetUpObject(false, false, false, false, true, false);
                        }
                        else if (i == 0.0f)
                        {
                            SetUpObject(false, false, true, false, false, false);
                        }
                        else if (i == 2.6f)
                        {
                            SetUpObject(false, false, false, true, false, false);
                        }
                        else if (j == 2.6f)
                        {
                            SetUpObject(true, false, false, false, false, false);
                        }
                        else if (j == 0)
                        {
                            SetUpObject(false, false, false, false, false, true);
                        }
                        else
                        {
                            SetUpObject(false, false, false, false, false, false);
                        }
                        DrawCube(i, j, k);
                    }
                }
            }
        }
        private static unsafe void DrawCube(float x, float y, float z)
        {
            Matrix4X4<float> trans = Matrix4X4.CreateTranslation(x, y, z);
            var modelMatrixForCenterCube = Matrix4X4.CreateScale(0.25f) + trans;
            SetModelMatrix(modelMatrixForCenterCube);
            Gl.BindVertexArray(glCubeRubic.Vao);
            Gl.DrawElements(GLEnum.Triangles, glCubeRubic.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
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

     
        private static unsafe void SetUpObject(bool blue,bool green,bool red,bool cian, bool yellow, bool magenta)
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

            glCubeRubic = GlCube.CreateCubeWithFaceColors(Gl, face1Color, face2Color, face3Color, face4Color, face5Color, face6Color);

        }

        

        private static void Window_Closing()
        {
            //glCubeCentered.ReleaseGlCube();
            //glCubeRotating.ReleaseGlCube();
            glCubeRubic.ReleaseGlCube();
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