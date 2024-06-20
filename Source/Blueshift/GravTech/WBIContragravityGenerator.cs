using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    public class WBIContragravityGenerator : WBIModuleGeneratorFX
    {
        /// <summary>
        /// A percentage value between 0 and 100, where 100% cancels all the local gravity.
        /// </summary>
        [KSPField]
        public float maxGForceCancellation = 95f;

        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_contragravityEffectiveG", guiUnits = "m/sec", guiFormat = "f2")]
        public double effectiveGravity = 1f;

        private float cancellationFactor;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            cancellationFactor = 1 - (Mathf.Clamp(maxGForceCancellation, 10, 100) / 100f);

            if (!string.IsNullOrEmpty(groupName))
            {
                Fields["effectiveGravity"].group.name = groupName;
                Fields["effectiveGravity"].group.displayName = groupName;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Get lift vector
            Vector3d accelerationVector = part.vessel.graviticAcceleration * -1f * cancellationFactor;
            effectiveGravity = Math.Abs(accelerationVector.magnitude);

            // Check activation state
            if (!IsActivated || isMissingResources)
            {
                effectiveGravity = Math.Abs(part.vessel.graviticAcceleration.magnitude);
                return;
            }

            // Check flight state
            if (this.part.vessel.situation == Vessel.Situations.ESCAPING ||
                this.part.vessel.situation == Vessel.Situations.DOCKED ||
                this.part.vessel.situation == Vessel.Situations.ORBITING)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_contragravityDeactivated"), 3.0f, ScreenMessageStyle.UPPER_LEFT);
                StopResourceConverter();
                return;
            }

            //Add acceleration. We do this manually instead of letting ModuleEnginesFX do it so that the craft can have any orientation desired.
            ApplyAccelerationVector(accelerationVector);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            if (!HighLogic.LoadedSceneIsFlight || !IsActivated || isMissingResources)
                return base.PrepareRecipe(deltatime);

            // Compute resourceConsumptionModifier based on vessel mass and local gravity.
            float vesselMass = vessel.GetTotalMass();
            double graviticAcceleration = part.vessel.graviticAcceleration.magnitude;

            resourceConsumptionModifier = 1.0f * graviticAcceleration * vesselMass;

            // Now prepare recipe
            return base.PrepareRecipe(deltatime);
        }

        private void ApplyAccelerationVector(Vector3d accelerationVector)
        {
            // If we're not the lead generator, then switch off.
            List<WBIContragravityGenerator> generators = this.part.vessel.FindPartModulesImplementing<WBIContragravityGenerator>();
            if (generators[0] != this)
            {
                StopResourceConverter();
                return;
            }

            int partCount = vessel.parts.Count;
            Part vesselPart;
            for (int index = 0; index < partCount; index++)
            {
                vesselPart = vessel.parts[index];
                if (vesselPart.rb != null)
                {
                    vesselPart.rb.AddForce(accelerationVector, ForceMode.Acceleration);
                }
            }
        }
    }
}
