using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueshiftMassEffect
{
    internal class WBIMassEffectUtilities
    {
        public static WBIMassEffectCore[] getActiveCores(Vessel vessel)
        {
            List<WBIMassEffectCore> massEffectCores = vessel.FindPartModulesImplementing<WBIMassEffectCore>();
            if (massEffectCores == null)
                return new WBIMassEffectCore[] { };

            int count = massEffectCores.Count;
            if (count <= 0)
                return new WBIMassEffectCore[] { };

            List<WBIMassEffectCore> cores = new List<WBIMassEffectCore>();
            for (int index = 0; index < count; index++)
            {
                if (massEffectCores[index].status == WBIMassEffectCoreStatus.isPowered)
                    cores.Add(massEffectCores[index]);
            }

            return cores.OrderByDescending(o => o.maxMassReductionPercent).ToArray();
        }

        public static WBIMassEffectCore[] getActiveCores(ShipConstruct ship)
        {
            List<WBIMassEffectCore> massEffectCores = new List<WBIMassEffectCore>();
            Part[] parts = EditorLogic.fetch.ship.parts.ToArray();
            Part part;

            for (int partIndex = 0; partIndex < parts.Length; partIndex++)
            {
                part = parts[partIndex];
                WBIMassEffectCore massEffectCore = part.FindModuleImplementing<WBIMassEffectCore>();
                if (massEffectCore != null && massEffectCore.status == WBIMassEffectCoreStatus.isPowered)
                    massEffectCores.Add(massEffectCore);
            }

            return massEffectCores.OrderByDescending(o => o.maxMassReductionPercent).ToArray();
        }

        public static float calculateMassReduction(WBIMassEffectCore[] massEffectCores)
        {
            int count = massEffectCores.Length;
            if (count <= 0)
                return 1f;

            float massReduction = 1f;
            float reductionFactor = 1f;

            for (int index = 0; index < count; index++)
            {
                massReduction = massEffectCores[index].getEffectiveMassReduction();
                if (massReduction > 0f)
                    reductionFactor *= massReduction;
            }

            if (float.IsNaN(reductionFactor) || float.IsInfinity(reductionFactor))
                return 0;

            return reductionFactor;
        }
    }
}
