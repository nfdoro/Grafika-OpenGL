using Silk.NET.OpenGL;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
namespace GrafikaSzeminarium
{
    internal class PanelModelDescriptor : IDisposable
    {
        private bool disposedValue;
        public uint Vao { get; private set; }
        public uint Vertices { get; private set; }
        public uint Colors { get; private set; }
        public uint Indices { get; private set; }
        public uint IndexArrayLength { get; private set; }

        private GL Gl;

        public unsafe static PanelModelDescriptor Create(GL Gl, float[] normal1, float[] normal2)
        {
            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[]
            {
                    // Position             // Normal
                    -0.5f, -1f, 0f,         normal1[0], normal1[1], normal1[2], // Bal also
                     0.5f, -1f, 0f,         normal2[0], normal2[1], normal2[2], // Jobb also
                     0.5f,  1f, 0f,         normal2[0], normal2[1], normal2[2], // Jobb felso
                    -0.5f,  1f, 0f,         normal1[0], normal1[1], normal1[2], // Bal felso
   
            };

            float[] colorArray = new float[] {
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
            };

            uint[] indexArray = new uint[]
            {
                0, 1, 2,
                0, 2, 3
            };

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            // 0 is position
            // 2 is normals
            uint offsetPos = 0;
            uint offsetNormals = offsetPos + 3 * sizeof(float);
            uint vertexSize = offsetNormals + 3 * sizeof(float);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
            Gl.EnableVertexAttribArray(2);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint colors = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray.AsSpan(), GLEnum.StaticDraw);
            // 1 is color
            Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(1);
            Gl.BindBuffer(GLEnum.ArrayBuffer, 0);

            uint indices = Gl.GenBuffer();
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
            Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            Gl.BindBuffer(GLEnum.ElementArrayBuffer, 0);

            return new PanelModelDescriptor() { 
                Vao = vao,
                Vertices = vertices,
                Colors = colors, 
                Indices = indices,
                IndexArrayLength = (uint)indexArray.Length,
                Gl = Gl };

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

        ~PanelModelDescriptor()
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
