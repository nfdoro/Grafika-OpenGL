namespace Szeminarium1_24_02_17_2
{
    internal class CubeArrangementModel
    {
        public double LeftFaceRotationAngle { get; set; } = 0;
        public double RightFaceRotationAngle { get; set; } = 0;
        public double TopFaceRotationAngle { get; set; } = 0;
        public double BottomFaceRotationAngle { get; set; } = 0;
        public double FrontFaceRotationAngle { get; set; } = 0;
        public double BackFaceRotationAngle { get; set; } = 0;

        public double TargetLeftFaceRotationAngle { get; set; } = 0;
        public double TargetRightFaceRotationAngle { get; set; } = 0;
        public double TargetTopFaceRotationAngle { get; set; } = 0;
        public double TargetBottomFaceRotationAngle { get; set; } = 0;
        public double TargetFrontFaceRotationAngle { get; set; } = 0;
        public double TargetBackFaceRotationAngle { get; set; } = 0;

        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;
        public bool LeftAnimationEnabled { get; set; } = false;
        public bool RightAnimationEnabled { get; set; } = false;
        public bool TopAnimationEnabled { get; set; } = false;
        public bool BottomAnimationEnabled { get; set; } = false;
        public bool FrontAnimationEnabled { get; set; } = false;

        public bool BackAnimationEnabled { get; set; } = false;
        public bool AnimationEnabled { get; set; } = false;


        public float PulseScale { get; private set; } = 1;
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


        public double PulseAnimationTime;

        public bool IsCubeSolved()
        {
            const double Tolerance = 0.001;
            return Math.Abs(LeftFaceRotationAngle % 360) < Tolerance &&
                   Math.Abs(RightFaceRotationAngle % 360) < Tolerance &&
                   Math.Abs(TopFaceRotationAngle % 360) < Tolerance &&
                   Math.Abs(BottomFaceRotationAngle % 360) < Tolerance &&
                   Math.Abs(FrontFaceRotationAngle % 360) < Tolerance &&
                   Math.Abs(BackFaceRotationAngle % 360) < Tolerance;
        }
        internal void AdvanceTime(double deltaTime)
        {
            // we do not advance the simulation when animation is stopped
            /*if (!AnimationEnabeld)
                return;
            */
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

            if (BottomAnimationEnabled && BottomFaceRotationAngle < TargetBottomFaceRotationAngle)
            {
                BottomFaceRotationAngle += 45.0 * deltaTime;
                if (BottomFaceRotationAngle >= TargetBottomFaceRotationAngle)
                {
                    BottomFaceRotationAngle = TargetBottomFaceRotationAngle;
                    BottomAnimationEnabled = false;
                }
            }

            if (FrontAnimationEnabled && FrontFaceRotationAngle < TargetFrontFaceRotationAngle)
            {
                FrontFaceRotationAngle += 45.0 * deltaTime;
                if (FrontFaceRotationAngle >= TargetFrontFaceRotationAngle)
                {
                    FrontFaceRotationAngle = TargetFrontFaceRotationAngle;
                    FrontAnimationEnabled = false;
                }
            }

            if (BackAnimationEnabled && BackFaceRotationAngle < TargetBackFaceRotationAngle)
            {
                BackFaceRotationAngle += 45.0 * deltaTime;
                if (BackFaceRotationAngle >= TargetBackFaceRotationAngle)
                {
                    BackFaceRotationAngle = TargetBackFaceRotationAngle;
                    BackAnimationEnabled = false;
                }
            }

            if (LeftFaceRotationAngle == TargetLeftFaceRotationAngle &&
            RightFaceRotationAngle == TargetRightFaceRotationAngle &&
            TopFaceRotationAngle == TargetTopFaceRotationAngle &&
            BottomFaceRotationAngle == TargetBottomFaceRotationAngle &&
            FrontFaceRotationAngle == TargetFrontFaceRotationAngle &&
            BackFaceRotationAngle == TargetBackFaceRotationAngle)
            {
                AnimationEnabeld = false;
            }

            if (IsCubeSolved())
            {
                PulseAnimationTime += deltaTime;
                double duration = 0.5; 
                double frequency = 2 * Math.PI / duration; 

                if (PulseAnimationTime < duration)
                {
                    PulseScale = 1.0f + 0.1f * (float)(Math.Sin(PulseAnimationTime * frequency) * 0.5 + 0.5);
                }
                else
                {
                    PulseScale = 1.0f;
                }
            }
            else
            {
                PulseAnimationTime = 0;
                PulseScale = 1.0f;
            }

        }


    }


}
