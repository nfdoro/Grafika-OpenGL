using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using System.Reflection;
using ImGuiNET;

namespace Projekt_OpenGL
{
    internal class Program
    {
        private static CameraController cameraController = new();
        private static ThumbleweedManager thumbleweedManager = new ThumbleweedManager();
        private static CactusManager cactusManager = new CactusManager();
        private static SunManager sunManager = new SunManager();
        private static CanManager canManager = new CanManager();
        private static HitboxRenderer hitboxRenderer = new HitboxRenderer();
        private static bool showHitboxes = false;

        // Game state
        private static bool isGameOver = false;
        private static bool gameOverMessageShown = false;

        private static IWindow window;
        private static GL Gl;
        private static ImGuiController imGuiController;

        private static uint program;
        private static uint skyboxProgram;
        private static uint objProgram;

        private static TexturedObjGlObject desertTextured;
        private static Skybox skybox;

        private static HashSet<Key> pressedKeys = new HashSet<Key>();

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ShinenessVariableName = "uShininess";
        private const string AmbientVariableName = "uAmbientStrength";
        private const string DiffuseVariableName = "uDiffuseStrength";
        private const string SpecularVariableName = "uSpecularStrength";
        private const string LightColorVariableName = "uLightColor";
        private const string LightPositionVariableName = "uLightPos";
        private const string ViewPositionVariableName = "uViewPos";
        private const string TextureVariableName = "uTexture";
        private const string UseTextureVariableName = "uUseTexture";

        private static float renderDistance = 500f;
        private static float renderDistanceInput = renderDistance;

        private static float shininess = 60;
        private static float ambient = 0.3f;
        private static float diffuse = 0.3f;
        private static float specular = 0.6f;
        private static float red = 1.0f;
        private static float green = 1.0f;
        private static float blue = 1.0f;
        private static float lightX = 0f;
        private static float lightY = 5.0f;
        private static float lightZ = 0f;

        private static float wallEScale = 1.0f;

        private static List<GlObject> wallEParts;
        private static TerrainHeightCalculator terrainCalculator;

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Wall-E in Desert";
            windowOptions.Size = new Vector2D<int>(1024, 768);
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
            IInputContext inputContext = window.CreateInput();

            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
                keyboard.KeyUp += Keyboard_KeyUp;
            }

            Gl = window.CreateOpenGL();
            imGuiController = new ImGuiController(Gl, window, inputContext);
            Gl.ClearColor(System.Drawing.Color.DarkGray);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            LinkProgram();
            Gl.Enable(EnableCap.CullFace);
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            SetUpObjects();
        }

        private static void Window_Update(double deltaTime)
        {

            if (!isGameOver)
            {
                foreach (var key in pressedKeys)
                {
                    if (key == Key.W || key == Key.A || key == Key.S || key == Key.D || key == Key.Q || key == Key.E)
                    {
                        cameraController.HandleMovement(key, (float)deltaTime);
                    }
                    else if (key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down)
                    {
                        cameraController.HandleFreeCameraRotation(key, (float)deltaTime);
                        if (cameraController.CurrentMode == CameraMode.FirstPerson)
                        {
                            cameraController.HandleCameraRotation(key, (float)deltaTime);
                        }
                    }
                }

                //Wall-e energy system
                var wallEController = cameraController.WallEController;
                if (wallEController != null)
                {
                    wallEController.UpdateEnergySystem((float)deltaTime);

                    if (wallEController.EnergySystem.IsEnergyEmpty)
                    {
                        TriggerGameOver();
                    }
                }
            }

            //cubeArrangementModel.AdvanceTime(deltaTime);
            thumbleweedManager.Update((float)deltaTime);
            //cactusManager.Update((float)deltaTime);
            sunManager.Update((float)deltaTime);
            //canManager.Update(deltaTime);

            imGuiController.Update((float)deltaTime);
        }

        private static void TriggerGameOver()
        {
            if (!isGameOver)
            {
                isGameOver = true;
                gameOverMessageShown = false;
            }
        }

        private static void RestartGame()
        {
            isGameOver = false;
            gameOverMessageShown = false;
          
            var wallEController = cameraController.WallEController;
           
            if (wallEController != null)
            {
                wallEController.ResetToTerrainCenter();
                wallEController.RestartGame();
            }

        }

        private static unsafe void Window_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            RenderSkybox();
            Gl.UseProgram(program);


            SetUniform3(LightColorVariableName, new Vector3(red, green, blue));
            SetUniform3(LightPositionVariableName, new Vector3(lightX, lightY, lightZ));
            SetUniform3(ViewPositionVariableName, new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));

            SetUniform1(ShinenessVariableName, shininess);
            SetUniform1(AmbientVariableName, ambient);
            SetUniform1(DiffuseVariableName, diffuse);
            SetUniform1(SpecularVariableName, specular);

            SetViewMatrix();
            SetProjectionMatrix();

            Gl.Disable(EnableCap.Blend);
            DrawWallEObject();

            thumbleweedManager.Render(shininess, ambient, diffuse, specular,
                                    new Vector3(red, green, blue),
                                    new Vector3(lightX, lightY, lightZ),
                                    new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));

            cactusManager.Render(shininess, ambient, diffuse, specular,
                       new Vector3(red, green, blue),
                       new Vector3(lightX, lightY, lightZ),
                       new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));

            sunManager.Render(shininess, ambient, diffuse, specular,
                       new Vector3(red, green, blue),
                       new Vector3(lightX, lightY, lightZ),
                       new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));

            canManager.Render(shininess, ambient, diffuse, specular,
                     new Vector3(red, green, blue),
                     new Vector3(lightX, lightY, lightZ),
                     new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));


            if (showHitboxes)
            {
                RenderHitboxes();
            }

            Gl.Enable(EnableCap.Blend);

            // UI
            RenderUI();
            imGuiController.Render();
        }
        
        // ------------------  UI ------------------------
        private static void RenderUI()
        {

            RenderEnergyBar();
            RenderScoreDisplay();

            // Game Over screen
            if (isGameOver)
            {
                RenderGameOverScreen();
            }

            // Camera mode info
            ImGui.Begin("Control Panel", ImGuiWindowFlags.AlwaysAutoResize);
            ImGuiNET.ImGui.Text("Camera Mode info");
            ImGuiNET.ImGui.Text($"Current Mode: {cameraController.CurrentMode}");
            ImGuiNET.ImGui.Text(cameraController.GetCurrentModeDescription());
            ImGuiNET.ImGui.Text("Press TAB to switch camera modes");
            ImGuiNET.ImGui.Text("Press H to show hitboxes");
            ImGuiNET.ImGui.Separator();

         

            // Lightning controls
            ImGuiNET.ImGui.Text("Ligthning Settings");
            ImGuiNET.ImGui.SliderFloat("Shininess", ref shininess, 5, 100);
            ImGuiNET.ImGui.SliderFloat("Ambient", ref ambient, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Diffuse", ref diffuse, 0, 1);
            ImGuiNET.ImGui.SliderFloat("Specular", ref specular, 0, 1);
      
            ImGuiNET.ImGui.Separator();

            ImGuiNET.ImGui.Text("Render Settings");
            ImGuiNET.ImGui.InputFloat("Render Distance", ref renderDistanceInput);
            if (ImGuiNET.ImGui.Button("Apply Render Distance"))
            {
                renderDistance = Math.Max(50f, Math.Min(1000f, renderDistanceInput));
                Console.WriteLine($"Render distance set to: {renderDistance}");
            }
            ImGuiNET.ImGui.Text($"Current Render Distance: {renderDistance:F0}");
            ImGuiNET.ImGui.End();
        }

        private static void RenderGameOverScreen()
        {
            if (!gameOverMessageShown)
            {
                gameOverMessageShown = true;
            }

            ImGuiNET.ImGui.SetNextWindowPos(System.Numerics.Vector2.Zero);
            ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(1024, 768));

            ImGuiNET.ImGui.Begin("Game Over",
                ImGuiNET.ImGuiWindowFlags.NoDecoration |
                ImGuiNET.ImGuiWindowFlags.NoMove |
                ImGuiNET.ImGuiWindowFlags.NoResize);

            var drawList = ImGuiNET.ImGui.GetWindowDrawList();
            drawList.AddRectFilled(
                System.Numerics.Vector2.Zero,
                new System.Numerics.Vector2(1024, 768),
                ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 0, 0, 0.8f))
            );

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(1024 / 2 - 150, 768 / 2 - 100));

            ImGuiNET.ImGui.PushStyleColor(ImGuiNET.ImGuiCol.Text, new System.Numerics.Vector4(1, 0, 0, 1));
            ImGuiNET.ImGui.Text("GAME OVER!");
            ImGuiNET.ImGui.PopStyleColor();

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(1024 / 2 - 100, 768 / 2 - 50));
            ImGuiNET.ImGui.Text("Wall-E ran out of energy!");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(1024 / 2 - 80, 768 / 2 - 20));
            ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), $"Final Score: {canManager.Score} points");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(1024 / 2 - 100, 768 / 2 + 10));
            ImGuiNET.ImGui.Text($"Cans collected: {canManager.Score / 10}");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(1024 / 2 - 80, 768 / 2 + 40));
            ImGuiNET.ImGui.Text("Press R to restart");

            ImGuiNET.ImGui.End();
        }

        private static void RenderScoreDisplay()
        {
            ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(1024 - 250, 10));
            ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(240, 110));
            ImGuiNET.ImGui.Begin("Score", ImGuiNET.ImGuiWindowFlags.NoDecoration | ImGuiNET.ImGuiWindowFlags.NoBackground);

            var drawList = ImGuiNET.ImGui.GetWindowDrawList();
            var pos = ImGuiNET.ImGui.GetWindowPos();

            drawList.AddRectFilled(
                new System.Numerics.Vector2(pos.X, pos.Y),
                new System.Numerics.Vector2(pos.X + 240, pos.Y + 110),
                ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.0f, 0.0f, 0.0f, 0.7f))
            );

            drawList.AddRect(
                new System.Numerics.Vector2(pos.X, pos.Y),
                new System.Numerics.Vector2(pos.X + 240, pos.Y + 110),
                ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
                0f, ImGuiNET.ImDrawFlags.None, 2f
            );

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(10, 10));
            ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "SCORE");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(10, 35));
            ImGuiNET.ImGui.Text($"{canManager.Score} points");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(10, 55));
            ImGuiNET.ImGui.Text($"Cans left: {canManager.GetRemainingCount()}");

            ImGuiNET.ImGui.SetCursorPos(new System.Numerics.Vector2(10, 75));
            ImGuiNET.ImGui.Text($"Suns left: {sunManager.GetRemainingCount()}");


            ImGuiNET.ImGui.End();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Tab:
                    cameraController.ToggleCameraMode();
                    break;
                case Key.Space:
                    thumbleweedManager.ToggleAnimation();
                    break;
                case Key.P:
                    cameraController.PrintTerrainDebugInfo();
                    break;
                case Key.H:
                    showHitboxes = !showHitboxes;
                    break;
                case Key.R: 
                    if (isGameOver)
                    {
                        RestartGame();
                    }
                    break;
                default:
                    if (IsMovementKey(key))
                    {
                        pressedKeys.Add(key);
                    }
                    break;
            }
        }

        private static void Keyboard_KeyUp(IKeyboard keyboard, Key key, int arg3)
        {
            pressedKeys.Remove(key);
        }

        private static bool IsMovementKey(Key key)
        {
            return key == Key.W || key == Key.A || key == Key.S || key == Key.D ||
                   key == Key.Q || key == Key.E ||
                   key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down;
        }


        // ---------------------------- RENDER OBJECTS ----------------------

        private static unsafe void DrawWallEObject()
        {
            Gl.Disable(EnableCap.CullFace);

            // Wall-E poz & rot
            Vector3D<float> pos = cameraController.WallEPosition;
            float yawRad = (cameraController.WallERotationY - 90) * (float)Math.PI / 180f;

            var scaleMat = Matrix4X4.CreateScale(wallEScale);
            var baseRotX = Matrix4X4.CreateRotationX(-MathF.PI / 2);
            var rotY = Matrix4X4.CreateRotationY(yawRad);
            var translation = Matrix4X4.CreateTranslation(pos.X, pos.Y, pos.Z);

            var modelMatrix = scaleMat * baseRotX * rotY * translation;

            SetModelMatrix(modelMatrix);

            foreach (var part in wallEParts)
            {
                var textured = part as ColladaResourceReader.TexturedGlObject;
                ResetAllTextureStates();

                if (textured != null)
                {
                    if (textured.AlbedoTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture0);
                        Gl.BindTexture(TextureTarget.Texture2D, textured.AlbedoTextureId.Value);
                        SetUniformTexture(program, UseTextureVariableName, TextureVariableName, 0, true);
                    }
                    if (textured.AOTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture1);
                        Gl.BindTexture(TextureTarget.Texture2D, textured.AOTextureId.Value);
                        SetUniformTexture(program, "uUseAO", "uAOTexture", 1, true);
                    }
                    if (textured.MetallicTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture2);
                        Gl.BindTexture(TextureTarget.Texture2D, textured.MetallicTextureId.Value);
                        SetUniformTexture(program, "uUseMetallic", "uMetallicTexture", 2, true);
                    }
                    if (textured.NormalTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture3);
                        Gl.BindTexture(TextureTarget.Texture2D, textured.NormalTextureId.Value);
                        SetUniformTexture(program, "uUseNormal", "uNormalTexture", 3, true);
                    }
                    if (textured.OpacityTextureId.HasValue)
                    {
                        Gl.ActiveTexture(TextureUnit.Texture4);
                        Gl.BindTexture(TextureTarget.Texture2D, textured.OpacityTextureId.Value);
                        SetUniformTexture(program, "uUseOpacity", "uOpacityTexture", 4, true);
                    }
                }

                Gl.BindVertexArray(part.Vao);
                Gl.DrawElements(GLEnum.Triangles, part.IndexArrayLength, GLEnum.UnsignedInt, null);
                Gl.BindVertexArray(0);
                UnbindAllTextures();
            }

            Gl.Enable(EnableCap.CullFace);
            RenderDesert();
        }


        private static unsafe void RenderDesert()
        {
            Gl.UseProgram(objProgram);

            SetUniformForObjShader(objProgram, "uShininess", shininess);
            SetUniformForObjShader(objProgram, "uAmbientStrength", ambient);
            SetUniformForObjShader(objProgram, "uDiffuseStrength", diffuse);
            SetUniformForObjShader(objProgram, "uSpecularStrength", specular);

            SetUniform3ForObjShader(objProgram, "uLightColor", new Vector3(red, green, blue));
            SetUniform3ForObjShader(objProgram, "uLightPos", new Vector3(lightX, lightY, lightZ));
            SetUniform3ForObjShader(objProgram, "uViewPos", new Vector3(cameraController.Position.X, cameraController.Position.Y, cameraController.Position.Z));

            SetViewMatrixForObjShader();
            SetProjectionMatrixForObjShader();

            var desertMatrix = Matrix4X4.CreateScale(50.0f) * Matrix4X4.CreateTranslation(0f, -5.0f, 0f);
            SetModelMatrixForObjShader(desertMatrix);

            ResetObjTextureStates();

            if (desertTextured.AOTextureId.HasValue)
            {
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, desertTextured.AOTextureId.Value);
                int useAOLocation = Gl.GetUniformLocation(objProgram, "uUseAO");
                if (useAOLocation != -1) Gl.Uniform1(useAOLocation, 1);
                int aoTexLocation = Gl.GetUniformLocation(objProgram, "uAOTexture");
                if (aoTexLocation != -1) Gl.Uniform1(aoTexLocation, 0);
            }

            if (desertTextured.NormalTextureId.HasValue)
            {
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2D, desertTextured.NormalTextureId.Value);
                int useNormalLocation = Gl.GetUniformLocation(objProgram, "uUseNormal");
                if (useNormalLocation != -1) Gl.Uniform1(useNormalLocation, 1);
                int normalTexLocation = Gl.GetUniformLocation(objProgram, "uNormalTexture");
                if (normalTexLocation != -1) Gl.Uniform1(normalTexLocation, 1);
            }

            Gl.Disable(EnableCap.CullFace);
            Gl.BindVertexArray(desertTextured.Vao);
            Gl.DrawElements(GLEnum.Triangles, desertTextured.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);
            Gl.Enable(EnableCap.CullFace);

            UnbindObjTextures();
            Gl.UseProgram(program);
        }

        private static unsafe void RenderSkybox()
        {
            var view = Matrix4X4.CreateLookAt(cameraController.Position, cameraController.Target, cameraController.UpVector);
            var skyboxView = new Matrix4X4<float>(
                view.M11, view.M12, view.M13, 0,
                view.M21, view.M22, view.M23, 0,
                view.M31, view.M32, view.M33, 0,
                0, 0, 0, 1);

            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, renderDistance);

            Gl.UseProgram(skyboxProgram);

            int viewLoc = Gl.GetUniformLocation(skyboxProgram, "view");
            int projLoc = Gl.GetUniformLocation(skyboxProgram, "projection");
            int skyboxLoc = Gl.GetUniformLocation(skyboxProgram, "skybox");

            if (viewLoc != -1) Gl.UniformMatrix4(viewLoc, 1, false, (float*)&skyboxView);
            if (projLoc != -1) Gl.UniformMatrix4(projLoc, 1, false, (float*)&projectionMatrix);
            if (skyboxLoc != -1) Gl.Uniform1(skyboxLoc, 0);

            skybox.Render(skyboxProgram);
        }

        private static void RenderEnergyBar()
        {
            var wallEController = cameraController.WallEController;
            if (wallEController == null) return;

            var energy = wallEController.EnergySystem;

            ImGuiNET.ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10));
            ImGuiNET.ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 80));
            ImGuiNET.ImGui.Begin("Energy", ImGuiNET.ImGuiWindowFlags.NoDecoration | ImGuiNET.ImGuiWindowFlags.NoBackground);

            var drawList = ImGuiNET.ImGui.GetWindowDrawList();
            var pos = ImGuiNET.ImGui.GetWindowPos();

            float barWidth = 280f;
            float barHeight = 20f;
            float barX = pos.X + 10;
            float barY = pos.Y + 30;

            drawList.AddRectFilled(
                new System.Numerics.Vector2(barX, barY),
                new System.Numerics.Vector2(barX + barWidth, barY + barHeight),
                ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f))
            );

            float energyWidth = barWidth * (energy.EnergyPercentage / 100.0f);
            var (r, g, b) = energy.GetEnergyBarColor();

            if (energyWidth > 0)
            {
                drawList.AddRectFilled(
                    new System.Numerics.Vector2(barX, barY),
                    new System.Numerics.Vector2(barX + energyWidth, barY + barHeight),
                    ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r, g, b, 1.0f))
                );
            }

            drawList.AddRect(
                new System.Numerics.Vector2(barX, barY),
                new System.Numerics.Vector2(barX + barWidth, barY + barHeight),
                ImGuiNET.ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
                0f, ImGuiNET.ImDrawFlags.None, 2f
            );

            ImGuiNET.ImGui.Text("ENERGY");
            ImGuiNET.ImGui.SetCursorPosY(ImGuiNET.ImGui.GetCursorPosY() + 25);
            ImGuiNET.ImGui.Text($"{energy.CurrentEnergy:F0}/{energy.MaxEnergy:F0} ({energy.EnergyPercentage:F0}%)");

            if (energy.IsEnergyLow)
            {
                ImGuiNET.ImGui.TextColored(new System.Numerics.Vector4(1, 0, 0, 1), "LOW ENERGY - Please Find sunlight!");
            }

            ImGuiNET.ImGui.End();
        }

        private static void RenderHitboxes()
        {
            var cameraPos = cameraController.Position;
            var cameraTarget = cameraController.Target;
            var upVector = cameraController.UpVector;
            float aspectRatio = 1024f / 768f;

            // cactus - green
            var cactusCollisionData = cactusManager.GetCollisionData();
            if (cactusCollisionData.Count > 0)
            {
                var cactusPositions = cactusManager.GetCactusPositions();
                hitboxRenderer.RenderDynamicHitboxes(cactusCollisionData, cactusPositions,
                                                   cameraPos, cameraTarget, upVector,
                                                   aspectRatio, renderDistance,
                                                   new Vector3(0.0f, 1.0f, 0.0f));
            }

            // thumbleweed - yellow
            var thumbleweedCollisions = thumbleweedManager.GetCollisionData();
            if (thumbleweedCollisions.Count > 0)
            {
                var thumbleweedPositions = thumbleweedManager.GetThumbleweedPositions();
                hitboxRenderer.RenderDynamicHitboxes(thumbleweedCollisions, thumbleweedPositions,
                                                   cameraPos, cameraTarget, upVector,
                                                   aspectRatio, renderDistance,
                                                   new Vector3(1.0f, 1.0f, 0.0f));
            }

            // suns - orange
            var sunCollisions = sunManager.GetCollisionData();
            if (sunCollisions.Count > 0)
            {
                var sunPositions = sunManager.GetSunPositions();
                hitboxRenderer.RenderDynamicHitboxes(sunCollisions, sunPositions,
                                                   cameraPos, cameraTarget, upVector,
                                                   aspectRatio, renderDistance,
                                                   new Vector3(1.0f, 0.5f, 0.0f));
            }

            // cans - red
            var canCollisions = canManager.GetCollisionData();
            if (canCollisions.Count > 0)
            {
                var canPositions = canManager.GetCanPositions();
                hitboxRenderer.RenderDynamicHitboxes(canCollisions, canPositions,
                                                   cameraPos, cameraTarget, upVector,
                                                   aspectRatio, renderDistance,
                                                   new Vector3(1.0f, 0.0f, 0.0f));
            }
            // WALL-E - blue
            hitboxRenderer.RenderWallEHitbox(cameraController.WallEPosition, 1.5f,
                                           cameraPos, cameraTarget, upVector,
                                           aspectRatio, renderDistance,
                                           new Vector3(0.0f, 0.0f, 1.0f));
        }


        private static unsafe void SetUpObjects()
        {
            float[] faceColor = new float[] { 1.0f, 0.5f, 0.3f, 1.0f };
            wallEParts = ColladaResourceReader.CreateMultipleObjectsFromResource(Gl, "model.dae", faceColor);
            Console.WriteLine($"Loaded {wallEParts.Count} parts of Wall-E");

            float[] desertColor = new float[] { 0.596f, 0.412f, 0.314f, 1.0f };
            desertTextured = ObjResourceReader.CreateObjectFromResourceWithTextures(Gl, "terrain.desert.obj", desertColor);
            Console.WriteLine("Desert terrain with textures loaded successfully!");

            SetupTerrainHeightCalculator();

            skybox = new Skybox(Gl);
            LinkSkyboxProgram();
            LinkObjProgram();
            thumbleweedManager.Initialize(Gl, objProgram, terrainCalculator);
            cactusManager.Initialize(Gl, objProgram, terrainCalculator);
            sunManager.Initialize(Gl, objProgram, terrainCalculator);
            canManager.Initialize(Gl, program, terrainCalculator);

            hitboxRenderer.Initialize(Gl, objProgram);

            cameraController.SetCollisionManagers(cactusManager, thumbleweedManager, sunManager, canManager);

        }

        private static void SetupTerrainHeightCalculator()
        {
            try
            {
                List<float[]> objVertices;
                List<int[]> objFaces;
                List<float[]> objNormals;
                List<float[]> objTexCoords;

                ObjResourceReader.ReadObjDataFromResource("terrain.desert.obj",
                   out objVertices, out objFaces, out objNormals, out objTexCoords);

                float terrainScale = 50.0f;
                var terrainOffset = new Vector3D<float>(0f, -5.0f, 0f);

                terrainCalculator = new TerrainHeightCalculator(objVertices, objFaces, terrainScale, terrainOffset);

                cameraController.InitializeTerrain(terrainCalculator);

                Console.WriteLine("Terrain height calculator setup completed!");

                terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);
                Console.WriteLine($"Terrain bounds: X({minX:F1} to {maxX:F1}), Z({minZ:F1} to {maxZ:F1})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up terrain calculator: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // --------------------- PROGRAM LINKING --------------------
        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string vError = Gl.GetShaderInfoLog(vshader);
                Console.WriteLine($"VERTEX SHADER ERROR: {vError}");
                throw new Exception("Vertex shader failed to compile: " + vError);
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.FragmentShader.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string fError = Gl.GetShaderInfoLog(fshader);
                Console.WriteLine($"FRAGMENT SHADER ERROR: {fError}");
                throw new Exception("Fragment shader failed to compile: " + fError);
            }

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                string linkError = Gl.GetProgramInfoLog(program);
                Console.WriteLine($"SHADER LINK ERROR: {linkError}");
            }

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void LinkSkyboxProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.SkyboxVertex.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string vError = Gl.GetShaderInfoLog(vshader);
                Console.WriteLine($"SKYBOX VERTEX SHADER ERROR: {vError}");
                throw new Exception("Skybox vertex shader failed to compile: " + vError);
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.SkyboxFragment.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string fError = Gl.GetShaderInfoLog(fshader);
                Console.WriteLine($"SKYBOX FRAGMENT SHADER ERROR: {fError}");
                throw new Exception("Skybox fragment shader failed to compile: " + fError);
            }

            skyboxProgram = Gl.CreateProgram();
            Gl.AttachShader(skyboxProgram, vshader);
            Gl.AttachShader(skyboxProgram, fshader);
            Gl.LinkProgram(skyboxProgram);
            Gl.GetProgram(skyboxProgram, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                string linkError = Gl.GetProgramInfoLog(skyboxProgram);
                Console.WriteLine($"SKYBOX SHADER LINK ERROR: {linkError}");
            }

            Gl.DetachShader(skyboxProgram, vshader);
            Gl.DetachShader(skyboxProgram, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void LinkObjProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, GetEmbeddedResourceAsString("Shaders.ObjVertex.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                string vError = Gl.GetShaderInfoLog(vshader);
                Console.WriteLine($"OBJ VERTEX SHADER ERROR: {vError}");
                throw new Exception("OBJ vertex shader failed to compile: " + vError);
            }

            Gl.ShaderSource(fshader, GetEmbeddedResourceAsString("Shaders.ObjFragment.frag"));
            Gl.CompileShader(fshader);
            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                string fError = Gl.GetShaderInfoLog(fshader);
                Console.WriteLine($"OBJ FRAGMENT SHADER ERROR: {fError}");
                throw new Exception("OBJ fragment shader failed to compile: " + fError);
            }

            objProgram = Gl.CreateProgram();
            Gl.AttachShader(objProgram, vshader);
            Gl.AttachShader(objProgram, fshader);
            Gl.LinkProgram(objProgram);
            Gl.GetProgram(objProgram, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                string linkError = Gl.GetProgramInfoLog(objProgram);
                Console.WriteLine($"OBJ SHADER LINK ERROR: {linkError}");
                throw new Exception("OBJ shader program failed to link: " + linkError);
            }

            Gl.DetachShader(objProgram, vshader);
            Gl.DetachShader(objProgram, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        // ------------------------------  UNIFORMS --------------------------------
        private static unsafe void SetUniform1(string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }
            Gl.Uniform1(location, uniformValue);
            CheckError();
        }

        private static unsafe void SetUniform3(string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(program, uniformName);
            if (location == -1)
            {
                throw new Exception($"{uniformName} uniform not found on shader.");
            }
            Gl.Uniform3(location, uniformValue);
            CheckError();
        }

        private static void SetUniformTexture(uint program, string useFlag, string texName, int unit, bool use)
        {
            int useLocation = Gl.GetUniformLocation(program, useFlag);
            if (useLocation != -1) Gl.Uniform1(useLocation, use ? 1 : 0);

            int texLocation = Gl.GetUniformLocation(program, texName);
            if (texLocation != -1) Gl.Uniform1(texLocation, unit);
        }

        // Helper methods for OBJ shader
        private static unsafe void SetUniformForObjShader(uint shaderProgram, string uniformName, float uniformValue)
        {
            int location = Gl.GetUniformLocation(shaderProgram, uniformName);
            if (location != -1)
            {
                Gl.Uniform1(location, uniformValue);
            }
        }

        private static unsafe void SetUniform3ForObjShader(uint shaderProgram, string uniformName, Vector3 uniformValue)
        {
            int location = Gl.GetUniformLocation(shaderProgram, uniformName);
            if (location != -1)
            {
                Gl.Uniform3(location, uniformValue);
            }
        }

        // ---------------------- SET MATRIX FUNCTIONS ---------------------------
        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraController.Position, cameraController.Target, cameraController.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, renderDistance);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetModelMatrixForObjShader(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(objProgram, ModelMatrixVariableName);
            if (location != -1)
            {
                Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            }

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(objProgram, NormalMatrixVariableName);
            if (location != -1)
            {
                Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            }
        }

        private static unsafe void SetViewMatrixForObjShader()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraController.Position, cameraController.Target, cameraController.UpVector);
            int location = Gl.GetUniformLocation(objProgram, ViewMatrixVariableName);
            if (location != -1)
            {
                Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            }
        }

        private static unsafe void SetProjectionMatrixForObjShader()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 500);
            int location = Gl.GetUniformLocation(objProgram, ProjectionMatrixVariableName);
            if (location != -1)
            {
                Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            }
        }

        private static void ResetObjTextureStates()
        {
            int useAOLocation = Gl.GetUniformLocation(objProgram, "uUseAO");
            int useNormalLocation = Gl.GetUniformLocation(objProgram, "uUseNormal");

            if (useAOLocation != -1) Gl.Uniform1(useAOLocation, 0);
            if (useNormalLocation != -1) Gl.Uniform1(useNormalLocation, 0);
        }

        private static void UnbindObjTextures()
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
        }

        private static void ResetAllTextureStates()
        {
            int useTexLocation = Gl.GetUniformLocation(program, UseTextureVariableName);
            int useAOLocation = Gl.GetUniformLocation(program, "uUseAO");
            int useMetallicLocation = Gl.GetUniformLocation(program, "uUseMetallic");
            int useNormalLocation = Gl.GetUniformLocation(program, "uUseNormal");
            int useOpacityLocation = Gl.GetUniformLocation(program, "uUseOpacity");
            int useRoughnessLocation = Gl.GetUniformLocation(program, "uUseRoughness");

            if (useTexLocation != -1) Gl.Uniform1(useTexLocation, 0);
            if (useAOLocation != -1) Gl.Uniform1(useAOLocation, 0);
            if (useMetallicLocation != -1) Gl.Uniform1(useMetallicLocation, 0);
            if (useNormalLocation != -1) Gl.Uniform1(useNormalLocation, 0);
            if (useOpacityLocation != -1) Gl.Uniform1(useOpacityLocation, 0);
            if (useRoughnessLocation != -1) Gl.Uniform1(useRoughnessLocation, 0);
        }

        private static void UnbindAllTextures()
        {
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture3);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture4);
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
        }

        private static void Window_Closing()
        {
            foreach (var part in wallEParts)
            {
                if (part is ColladaResourceReader.TexturedGlObject texturedObject)
                {
                    if (texturedObject.AlbedoTextureId.HasValue)
                        Gl.DeleteTexture(texturedObject.AlbedoTextureId.Value);
                    if (texturedObject.AOTextureId.HasValue)
                        Gl.DeleteTexture(texturedObject.AOTextureId.Value);
                    if (texturedObject.NormalTextureId.HasValue)
                        Gl.DeleteTexture(texturedObject.NormalTextureId.Value);
                    if (texturedObject.MetallicTextureId.HasValue)
                        Gl.DeleteTexture(texturedObject.MetallicTextureId.Value);
                    if (texturedObject.RoughnessTextureId.HasValue)
                        Gl.DeleteTexture(texturedObject.RoughnessTextureId.Value);
                }
                part.ReleaseGlObject();
            }
            desertTextured?.ReleaseGlObject();
            thumbleweedManager.Dispose();
            cactusManager.Dispose();
            sunManager.Dispose();
            canManager.Dispose();
            hitboxRenderer.Dispose();
            skybox?.Dispose();
            if (skyboxProgram != 0) Gl.DeleteProgram(skyboxProgram);
            if (objProgram != 0) Gl.DeleteProgram(objProgram);
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }

        private static string GetEmbeddedResourceAsString(string resourceRelativePath)
        {
            string resourceFullPath = Assembly.GetExecutingAssembly().GetName().Name + "." + resourceRelativePath;
            using (var resStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceFullPath))
            using (var resStreamReader = new StreamReader(resStream))
            {
                return resStreamReader.ReadToEnd();
            }
        }
    }
}