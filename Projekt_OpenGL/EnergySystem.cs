using System;

namespace Projekt_OpenGL
{
    public class EnergySystem
    {
        private float maxEnergy = 100.0f;
        private float currentEnergy = 100.0f;

        private const float ENERGY_DRAIN_PER_SECOND = 1.5f;    
        private const float ENERGY_RECHARGE_RATE = 25.0f;      
        private const float IDLE_DRAIN_RATE = 0.1f;           

        private const float SUN_DETECTION_RADIUS = 7.0f;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public float EnergyPercentage => (currentEnergy / maxEnergy) * 100.0f;
        public bool IsEnergyEmpty => currentEnergy <= 0.0f;
        public bool IsEnergyLow => currentEnergy <= 20.0f; 

        public event Action OnEnergyEmpty;
        public event Action OnEnergyLow;
        public event Action OnEnergyRecovered;

        private bool wasLowEnergy = false;

        public EnergySystem(float maxEnergy = 100.0f)
        {
            this.maxEnergy = maxEnergy;
            this.currentEnergy = maxEnergy;
        }

        public void Update(float deltaTime, bool isMoving, bool isNearSun)
        {
            float oldEnergy = currentEnergy;

            if (isNearSun)
            {
                currentEnergy += ENERGY_RECHARGE_RATE * deltaTime;
                if (currentEnergy > maxEnergy)
                    currentEnergy = maxEnergy;
            }
            else if (isMoving)
            {
                currentEnergy -= ENERGY_DRAIN_PER_SECOND * deltaTime;
            }
            else
            {
                currentEnergy -= IDLE_DRAIN_RATE * deltaTime;
            }

            if (currentEnergy < 0.0f)
                currentEnergy = 0.0f;

            CheckEnergyEvents(oldEnergy);
        }

        public bool IsNearSun(float wallEX, float wallEZ, SunManager sunManager)
        {
            if (sunManager == null) return false;

            var sunPositions = sunManager.GetSunPositions();

            foreach (var sunPos in sunPositions)
            {
                float distance = (float)Math.Sqrt(
                    Math.Pow(wallEX - sunPos.X, 2) +
                    Math.Pow(wallEZ - sunPos.Z, 2)
                );

                if (distance <= SUN_DETECTION_RADIUS)
                {
                    return true;
                }
            }

            return false;
        }
        public bool TryCollectSun(float wallEX, float wallEZ, SunManager sunManager)
        {
            if (sunManager == null) return false;

            bool collected = sunManager.TryCollectSun(wallEX, wallEZ);

            if (collected)
            {

                currentEnergy += 30.0f;
                if (currentEnergy > maxEnergy)
                    currentEnergy = maxEnergy;

                return true;
            }

            return false;
        }

        private void CheckEnergyEvents(float oldEnergy)
        {

            if (currentEnergy <= 0.0f && oldEnergy > 0.0f)
            {
                OnEnergyEmpty?.Invoke();
            }


            if (IsEnergyLow && !wasLowEnergy)
            {
                OnEnergyLow?.Invoke();
                wasLowEnergy = true;
            }


            if (!IsEnergyLow && wasLowEnergy)
            {
                OnEnergyRecovered?.Invoke();
                wasLowEnergy = false;
            }
        }

        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            wasLowEnergy = false;
        }

        public void SetEnergy(float energy)
        {
            currentEnergy = Math.Clamp(energy, 0.0f, maxEnergy);
        }

        public (float r, float g, float b) GetEnergyBarColor()
        {
            float percentage = EnergyPercentage / 100.0f;

            if (percentage > 0.6f)
            {
                return (0.0f, 1.0f, 0.0f);
            }
            else if (percentage > 0.3f)
            {
                return (1.0f, 1.0f, 0.0f);
            }
            else
            {
                return (1.0f, 0.0f, 0.0f);
            }
        }
    }
}