using Silk.NET.Maths;
using System;
using System.Collections.Generic;

namespace Projekt_OpenGL
{
    public class TerrainHeightCalculator
    {
        private readonly List<float[]> vertices;
        private readonly List<int[]> faces;
        private readonly float terrainScale;
        private readonly Vector3D<float> terrainOffset;

        private float minX, maxX, minZ, maxZ;
        private bool boundsCalculated = false;

        public TerrainHeightCalculator(List<float[]> objVertices, List<int[]> objFaces, float scale, Vector3D<float> offset)
        {
            vertices = objVertices ?? throw new ArgumentNullException(nameof(objVertices));
            faces = objFaces ?? throw new ArgumentNullException(nameof(objFaces));
            terrainScale = scale;
            terrainOffset = offset;

            CalculateBounds();

            Console.WriteLine($"TerrainHeightCalculator initialized:");
            Console.WriteLine($"  Vertices: {vertices.Count}");
            Console.WriteLine($"  Faces: {faces.Count}");
            Console.WriteLine($"  Scale: {terrainScale}");
            Console.WriteLine($"  Offset: {terrainOffset}");
            Console.WriteLine($"  Bounds: X({minX:F1} to {maxX:F1}), Z({minZ:F1} to {maxZ:F1})");
        }

        private void CalculateBounds()
        {
            if (vertices.Count == 0)
            {
                minX = maxX = minZ = maxZ = 0;
                boundsCalculated = true;
                return;
            }

            var firstVertex = GetTransformedVertex(0);
            minX = maxX = firstVertex.X;
            minZ = maxZ = firstVertex.Z;

  
            for (int i = 1; i < vertices.Count; i++)
            {
                var vertex = GetTransformedVertex(i);

                if (vertex.X < minX) minX = vertex.X;
                if (vertex.X > maxX) maxX = vertex.X;
                if (vertex.Z < minZ) minZ = vertex.Z;
                if (vertex.Z > maxZ) maxZ = vertex.Z;
            }

            boundsCalculated = true;
        }

        private Vector3D<float> GetTransformedVertex(int index)
        {
            if (index < 0 || index >= vertices.Count)
                return Vector3D<float>.Zero;

            var vertex = vertices[index];
            return new Vector3D<float>(
                vertex[0] * terrainScale + terrainOffset.X,
                vertex[1] * terrainScale + terrainOffset.Y,
                vertex[2] * terrainScale + terrainOffset.Z
            );
        }

        public float GetHeightAtPosition(float worldX, float worldZ)
        {
            float defaultHeight = terrainOffset.Y;

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];

                int v1Index = face[0]; 
                int v2Index = face[3];  
                int v3Index = face[6]; 


                if (v1Index < 0 || v1Index >= vertices.Count ||
                    v2Index < 0 || v2Index >= vertices.Count ||
                    v3Index < 0 || v3Index >= vertices.Count)
                    continue;


                var v1 = GetTransformedVertex(v1Index);
                var v2 = GetTransformedVertex(v2Index);
                var v3 = GetTransformedVertex(v3Index);

                if (IsPointInTriangle(worldX, worldZ, v1.X, v1.Z, v2.X, v2.Z, v3.X, v3.Z))
                {
                    float height = InterpolateHeight(worldX, worldZ, v1, v2, v3);
                    return height;
                }
            }

            return defaultHeight;
        }

        private bool IsPointInTriangle(float px, float pz, float x1, float z1, float x2, float z2, float x3, float z3)
        {
            float denom = (z2 - z3) * (x1 - x3) + (x3 - x2) * (z1 - z3);
            if (Math.Abs(denom) < 1e-10f) return false; 

            float a = ((z2 - z3) * (px - x3) + (x3 - x2) * (pz - z3)) / denom;
            float b = ((z3 - z1) * (px - x3) + (x1 - x3) * (pz - z3)) / denom;
            float c = 1 - a - b;

            return a >= -1e-6f && b >= -1e-6f && c >= -1e-6f; 
        }

        private float InterpolateHeight(float px, float pz, Vector3D<float> v1, Vector3D<float> v2, Vector3D<float> v3)
        {
            float denom = (v2.Z - v3.Z) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Z - v3.Z);
            if (Math.Abs(denom) < 1e-10f) return v1.Y; 

            float a = ((v2.Z - v3.Z) * (px - v3.X) + (v3.X - v2.X) * (pz - v3.Z)) / denom;
            float b = ((v3.Z - v1.Z) * (px - v3.X) + (v1.X - v3.X) * (pz - v3.Z)) / denom;
            float c = 1 - a - b;

            return a * v1.Y + b * v2.Y + c * v3.Y;
        }

        public bool IsPositionWithinTerrain(float worldX, float worldZ)
        {
            if (!boundsCalculated) return false;

            float margin = 2.0f;
            return worldX >= (minX - margin) && worldX <= (maxX + margin) &&
                   worldZ >= (minZ - margin) && worldZ <= (maxZ + margin);
        }

        public void GetTerrainBounds(out float minX, out float maxX, out float minZ, out float maxZ)
        {
            minX = this.minX;
            maxX = this.maxX;
            minZ = this.minZ;
            maxZ = this.maxZ;
        }
    }
}