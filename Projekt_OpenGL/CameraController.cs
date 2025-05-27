using Silk.NET.Input;
using Silk.NET.Maths;
using System;

namespace Projekt_OpenGL
{
    public enum CameraMode
    {
        WallEFirstPerson,
        WallEThirdPerson,
        Free
    }

    public class CameraController
    {
        private WallEController wallEController;
        private CameraMode currentMode = CameraMode.WallEThirdPerson;


        private Vector3D<float> freeCameraPosition = new Vector3D<float>(0, 10, 20);
        private float freeCameraYaw = -90f;
        private float freeCameraPitch = -15f;
        private const float FreeCameraSpeed = 15f;
        private const float FreeCameraSensitivity = 80f;

  
        private float firstPersonPitch = 0f;
        private const float FirstPersonSensitivity = 80f;
        private const float MaxPitch = 60f;

        public CameraMode CurrentMode => currentMode;

        public CameraController()
        {
           
            wallEController = new WallEController();
        }

        public void InitializeTerrain(TerrainHeightCalculator terrainCalculator)
        {
                  wallEController.InitializeTerrain(terrainCalculator);
        }

        public void ToggleCameraMode()
        {
            currentMode = (CameraMode)(((int)currentMode + 1) % 3);

            if (currentMode == CameraMode.WallEFirstPerson)
            {
                firstPersonPitch = 0f; 
            }
            else if (currentMode == CameraMode.Free)
            {
     
                var wallEPos = wallEController.Position;
                freeCameraPosition = new Vector3D<float>(wallEPos.X, wallEPos.Y +5f, wallEPos.Z -15f);


                freeCameraYaw = 0f;   
                freeCameraPitch = -15f; 
            }

            Console.WriteLine($"Camera mode switched to: {currentMode}");
        }

        public void HandleMovement(Key key, float deltaTime)
        {
            switch (currentMode)
            {
                case CameraMode.Free:
                    HandleFreeCameraMovement(key, deltaTime);
                    break;

                case CameraMode.WallEFirstPerson:
                case CameraMode.WallEThirdPerson:
                    HandleWallEMovement(key, deltaTime);
                    break;
            }
        }

        public void HandleCameraRotation(Key key, float deltaTime)
        {
            switch (currentMode)
            {
                case CameraMode.Free:
                    HandleFreeCameraRotation(key, deltaTime);
                    break;

                case CameraMode.WallEFirstPerson:
                    HandleFirstPersonRotation(key, deltaTime);
                    break;

                case CameraMode.WallEThirdPerson:
                     HandleWallERotation(key, deltaTime);
                    break;
            }
        }

        private void HandleFreeCameraMovement(Key key, float deltaTime)
        {
            float yawRad = freeCameraYaw * (float)Math.PI / 180f;
            float pitchRad = freeCameraPitch * (float)Math.PI / 180f;

            var forward = new Vector3D<float>(
                MathF.Cos(yawRad) * MathF.Cos(pitchRad),
                MathF.Sin(pitchRad),
                MathF.Sin(yawRad) * MathF.Cos(pitchRad)
            );
            var right = Vector3D.Cross(forward, Vector3D<float>.UnitY);
            float length = MathF.Sqrt(right.X * right.X + right.Y * right.Y + right.Z * right.Z);
            if (length > 0)
                right = new Vector3D<float>(right.X / length, right.Y / length, right.Z / length);

            var up = Vector3D<float>.UnitY;

            float speed = FreeCameraSpeed * deltaTime;

            switch (key)
            {
                case Key.W:
                    freeCameraPosition += forward * speed;
                    break;
                case Key.S:
                    freeCameraPosition -= forward * speed;
                    break;
                case Key.A:
                    freeCameraPosition -= right * speed;
                    break;
                case Key.D:
                    freeCameraPosition += right * speed;
                    break;
                case Key.R:
                    freeCameraPosition += up * speed;
                    break;
                case Key.F:
                    freeCameraPosition -= up * speed;
                    break;
            }
        }

        private void HandleFreeCameraRotation(Key key, float deltaTime)
        {
            float sensitivity = FreeCameraSensitivity * deltaTime;

            switch (key)
            {
                case Key.Left:
                    freeCameraYaw -= sensitivity;
                    break;
                case Key.Right:
                    freeCameraYaw += sensitivity;
                    break;
                case Key.Up:
                    freeCameraPitch += sensitivity;
                    break;
                case Key.Down:
                    freeCameraPitch -= sensitivity;
                    break;
            }

            freeCameraPitch = Math.Clamp(freeCameraPitch, -89f, 89f);
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

        private void HandleWallERotation(Key key, float deltaTime)
        {
            switch (key)
            {
                case Key.Left:
                    wallEController.RotateLeft(deltaTime);
                    break;
                case Key.Right:
                    wallEController.RotateRight(deltaTime);
                    break;
            }
        }

        private void HandleFirstPersonRotation(Key key, float deltaTime)
        {
            float sensitivity = FirstPersonSensitivity * deltaTime;

            switch (key)
            {
                case Key.Left:

                    wallEController.RotateLeft(deltaTime);
                    break;
                case Key.Right:
                    wallEController.RotateRight(deltaTime);
                    break;
                case Key.Up:

                    firstPersonPitch += sensitivity;
                    firstPersonPitch = Math.Clamp(firstPersonPitch, -MaxPitch, MaxPitch);
                    break;
                case Key.Down:
                    firstPersonPitch -= sensitivity;
                    firstPersonPitch = Math.Clamp(firstPersonPitch, -MaxPitch, MaxPitch);
                    break;
            }
        }

        public Vector3D<float> Position
        {
            get
            {
                return currentMode switch
                {
                    CameraMode.Free => freeCameraPosition,
                    CameraMode.WallEThirdPerson => wallEController.GetThirdPersonCameraPosition(),
                    CameraMode.WallEFirstPerson => wallEController.GetFirstPersonCameraPosition(),
                    _ => freeCameraPosition
                };
            }
        }

        public Vector3D<float> Target
        {
            get
            {
                return currentMode switch
                {
                    CameraMode.Free => freeCameraPosition + new Vector3D<float>(
                        MathF.Cos(freeCameraYaw * (float)Math.PI / 180f) * MathF.Cos(freeCameraPitch * (float)Math.PI / 180f),
                        MathF.Sin(freeCameraPitch * (float)Math.PI / 180f),
                        MathF.Sin(freeCameraYaw * (float)Math.PI / 180f) * MathF.Cos(freeCameraPitch * (float)Math.PI / 180f)
                    ),
                    CameraMode.WallEThirdPerson => wallEController.GetCameraTarget(),
                    CameraMode.WallEFirstPerson => GetFirstPersonTarget(),
                    _ => freeCameraPosition + Vector3D<float>.UnitZ
                };
            }
        }

        private Vector3D<float> GetFirstPersonTarget()
        {
            var wallEPos = wallEController.Position;


            float wallEYawRad = wallEController.RotationY * (float)Math.PI / 180f;
            float pitchRad = firstPersonPitch * (float)Math.PI / 180f;

            var targetDirection = new Vector3D<float>(
                (float)Math.Sin(wallEYawRad) * (float)Math.Cos(pitchRad),
                (float)Math.Sin(pitchRad),
                (float)Math.Cos(wallEYawRad) * (float)Math.Cos(pitchRad)
            );

            return wallEPos + new Vector3D<float>(0, 1.5f, 0) + targetDirection * 10f;
        }

        public Vector3D<float> UpVector => Vector3D<float>.UnitY;


        public Vector3D<float> WallEPosition => wallEController.Position;
        public float WallERotationY => wallEController.RotationY;

        public string GetCurrentModeDescription()
        {
            return currentMode switch
            {
                CameraMode.Free => "Free Camera - WASD: move, Arrow keys: look, R/F: up/down",
                CameraMode.WallEThirdPerson => "Third Person - WASD: move Wall-E, Arrow keys: rotate Wall-E",
                CameraMode.WallEFirstPerson => "First Person - WASD: move Wall-E, Left/Right: turn, Up/Down: look",
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
    }
}