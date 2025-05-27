using Silk.NET.Maths;

namespace Projekt_OpenGL
{
    internal class NewCameraDescriptor
    {
        private Vector3D<float> position = new Vector3D<float>(0, 5, 10);
        private Vector3D<float> target = new Vector3D<float>(0, 0, 0);
        private Vector3D<float> upVector = new Vector3D<float>(0, 1, 0);

        private float yaw = -90.0f;
        private float pitch = 0.0f;
        private const float moveSpeed = 0.5f;
        private const float rotationSpeed = 5.0f;

        public Vector3D<float> Position => position;
        public Vector3D<float> Target => target;
        public Vector3D<float> UpVector => upVector;

        private Vector3D<float> GetFront()
        {
            float yawRad = yaw * (float)Math.PI / 180f;
            float pitchRad = pitch * (float)Math.PI / 180f;

            return new Vector3D<float>(
                (float)(Math.Cos(yawRad) * Math.Cos(pitchRad)),
                (float)Math.Sin(pitchRad),
                (float)(Math.Sin(yawRad) * Math.Cos(pitchRad))
            );
        }

        private Vector3D<float> GetRight()
        {
            var front = GetFront();
            return Vector3D.Normalize(Vector3D.Cross(front, new Vector3D<float>(0, 1, 0)));
        }

        private void UpdateTarget()
        {
            target = position + GetFront();
        }

        public void MoveForward()
        {
            position += GetFront() * moveSpeed;
            UpdateTarget();
        }

        public void MoveBackward()
        {
            position -= GetFront() * moveSpeed;
            UpdateTarget();
        }

        public void MoveLeft()
        {
            position -= GetRight() * moveSpeed;
            UpdateTarget();
        }

        public void MoveRight()
        {
            position += GetRight() * moveSpeed;
            UpdateTarget();
        }

        public void MoveUp()
        {
            position += upVector * moveSpeed;
            UpdateTarget();
        }

        public void MoveDown()
        {
            position -= upVector * moveSpeed;
            UpdateTarget();
        }

        public void YawLeft()
        {
            yaw -= rotationSpeed;
            UpdateTarget();
        }

        public void YawRight()
        {
            yaw += rotationSpeed;
            UpdateTarget();
        }

        public void PitchUp()
        {
            pitch += rotationSpeed;
            if (pitch > 89.0f) pitch = 89.0f;
            UpdateTarget();
        }

        public void PitchDown()
        {
            pitch -= rotationSpeed;
            if (pitch < -89.0f) pitch = -89.0f;
            UpdateTarget();
        }
    }
}