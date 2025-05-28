using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class CanManager
    {
        private GL gl;
        private uint shaderProgram;
        private TerrainHeightCalculator terrainCalculator;

        private List<TexturedObjGlObject> canModels = new List<TexturedObjGlObject>();
        private List<CanInstance> canInstances = new List<CanInstance>();
        private Random random = new Random();

        private const int MIN_CAN_COUNT = 35;
        private const int MAX_CAN_COUNT = 50;
        private const float MIN_SCALE = 0.8f;
        private const float MAX_SCALE = 1.2f;
        private const float MIN_DISTANCE_FROM_CENTER = 10f;
        private const float MIN_DISTANCE_BETWEEN_CANS = 8.0f;

        private const float UNIFORM_HITBOX_RADIUS = 2.0f;

        public int Score { get; private set; } = 0;
        public const int POINTS_PER_CAN = 10;

        private class CanInstance
        {
            public float X, Z, Y;
            public float Rotation, Scale;
            public bool IsCollected;
            public int ModelIndex; 

            public CanInstance(Random rand, int modelCount)
            {
                Scale = MIN_SCALE + (float)(rand.NextDouble() * (MAX_SCALE - MIN_SCALE));
                Rotation = (float)(rand.NextDouble() * 360.0);
                IsCollected = false;
                ModelIndex = rand.Next(0, modelCount); 
            }
        }

        public void Initialize(GL gl, uint shaderProgram, TerrainHeightCalculator terrainCalculator)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
            this.terrainCalculator = terrainCalculator;

            LoadCanModels();
            CreateInstances();

            Console.WriteLine($"CanManager initialized with {canInstances.Count} cans");
        }

        public List<CollisionObject> GetCollisionData()
        {
            var collisionData = new List<CollisionObject>();

            foreach (var can in canInstances)
            {
                if (!can.IsCollected)
                {
                    collisionData.Add(new CollisionObject(
                        can.X,
                        can.Z,
                        UNIFORM_HITBOX_RADIUS
                    ));
                }
            }

            return collisionData;
        }

        public List<Vector3> GetCanPositions()
        {
            var positions = new List<Vector3>();

            foreach (var can in canInstances)
            {
                if (!can.IsCollected)
                {
                    positions.Add(new Vector3(can.X, can.Y, can.Z));
                }
            }

            return positions;
        }

        public bool TryCollectCan(float wallEX, float wallEZ, float collectRadius = 3.0f)
        {
            bool collected = false;

            foreach (var can in canInstances)
            {
                if (!can.IsCollected)
                {
                    float distance = MathF.Sqrt(
                        (wallEX - can.X) * (wallEX - can.X) +
                        (wallEZ - can.Z) * (wallEZ - can.Z)
                    );

                    if (distance <= collectRadius)
                    {
                        can.IsCollected = true;
                        collected = true;
                        Score += POINTS_PER_CAN;
                        Console.WriteLine($"Can collected at ({can.X:F1}, {can.Z:F1})! Score: {Score}");
                    }
                }
            }

            return collected;
        }

        public int GetRemainingCount()
        {
            int count = 0;
            foreach (var can in canInstances)
            {
                if (!can.IsCollected) count++;
            }
            return count;
        }

        public int GetCount()
        {
            int count = 0;
            foreach (var can in canInstances)
            {
                if (can.IsCollected) count++;
            }
            return count;
        }

        public void RespawnCans()
        {
            foreach (var can in canInstances)
            {
                can.IsCollected = false;
            }
            Console.WriteLine("All cans respawned!");
        }

        public void ResetScore()
        {
            Score = 0;
        }
        private void LoadCanModels()
        {
            string[] canFiles = {
                "GameOBJ.can1.obj",  // GameOBJ/can1.obj
                "GameOBJ.can2.obj"   // GameOBJ/can2.obj
            };

            float[] canColor = new float[] { 0.8f, 0.8f, 0.8f, 1.0f };

            foreach (string canFile in canFiles)
            {
                try
                {
                    var canModel = ObjResourceReader.CreateObjectFromResourceWithTextures(gl, canFile, canColor);
                    canModels.Add(canModel);
                    Console.WriteLine($"Loaded can model: {canFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load can model {canFile}: {ex.Message}");
                }
            }

            if (canModels.Count == 0)
            {
                Console.WriteLine("Warning: No can models were loaded successfully!");
            }
        }

        private void CreateInstances()
        {
            canInstances.Clear();

            if (canModels.Count == 0 || terrainCalculator == null)
            {
                Console.WriteLine("Cannot generate can instances: no models loaded or terrain calculator missing");
                return;
            }

            terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);

            int canCount = random.Next(MIN_CAN_COUNT, MAX_CAN_COUNT + 1);
            int attempts = 0;
            int maxAttempts = canCount * 10;

            while (canInstances.Count < canCount && attempts < maxAttempts)
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
                foreach (var existingCan in canInstances)
                {
                    float distance = MathF.Sqrt(
                        (x - existingCan.X) * (x - existingCan.X) +
                        (z - existingCan.Z) * (z - existingCan.Z)
                    );

                    if (distance < MIN_DISTANCE_BETWEEN_CANS)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                float terrainHeight = terrainCalculator.GetHeightAtPosition(x, z);

                var canInstance = new CanInstance(random, canModels.Count)
                {
                    X = x,
                    Z = z,
                    Y = terrainHeight 
                };

                canInstances.Add(canInstance);
            }

            Console.WriteLine($"Succesfully generated generated {canInstances.Count}");
        }

        public unsafe void Render(float shininess, float ambient, float diffuse, float specular,
                                Vector3 lightColor, Vector3 lightPosition, Vector3 viewPosition)
        {
            if (canModels.Count == 0 || canInstances.Count == 0)
                return;

            gl.UseProgram(shaderProgram);

            SetUniformFloat("uShininess", shininess);
            SetUniformFloat("uAmbientStrength", ambient);
            SetUniformFloat("uDiffuseStrength", diffuse);
            SetUniformFloat("uSpecularStrength", specular);
            SetUniformVector3("uLightColor", lightColor);
            SetUniformVector3("uLightPos", lightPosition);
            SetUniformVector3("uViewPos", viewPosition);

            foreach (var can in canInstances)
            {
                if (!can.IsCollected)
                {
                    RenderCanInstance(can);
                }
            }
        }
        private unsafe void RenderCanInstance(CanInstance can)
        {
            if (canModels.Count == 0 || can.ModelIndex >= canModels.Count)
                return;

            var model = canModels[can.ModelIndex];

            float baseScale = 0.2f;
            float totalScale = baseScale * can.Scale;

            var scaleMatrix = Matrix4X4.CreateScale(totalScale);
            var rotationMatrix = Matrix4X4.CreateRotationY(can.Rotation * MathF.PI / 180f);
            var translationMatrix = Matrix4X4.CreateTranslation(can.X, can.Y, can.Z);

            var transform = scaleMatrix * rotationMatrix * translationMatrix;

            SetModelMatrix(transform);
            ResetTextureStates();

            int textureUnit = 0;

            if (model.BaseColorTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                gl.BindTexture(TextureTarget.Texture2D, model.BaseColorTextureId.Value);
                SetUniformInt("uUseTexture", 1);        
                SetUniformInt("uTexture", textureUnit);
                textureUnit++;
            }

            // AO 
            if (model.AOTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                gl.BindTexture(TextureTarget.Texture2D, model.AOTextureId.Value);
                SetUniformInt("uUseAO", 1);
                SetUniformInt("uAOTexture", textureUnit);
                textureUnit++;
            }

            // Normal 
            if (model.NormalTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                gl.BindTexture(TextureTarget.Texture2D, model.NormalTextureId.Value);
                SetUniformInt("uUseNormal", 1);
                SetUniformInt("uNormalTexture", textureUnit);
                textureUnit++;
            }

            // Metallic
            if (model.MetallicTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                gl.BindTexture(TextureTarget.Texture2D, model.MetallicTextureId.Value);
                SetUniformInt("uUseMetallic", 1);
                SetUniformInt("uMetallicTexture", textureUnit);
                textureUnit++;
            }

            // Roughness 
            if (model.RoughnessTextureId.HasValue)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + textureUnit);
                gl.BindTexture(TextureTarget.Texture2D, model.RoughnessTextureId.Value);
                SetUniformInt("uUseRoughness", 1);
                SetUniformInt("uRoughnessTexture", textureUnit);
                textureUnit++;
            }

            gl.BindVertexArray(model.Vao);
            gl.DrawElements(GLEnum.Triangles, model.IndexArrayLength, GLEnum.UnsignedInt, null);
            gl.BindVertexArray(0);

            UnbindAllTextures(textureUnit);
        }

        private void ResetTextureStates()
        {
            SetUniformInt("uUseTexture", 0);   
            SetUniformInt("uUseAO", 0);
            SetUniformInt("uUseNormal", 0);
            SetUniformInt("uUseMetallic", 0);
            SetUniformInt("uUseRoughness", 0);
        }
        private void UnbindAllTextures(int maxTextureUnit)
        {
            for (int i = 0; i < maxTextureUnit; i++)
            {
                gl.ActiveTexture(TextureUnit.Texture0 + i);
                gl.BindTexture(TextureTarget.Texture2D, 0);
            }
            gl.ActiveTexture(TextureUnit.Texture0); // Reset to texture unit 0
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
            if (canModels != null)
            {
                foreach (var model in canModels)
                {
                    model?.ReleaseGlObject();
                }
                canModels.Clear();
            }

            canInstances?.Clear();

            Console.WriteLine("CanManager disposed");
        }
    }
}