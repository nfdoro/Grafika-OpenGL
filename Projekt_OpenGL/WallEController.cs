using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Projekt_OpenGL
{
    public class WallEController
    {
        private float positionX = 0f;
        private float positionY = 0f;
        private float positionZ = 0f;
        private float rotationY = 0f;

        private const float MoveSpeed = 12.0f;
        private const float RotationSpeed = 90.0f;
        private const float WallEHeightOffset = 0.5f;
        private const float WallERadius = 1.0f;

        private TerrainHeightCalculator terrainCalculator;
        private bool terrainInitialized = false;

        // References to collision managers
        private CactusManager cactusManager;
        private ThumbleweedManager thumbleweedManager;
        private SunManager sunManager; 
        private EnergySystem energySystem;
        private CanManager canManager;

        private Vector3D<float> currentPosition;

        // Movement tracking for energy system
        private bool isCurrentlyMoving = false;


        private float pitch = 0.0f;
        private const float PitchLimit = MathF.PI / 3f;
        private const float PitchStep = MathF.PI / 180f * 2f;


        public Vector3D<float> Position
        {
            get
            {
                UpdateHeightFromTerrain();
                currentPosition = new Vector3D<float>(positionX, positionY, positionZ);
                return currentPosition;
            }
        }

        public float RotationY => rotationY;
        public EnergySystem EnergySystem => energySystem; 

        public WallEController()
        {

            energySystem = new EnergySystem(100.0f);

            energySystem.OnEnergyEmpty += OnEnergyEmpty;
            energySystem.OnEnergyLow += OnEnergyLow;
            energySystem.OnEnergyRecovered += OnEnergyRecovered;
        }

        public void InitializeTerrain(TerrainHeightCalculator calculator)
        {
            terrainCalculator = calculator;
            terrainInitialized = calculator != null;

            if (terrainInitialized)
            {
                terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);
                positionX = (minX + maxX) / 2f;
                positionZ = (minZ + maxZ) / 2f;
                rotationY = 0f;
                UpdateHeightFromTerrain();
                UpdateCurrentPosition();
                Console.WriteLine($"Wall-E initialized at: ({positionX:F1}, {positionY:F1}, {positionZ:F1})");
            }
            else
            {
                positionX = 0f;
                positionZ = 0f;
                positionY = WallEHeightOffset;
                rotationY = 0f;
                UpdateCurrentPosition();
            }
        }

        public void SetCollisionManagers(CactusManager cactusManager, ThumbleweedManager thumbleweedManager, SunManager sunManager, CanManager canManager)
        {
            this.cactusManager = cactusManager;
            this.thumbleweedManager = thumbleweedManager;
            this.sunManager = sunManager; 
            this.canManager = canManager; 
        }

        public void UpdateEnergySystem(float deltaTime)
        {
            if (energySystem != null && sunManager != null)
            {
                bool isNearSun = energySystem.IsNearSun(positionX, positionZ, sunManager);
                energySystem.Update(deltaTime, isCurrentlyMoving, isNearSun);

                energySystem.TryCollectSun(positionX, positionZ, sunManager);
            }

            if (canManager != null)
            {
                bool canCollected = canManager.TryCollectCan(positionX, positionZ, 3.0f);
            }
            isCurrentlyMoving = false;
        }

        public void MoveForward(float deltaTime)
        {
            if (energySystem.IsEnergyEmpty)
            {
                Console.WriteLine("Wall-E has no energy to move!");
                return;
            }

            float radians = rotationY * (float)Math.PI / 180f;
            float moveX = (float)Math.Sin(radians) * MoveSpeed * deltaTime;
            float moveZ = (float)Math.Cos(radians) * MoveSpeed * deltaTime;

            float newX = positionX + moveX;
            float newZ = positionZ + moveZ;

            if (IsValidPosition(newX, newZ) && !CheckCollision(newX, newZ))
            {
                positionX = newX;
                positionZ = newZ;
                UpdateHeightFromTerrain();
                UpdateCurrentPosition();
                isCurrentlyMoving = true; 
            }
        }

        public void MoveBackward(float deltaTime)
        {
            if (energySystem.IsEnergyEmpty)
            {
                Console.WriteLine("Wall-E has no energy to move!");
                return;
            }

            float radians = rotationY * (float)Math.PI / 180f;
            float moveX = (float)Math.Sin(radians) * MoveSpeed * deltaTime;
            float moveZ = (float)Math.Cos(radians) * MoveSpeed * deltaTime;

            float newX = positionX - moveX;
            float newZ = positionZ - moveZ;

            if (IsValidPosition(newX, newZ) && !CheckCollision(newX, newZ))
            {
                positionX = newX;
                positionZ = newZ;
                UpdateHeightFromTerrain();
                UpdateCurrentPosition();
                isCurrentlyMoving = true;
            }
        }

        public void RotateLeft(float deltaTime)
        {
            rotationY -= RotationSpeed * deltaTime;
            while (rotationY < 0f) rotationY += 360f;
            UpdateCurrentPosition();
            isCurrentlyMoving = true;
        }

        public void RotateRight(float deltaTime)
        {
            rotationY += RotationSpeed * deltaTime;
            while (rotationY >= 360f) rotationY -= 360f;
            UpdateCurrentPosition();
            isCurrentlyMoving = true;
        }

        private void UpdateCurrentPosition()
        {
            currentPosition = new Vector3D<float>(positionX, positionY, positionZ);
        }

        public void AdjustPitch(bool up)
        {
            if (up)
            {
                pitch -= PitchStep;
                if (pitch < -PitchLimit) pitch = -PitchLimit;
            }
            else
            {
                pitch += PitchStep;
                if (pitch > PitchLimit) pitch = PitchLimit;
            }
        }


        private bool CheckCollision(float newX, float newZ)
        {
  
            if (cactusManager != null)
            {
                var cactusCollisions = cactusManager.GetCollisionData();
                foreach (var cactus in cactusCollisions)
                {
                    float distance = (float)Math.Sqrt(
                        Math.Pow(newX - cactus.X, 2) + Math.Pow(newZ - cactus.Z, 2)
                    );

                    float minDistance = WallERadius + cactus.Radius;

                    if (distance < minDistance)
                    {
                        return true;
                    }
                }
            }

            if (thumbleweedManager != null)
            {
                var thumbleweedCollisions = thumbleweedManager.GetCollisionData();
                foreach (var tumbleweed in thumbleweedCollisions)
                {
                    float distance = (float)Math.Sqrt(
                        Math.Pow(newX - tumbleweed.X, 2) + Math.Pow(newZ - tumbleweed.Z, 2)
                    );

                    float minDistance = WallERadius + tumbleweed.Radius;

                    if (distance < minDistance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsValidPosition(float x, float z)
        {
            if (terrainInitialized)
            {
                return terrainCalculator.IsPositionWithinTerrain(x, z);
            }
            return x >= -50f && x <= 50f && z >= -50f && z <= 50f;
        }

        private void UpdateHeightFromTerrain()
        {
            if (terrainInitialized)
            {
                float terrainHeight = terrainCalculator.GetHeightAtPosition(positionX, positionZ);
                positionY = terrainHeight + WallEHeightOffset;
            }
            else
            {
                positionY = WallEHeightOffset;
            }
        }

        public Vector3D<float> GetThirdPersonCameraPosition()
        {
            var wallEPos = Position;
            float radians = rotationY * (float)Math.PI / 180f;

            float behindDistance = 15f;
            float heightOffset = 8f;

            float behindX = wallEPos.X - (float)Math.Sin(radians) * behindDistance;
            float behindZ = wallEPos.Z - (float)Math.Cos(radians) * behindDistance;

            return new Vector3D<float>(behindX, wallEPos.Y + heightOffset, behindZ);
        }

        public Vector3D<float> GetCameraTarget()
        {
            var wallEPos = Position;
            float radians = rotationY * (float)Math.PI / 180f;
            float frontDistance = 5f;
            float targetX = wallEPos.X + (float)Math.Sin(radians) * frontDistance;
            float targetZ = wallEPos.Z + (float)Math.Cos(radians) * frontDistance;

            return new Vector3D<float>(targetX, wallEPos.Y + 1f, targetZ);
        }

        public Vector3D<float> GetFirstPersonCameraTarget()
        {
            var eye = GetFirstPersonCameraPosition();
            float yaw = rotationY * MathF.PI / 180f;

            float dirX = MathF.Sin(yaw) * MathF.Cos(pitch);
            float dirY = MathF.Sin(pitch);
            float dirZ = MathF.Cos(yaw) * MathF.Cos(pitch);

            var direction = Vector3D.Normalize(new Vector3D<float>(dirX, dirY, dirZ));
            return eye + direction * 5.0f;
        }

        public Vector3D<float> GetFirstPersonCameraPosition()
        {
            var wallEPos = Position;
            return new Vector3D<float>(wallEPos.X, wallEPos.Y + 6f, wallEPos.Z);
        }

        public void ResetToTerrainCenter()
        {
            if (terrainInitialized)
            {
                terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);
                positionX = (minX + maxX) / 2f;
                positionZ = (minZ + maxZ) / 2f;
                rotationY = 0f;
                UpdateHeightFromTerrain();
                UpdateCurrentPosition();

                energySystem.ResetEnergy();

                Console.WriteLine($"Wall-E reset to: ({positionX:F1}, {positionY:F1}, {positionZ:F1})");
            }
            else
            {
                positionX = 0f;
                positionZ = 0f;
                positionY = WallEHeightOffset;
                rotationY = 0f;
                UpdateCurrentPosition();
                energySystem.ResetEnergy();
            }
        }

        public void RestartGame()
        {
            if (sunManager != null)
            {
                sunManager.RespawnSuns();
            }
            if (canManager != null)
            {
                canManager.RespawnCans();
                canManager.ResetScore();
            }

            Console.WriteLine("Game restarted!");
        }

        public void PrintTerrainInfo()
        {
            Console.WriteLine($"=== Wall-E Status ===");
            Console.WriteLine($"Position: ({positionX:F2}, {positionY:F2}, {positionZ:F2})");
            Console.WriteLine($"Rotation: {rotationY:F1}°");
            Console.WriteLine($"Energy: {energySystem.CurrentEnergy:F1}/{energySystem.MaxEnergy} ({energySystem.EnergyPercentage:F1}%)");

            if (terrainInitialized)
            {
                float terrainHeight = terrainCalculator.GetHeightAtPosition(positionX, positionZ);
                bool withinTerrain = terrainCalculator.IsPositionWithinTerrain(positionX, positionZ);
                Console.WriteLine($"Terrain Height: {terrainHeight:F2}");
                Console.WriteLine($"Within Terrain: {withinTerrain}");

                terrainCalculator.GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ);
                Console.WriteLine($"Terrain Bounds: X({minX:F1} to {maxX:F1}), Z({minZ:F1} to {maxZ:F1})");
            }
            else
            {
                Console.WriteLine("No terrain available");
            }

            if (sunManager != null)
            {
                Console.WriteLine($"Remaining suns: {sunManager.GetRemainingCount()}");
                bool nearSun = energySystem.IsNearSun(positionX, positionZ, sunManager);
                Console.WriteLine($"Near sun: {nearSun}");
            }
        }

        private void OnEnergyEmpty()
        {
            Console.WriteLine("GAME OVER: Wall-E ran out of energy!");
        }

        private void OnEnergyLow()
        {
            Console.WriteLine(" WARNING: Wall-E's energy is low! Plase find some sunlight!");
        }

        private void OnEnergyRecovered()
        {
            Console.WriteLine("Wall-E's energy recovered!");
        }
    }

    public struct CollisionObject
    {
        public float X;
        public float Z;
        public float Radius;
        public float Height;

        public CollisionObject(float x, float z, float radius)
        {
            X = x;
            Z = z;
            Radius = radius;
        }
    }
}