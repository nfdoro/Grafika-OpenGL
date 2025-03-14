﻿using Silk.NET.OpenGL;
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

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);
            //x y z
            
            float[] vertexArray = new float[] {
                 0.0f, 0.0f, 0.0f, //a --0
                 +0.43f, +0.25f, 0.0f, //b --1
                 +0.43f, -0.25f, 0.0f, //c -- 2
                 0, -0.5f, 0.0f,//d -- 3

                 0.0f, 0.0f, 0.0f, //a --4
                 0, -0.5f, 0f,//d -- 5
                 -0.43f, -0.25f, 0.0f, //e -- 6
                 -0.43f, +0.25f, 0.0f, //f -- 7

                 0.0f, 0.0f, 0.0f, //a -- 8
                 -0.43f, +0.25f, 0.0f,//f -- 9
                 +0.43f, +0.25f, 0.0f, //b --10
                 0, 0.5f, 0f,//g --11
            };

            //r g b a 
            float[] colorArray = new float[] {
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

            };


            //haromszogek a pontok alapjan 
            uint[] indexArray = new uint[] {
                                0, 2, 1, //1 oldal
                                0, 3, 2,

                                4, 6, 5, //2. oldal
                                4, 7, 6,

                                8, 11, 9, //3.oldal
                                8, 10, 11

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
            Gl.BindVertexArray(vao);

            // always unbound the vertex buffer first, so no halfway results are displayed by accident
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);
            Gl.DeleteVertexArray(vao);
        }
    }
}
