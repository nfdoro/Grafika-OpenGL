using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class ThumbleweedManager
    {
        private List<ThumbleweedInstance> thumbleweeds = new List<ThumbleweedInstance>();
        private TexturedObjGlObject thumbleweedObject;
        private Random random = new Random();
        private TerrainHeightCalculator terrainCalculator;
        private GL gl;
        private uint objProgram;

        public int Count => thumbleweeds.Count;
        private bool isAnimationEnabled = true;
        public bool IsAnimationEnabled
        {
            get => isAnimationEnabled;
            set => isAnimationEnabled = value;
        }

        private const float UNIFORM_HITBOX_RADIUS = 2.0f; 
        private const float HITBOX_HEIGHT_OFFSET = 0.5f; 

        private class ThumbleweedInstance
        {
            public float X, Z, Y;
            public float Scale, Rotation, MoveDirection, MoveSpeed;
            public float DirectionChangeTimer, DirectionChangeInterval;
            public float VerticalVelocity, VerticalBounceTimer, VerticalBounceInterval;
            public bool IsJumping;
            public float BaseHeight;

            public ThumbleweedInstance(Random rand)
            {
                Scale = 0.8f + (float)(rand.NextDouble() * 0.6f);
                MoveSpeed = 2.0f + (float)(rand.NextDouble() * 3.0f);
                MoveDirection = (float)(rand.NextDouble() * 2 * Math.PI);
                DirectionChangeInterval = 1.0f + (float)(rand.NextDouble() * 4.0f);
                VerticalBounceInterval = 2.0f + (float)(rand.NextDouble() * 3.0f);
                BaseHeight = 0.4f + (float)(rand.NextDouble() * 0.6f);
                Y = BaseHeight;
                IsJumping = false;
                VerticalVelocity = 0f;
            }
        }

        public void Initialize(GL gl, uint objProgram, TerrainHeightCalculator terrainCalculator)
        {
            this.gl = gl;
            this.objProgram = objProgram;
            this.terrainCalculator = terrainCalculator;

            LoadThumbleweedObject();
        }

        public List<CollisionObject> GetCollisionData()
        {
            var collisionData = new List<CollisionObject>();

            foreach (var tumbleweed in thumbleweeds)
            {
                collisionData.Add(new CollisionObject(
                    tumbleweed.X,
                    tumbleweed.Z,
                    UNIFORM_HITBOX_RADIUS
                ));
            }

            return collisionData;
        }

        private void LoadThumbleweedObject()
        {
            float[] thumbleweedColor = new float[] { 0.3f, 0.2f, 0.1f, 1.0f };

            try
            {
                thumbleweedObject = ObjResourceReader.CreateObjectFromResourceWithTextures(gl, "thumbleweed.obj", thumbleweedColor);
                Console.WriteLine("Thumbleweed loaded successfully!");

                int randomCount = random.Next(10, 21);
                CreateInstances(randomCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load thumbleweed: {ex.Message}");
            
            }
        }

        public void CreateInstances(int count)
        {
            thumbleweeds.Clear();

            if (terrainCalculator == null) return;

            terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);

            float centerX = (minX + maxX) / 2f;
            float centerZ = (minZ + maxZ) / 2f;
            float visibilityRange = 30f;

            for (int i = 0; i < count; i++)
            {
                var tumbleweed = new ThumbleweedInstance(random);

                float angle = (float)(random.NextDouble() * 2 * Math.PI);
                float radius = (float)(random.NextDouble() * visibilityRange);

                tumbleweed.X = centerX + radius * (float)Math.Cos(angle);
                tumbleweed.Z = centerZ + radius * (float)Math.Sin(angle);

                tumbleweed.X = Math.Clamp(tumbleweed.X, minX + 5f, maxX - 5f);
                tumbleweed.Z = Math.Clamp(tumbleweed.Z, minZ + 5f, maxZ - 5f);

                thumbleweeds.Add(tumbleweed);
            }

            Console.WriteLine($"Created {count} thumbleweed instances in visible area");
        }

        public void Update(float deltaTime)
        {
            if (!isAnimationEnabled || thumbleweeds.Count == 0) return;

            foreach (var tumbleweed in thumbleweeds)
            {
                tumbleweed.Rotation += 60.0f * deltaTime;
                if (tumbleweed.Rotation >= 360f) tumbleweed.Rotation -= 360f;

                tumbleweed.DirectionChangeTimer += deltaTime;
                if (tumbleweed.DirectionChangeTimer >= tumbleweed.DirectionChangeInterval)
                {
                    tumbleweed.MoveDirection = (float)(random.NextDouble() * 2 * Math.PI);
                    tumbleweed.DirectionChangeTimer = 0f;
                    tumbleweed.DirectionChangeInterval = 1.0f + (float)(random.NextDouble() * 4.0f);
                }

                tumbleweed.VerticalBounceTimer += deltaTime;
                if (!tumbleweed.IsJumping && tumbleweed.VerticalBounceTimer >= tumbleweed.VerticalBounceInterval)
                {
                    tumbleweed.IsJumping = true;
                    tumbleweed.VerticalVelocity = 7.0f + (float)(random.NextDouble() * 5.0f);
                    tumbleweed.VerticalBounceTimer = 0f;
                    tumbleweed.VerticalBounceInterval = 2.0f + (float)(random.NextDouble() * 4.0f);
                }

                if (tumbleweed.IsJumping)
                {
                    tumbleweed.Y += tumbleweed.VerticalVelocity * deltaTime;
                    tumbleweed.VerticalVelocity -= 9.81f * deltaTime;

                    if (tumbleweed.Y <= tumbleweed.BaseHeight)
                    {
                        tumbleweed.Y = tumbleweed.BaseHeight;
                        tumbleweed.IsJumping = false;
                        tumbleweed.VerticalVelocity = 0f;
                    }
                }

                float moveX = (float)Math.Cos(tumbleweed.MoveDirection) * tumbleweed.MoveSpeed * deltaTime;
                float moveZ = (float)Math.Sin(tumbleweed.MoveDirection) * tumbleweed.MoveSpeed * deltaTime;

                float newX = tumbleweed.X + moveX;
                float newZ = tumbleweed.Z + moveZ;

                if (terrainCalculator != null && terrainCalculator.IsPositionWithinTerrain(newX, newZ))
                {
                    tumbleweed.X = newX;
                    tumbleweed.Z = newZ;
                }
                else
                {
                    tumbleweed.MoveDirection += (float)Math.PI;
                    if (tumbleweed.MoveDirection >= 2 * Math.PI)
                        tumbleweed.MoveDirection -= (float)(2 * Math.PI);
                }
            }
        }

        public List<Vector3> GetThumbleweedPositions()
        {
            var positions = new List<Vector3>();

            foreach (var tumbleweed in thumbleweeds)
            {
                float terrainHeight = 0f;
                if (terrainCalculator != null)
                {
                    terrainHeight = terrainCalculator.GetHeightAtPosition(tumbleweed.X, tumbleweed.Z);
                }

                float hitboxY = terrainHeight + HITBOX_HEIGHT_OFFSET + tumbleweed.Y;

                positions.Add(new Vector3(tumbleweed.X, hitboxY, tumbleweed.Z));
            }

            return positions;
        }

        public unsafe void Render(float shininess, float ambient, float diffuse, float specular,
                                Vector3 lightColor, Vector3 lightPos, Vector3 viewPos)
        {
            if (thumbleweedObject == null || thumbleweeds.Count == 0) return;

            gl.UseProgram(objProgram);

            SetUniformForObjShader("uShininess", shininess);
            SetUniformForObjShader("uAmbientStrength", ambient);
            SetUniformForObjShader("uDiffuseStrength", diffuse);
            SetUniformForObjShader("uSpecularStrength", specular);

            SetUniform3ForObjShader("uLightColor", lightColor);
            SetUniform3ForObjShader("uLightPos", lightPos);
            SetUniform3ForObjShader("uViewPos", viewPos);

            ResetTextureStates();

            if (thumbleweedObject.AOTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, thumbleweedObject.AOTextureId.Value);
                int useAOLocation = gl.GetUniformLocation(objProgram, "uUseAO");
                if (useAOLocation != -1) gl.Uniform1(useAOLocation, 1);
                int aoTexLocation = gl.GetUniformLocation(objProgram, "uAOTexture");
                if (aoTexLocation != -1) gl.Uniform1(aoTexLocation, 0);
            }

            if (thumbleweedObject.NormalTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture1);
                gl.BindTexture(TextureTarget.Texture2D, thumbleweedObject.NormalTextureId.Value);
                int useNormalLocation = gl.GetUniformLocation(objProgram, "uUseNormal");
                if (useNormalLocation != -1) gl.Uniform1(useNormalLocation, 1);
                int normalTexLocation = gl.GetUniformLocation(objProgram, "uNormalTexture");
                if (normalTexLocation != -1) gl.Uniform1(normalTexLocation, 1);
            }

            gl.Disable(EnableCap.CullFace);

            foreach (var tumbleweed in thumbleweeds)
            {
                float terrainHeight = 0f;
                if (terrainCalculator != null)
                {
                    terrainHeight = terrainCalculator.GetHeightAtPosition(tumbleweed.X, tumbleweed.Z);
                }

                float thumbleweedRadius = tumbleweed.Scale * 0.7f;
                float renderY = terrainHeight + thumbleweedRadius + tumbleweed.Y + 0.6f;

                var transform = Matrix4X4.CreateScale(tumbleweed.Scale) *
                              Matrix4X4.CreateRotationY(tumbleweed.Rotation * (float)Math.PI / 180f) *
                              Matrix4X4.CreateTranslation(tumbleweed.X, renderY, tumbleweed.Z);

                SetModelMatrix(transform);

                gl.BindVertexArray(thumbleweedObject.Vao);
                gl.DrawElements(GLEnum.Triangles, thumbleweedObject.IndexArrayLength, GLEnum.UnsignedInt, null);
            }

            gl.BindVertexArray(0);
            gl.Enable(EnableCap.CullFace);
            UnbindTextures();
        }

        public void ToggleAnimation()
        {
            isAnimationEnabled = !isAnimationEnabled;
            if (isAnimationEnabled)
            {
                Console.WriteLine($"Thumbleweed animation turn on ({thumbleweeds.Count})");
            }
            else
            {
                Console.WriteLine("Thumbleweed animation turn of");
            }
        }

        private unsafe void SetUniformForObjShader(string uniformName, float uniformValue)
        {
            int location = gl.GetUniformLocation(objProgram, uniformName);
            if (location != -1)
            {
                gl.Uniform1(location, uniformValue);
            }
        }

        private unsafe void SetUniform3ForObjShader(string uniformName, Vector3 uniformValue)
        {
            int location = gl.GetUniformLocation(objProgram, uniformName);
            if (location != -1)
            {
                gl.Uniform3(location, uniformValue);
            }
        }

        private unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = gl.GetUniformLocation(objProgram, "uModel");
            if (location != -1)
            {
                gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            }

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = gl.GetUniformLocation(objProgram, "uNormal");
            if (location != -1)
            {
                gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            }
        }

        private void ResetTextureStates()
        {
            int useAOLocation = gl.GetUniformLocation(objProgram, "uUseAO");
            int useNormalLocation = gl.GetUniformLocation(objProgram, "uUseNormal");

            if (useAOLocation != -1) gl.Uniform1(useAOLocation, 0);
            if (useNormalLocation != -1) gl.Uniform1(useNormalLocation, 0);
        }

        private void UnbindTextures()
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.ActiveTexture(TextureUnit.Texture0);
        }

        public void Dispose()
        {
            thumbleweedObject?.ReleaseGlObject();
        }
    }
}