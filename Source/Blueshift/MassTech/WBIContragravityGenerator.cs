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
        #region Constants
        const double standardGee = 9.81;
        const double maxGravityNegatedPercent = 0.95;
        #endregion

        #region Fields
        /// <summary>
        /// In meters per second-squared, the amount of acceleration due to gravity that can be negated. If this value meets or exceeds the local gravity, then only 95% of local gravity can be negated.
        /// </summary>
        [KSPField]
        public float maxGForceCancellation = 9.810001f;

        /// <summary>
        /// Display value of the vessel's effective gravity, in units of g.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_contragravityEffectiveG", guiUnits = "g", guiFormat = "f2")]
        public double effectiveGravity = 1f;

        /// <summary>
        /// Amount of increase in Electric Charge that it costs to run the generator.
        /// Computed as a percentage of vessel mass. So, if this value is 0.05 (the default),
        /// and the vessel is 100 tonnes, then the EC cost increases by 5.
        /// This is a value between 0 and 1.
        /// </summary>
        [KSPField]
        public float ecMassPercentIncrease = 0.05f;
        #endregion

        #region Housekeeping
        float cancellationFactor;
        List<WBIContragravityGenerator> contragravityGenerators;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ecMassPercentIncrease = Mathf.Clamp(ecMassPercentIncrease, 0, 1);

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

            // Get the list of generators.
            contragravityGenerators = part.vessel.FindPartModulesImplementing<WBIContragravityGenerator>();

            // Check activation state
            if (!IsActivated || isMissingResources)
            {
                effectiveGravity = Math.Abs(part.vessel.graviticAcceleration.magnitude) / standardGee;
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

            // If we're not the lead generator, then go no further.
            if (contragravityGenerators[0] != this)
            {
                effectiveGravity = contragravityGenerators[0].effectiveGravity;
                return;
            }

            // Compute the combined max gravity negation.
            double combinedMaxGravityNegated = 0;
            int count = contragravityGenerators.Count;
            for (int index = 0; index < count; index++)
            {
                combinedMaxGravityNegated += contragravityGenerators[index].maxGForceCancellation;
            }

            // Calculate amount of gravitic acceleration that we can negate.
            double localGravity = Math.Abs(part.vessel.graviticAcceleration.magnitude);
            double effectiveLocalGravity = combinedMaxGravityNegated >= localGravity ? (1 - maxGravityNegatedPercent) * localGravity : localGravity - combinedMaxGravityNegated;

            // Update effective gravity display
            effectiveGravity = effectiveLocalGravity / standardGee;

            //Get lift vector
            double vectorMagnitude = combinedMaxGravityNegated >= localGravity ? maxGravityNegatedPercent : (localGravity - combinedMaxGravityNegated) / localGravity;
            Vector3d accelerationVector = part.vessel.graviticAcceleration * -vectorMagnitude;

            //Add acceleration.
            ApplyAccelerationVector(accelerationVector);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            ConversionRecipe recipe = base.PrepareRecipe(deltatime);

            if (!HighLogic.LoadedSceneIsFlight || !IsActivated || isMissingResources)
                return recipe;

            // Compute modifiers based on vessel mass.
            float vesselMass = vessel.GetTotalMass();
            List<ResourceRatio> recipeInputs = recipe.Inputs;
            int count = recipeInputs.Count;
            ResourceRatio resource;
            for (int index = 0; index < count; index++)
            {
                // E.C. increases based on a percentage of the vessel's mass.
                if (recipe.Inputs[index].ResourceName == "electricCharge")
                {
                    resource = recipeInputs[index];
                    resource.Ratio += (1 + ecMassPercentIncrease) * vesselMass;
                    recipeInputs[index] = resource;
                    continue;
                }

                resource = recipeInputs[index];
                resource.Ratio *= vesselMass;
                recipeInputs[index] = resource;
            }

            // Now prepare recipe
            recipe.SetInputs(recipeInputs);
            return recipe;
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
