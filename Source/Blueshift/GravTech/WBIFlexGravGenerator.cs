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
    /// <summary>
    /// Counters the pull of gravity up to a maximum amount of gravitic acceleration.
    /// </summary>
    public class WBIFlexGravGenerator : WBIModuleGeneratorFX
    {
        #region Constants
        const double standardGee = 9.81;
        #endregion

        #region Fields
        /// <summary>
        /// In meters per second-squared, the amount of acceleration due to gravity that the device is rated for. If this value meets or exceeds the local gravity, then only 95% of local gravity can be negated.
        /// </summary>
        [KSPField]
        public float maxGForceCancellation = 9.810001f;

        /// <summary>
        /// A value between 0 and 100, this field represents the maximum percentage of local gravity that can be negated. If multiple generators are present, then this value is averaged between the active generators.
        /// </summary>
        [KSPField]
        float maxGravityNegatedPercent = 95.0f;

        /// <summary>
        /// Redirects gravity forward (0) or upward (100)
        /// </summary>
        [KSPField(isPersistant = true, guiName = "#LOC_BLUESHIFT_flexGravVerticalSlider", guiUnits = "%", guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, minValue = 0f, maxValue = 100f)]
        public float verticalAccelerationPercent = 100f;

        /// <summary>
        /// Display value of the vessel's horizontal acceleration, in units of m/s^2.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravHorizontalG", guiUnits = "m/s^2", guiFormat = "f2")]
        public double horizontalAcceleration = 1f;

        /// <summary>
        /// Display value of the vessel's vertical acceleration, in units of m/s^2.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravVerticalG", guiUnits = "m/s^2", guiFormat = "f2")]
        public double verticalAcceleration = 1f;

        /// <summary>
        /// Flag to indicate whether or not to control horizontal acceleration via main throttle.
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravHorizThrottle")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled", disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool horizontalAccelerationThrottled = false;

        /// <summary>
        /// Flag to indicate whether or not to control vertical acceleration via main throttle.
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravVertThrottle")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled", disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool verticalAccelerationThrottled = false;

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
        /// <summary>
        /// Current vessel part count.
        /// </summary>
        protected int vesselPartCount = 0;

        /// <summary>
        /// List of contragravity generators on the vessel.
        /// </summary>
        protected List<WBIFlexGravGenerator> flexGravGenerators;
        double combinedHorizontalAcceleration = 0;
        double combinedVerticalAcceleration = 0;
        float maxGravityNegatedFactor = 0;
        float averageMaxGravityNegatedFactor = 0;
        #endregion

        #region Actions
        /// <summary>
        /// Sets acceleration fully forward.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravFwdActn")]
        public void SetForwardAccelerationAction(KSPActionParam param)
        {
            verticalAccelerationPercent = 0;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravFwdActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Sets acceleration fully vertical.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravVertActn")]
        public void SetVerticalAccelerationAction(KSPActionParam param)
        {
            verticalAccelerationPercent = 100;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravVertActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Toggles vertical acceleration throttle.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravVertThrotToggle")]
        public void ToggleVerticalAccelerationThrottleAction(KSPActionParam param)
        {
            verticalAccelerationThrottled = !verticalAccelerationThrottled;

            string message = Localizer.Format("#LOC_BLUESHIFT_flexGravVertThrottle") + " - " ;
            if (verticalAccelerationThrottled)
                message += Localizer.Format("#LOC_BLUESHIFT_enabled");
            else
                message += Localizer.Format("#LOC_BLUESHIFT_disabled");

            ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Toggles horizontal acceleration throttle.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravHorzThrotToggle")]
        public void ToggleHorizontalAccelerationThrottleAction(KSPActionParam param)
        {
            horizontalAccelerationThrottled = !horizontalAccelerationThrottled;

            string message = Localizer.Format("#LOC_BLUESHIFT_flexGravHorzThrotToggle") + " - ";
            if (horizontalAccelerationThrottled)
                message += Localizer.Format("#LOC_BLUESHIFT_enabled");
            else
                message += Localizer.Format("#LOC_BLUESHIFT_disabled");

            ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ecMassPercentIncrease = Mathf.Clamp(ecMassPercentIncrease, 0, 1);
            maxGravityNegatedFactor = Mathf.Clamp(maxGravityNegatedPercent, 0, 100f);

            if (!string.IsNullOrEmpty(groupName))
            {
                Fields["verticalAcceleration"].group.name = groupName;
                Fields["verticalAcceleration"].group.displayName = groupName;
                Fields["horizontalAcceleration"].group.name = groupName;
                Fields["horizontalAcceleration"].group.displayName = groupName;
                Fields["verticalAccelerationPercent"].group.name = groupName;
                Fields["verticalAccelerationPercent"].group.displayName = groupName;
                Fields["verticalAccelerationThrottled"].group.name = groupName;
                Fields["verticalAccelerationThrottled"].group.displayName = groupName;
                Fields["horizontalAccelerationThrottled"].group.name = groupName;
                Fields["horizontalAccelerationThrottled"].group.displayName = groupName;
            }

            if (HighLogic.LoadedSceneIsFlight)
                vesselPartCount = part.vessel.parts.Count;

            getGenerators();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (HighLogic.LoadedSceneIsEditor)
            {
                updateDisplaysForEditor();
                return;
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get the list of generators.
            if (flexGravGenerators == null || vesselPartCount != part.vessel.parts.Count)
            {
                vesselPartCount = part.vessel.parts.Count;
                getGenerators();
            }

            // Update our accelerations
            updateAccelerations();

            // Update GUI based on activation and resource states.
            Fields["verticalAcceleration"].guiActive = IsActivated && !isMissingResources;
            Fields["horizontalAcceleration"].guiActive = IsActivated && !isMissingResources;

            // Check activation state
            if (!IsActivated || isMissingResources)
            {
                return;
            }

            // Check flight state
            if (part.vessel.situation == Vessel.Situations.DOCKED || part.vessel.orbit.altitude > part.vessel.orbit.referenceBody.Radius * 3.0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravDeactivated"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.yellow);
                StopResourceConverter();
                return;
            }

            // If we're not the lead generator, then go no further.
            if (flexGravGenerators[0] != this)
            {
                return;
            }

            // Compute the combined accelerations
            updateCombinedAccelerations();

            // Add lift acceleration
            Vector3d accelerationVector = (part.vessel.GetWorldPos3D() - vessel.mainBody.position).normalized * combinedVerticalAcceleration;
            //Vector3d accelerationVector = part.vessel.graviticAcceleration.normalized * -combinedVerticalAcceleration;
            ApplyAccelerationVector(accelerationVector);

            // Add horizontal acceleration
            accelerationVector = part.vessel.GetReferenceTransformPart().transform.up.normalized * (float)combinedHorizontalAcceleration;
            ApplyAccelerationVector(accelerationVector);
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            ConversionRecipe recipe = base.PrepareRecipe(deltatime);

            if (!HighLogic.LoadedSceneIsFlight || !IsActivated || isMissingResources)
                return recipe;


            // Compute modifiers based on vessel mass.
            float throttle = vessel.ctrlState.mainThrottle;
            float ratioMultiplier = vessel.GetTotalMass();
            List<ResourceRatio> recipeInputs = recipe.Inputs;
            int count = recipeInputs.Count;
            ResourceRatio resource;
            for (int index = 0; index < count; index++)
            {
                // E.C. increases based on a percentage of the vessel's mass.
                if (recipe.Inputs[index].ResourceName == "ElectricCharge")
                {
                    resource = recipeInputs[index];
                    resource.Ratio += (1 + ecMassPercentIncrease) * ratioMultiplier;
                    recipeInputs[index] = resource;
                    continue;
                }

                resource = recipeInputs[index];
                resource.Ratio *= ratioMultiplier;

                // Account for throttle toggles.
                if (horizontalAccelerationThrottled || verticalAccelerationThrottled)
                    resource.Ratio *= throttle;

                recipeInputs[index] = resource;
            }

            // Now prepare recipe
            recipe.SetInputs(recipeInputs);
            return recipe;
        }
        #endregion

        #region Helpers
        private void updateDisplaysForEditor()
        {
            // Get the generators
            ShipConstruct ship = EditorLogic.fetch.ship;
            if (vesselPartCount != ship.parts.Count)
            {
                flexGravGenerators = new List<WBIFlexGravGenerator>();
                int shipPartsCount = ship.parts.Count;
                vesselPartCount = shipPartsCount;
                List<WBIFlexGravGenerator> generators;
                for (int index = 0; index < shipPartsCount; index++)
                {
                    generators = ship.parts[index].FindModulesImplementing<WBIFlexGravGenerator>();
                    if (generators.Count > 0)
                        flexGravGenerators.AddRange(generators);
                }
            }

            calculateAverageMaxGravityNegated();

            // Update our accelerations
            updateAccelerations();

            // Compute the combined accelerations
            updateCombinedAccelerations();

            // Now set our accelerations to the combined
            horizontalAcceleration = combinedHorizontalAcceleration;
            verticalAcceleration = combinedVerticalAcceleration;
        }

        private void calculateAverageMaxGravityNegated()
        {
            averageMaxGravityNegatedFactor = 0;
            int count = flexGravGenerators.Count;

            for (int index = 0; index < count; index++)
                averageMaxGravityNegatedFactor += flexGravGenerators[index].maxGravityNegatedPercent;

            averageMaxGravityNegatedFactor = averageMaxGravityNegatedFactor / count / 100f;
        }

        private void updateCombinedAccelerations()
        {
            combinedHorizontalAcceleration = 0;
            combinedVerticalAcceleration = 0;
            int count = flexGravGenerators.Count;
            for (int index = 0; index < count; index++)
            {
                if (flexGravGenerators[index].IsActivated == false || flexGravGenerators[index].isMissingResources)
                    continue;

                combinedHorizontalAcceleration += flexGravGenerators[index].horizontalAcceleration;
                combinedVerticalAcceleration += flexGravGenerators[index].verticalAcceleration;
            }
        }

        private void updateAccelerations()
        {
            double localGravity = standardGee;
            float throttle = 1.0f;
            if (HighLogic.LoadedSceneIsFlight)
            {
                localGravity = FlightGlobals.getGeeForceAtPosition(vessel.transform.position).magnitude;
                throttle = vessel.ctrlState.mainThrottle;
            }

            float verticalAccelerationFactor = verticalAccelerationPercent / 100;
            float horizontalAcceleractionFactor = 1 - verticalAccelerationFactor;
            double vectorMagnitude = maxGForceCancellation >= localGravity ? averageMaxGravityNegatedFactor : (localGravity - maxGForceCancellation) / localGravity;
            double liftVectorMagnitude = vectorMagnitude * verticalAccelerationFactor;
            double horizontalVectorMagnitude = vectorMagnitude * horizontalAcceleractionFactor;

            // Account for throttle toggles.
            if (horizontalAccelerationThrottled)
                horizontalVectorMagnitude *= throttle;

            if (verticalAccelerationThrottled)
                liftVectorMagnitude *= throttle;

            // Update the gravity displays
            verticalAcceleration = liftVectorMagnitude * localGravity;
            horizontalAcceleration = horizontalVectorMagnitude * localGravity;
        }

        protected virtual void getGenerators()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                return;
            }

            flexGravGenerators = part.vessel.FindPartModulesImplementing<WBIFlexGravGenerator>();
            calculateAverageMaxGravityNegated();
        }

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
