using Silk.NET.Maths;
using System;

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

        private TerrainHeightCalculator terrainCalculator;
        private bool terrainInitialized = false;

        public Vector3D<float> Position
        {
            get
            {
                UpdateHeightFromTerrain();
                return new Vector3D<float>(positionX, positionY, positionZ);
            }
        }

        public float RotationY => rotationY;

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
                Console.WriteLine($"Wall-E initialized at: ({positionX:F1}, {positionY:F1}, {positionZ:F1})");
            }
            else
            {
                positionX = 0f;
                positionZ = 0f;
                positionY = WallEHeightOffset;
                rotationY = 0f;
            }
        }

        public void MoveForward(float deltaTime)
        {
            
            float radians = rotationY * (float)Math.PI / 180f;
            float moveX = (float)Math.Sin(radians) * MoveSpeed * deltaTime;
            float moveZ = (float)Math.Cos(radians) * MoveSpeed * deltaTime;

            float newX = positionX + moveX;
            float newZ = positionZ + moveZ;

            if (IsValidPosition(newX, newZ))
            {
                positionX = newX;
                positionZ = newZ;
                UpdateHeightFromTerrain();
            }
        }

        public void MoveBackward(float deltaTime)
        {
            float radians = rotationY * (float)Math.PI / 180f;
            float moveX = (float)Math.Sin(radians) * MoveSpeed * deltaTime;
            float moveZ = (float)Math.Cos(radians) * MoveSpeed * deltaTime;

            float newX = positionX - moveX;
            float newZ = positionZ - moveZ;

            if (IsValidPosition(newX, newZ))
            {
                positionX = newX;
                positionZ = newZ;
                UpdateHeightFromTerrain();
            }
        }

        public void RotateLeft(float deltaTime)
        {
            rotationY -= RotationSpeed * deltaTime;
            while (rotationY < 0f) rotationY += 360f;
        }

        public void RotateRight(float deltaTime)
        {
            rotationY += RotationSpeed * deltaTime;
            while (rotationY >= 360f) rotationY -= 360f;
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

        public Vector3D<float> GetFirstPersonCameraPosition()
        {
            var wallEPos = Position;
            return new Vector3D<float>(wallEPos.X, wallEPos.Y +5f, wallEPos.Z);
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
                Console.WriteLine($"Wall-E reset to: ({positionX:F1}, {positionY:F1}, {positionZ:F1})");
            }
            else
            {
                positionX = 0f;
                positionZ = 0f;
                positionY = WallEHeightOffset;
                rotationY = 0f;
            }
        }

        public void PrintTerrainInfo()
        {
            Console.WriteLine($"=== Wall-E Status ===");
            Console.WriteLine($"Position: ({positionX:F2}, {positionY:F2}, {positionZ:F2})");
            Console.WriteLine($"Rotation: {rotationY:F1}°");

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
        }
    }
}