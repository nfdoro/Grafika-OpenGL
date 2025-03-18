using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Szeminarium1
{
    internal static class Program
    {
        private static IWindow graphicWindow;

        private static GL Gl;

        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
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
            windowOptions.Title = "1. szeminárium - háromszög";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);

            graphicWindow = Window.Create(windowOptions);

            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;


            graphicWindow.Run();
        }

        private static void GraphicWindow_Load()
        {
            // egszeri beallitasok
            //Console.WriteLine("Loaded");

            Gl = graphicWindow.CreateOpenGL();

            Gl.ClearColor(System.Drawing.Color.White);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader)); 
            
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            //fragment shader hiba kezeles
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + Gl.GetShaderInfoLog(fshader)); 

            program = Gl.CreateProgram();
        
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);

            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

        }

        //meg nem jelenitett objektumok frissitese a vilagban
        private static void GraphicWindow_Update(double deltaTime)
        {
            //ide nem kell OpenGL kodot irni
            //szalbiztos
            //Console.WriteLine($"Update after {deltaTime}[s].");
        }

        //megjelenites
        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s]");
            //ide jonnek az OpenGL kodok
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            uint vaoTriangle = Gl.GenVertexArray();
            Gl.BindVertexArray(vaoTriangle);

            //x y z
            
            float[] vertexArray = new float[] {
            
             //JOBB OLDAL
             //elso sor
             0.0f, -0.36f, 0.0f, //a -- 0
            +0.20f, -0.24f, 0.0f, //b -- 1
            +0.20f, -0.48f, 0.0f, //c -- 2
             0, -0.6f, 0.0f, //d -- 3

             0.20f, -0.24f, 0.0f, //--4
             0.40f, -0.12f, 0.0f, //--5
             0.40f, -0.36f, 0.0f, // --6
             0.20f, -0.48f, 0.0f, //--7

             0.40f, -0.12f, 0.0f, //--8
             0.60f, 0.0f, 0.0f, //--9
             0.60f, -0.24f, 0.0f, //--10
             0.4f, -0.36f, 0.0f, //--11
             
             //masodik soR
             0.0f, -0.12f, 0.0f, //a -- 12
            +0.20f, 0.0f, 0.0f, //b -- 13
            +0.20f, -0.24f, 0.0f, //c -- 14
             0, -0.36f, 0.0f, //d -- 15

             0.20f, 0.0f, 0.0f, //--16
             0.40f, 0.12f, 0.0f, //--17
             0.40f, -0.12f, 0.0f, // --18
             0.20f, -0.24f, 0.0f, //--19

             0.40f, +0.12f, 0.0f, //--20
             0.60f, 0.24f, 0.0f, //--21
             0.60f, 0.0f, 0.0f, //--22
             0.4f, -0.12f, 0.0f, //--23

             //harmadik sor
             0.0f, 0.12f, 0.0f, //a -- 24
            +0.20f, 0.24f, 0.0f, //b -- 25
            +0.20f, 0.0f, 0.0f, //c -- 26
             0, -0.12f, 0.0f, //d -- 27

             0.20f, 0.24f, 0.0f, //--28
             0.40f, 0.36f, 0.0f, //--29
             0.40f, +0.12f, 0.0f, // --30
             0.20f, 0.0f, 0.0f, //--31

             0.40f, +0.36f, 0.0f, //--32
             0.60f, 0.48f, 0.0f, //--33
             0.60f, 0.24f, 0.0f, //--34
             0.4f, +0.12f, 0.0f, //--35

             //BAL OLDAL

             //elso
             0.0f, -0.36f, 0.0f, //a -- 36
            -0.20f, -0.24f, 0.0f, //b -- 37
            -0.20f, -0.48f, 0.0f, //c -- 38
             0, -0.6f, 0.0f, //d -- 39

             -0.20f, -0.24f, 0.0f, // --40
             -0.40f, -0.12f, 0.0f, // --41
             -0.40f, -0.36f, 0.0f, // --42
             -0.20f, -0.48f, 0.0f, // --43

             -0.40f, -0.12f, 0.0f, //--44
             -0.60f, 0.0f, 0.0f, //--45
             -0.60f, -0.24f, 0.0f, //--46
             -0.4f, -0.36f, 0.0f, // -- 47


             //masodik soR
             0.0f, -0.12f, 0.0f, //-- 48
             -0.20f, 0.0f, 0.0f, // -- 49
             -0.20f, -0.24f, 0.0f, //-- 50
             0, -0.36f, 0.0f, //-- 51

             -0.20f, 0.0f, 0.0f, //--52
             -0.40f, 0.12f, 0.0f, //--53
             -0.40f, -0.12f, 0.0f, // --54
             -0.20f, -0.24f, 0.0f, //--55

             -0.40f, +0.12f, 0.0f, //--56
             -0.60f, 0.24f, 0.0f, //--57
             -0.60f, 0.0f, 0.0f, //--58
             -0.4f, -0.12f, 0.0f, //--59

              //harmadik sor
             0.0f, 0.12f, 0.0f, // -- 60
            -0.20f, 0.24f, 0.0f, // -- 61
            -0.20f, 0.0f, 0.0f, // -- 62
             0, -0.12f, 0.0f, // -- 63

             -0.20f, 0.24f, 0.0f, //--64
             -0.40f, 0.36f, 0.0f, //--65
             -0.40f, +0.12f, 0.0f, // -- 66
             -0.20f, 0.0f, 0.0f, //-- 67

             -0.40f, +0.36f, 0.0f, //--68
             -0.60f, 0.48f, 0.0f, //--69
             -0.60f, 0.24f, 0.0f, //--70
             -0.4f, +0.12f, 0.0f, //--71


             //TETEJE
             0.0f, 0.12f,0.0f,  // --72
             0.2f, 0.24f,0.0f,  // --73
             0.0f, 0.36f,0.0f,  // --74
             -0.2f, 0.24f,0.0f, // --75

             0.2f, 0.24f,0.0f, // -- 76
             0.4f, 0.36f,0.0f, // -- 77
             0.2f, 0.48f,0.0f, // -- 78
             0.0f, 0.36f,0.0f, // -- 79

             0.4f, 0.36f,0.0f, // -- 80
             0.6f, 0.48f,0.0f, // -- 81
             0.4f, 0.6f,0.0f, // -- 82
             0.2f, 0.48f,0.0f, // -- 83

             //kovi sor
             -0.2f, 0.24f,0.0f,  // -- 84
             0.0f, 0.36f,0.0f,  // -- 85
             -0.2f, 0.48f,0.0f,  // -- 86
             -0.4f, 0.36f,0.0f, // -- 87

             0.0f, 0.36f,0.0f, // -- 88
             0.2f, 0.48f,0.0f, // -- 89
             0.0f, 0.6f,0.0f, // -- 90
             -0.2f, 0.48f,0.0f, // -- 91

             0.2f, 0.48f,0.0f, // -- 92
             0.4f, 0.6f,0.0f, // -- 93
             0.2f, 0.72f,0.0f, // -- 94
             0.0f, 0.6f,0.0f, // -- 95

             //kovi sor

             -0.4f, 0.36f,0.0f,  // -- 96
             -0.2f, 0.48f,0.0f,  // -- 97
             -0.4f, 0.6f,0.0f,  // -- 98
             -0.6f, 0.48f,0.0f, // -- 99

             -0.2f, 0.48f,0.0f, // -- 100
             0.0f, 0.6f,0.0f, // -- 101
             -0.2f, 0.72f,0.0f, // -- 102
             -0.4f, 0.6f,0.0f, // -- 103

             0.0f, 0.6f,0.0f, // -- 104
             0.2f, 0.72f,0.0f, // -- 105
             0.0f, 0.85f,0.0f, // -- 106
             -0.2f, 0.72f,0.0f, // -- 107

            };

            //r g b a 
            float[] colorArray = new float[] {
                
                
                
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,


                //BAL OLDAL
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                //TETEJE

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 0.0f, 1.0f,

            };


            //haromszogek a pontok alapjan 
            uint[] indexArray = new uint[] {
               
                //PIROS OLDAL
                //elso sor
                 0, 2, 1, 
                 0, 3, 2,

                 4, 6, 5, 
                 4, 7, 6,

                 8, 10, 9, 
                 8, 11, 10,

                 //masodik sor
                 12, 14, 13,
                 12 ,15, 14,

                 16, 18, 17,
                 16, 19, 18,

                 20, 22, 21,
                 20, 23, 22,

                 //harmadik soR
                 24, 26, 25,
                 24, 27, 26,

                 28, 30, 29,
                 28, 31, 30,

                 32, 34, 33,
                 32, 35, 34,

                 //BAL OLDAL
                 //elso sor
                 36 ,37,38,
                 36 ,38, 39,

                 40, 41,42,
                 40,42,43,

                 44,45,46,
                 44,46,47,

                 //masodik sor
                 
                 48,49,50,
                 48,50,51,
                 
                 52,53,54,
                 52,54,55,
            
                 56,57,58,
                 56,58,59,
                 
                 //harmadik sor
                 60,61,62,
                 60,62,63,

                 64,65,66,
                 64,66,67,

                 68,69,70,
                 68,70,71,

                 //TETEJE

                 72,73,74,
                 72,74,75,

                 76,77,78,
                 76,78,79,
                 
                 80,81,82,
                 80,82,83,

                 //kovi sor
                 84,85,86,
                 84,86,87,

                 88,89,90,
                 88,90,91,

                 92,93,94,
                 92,94,95,

                 //kovi

                 
                  96,97,98,
                  96,98,99,
                   
                  100,101,102,
                  100,102,103,

                  104,105,106,
                  104,106,107

            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);

            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent a vertexArray kezelesekor");
            }

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent a colorArray kezelesekor");
            }

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent az indexArray kezelesekor");
            }

            Gl.UseProgram(program);

            Gl.DrawElements(GLEnum.Triangles, (uint)indexArray.Length, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vaoTriangle);

            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vaoTriangle);

            //------------------


            uint vaoLine = Gl.GenVertexArray();
            Gl.BindVertexArray(vaoLine);

            //vonalak kezdo es vegso pontjai
            float[] vertexArrayLine = new float[]
            {
                0.0f,0.12f, 0.0f, // -- 0
                0.0f,-0.6f, 0.0f, // -- 1
                0.6f,-0.24f, 0.0f,// -- 2
                0.6f,0.48f,0.0f,  // -- 3
              
                -0.6f,0.48f, 0.0f, // -- 4
                -0.6f,-0.24f, 0.0f, // -- 5

                0.0f,0.85f,0.0f, // --6

               //Jobb oldal
                0.2f, 0.24f,0.0f, // --7
                0.2f, -0.48f,0.0f, // --8

                0.4f, 0.36f,0.0f, // --9
                0.4f, -0.36f,0.0f, // --10

                0.0f, -0.12f,0.0f, // -- 11
                0.6f, +0.24f,0.0f, // -- 12

                0.0f, -0.36f,0.0f, // -- 13
                0.6f, 0.0f,0.0f, // -- 14

                //Bal oldal

                -0.2f, 0.24f,0.0f, // -- 15
                -0.2f, -0.48f,0.0f, // -- 16

                -0.4f, 0.36f,0.0f, // -- 17
                -0.4f, -0.36f,0.0f, // -- 18

                -0.6f, 0.24f,0.0f, // -- 19
                 0.0f, -0.12f,0.0f, // -- 20

                -0.6f, 0.0f,0.0f, // -- 21
                 0.0f, -0.36f,0.0f,// -- 22

                 //teteje

                 0.2f, 0.72f,0.0f, // -- 23
                 0.4f, 0.6f,0.0f, // -- 24

                 -0.4f, 0.6f,0.0f, // -- 25
                 -0.2f, 0.72f,0.0f, // -- 26




            };

            uint[] indexArrayLine = new uint[]
            {
                //Korvonal
                0, 1, 
                1 ,2,
                2, 3,
                3, 0,

                1, 5,
                5, 4,
                4, 0,

                4, 6,
                6,3,

                //jobb oldal
                7,8,
                9,10,
                11,12,
                13,14,

                //bal oldal
                15,16,
                17,18,
                19,20,
                21,22,

                //teteje
                23,17,
                24,15,
                25,7,
                26,9,
           

            };

            float[] colorArrayLine = new float[] {

                 0.0f, 0.0f, 0.0f, 1.0f,
                 0.0f, 0.0f, 0.0f, 1.0f,
            };

            
            //VONALAK CUCCAI
            
            uint lineVertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, lineVertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArrayLine.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(0);

            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent a vertexArray kezelesekor a Vonalnal");
            }

            uint lineColors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, lineColors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArrayLine.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent a colorArray kezelesekor");
            }

            uint lineIndices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, lineIndices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArrayLine.AsSpan(), GLEnum.StaticDraw);

            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            if (Gl.GetError() != GLEnum.NoError)
            {
                throw new Exception("Hiba torent az indexArray kezelesekor A vonalnal");
            }
      
            Gl.DrawElements(GLEnum.Lines, (uint)indexArrayLine.Length, GLEnum.UnsignedInt, null); // we used element buffer
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);
            Gl.BindVertexArray(vaoLine);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(lineVertices);
            Gl.DeleteBuffer(lineColors);
            Gl.DeleteBuffer(lineIndices);
            Gl.DeleteVertexArray(vaoLine);

        }
    }
}
