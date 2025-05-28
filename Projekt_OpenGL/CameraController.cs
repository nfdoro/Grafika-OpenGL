using Silk.NET.Input;
using Silk.NET.Maths;

namespace Projekt_OpenGL
{
    public enum CameraMode
    {
        ThirdPerson,
        FirstPerson,
        Free
    }

    public class CameraController
    {
        private CameraMode currentMode = CameraMode.ThirdPerson;
        private WallEController wallEController;

        private Vector3D<float> freeCameraPosition = new Vector3D<float>(0f, 10f, 20f);
        private Vector3D<float> freeCameraTarget = new Vector3D<float>(0f, 0f, 0f);
        private float freeCameraYaw = 0f;
        private float freeCameraPitch = -20f;
        private const float FreeCameraMoveSpeed = 15f;
        private const float FreeCameraRotationSpeed = 60f;

        public CameraMode CurrentMode => currentMode;
        public WallEController WallEController => wallEController;

        public Vector3D<float> Position
        {
            get
            {
                return currentMode switch
                {
                    CameraMode.ThirdPerson => wallEController?.GetThirdPersonCameraPosition() ?? new Vector3D<float>(0, 10, 20),
                    CameraMode.FirstPerson => wallEController?.GetFirstPersonCameraPosition() ?? new Vector3D<float>(0, 6, 0),
                    CameraMode.Free => freeCameraPosition,
                    _ => new Vector3D<float>(0, 10, 20)
                };
            }
        }

        public Vector3D<float> Target
        {
            get
            {
                return currentMode switch
                {
                    CameraMode.ThirdPerson => wallEController?.GetCameraTarget() ?? new Vector3D<float>(0, 0, 0),
                    CameraMode.FirstPerson => wallEController?.GetFirstPersonCameraTarget() ?? new Vector3D<float>(0, 0,0),
                    CameraMode.Free => freeCameraTarget,
                    _ => new Vector3D<float>(0, 0, 0)
                };
            }
        }

        public Vector3D<float> UpVector => new Vector3D<float>(0f, 1f, 0f);

        public Vector3D<float> WallEPosition => wallEController?.Position ?? new Vector3D<float>(0, 0, 0);
        public float WallERotationY => wallEController?.RotationY ?? 0f;

        public CameraController()
        {
            wallEController = new WallEController();
        }

        public void InitializeTerrain(TerrainHeightCalculator terrainCalculator)
        {
            wallEController.InitializeTerrain(terrainCalculator);
        }

        public void SetCollisionManagers(CactusManager cactusManager, ThumbleweedManager thumbleweedManager, SunManager sunManager, CanManager canManager)
        {
            wallEController.SetCollisionManagers(cactusManager, thumbleweedManager, sunManager,canManager);
        }

        public void HandleMovement(Key key, float deltaTime)
        {
            switch (currentMode)
            {
                case CameraMode.ThirdPerson:
                case CameraMode.FirstPerson:
                    HandleWallEMovement(key, deltaTime);
                    break;
                case CameraMode.Free:
                    HandleFreeCameraMovement(key, deltaTime);
                    HandleFreeCameraRotation(key, deltaTime);   
                    break;
            }
        }

        public void HandleCameraRotation(Key key, float deltaTime)
        {
            if (CurrentMode == CameraMode.FirstPerson)
            {
                if (key == Key.Up)
                    wallEController.AdjustPitch(true);
                else if (key == Key.Down)
                    wallEController.AdjustPitch(false);
            }
            else
            {
                if (key == Key.Left)
                    wallEController.RotateLeft(deltaTime);
                else if (key == Key.Right)
                    wallEController.RotateRight(deltaTime);
            }
        }



        private void HandleWallEMovement(Key key, float deltaTime)
        {
            switch (key)
            {
                case Key.W:
                    wallEController.MoveForward(deltaTime);
                    break;
                case Key.S:
                    wallEController.MoveBackward(deltaTime);
                    break;
                case Key.A:
                    wallEController.RotateLeft(deltaTime);
                    break;
                case Key.D:
                    wallEController.RotateRight(deltaTime);
                    break;
            }
        }

        private void HandleFreeCameraMovement(Key key, float deltaTime)
        {
            float yawRad = freeCameraYaw * (float)Math.PI / 180f;
            float pitchRad = freeCameraPitch * (float)Math.PI / 180f;

            Vector3D<float> forward = new Vector3D<float>(
                (float)Math.Sin(yawRad) * (float)Math.Cos(pitchRad),
                (float)Math.Sin(pitchRad),
                (float)Math.Cos(yawRad) * (float)Math.Cos(pitchRad)
            );

            Vector3D<float> right = Vector3D.Cross(forward, UpVector);
            right = Vector3D.Normalize(right);

            Vector3D<float> up = Vector3D.Cross(right, forward);

            float moveSpeed = FreeCameraMoveSpeed * deltaTime;

            switch (key)
            {
                case Key.W:
                    freeCameraPosition += forward * moveSpeed;
                    break;
                case Key.S:
                    freeCameraPosition -= forward * moveSpeed;
                    break;
                case Key.A:
                    freeCameraPosition -= right * moveSpeed;
                    break;
                case Key.D:
                    freeCameraPosition += right * moveSpeed;
                    break;
                case Key.Q:
                    freeCameraPosition += up * moveSpeed;
                    break;
                case Key.E:
                    freeCameraPosition -= up * moveSpeed;
                    break;
            }

            UpdateFreeCameraTarget();
        }

        public void HandleFreeCameraRotation(Key key, float deltaTime)
        {
            float rotationSpeed = FreeCameraRotationSpeed * deltaTime;

            switch (key)
            {
                case Key.Left:
                    freeCameraYaw -= rotationSpeed;
                    break;
                case Key.Right:
                    freeCameraYaw += rotationSpeed;
                    break;
                case Key.Up:
                    freeCameraPitch += rotationSpeed;
                    freeCameraPitch = Math.Clamp(freeCameraPitch, -89f, 89f);
                    break;
                case Key.Down:
                    freeCameraPitch -= rotationSpeed;
                    freeCameraPitch = Math.Clamp(freeCameraPitch, -89f, 89f);
                    break;
            }

            // Normalize yaw
            while (freeCameraYaw < 0f) freeCameraYaw += 360f;
            while (freeCameraYaw >= 360f) freeCameraYaw -= 360f;

            UpdateFreeCameraTarget();
        }

        private void UpdateFreeCameraTarget()
        {
            float yawRad = freeCameraYaw * (float)Math.PI / 180f;
            float pitchRad = freeCameraPitch * (float)Math.PI / 180f;

            Vector3D<float> direction = new Vector3D<float>(
                (float)Math.Sin(yawRad) * (float)Math.Cos(pitchRad),
                (float)Math.Sin(pitchRad),
                (float)Math.Cos(yawRad) * (float)Math.Cos(pitchRad)
            );

            freeCameraTarget = freeCameraPosition + direction;
        }

        public void ToggleCameraMode()
        {
            currentMode = currentMode switch
            {
                CameraMode.ThirdPerson => CameraMode.FirstPerson,
                CameraMode.FirstPerson => CameraMode.Free,
                CameraMode.Free => CameraMode.ThirdPerson,
                _ => CameraMode.ThirdPerson
            };

            Console.WriteLine($"Camera mode switched to: {currentMode}");
        }

        public string GetCurrentModeDescription()
        {
            return currentMode switch
            {
                CameraMode.ThirdPerson => "WASD: Move Wall-E, Arrow Keys: N/A",
                CameraMode.FirstPerson => "WASD: Move Wall-E, Arrow Keys: N/A",
                CameraMode.Free => "WASD/RF: Move Camera, Arrow Keys: Look Around",
                _ => "Unknown mode"
            };
        }

        public void ResetWallEPosition()
        {
            wallEController.ResetToTerrainCenter();
        }

        public void PrintTerrainDebugInfo()
        {
            wallEController.PrintTerrainInfo();
        }

        public void UpdateEnergySystem(float deltaTime)
        {
            wallEController?.UpdateEnergySystem(deltaTime);
        }

        public bool IsGameOver()
        {
            return wallEController?.EnergySystem?.IsEnergyEmpty ?? false;
        }

        public void RestartGame()
        {
            wallEController?.RestartGame();
        }
    }
}