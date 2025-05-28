using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class HitboxRenderer
    {
        private GL gl;
        private uint shaderProgram;
        private uint vao, vbo, ebo;
        private uint indexCount;
        private bool isInitialized = false;

        private float[] vertices;
        private uint[] indices;

        public HitboxRenderer()
        {
        }

        public void Initialize(GL gl, uint shaderProgram)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;

            CreateCircleGeometry();
            SetupBuffers();
            isInitialized = true;

            Console.WriteLine("HitboxRenderer initialized");
        }

        private void CreateCircleGeometry()
        {
            const int segments = 16;
            const float halfHeight = 2f;

            var vertexList = new List<float>();
            var indexList = new List<uint>();
 
            // lower circle
            for (int i = 0; i < segments; i++)
            {
                float θ = 2 * MathF.PI * i / segments;
                float x = MathF.Cos(θ), z = MathF.Sin(θ);

                vertexList.Add(x);
                vertexList.Add(-halfHeight);
                vertexList.Add(z);

                vertexList.Add(1.0f);
                vertexList.Add(1.0f);
                vertexList.Add(1.0f);
                vertexList.Add(1.0f);

                vertexList.Add(x);
                vertexList.Add(0.0f);
                vertexList.Add(z);
            }

            // upper circle
            for (int i = 0; i < segments; i++)
            {
                float θ = 2 * MathF.PI * i / segments;
                float x = MathF.Cos(θ), z = MathF.Sin(θ);

                vertexList.Add(x);
                vertexList.Add(+halfHeight);
                vertexList.Add(z);

                vertexList.Add(1.0f);
                vertexList.Add(1.0f);
                vertexList.Add(1.0f);
                vertexList.Add(1.0f);

                vertexList.Add(x);
                vertexList.Add(0.0f);
                vertexList.Add(z);
            }

            for (int i = 0; i < segments; i++)
            {
                indexList.Add((uint)i);
                indexList.Add((uint)((i + 1) % segments));
                indexList.Add((uint)(segments + i));
                indexList.Add((uint)(segments + (i + 1) % segments));
            }
            for (int i = 0; i < segments; i += 2)
            {
                indexList.Add((uint)i);
                indexList.Add((uint)(segments + i));
            }

            vertices = vertexList.ToArray();
            indices = indexList.ToArray();
            indexCount = (uint)indices.Length;
        }

        private unsafe void SetupBuffers()
        {
            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
            ebo = gl.GenBuffer();

            gl.BindVertexArray(vao);

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* vertexPtr = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), vertexPtr, BufferUsageARB.StaticDraw);
            }

            gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
            fixed (uint* indexPtr = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indexPtr, BufferUsageARB.StaticDraw);
            }

            int stride = 10 * sizeof(float);

            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)0);
            gl.EnableVertexAttribArray(0);

            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, (uint)stride, (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);

            gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, (uint)stride, (void*)(7 * sizeof(float)));
            gl.EnableVertexAttribArray(2);

            gl.BindVertexArray(0);
        }

        private unsafe void UpdateVertexColors(Vector3 color)
        {
            const int vertexSize = 10;
            const int colorOffset = 3; 

            int vertexCount = vertices.Length / vertexSize;

            for (int i = 0; i < vertexCount; i++)
            {
                int colorStartIndex = i * vertexSize + colorOffset;
                vertices[colorStartIndex + 0] = color.X; // R
                vertices[colorStartIndex + 1] = color.Y; // G  
                vertices[colorStartIndex + 2] = color.Z; // B
                vertices[colorStartIndex + 3] = 1.0f;    // A
            }

            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            fixed (float* vertexPtr = vertices)
            {
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)(vertices.Length * sizeof(float)), vertexPtr);
            }
        }

        public unsafe void RenderDynamicHitboxes(List<CollisionObject> collisionObjects, List<Vector3> exactPositions,
                                               Vector3D<float> cameraPos, Vector3D<float> cameraTarget, Vector3D<float> upVector,
                                               float aspectRatio, float renderDistance, Vector3 color)
        {
            if (!isInitialized || collisionObjects.Count == 0 || exactPositions == null || exactPositions.Count != collisionObjects.Count)
                return;

            UpdateVertexColors(color);

            SetupRenderState(cameraPos, cameraTarget, upVector, aspectRatio, renderDistance, color);

            gl.BindVertexArray(vao);

            for (int i = 0; i < collisionObjects.Count; i++)
            {
                var collision = collisionObjects[i];
                var exactPosition = exactPositions[i];

                var scaleMatrix = Matrix4X4.CreateScale<float>(collision.Radius);
                var translationMatrix = Matrix4X4.CreateTranslation<float>(exactPosition.X, exactPosition.Y + 2.5f, exactPosition.Z);
                var modelMatrix = scaleMatrix * translationMatrix;

                SetModelMatrix(modelMatrix);
                gl.DrawElements(GLEnum.Lines, (uint)indexCount, GLEnum.UnsignedInt, null);
            }

            gl.BindVertexArray(0);
            RestoreRenderState();
        }

        public unsafe void RenderWallEHitbox(Vector3D<float> wallEPosition, float wallERadius,
                                           Vector3D<float> cameraPos, Vector3D<float> cameraTarget, Vector3D<float> upVector,
                                           float aspectRatio, float renderDistance, Vector3 color)
        {
            if (!isInitialized)
                return;

            UpdateVertexColors(color);

            SetupRenderState(cameraPos, cameraTarget, upVector, aspectRatio, renderDistance, color);

            gl.BindVertexArray(vao);

            var scaleMatrix = Matrix4X4.CreateScale<float>(wallERadius);
            var translationMatrix = Matrix4X4.CreateTranslation<float>(wallEPosition.X, wallEPosition.Y+2.5f, wallEPosition.Z);
            var modelMatrix = scaleMatrix * translationMatrix;

            SetModelMatrix(modelMatrix);
            gl.DrawElements(GLEnum.Lines, (uint)indexCount, GLEnum.UnsignedInt, null);

            gl.BindVertexArray(0);
            RestoreRenderState();
        }

        private unsafe void SetupRenderState(Vector3D<float> cameraPos, Vector3D<float> cameraTarget, Vector3D<float> upVector,
                                           float aspectRatio, float renderDistance, Vector3 color)
        {
            gl.UseProgram(shaderProgram);

            var viewMatrix = Matrix4X4.CreateLookAt(cameraPos, cameraTarget, upVector);
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, aspectRatio, 0.1f, renderDistance);

            int viewLoc = gl.GetUniformLocation(shaderProgram, "uView");
            int projLoc = gl.GetUniformLocation(shaderProgram, "uProjection");
            int lightColorLoc = gl.GetUniformLocation(shaderProgram, "uLightColor");
            int ambientLoc = gl.GetUniformLocation(shaderProgram, "uAmbientStrength");
            int diffuseLoc = gl.GetUniformLocation(shaderProgram, "uDiffuseStrength");
            int specularLoc = gl.GetUniformLocation(shaderProgram, "uSpecularStrength");

            if (viewLoc != -1) gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewMatrix);
            if (projLoc != -1) gl.UniformMatrix4(projLoc, 1, false, (float*)&projectionMatrix);

            if (lightColorLoc != -1) gl.Uniform3(lightColorLoc, Vector3.One);

            if (ambientLoc != -1) gl.Uniform1(ambientLoc, 1.0f);
            if (diffuseLoc != -1) gl.Uniform1(diffuseLoc, 0.0f);
            if (specularLoc != -1) gl.Uniform1(specularLoc, 0.0f);

            int useAOLoc = gl.GetUniformLocation(shaderProgram, "uUseAO");
            int useNormalLoc = gl.GetUniformLocation(shaderProgram, "uUseNormal");
            if (useAOLoc != -1) gl.Uniform1(useAOLoc, 0);
            if (useNormalLoc != -1) gl.Uniform1(useNormalLoc, 0);

            gl.Disable(EnableCap.DepthTest);
            gl.Disable(EnableCap.CullFace);
            gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
            gl.LineWidth(4.0f); 
        }

        private unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int modelLoc = gl.GetUniformLocation(shaderProgram, "uModel");
            if (modelLoc != -1)
            {
                gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelMatrix);

                var normalMatrix = new Matrix3X3<float>(modelMatrix.M11, modelMatrix.M12, modelMatrix.M13,
                                                       modelMatrix.M21, modelMatrix.M22, modelMatrix.M23,
                                                       modelMatrix.M31, modelMatrix.M32, modelMatrix.M33);
                int normalLoc = gl.GetUniformLocation(shaderProgram, "uNormal");
                if (normalLoc != -1) gl.UniformMatrix3(normalLoc, 1, false, (float*)&normalMatrix);
            }
        }

        private void RestoreRenderState()
        {
            gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
            gl.Enable(EnableCap.DepthTest);
            gl.Enable(EnableCap.CullFace);
            gl.LineWidth(1.0f);
        }


        public void Dispose()
        {
            if (isInitialized)
            {
                gl.DeleteVertexArray(vao);
                gl.DeleteBuffer(vbo);
                gl.DeleteBuffer(ebo);
                isInitialized = false;
                Console.WriteLine("HitboxRenderer disposed");
            }
        }
    }
}