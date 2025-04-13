using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Szeminarium1_24_02_17_2
{
    internal class GlCube:IDisposable
    {
        private bool disposedValue;
        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        /*private GlCube(uint vao, uint vertices, uint colors, uint indeces, uint indexArrayLength, GL gl)
        {
            this.Vao = vao;
            this.Vertices = vertices;
            this.Colors = colors;
            this.Indices = indeces;
            this.IndexArrayLength = indexArrayLength;
            this.Gl = gl;
        }*/
        public static unsafe GlCube CreateCubeWithFaceColors(GL Gl, float[] face1Color, float[] face2Color, float[] face3Color, float[] face4Color, float[] face5Color, float[] face6Color)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            // counter clockwise is front facing
            float[] vertexArray = new float[] {
                // Top face (Y+)
                -0.5f, 0.5f, 0.5f,    0f, 1f, 0f,
                 0.5f, 0.5f, 0.5f,    0f, 1f, 0f,
                 0.5f, 0.5f, -0.5f,   0f, 1f, 0f,
                -0.5f, 0.5f, -0.5f,   0f, 1f, 0f,

                // Front face (Z+)
                -0.5f, 0.5f, 0.5f,    0f, 0f, 1f,
                -0.5f, -0.5f, 0.5f,   0f, 0f, 1f,
                 0.5f, -0.5f, 0.5f,   0f, 0f, 1f,
                 0.5f, 0.5f, 0.5f,    0f, 0f, 1f,

                // Left face (X-)
                -0.5f, 0.5f, 0.5f,    -1f, 0f, 0f,
                -0.5f, 0.5f, -0.5f,   -1f, 0f, 0f,
                -0.5f, -0.5f, -0.5f,  -1f, 0f, 0f,
                -0.5f, -0.5f, 0.5f,   -1f, 0f, 0f,

                // Bottom face (Y-)
                -0.5f, -0.5f, 0.5f,   0f, -1f, 0f,
                 0.5f, -0.5f, 0.5f,   0f, -1f, 0f,
                 0.5f, -0.5f, -0.5f,  0f, -1f, 0f,
                -0.5f, -0.5f, -0.5f,  0f, -1f, 0f,

                // Back face (Z-)
                 0.5f, 0.5f, -0.5f,   0f, 0f, -1f,
                -0.5f, 0.5f, -0.5f,   0f, 0f, -1f,
                -0.5f, -0.5f, -0.5f,  0f, 0f, -1f,
                 0.5f, -0.5f, -0.5f,  0f, 0f, -1f,

                // Right face (X+)
                 0.5f, 0.5f, 0.5f,    1f, 0f, 0f,
                 0.5f, 0.5f, -0.5f,   1f, 0f, 0f,
                 0.5f, -0.5f, -0.5f,  1f, 0f, 0f,
                 0.5f, -0.5f, 0.5f,   1f, 0f, 0f,

            };

            List<float> colorsList = new List<float>();
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);
            colorsList.AddRange(face1Color);

            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);
            colorsList.AddRange(face2Color);

            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);
            colorsList.AddRange(face3Color);

            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);
            colorsList.AddRange(face4Color);

            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);
            colorsList.AddRange(face5Color);

            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);
            colorsList.AddRange(face6Color);


            float[] colorArray = colorsList.ToArray();

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);


            Gl.BindVertexArray(0);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            return new GlCube()
            {
                Vao = vao,
                Vertices = vertices,
                Colors = colors,
                Indices = indices,
                IndexArrayLength = (uint)indexArray.Length,
                Gl = Gl
            };

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null


                // always unbound the vertex buffer first, so no halfway results are displayed by accident
                Gl.DeleteBuffer(Vertices);
                Gl.DeleteBuffer(Colors);
                Gl.DeleteBuffer(Indices);
                Gl.DeleteVertexArray(Vao);

                disposedValue = true;
            }
        }
        ~GlCube()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }
}
