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
    public class WBIGravityGenerator : WBIModuleGeneratorFX
    {
        #region Constants
        const double standardGee = 9.81;
        const double maxGravityNegatedPercent = 0.95;
        #endregion

        #region Fields
        /// <summary>
        /// Max amount of mass that the generator can support before experiencing reductions in gravitic acceleration.
        /// </summary>
        [KSPField]
        public float maxTonnage = 20f;
        #endregion

        #region Housekeeping
        List<WBIGravityGenerator> gravityGenerators;
        int currentPartCount = 0;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsFlight)
            {
                currentPartCount = part.vessel.parts.Count;
                gravityGenerators = part.vessel.FindPartModulesImplementing<WBIGravityGenerator>();
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Check activation state
            if (!IsActivated || isMissingResources)
            {
                return;
            }

            // Check gravity. If we're already at 1g or greater then we're done.
            double localGravity = Math.Abs(part.vessel.graviticAcceleration.magnitude);
            if (localGravity >= standardGee)
            {
                return;
            }

            // Check flight state
            if (this.part.vessel.situation == Vessel.Situations.ESCAPING ||
                this.part.vessel.situation == Vessel.Situations.DOCKED ||
                this.part.vessel.situation == Vessel.Situations.ORBITING)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_gravityDeactivated"), 3.0f, ScreenMessageStyle.UPPER_LEFT);
                StopResourceConverter();
                return;
            }

            // Get the list of generators.
            if (currentPartCount != part.vessel.parts.Count)
            {
                currentPartCount = part.vessel.parts.Count;
                gravityGenerators = part.vessel.FindPartModulesImplementing<WBIGravityGenerator>();
            }

            // If we're not the lead generator, then go no further.
            if (gravityGenerators[0] != this)
            {
                return;
            }

            // Compute the combined max tonnage.
            float totalMaxTonnage = 0;
            int count = gravityGenerators.Count;
            for (int index = 0; index < count; index++)
            {
                totalMaxTonnage += gravityGenerators[index].maxTonnage;
            }

            float heavyVesselPenalty = 1f;
            float vesselMass = part.vessel.GetTotalMass();
            if (vesselMass > totalMaxTonnage)
            {
                heavyVesselPenalty = totalMaxTonnage / vesselMass;
            }

            // Calculate amount of gravitic acceleration that we need to add.
            double deltaGravity = (standardGee - localGravity) * heavyVesselPenalty;

            // Get gravity vector
            Vector3d accelerationVector = part.vessel.graviticAcceleration + (part.vessel.graviticAcceleration.normalized * deltaGravity);

            //Add acceleration.
            ApplyAccelerationVector(accelerationVector);
        }
        #endregion

        #region Helpers
        private void ApplyAccelerationVector(Vector3d accelerationVector)
        {
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
        #endregion

    }
}
