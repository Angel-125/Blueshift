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
        protected const double standardGee = 9.81;
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
        /// Shows percentage of output of the FlexGrav generator; based on current throttle setting.
        /// </summary>
        [KSPField(guiName = "#LOC_BLUESHIFT_flexGravOutput", guiUnits = "%", guiFormat = "f2", guiActive = true)]
        public double flexGravOutput;

        /// <summary>
        /// Shows percentage of how well the generator can interact with local gravity.
        /// </summary>
        [KSPField(guiName = "#LOC_BLUESHIFT_flexGravCoupling", guiUnits = "%", guiFormat = "f2", guiActive = true)]
        public double gravityCoupling;

        /// <summary>
        /// Shows amount of acceleration available.
        /// </summary>
        [KSPField(guiName = "#LOC_BLUESHIFT_flexGravAcceleration", guiActive = true, guiUnits = " m/s^2", guiFormat = "f2")]
        public double flexGravAcceleration;

        /// <summary>
        /// Display value of the vessel's horizontal acceleration, in units of m/s^2.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_flexGravHorizontalG", guiUnits = " m/s^2", guiFormat = "f2")]
        public double horizontalAcceleration = 1f;

        /// <summary>
        /// Display value of the vessel's vertical acceleration, in units of m/s^2.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_flexGravVerticalG", guiUnits = " m/s^2", guiFormat = "f2")]
        public double verticalAcceleration = 1f;

        /// <summary>
        /// Redirects gravity forward (0) or upward (90)
        /// </summary>
        [KSPField(isPersistant = true, guiName = "#LOC_BLUESHIFT_flexGravVerticalSlider", guiUnits = "deg", guiActive = true, guiActiveEditor = true)]
        [UI_FloatRange(stepIncrement = 1f, minValue = 0f, maxValue = 90f)]
        public float verticalLiftAngle = 90f;

        /// <summary>
        /// Sets output of the generator in manual mode.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "#LOC_BLUESHIFT_flexGravOutput", guiUnits = "%", guiActive = true)]
        [UI_FloatRange(stepIncrement = 1f, minValue = 0f, maxValue = 100f)]
        public float flexGravManualOutput = 100f;

        /// <summary>
        /// Flag to indicate whether or not to use the main throttle to control generator output.
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravOutputControl")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_flexGravOutputThrottle", disabledText = "#LOC_BLUESHIFT_flexGravOutputManual")]
        public bool throttleControlEnabled = false;

        /// <summary>
        /// Flag to indicate whether or not to forward or reverse acceleration
        /// </summary>
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_flexGravVector")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_flexGravVectorFwd", disabledText = "#LOC_BLUESHIFT_flexGravVectorRev")]
        public bool useForwardVector = true;

        /// <summary>
        /// Amount of increase in Electric Charge that it costs to run the generator.
        /// Computed as a percentage of vessel mass. So, if this value is 0.05 (the default),
        /// and the vessel is 100 tonnes, then the EC cost increases by 5.
        /// This is a value between 0 and 1.
        /// </summary>
        [KSPField]
        public float ecMassPercentIncrease = 0.05f;

        // Translation keys to increase or decrease the lift angle
        [KSPAxisField(axisGroup = KSPAxisGroup.TranslateY, axisMode = KSPAxisMode.Absolute, guiActive = false, guiActiveEditor = false, guiName = "#LOC_BLUESHIFT_flexGravUpDown", ignoreIncrementByZero = true, incrementalSpeed = 1f, isPersistant = true, maxValue = 1f, minValue = -1f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 1f, minValue = -1f, stepIncrement = 1f)]
        public float translateUpDn;
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
            verticalLiftAngle = 0;
            useForwardVector = true;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravFwdActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Sets acceleration fully reverse.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravRevActn")]
        public void SetReversedAccelerationAction(KSPActionParam param)
        {
            verticalLiftAngle = 0;
            useForwardVector = false;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravRevActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Sets acceleration fully vertical.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravVertActn")]
        public void SetVerticalAccelerationAction(KSPActionParam param)
        {
            verticalLiftAngle = 90;
            ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravVertActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }

        /// <summary>
        /// Toggles lift angle.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravToggleLiftAngle")]
        public void ToggleVerticalAccelerationThrottleAction(KSPActionParam param)
        {
            if (verticalLiftAngle < 1E-09)
            {
                SetVerticalAccelerationAction(param);
            }
            else if (useForwardVector)
            {
                SetForwardAccelerationAction(param);
            }
            else
            {
                SetReversedAccelerationAction(param);
            }
        }

        /// <summary>
        /// Toggles lift angle.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravVectorToggle")]
        public void ToggleVerticalAccelerationVectorAction(KSPActionParam param)
        {
            useForwardVector = !useForwardVector;

            if (useForwardVector)
            {
                SetForwardAccelerationAction(param);
            }
            else
            {
                SetReversedAccelerationAction(param);
            }
        }

        /// <summary>
        /// Toggles throttle control.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_flexGravToggleThrottleCtrl")]
        public void ToggleThrottleControlAction(KSPActionParam param)
        {
            throttleControlEnabled = !throttleControlEnabled;
            Fields["flexGravManualOutput"].guiActive = !throttleControlEnabled;

            string message = Localizer.Format("#LOC_BLUESHIFT_flexGravThrottleCtrl") + " - " ;
            if (throttleControlEnabled)
            {
                message += Localizer.Format("#LOC_BLUESHIFT_flexGravOutputThrottle");
            }
            else
            {
                message += Localizer.Format("#LOC_BLUESHIFT_flexGravOutputManual");
            }

            ScreenMessages.PostScreenMessage(message, 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            ecMassPercentIncrease = Mathf.Clamp(ecMassPercentIncrease, 0, 1);
            maxGravityNegatedFactor = Mathf.Clamp(maxGravityNegatedPercent, 0, 100f);

            Fields["flexGravManualOutput"].guiActive = !throttleControlEnabled;
            Fields["flexGravOutput"].guiActive = throttleControlEnabled && IsActivated;
            if (!string.IsNullOrEmpty(groupName))
            {
                Fields["flexGravOutput"].group.name = groupName;
                Fields["flexGravOutput"].group.displayName = groupName;
                Fields["gravityCoupling"].group.name = groupName;
                Fields["gravityCoupling"].group.displayName = groupName;
                Fields["flexGravAcceleration"].group.name = groupName;
                Fields["flexGravAcceleration"].group.displayName = groupName;                
                Fields["verticalAcceleration"].group.name = groupName;
                Fields["verticalAcceleration"].group.displayName = groupName;
                Fields["horizontalAcceleration"].group.name = groupName;
                Fields["horizontalAcceleration"].group.displayName = groupName;
                Fields["verticalLiftAngle"].group.name = groupName;
                Fields["verticalLiftAngle"].group.displayName = groupName;              
                Fields["flexGravManualOutput"].group.name = groupName;
                Fields["flexGravManualOutput"].group.displayName = groupName;                
                Fields["throttleControlEnabled"].group.name = groupName;
                Fields["throttleControlEnabled"].group.displayName = groupName;
                Fields["useForwardVector"].group.name = groupName;
                Fields["useForwardVector"].group.displayName = groupName;
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
            Fields["flexGravManualOutput"].guiActive = !throttleControlEnabled;
            Fields["flexGravOutput"].guiActive = IsActivated && throttleControlEnabled && !isMissingResources && statusPercent > 1E-09;
            Fields["gravityCoupling"].guiActive = IsActivated && !isMissingResources && statusPercent > 1E-09;
            Fields["useForwardVector"].guiActive = IsActivated && !isMissingResources && statusPercent > 1E-09;
            Fields["flexGravAcceleration"].guiActive = IsActivated && !isMissingResources && statusPercent > 1E-09;
            Fields["verticalAcceleration"].guiActive = IsActivated && !isMissingResources && statusPercent > 1E-09;
            Fields["horizontalAcceleration"].guiActive = IsActivated && !isMissingResources && statusPercent > 1E-09;

            // Check activation state
            if (!IsActivated || isMissingResources || statusPercent <= 1E-0)
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
            if (statusPercent > 0) // statusPercent <= 0 means the converter is stuck
            {
                //Vector3d accelerationVector = part.vessel.graviticAcceleration.normalized * -combinedVerticalAcceleration;
                ApplyAccelerationVector(accelerationVector);

                // Add horizontal acceleration
                accelerationVector = part.vessel.GetReferenceTransformPart().transform.up.normalized * (float)combinedHorizontalAcceleration;
                ApplyAccelerationVector(accelerationVector);
            }
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            ConversionRecipe recipe = base.PrepareRecipe(deltatime);

            if (!HighLogic.LoadedSceneIsFlight || !IsActivated || isMissingResources)
                return recipe;


            // Compute modifiers based on vessel mass.
            float throttle = vessel.ctrlState.mainThrottle;
            if (!throttleControlEnabled)
                throttle = flexGravManualOutput / 100f;
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
                resource.Ratio *= ratioMultiplier * throttle;
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
            if (HighLogic.LoadedSceneIsFlight && isMissingResources)
            {
                verticalAcceleration = 0;
                horizontalAcceleration = 0;
            }

            double localGravity = standardGee;
            float throttle = 1.0f;
            if (HighLogic.LoadedSceneIsFlight)
            {
                localGravity = FlightGlobals.getGeeForceAtPosition(vessel.transform.position).magnitude;
                throttle = vessel.ctrlState.mainThrottle;
                if (!throttleControlEnabled)
                    throttle = flexGravManualOutput / 100f;

                if (FlightGlobals.ActiveVessel == part.vessel && translateUpDn != 0)
                {
                    if (translateUpDn > 0)
                        verticalLiftAngle += 1;
                    else
                        verticalLiftAngle -= 1;

                    verticalLiftAngle = Mathf.Clamp(verticalLiftAngle, 0, 90);

                    if (verticalLiftAngle >= 90)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravVertActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                    else if (verticalLiftAngle <= 0 && useForwardVector)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravFwdActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                    else if (verticalLiftAngle <= 0 && !useForwardVector)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravRevActn"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                    else if (verticalLiftAngle >= 58 && verticalLiftAngle <= 62)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravLift60"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                    else if (verticalLiftAngle >= 43 && verticalLiftAngle <= 47)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravLift45"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                    else if (verticalLiftAngle >= 28 && verticalLiftAngle <= 32)
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_flexGravLift30"), 3.0f, ScreenMessageStyle.UPPER_LEFT, Color.white);
                }
            }

            float verticalAccelerationFactor = verticalLiftAngle / 90;
            float horizontalAcceleractionFactor = 1 - verticalAccelerationFactor;
            double vectorMagnitude = maxGForceCancellation >= localGravity ? averageMaxGravityNegatedFactor : (localGravity - maxGForceCancellation) / localGravity;
            double liftVectorMagnitude = vectorMagnitude * verticalAccelerationFactor;
            double horizontalVectorMagnitude = vectorMagnitude * horizontalAcceleractionFactor;

            // Horizontal vector direction
            if (useForwardVector == false)
                horizontalVectorMagnitude *= -1;

            // Account for throttle toggles.
            horizontalVectorMagnitude *= throttle;
            liftVectorMagnitude *= throttle;

            // Update the gravity displays
            if (HighLogic.LoadedSceneIsFlight)
                gravityCoupling = Mathf.Clamp((float)(localGravity / (vessel.mainBody.GeeASL * standardGee) * 100f), 0, 100);
            flexGravOutput = throttle * 100;
            flexGravAcceleration = vectorMagnitude * localGravity * throttle;
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

        protected virtual void ApplyAccelerationVector(Vector3d accelerationVector)
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
