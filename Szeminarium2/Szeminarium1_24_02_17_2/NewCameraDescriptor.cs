using Silk.NET.Maths;

namespace Szeminarium1_24_02_17_2
{
    internal class NewCameraDescriptor
    {
        
        private float yaw = MathF.PI;   // 180 fok forgatasert kell
        private float pitch = 0; 
      

        private const float AngleStep = MathF.PI / 180 * 5; // 5 fok a leptek
        private const float MoveStep = 0.2f;

        private static readonly Vector3D<float> WorldUp = new Vector3D<float>(0, 1, 0);

        public Vector3D<float> Position { get; set; } = new Vector3D<float>(0, 0, 8);

        public Vector3D<float> ForwardVector
        {
            get
            {
                float x = MathF.Cos(pitch) * MathF.Sin(yaw);
                float y = MathF.Sin(pitch);
                float z = MathF.Cos(pitch) * MathF.Cos(yaw);
                return Vector3D.Normalize(new Vector3D<float>(x, y, z));
            }
        }

        public Vector3D<float> RightVector
        {
            get
            {
                return Vector3D.Normalize(Vector3D.Cross(ForwardVector, WorldUp));
            }
        }

        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D.Normalize(Vector3D.Cross(RightVector, ForwardVector));
            }
        }

        public Vector3D<float> Target
        {
            get
            {
                return Position + ForwardVector;
            }
        }

        public void YawLeft() { yaw -= AngleStep; }
        public void YawRight() { yaw += AngleStep; }
        public void PitchUp() { pitch += AngleStep; }
        public void PitchDown() { pitch -= AngleStep; }


        public void MoveForward() { Position += ForwardVector * MoveStep; }
        public void MoveBackward() { Position -= ForwardVector * MoveStep; }
        public void MoveRight() { Position += RightVector * MoveStep; }
        public void MoveLeft() { Position -= RightVector * MoveStep; }
        public void MoveUp() { Position += UpVector * MoveStep; }
        public void MoveDown() { Position -= UpVector * MoveStep; }
    }
}
