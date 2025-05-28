using Silk.NET.Maths;
using Silk.NET.OpenGL;
using System.Numerics;

namespace Projekt_OpenGL
{
    public class CactusManager
    {
        private GL gl;
        private uint shaderProgram;
        private TerrainHeightCalculator terrainCalculator;

        private List<TexturedObjGlObject> cactusModels = new List<TexturedObjGlObject>();
        private List<(Vector3 min, Vector3 max)> cactusModelBounds = new List<(Vector3, Vector3)>();
        private List<CactusInstance> cactusInstances = new List<CactusInstance>();
        private Random random = new Random();

        private const int MIN_CACTUS_COUNT = 35;
        private const int MAX_CACTUS_COUNT = 50;
        private const float MIN_SCALE = 4.0f;
        private const float MAX_SCALE = 6.0f;
        private const float MIN_DISTANCE_FROM_CENTER = 20f;
        private const float MIN_DISTANCE_BETWEEN_CACTUS = 8.0f;

        private const float UNIFORM_HITBOX_RADIUS = 2.0f; 

        public int Count => cactusInstances.Count;

        private class CactusInstance
        {
            public float X, Z, Y;
            public float Rotation, Scale;
            public int ModelIndex;
            public float HitboxY; 
            public Vector3 ModelBoundsMin, ModelBoundsMax; 

            public CactusInstance(Random rand)
            {
                Scale = MIN_SCALE + (float)(rand.NextDouble() * (MAX_SCALE - MIN_SCALE));
                Rotation = (float)(rand.NextDouble() * 360.0);
            }
        }

        public void Initialize(GL gl, uint shaderProgram, TerrainHeightCalculator terrainCalculator)
        {
            this.gl = gl;
            this.shaderProgram = shaderProgram;
            this.terrainCalculator = terrainCalculator;

            LoadCactusModels();
            CreateInstances();

            Console.WriteLine($"CactusManager initialized with {cactusInstances.Count} cactUS using {cactusModels.Count} different models");
        }

        public List<CollisionObject> GetCollisionData()
        {
            var collisionData = new List<CollisionObject>();

            foreach (var cactus in cactusInstances)
            {
                collisionData.Add(new CollisionObject(
                    cactus.X,
                    cactus.Z,
                    UNIFORM_HITBOX_RADIUS
                ));
            }

            return collisionData;
        }

        public List<Vector3> GetCactusPositions()
        {
            var positions = new List<Vector3>();

            foreach (var cactus in cactusInstances)
            {
                positions.Add(new Vector3(cactus.X, cactus.HitboxY, cactus.Z));
            }

            return positions;
        }

        private void LoadCactusModels()
        {
            string[] cactusFiles = {
                "cactus.Cactus1.obj",
                "cactus.Cactus2.obj",
                "cactus.Cactus3.obj",
                "cactus.Cactus4.obj",
                "cactus.Cactus5.obj"
            };

            float[] cactusColor = new float[] { 0.0f, 0.329f, 0.043f, 1.0f };

            foreach (string cactusFile in cactusFiles)
            {
                try
                {
                    var cactusModel = ObjResourceReader.CreateObjectFromResourceWithTextures(gl, cactusFile, cactusColor);
                    cactusModels.Add(cactusModel);

                    var bounds = ObjResourceReader.GetLocalBoundingBox(cactusFile);
                    cactusModelBounds.Add(bounds);

                    Console.WriteLine($"Loaded cactus model: {cactusFile}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load cactus model {cactusFile}: {ex.Message}");
                }
            }

            if (cactusModels.Count == 0)
            {
                Console.WriteLine("Warning: No cactus models were loaded successfully!");
            }
        }

        private void CreateInstances()
        {
            cactusInstances.Clear();

            if (cactusModels.Count == 0 || terrainCalculator == null)
            {
                Console.WriteLine("Cannot generate cactus instances: no models loaded or terrain calculator missing");
                return;
            }

            terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);

            int cactusCount = random.Next(MIN_CACTUS_COUNT, MAX_CACTUS_COUNT + 1);
            int attempts = 0;
            int maxAttempts = cactusCount * 10;

            while (cactusInstances.Count < cactusCount && attempts < maxAttempts)
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
                foreach (var existingCactus in cactusInstances)
                {
                    float distance = MathF.Sqrt(
                        (x - existingCactus.X) * (x - existingCactus.X) +
                        (z - existingCactus.Z) * (z - existingCactus.Z)
                    );

                    if (distance < MIN_DISTANCE_BETWEEN_CACTUS)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                float terrainHeight = terrainCalculator.GetHeightAtPosition(x, z);

                var cactusInstance = new CactusInstance(random)
                {
                    X = x,
                    Z = z,
                    Y = terrainHeight-0.2f,
                    ModelIndex = random.Next(0, cactusModels.Count)
                };


                cactusInstance.HitboxY = terrainHeight;
                cactusInstances.Add(cactusInstance);
            }

            Console.WriteLine($"Succesfully generated {cactusInstances.Count} cactus instances!");
        }

        public void Update(float deltaTime)
        {
            // static
        }

        public unsafe void Render(float shininess, float ambient, float diffuse, float specular,
                                Vector3 lightColor, Vector3 lightPosition, Vector3 viewPosition)
        {
            if (cactusModels.Count == 0 || cactusInstances.Count == 0)
                return;

            gl.UseProgram(shaderProgram);

            SetUniformFloat("uShininess", shininess);
            SetUniformFloat("uAmbientStrength", ambient);
            SetUniformFloat("uDiffuseStrength", diffuse);
            SetUniformFloat("uSpecularStrength", specular);
            SetUniformVector3("uLightColor", lightColor);
            SetUniformVector3("uLightPos", lightPosition);
            SetUniformVector3("uViewPos", viewPosition);

            foreach (var cactus in cactusInstances)
            {
                RenderCactusInstance(cactus);
            }
        }

        private unsafe void RenderCactusInstance(CactusInstance cactus)
        {
            if (cactus.ModelIndex >= cactusModels.Count)
                return;

            var model = cactusModels[cactus.ModelIndex];

            var modelBounds = cactusModelBounds[cactus.ModelIndex];
            Vector3 modelCenter = new Vector3(
                (modelBounds.min.X + modelBounds.max.X) * 0.5f,  
                modelBounds.min.Y,                               
                (modelBounds.min.Z + modelBounds.max.Z) * 0.5f   
            );

            var centerOffsetMatrix = Matrix4X4.CreateTranslation(-modelCenter.X, -modelCenter.Y, -modelCenter.Z);
            var scaleMatrix = Matrix4X4.CreateScale(cactus.Scale);
            var rotationMatrix = Matrix4X4.CreateRotationY(cactus.Rotation * MathF.PI / 180f);
            var finalPositionMatrix = Matrix4X4.CreateTranslation(cactus.X, cactus.Y, cactus.Z);

            var transform = centerOffsetMatrix * scaleMatrix * rotationMatrix * finalPositionMatrix;

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
            if (cactusModels != null)
            {
                foreach (var model in cactusModels)
                {
                    model?.ReleaseGlObject();
                }
                cactusModels.Clear();
            }

            cactusInstances?.Clear();

            Console.WriteLine("CactusManager disposed");
        }
    }
}