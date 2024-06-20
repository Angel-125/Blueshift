using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BlueshiftMassEffect
{
    public enum WBIMassEffectCoreStatus
    {
        off,
        isPowered,
        missingResources
    }

    public class WBIMassEffectCore: PartModule
    {
        public WBIMassEffectCoreStatus status = WBIMassEffectCoreStatus.isPowered;

        /// <summary>
        /// Maximum percentage of mass that the core can reduce. from 0-100%.
        /// </summary>
        [KSPField]
        public float maxMassReductionPercent = 99.999f;

        /// <summary>
        /// Reduction power - 0 - 100% of max possible
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Reduction Power")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 100f, minValue = 0f)]
        public float massReductionPercent = 100f;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Activate Mass Effect", groupName = "MassEffect", groupDisplayName = "Mass Effect", groupStartCollapsed = true)]
        public void ActivateMassEffect()
        {
            status = WBIMassEffectCoreStatus.isPowered;
            updateGUI();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Deactivate Mass Effect", groupName = "MassEffect", groupDisplayName = "Mass Effect", groupStartCollapsed = true)]
        public void DeactivateMassEffect()
        {
            status = WBIMassEffectCoreStatus.off;
            updateGUI();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            updateGUI();
        }

        /// <summary>
        /// Returns the percentage of mass reduction
        /// </summary>
        /// <returns></returns>
        public float getEffectiveMassReduction()
        {
            if (status != WBIMassEffectCoreStatus.isPowered)
                return 0;

            return 1 - ((maxMassReductionPercent * (massReductionPercent / 100f)) / 100f);
        }

        private void updateGUI()
        {
            switch (status)
            {
                case WBIMassEffectCoreStatus.off:
                    Events["ActivateMassEffect"].active = true;
                    Events["DeactivateMassEffect"].active = false;
                    break;

                case WBIMassEffectCoreStatus.isPowered:
                    Events["ActivateMassEffect"].active = false;
                    Events["DeactivateMassEffect"].active = true;
                    break;

                default:
                    break;
            }

        }
    }
}