using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class SunManager
    {
        private GL gl;
        private uint shaderProgram;
        private TerrainHeightCalculator terrainCalculator;

        private List<TexturedObjGlObject> sunModels = new List<TexturedObjGlObject>();
        private List<SunInstance> sunInstances = new List<SunInstance>();
        private Random random = new Random();

        private const int MIN_SUN_COUNT = 35;
        private const int MAX_SUN_COUNT = 50;
        private const float MIN_SCALE = 0.8f;
        private const float MAX_SCALE = 1.2f;
        private const float MIN_DISTANCE_FROM_CENTER = 15f;
        private const float MIN_DISTANCE_BETWEEN_SUNS = 10.0f;

        private const float UNIFORM_HITBOX_RADIUS = 2.5f;

        private class SunInstance
        {
            public float X, Z, Y;
            public float Rotation, Scale;
            public float RotationSpeed;
            public float PulseTime;
            public float PulseSpeed;
            public bool IsCollected;
            public float HitboxY;

            public SunInstance(Random rand)
            {
                Scale = MIN_SCALE + (float)(rand.NextDouble() * (MAX_SCALE - MIN_SCALE));
                Rotation = (float)(rand.NextDouble() * 360.0);
                RotationSpeed = 20.0f + (float)(rand.NextDouble() * 40.0f);
                PulseTime = (float)(rand.NextDouble() * Math.PI * 2);
                PulseSpeed = 1.0f + (float)(rand.NextDouble() * 2.0f);
                IsCollected = false;
            }
        }

        public void Initialize(GL gl, uint shaderProgram, TerrainHeightCalculator terrainCalculator)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
            this.terrainCalculator = terrainCalculator;

            LoadSunModels();
            CreateInstances();

            Console.WriteLine($"SunManager initialized with {sunInstances.Count} suns");
        }

        public List<CollisionObject> GetCollisionData()
        {
            var collisionData = new List<CollisionObject>();

            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected)
                {
                    collisionData.Add(new CollisionObject(
                        sun.X,
                        sun.Z,
                        UNIFORM_HITBOX_RADIUS
                    ));
                }
            }

            return collisionData;
        }

        public List<Vector3> GetSunPositions()
        {
            var positions = new List<Vector3>();

            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected)
                {
                    positions.Add(new Vector3(sun.X, sun.HitboxY, sun.Z));
                }
            }

            return positions;
        }

        public bool TryCollectSun(float wallEX, float wallEZ, float collectRadius = 3.0f)
        {
            bool collected = false;

            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected)
                {
                    float distance = MathF.Sqrt(
                        (wallEX - sun.X) * (wallEX - sun.X) +
                        (wallEZ - sun.Z) * (wallEZ - sun.Z)
                    );

                    if (distance <= collectRadius)
                    {
                        sun.IsCollected = true;
                        collected = true;
                        Console.WriteLine($"Sun collected at ({sun.X:F1}, {sun.Z:F1})!");
                    }
                }
            }

            return collected;
        }

        public int GetRemainingCount()
        {
            int count = 0;
            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected) count++;
            }
            return count;
        }

        public void RespawnSuns()
        {
            foreach (var sun in sunInstances)
            {
                sun.IsCollected = false;
            }
            Console.WriteLine("All suns respawned!");
        }

        private void LoadSunModels()
        {

            string[] sunFiles = {
                "GameOBJ.sun.obj"  
            };

            float[] sunColor = new float[] { 1.0f, 1.0f, 0.0f, 1.0f }; 

            foreach (string sunFile in sunFiles)
            {
                try
                {
                    var sunModel = ObjResourceReader.CreateObjectFromResourceWithTextures(gl, sunFile, sunColor);
                    sunModels.Add(sunModel);
                    Console.WriteLine($"Loaded sun model: {sunFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load sun model {sunFile}: {ex.Message}");
                }
            }

            if (sunModels.Count == 0)
            {
                Console.WriteLine("Warning: No sun models were loaded successfully!");
            }
        }

        private void CreateInstances()
        {
            sunInstances.Clear();

            if (sunModels.Count == 0 || terrainCalculator == null)
            {
                Console.WriteLine("Cannot generate sun instances: no models loaded or terrain calculator missing");
                return;
            }

            terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);

            int sunCount = random.Next(MIN_SUN_COUNT, MAX_SUN_COUNT + 1);
            int attempts = 0;
            int maxAttempts = sunCount * 10;

            while (sunInstances.Count < sunCount && attempts < maxAttempts)
            {
                attempts++;

                float x = (float)(random.NextDouble() * (maxX - minX) + minX);
                float z = (float)(random.NextDouble() * (maxZ - minZ) + minZ);

                if (!terrainCalculator.IsPositionWithinTerrain(x, z))
                    continue;

                float distanceFromCenter = MathF.Sqrt(x * x + z * z);
                if (distanceFromCenter < MIN_DISTANCE_FROM_CENTER)
                    continue;

                bool tooClose = false;
                foreach (var existingSun in sunInstances)
                {
                    float distance = MathF.Sqrt(
                        (x - existingSun.X) * (x - existingSun.X) +
                        (z - existingSun.Z) * (z - existingSun.Z)
                    );

                    if (distance < MIN_DISTANCE_BETWEEN_SUNS)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                float terrainHeight = terrainCalculator.GetHeightAtPosition(x, z);

                var sunInstance = new SunInstance(random)
                {
                    X = x,
                    Z = z,
                    Y = terrainHeight + 2.0f, 
                    HitboxY = terrainHeight + 1.0f
                };

                sunInstances.Add(sunInstance);
            }

            Console.WriteLine($"Succesfully generated {sunInstances.Count} sun");
        }

        public void Update(float deltaTime)
        {
            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected)
                {
                    sun.Rotation += sun.RotationSpeed * deltaTime;
                    if (sun.Rotation >= 360f) sun.Rotation -= 360f;

                    sun.PulseTime += sun.PulseSpeed * deltaTime;
                }
            }
        }

        public unsafe void Render(float shininess, float ambient, float diffuse, float specular,
                                Vector3 lightColor, Vector3 lightPosition, Vector3 viewPosition)
        {
            if (sunModels.Count == 0 || sunInstances.Count == 0)
                return;

            gl.UseProgram(shaderProgram);


            SetUniformFloat("uShininess", shininess);
            SetUniformFloat("uAmbientStrength", ambient);
            SetUniformFloat("uDiffuseStrength", diffuse);
            SetUniformFloat("uSpecularStrength", specular);
            SetUniformVector3("uLightColor", lightColor);
            SetUniformVector3("uLightPos", lightPosition);
            SetUniformVector3("uViewPos", viewPosition);


            foreach (var sun in sunInstances)
            {
                if (!sun.IsCollected)
                {
                    RenderSunInstance(sun);
                }
            }
        }

        private unsafe void RenderSunInstance(SunInstance sun)
        {
            if (sunModels.Count == 0)
                return;

            var model = sunModels[0]; 

            float pulseScale = 1.0f + 0.2f * MathF.Sin(sun.PulseTime);
            float baseScale = 0.02f; 
            float totalScale = baseScale * sun.Scale * pulseScale;


            var scaleMatrix = Matrix4X4.CreateScale(totalScale);
            var rotationUpMatrix = Matrix4X4.CreateRotationX(-MathF.PI / 2f);
            var rotationYMatrix = Matrix4X4.CreateRotationY(sun.Rotation * MathF.PI / 180f);
            var translationMatrix = Matrix4X4.CreateTranslation(sun.X, sun.Y + 2.0f, sun.Z);

            var transform = scaleMatrix * rotationUpMatrix * rotationYMatrix * translationMatrix;


            SetModelMatrix(transform);

            ResetTextureStates();

            if (model.AOTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0);
                gl.BindTexture(TextureTarget.Texture2D, model.AOTextureId.Value);
                SetUniformInt("uUseAO", 1);
                SetUniformInt("uAOTexture", 0);
            }

            if (model.NormalTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture1);
                gl.BindTexture(TextureTarget.Texture2D, model.NormalTextureId.Value);
                SetUniformInt("uUseNormal", 1);
                SetUniformInt("uNormalTexture", 1);
            }

            gl.BindVertexArray(model.Vao);
            gl.DrawElements(GLEnum.Triangles, model.IndexArrayLength, GLEnum.UnsignedInt, null);
            gl.BindVertexArray(0);

            UnbindTextures();
        }

        private void ResetTextureStates()
        {
            SetUniformInt("uUseAO", 0);
            SetUniformInt("uUseNormal", 0);
        }

        private void UnbindTextures()
        {
            gl.ActiveTexture(TextureUnit.Texture0);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.ActiveTexture(TextureUnit.Texture1);
            gl.BindTexture(TextureTarget.Texture2D, 0);
            gl.ActiveTexture(TextureUnit.Texture0);
        }

        private void SetUniformFloat(string name, float value)
        {
            int location = gl.GetUniformLocation(shaderProgram, name);
            if (location != -1)
            {
                gl.Uniform1(location, value);
            }
        }

        private void SetUniformInt(string name, int value)
        {
            int location = gl.GetUniformLocation(shaderProgram, name);
            if (location != -1)
            {
                gl.Uniform1(location, value);
            }
        }

        private void SetUniformVector3(string name, Vector3 value)
        {
            int location = gl.GetUniformLocation(shaderProgram, name);
            if (location != -1)
            {
                gl.Uniform3(location, value);
            }
        }

        private unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = gl.GetUniformLocation(shaderProgram, "uModel");
            if (location != -1)
            {
                gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            }

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(
                modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInverse;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInverse);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInverse));

            location = gl.GetUniformLocation(shaderProgram, "uNormal");
            if (location != -1)
            {
                gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            }
        }

        public void Dispose()
        {
            if (sunModels != null)
            {
                foreach (var model in sunModels)
                {
                    model?.ReleaseGlObject();
                }
                sunModels.Clear();
            }

            sunInstances?.Clear();

            Console.WriteLine("SunManager disposed");
        }
    }
}