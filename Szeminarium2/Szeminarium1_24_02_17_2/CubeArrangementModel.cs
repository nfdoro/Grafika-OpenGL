namespace Szeminarium1_24_02_17_2
{
    internal class CubeArrangementModel
    {
        public double LeftFaceRotationAngle { get; set; } = 0;
        public double RightFaceRotationAngle { get; set; } = 0;
        public double TopFaceRotationAngle { get; set; } = 0;

        public double TargetLeftFaceRotationAngle { get; set; } = 0;
        public double TargetRightFaceRotationAngle { get; set; } = 0;
        public double TargetTopFaceRotationAngle { get; set; } = 0;
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;
        public bool LeftAnimationEnabled { get; set; } = false;
        public bool RightAnimationEnabled { get; set; } = false;
        public bool TopAnimationEnabled { get; set; } = false;

        /// <summary>
        /// The time of the simulation. It helps to calculate time dependent values.
        /// </summary>
        private double Time { get; set; } = 0;

        /// <summary>
        /// The value by which the center cube is scaled. It varies between 0.8 and 1.2 with respect to the original size.
        /// </summary>
        public double CenterCubeScale { get; private set; } = 1;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleOwnRevolution { get; private set; } = 0;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleRevolutionOnGlobalY { get; private set; } = 0;

        internal void AdvanceTime(double deltaTime)
        {
            // we do not advance the simulation when animation is stopped
            if (!AnimationEnabeld)
                return;

            // set a simulation time
            Time += deltaTime;

            // lets produce an oscillating scale in time
            CenterCubeScale = 1 + 0.2 * Math.Sin(1.5 * Time);

            DiamondCubeAngleOwnRevolution = Time * 10;

            DiamondCubeAngleRevolutionOnGlobalY = -Time;


            if (LeftAnimationEnabled && LeftFaceRotationAngle < TargetLeftFaceRotationAngle)
            {
                LeftFaceRotationAngle += 45.0 * deltaTime;
                if (LeftFaceRotationAngle >= TargetLeftFaceRotationAngle)
                {
                    LeftFaceRotationAngle = TargetLeftFaceRotationAngle;
                    LeftAnimationEnabled = false;
                }
            }

            if (RightAnimationEnabled && RightFaceRotationAngle < TargetRightFaceRotationAngle)
            {
                RightFaceRotationAngle += 45.0 * deltaTime;
                if (RightFaceRotationAngle >= TargetRightFaceRotationAngle)
                {
                    RightFaceRotationAngle = TargetRightFaceRotationAngle;
                    RightAnimationEnabled = false;
                }
            }

            if (TopAnimationEnabled && TopFaceRotationAngle < TargetTopFaceRotationAngle)
            {
                TopFaceRotationAngle += 45.0 * deltaTime;
                if (TopFaceRotationAngle >= TargetTopFaceRotationAngle)
                {
                    TopFaceRotationAngle = TargetTopFaceRotationAngle;
                    TopAnimationEnabled = false;
                }
            }

            if (LeftFaceRotationAngle == TargetLeftFaceRotationAngle &&
      RightFaceRotationAngle == TargetRightFaceRotationAngle && TopFaceRotationAngle == TargetTopFaceRotationAngle)
            {
                AnimationEnabeld = false;
            }
        }
    }
}
