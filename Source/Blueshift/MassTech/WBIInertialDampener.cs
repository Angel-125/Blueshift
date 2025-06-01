using System.Collections.Generic;
using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// This part module enhances engine thrust and Isp. While the vessel's reported mass will remain unchanged, thrust, Isp, TWR, and delta-v values will be affected.
    /// </summary>
    public class WBIInertialDampener : WBIModuleGeneratorFX
    {
        #region GameEvents
        /// <summary>
        /// Signals that the inertial dampener was updated.
        /// </summary>
        public static EventData<WBIInertialDampener, Vessel> onDampenerUpdated = new EventData<WBIInertialDampener, Vessel>("onDampenerUpdated");

        /// <summary>
        /// Signals that the inertial dampener was updated in the editor.
        /// </summary>
        public static EventData<WBIInertialDampener> onDampenerUpdatedEditor = new EventData<WBIInertialDampener>("onDampenerUpdatedEditor");
        #endregion

        #region Fields
        /// <summary>
        /// How much internal dampening to produce
        /// </summary>
        [KSPField(isPersistant = true, guiName = "#LOC_BLUESHIFT_inertialDampenerFactor", guiActive = true)]
        [UI_FloatRange(stepIncrement = 1f, maxValue = 100f, minValue = 0f)]
        public float inertialDampeningFactor = 100.0f;

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
        internal float totalInertialDampeningFactor = 0f;

        /// <summary>
        /// List of inertial dampeners on the vessel.
        /// </summary>
        protected List<WBIInertialDampener> inertialDampeners;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            if (!string.IsNullOrEmpty(groupName))
            {
                Fields["inertialDampeningFactor"].group.name = groupName;
                Fields["inertialDampeningFactor"].group.displayName = groupName;
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselWasModified.Add(onVesselWasModified);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartEvent.Add(onEditorPartEvent);
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
            }
            Fields["inertialDampeningFactor"].OnValueModified += new Callback<object>(onValueModified);

            getGenerators();
            updateDampeningFields();

            ecMassPercentIncrease = Mathf.Clamp(ecMassPercentIncrease, 0, 1);
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselWasModified.Remove(onVesselWasModified);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorPartEvent.Remove(onEditorPartEvent);
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            }
            Fields["inertialDampeningFactor"].OnValueModified -= new Callback<object>(onValueModified);
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
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();

            enableJointReinforcement();
            updateDampeningFields();
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();

            updateDampeningFields();
            disableJointReinforcement();
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            ConversionRecipe recipe = base.PrepareRecipe(deltatime);

            if (!HighLogic.LoadedSceneIsFlight || !IsActivated || isMissingResources)
                return recipe;

            // Compute modifiers based on vessel mass.
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
                recipeInputs[index] = resource;
            }

            // Now prepare recipe
            recipe.SetInputs(recipeInputs);
            return recipe;
        }
        #endregion

        #region Helpers
        void enableJointReinforcement()
        {
            if (CheatOptions.UnbreakableJoints || inertialDampeners[0] != this)
                return;
        }

        void disableJointReinforcement()
        {
            if (CheatOptions.UnbreakableJoints || inertialDampeners[0] != this)
                return;
        }

        void updateDampeningFields()
        {
            // If we're not the lead dampener, then go no further.
            if (inertialDampeners[0] != this)
            {
                return;
            }

            // If we aren't activated or we're missing resources then we're done.
            if (!IsActivated || (IsActivated && isMissingResources))
            {
                totalInertialDampeningFactor = 0;

                if (HighLogic.LoadedSceneIsFlight)
                    onDampenerUpdated.Fire(this, part.vessel);
                else if (HighLogic.LoadedSceneIsEditor)
                    onDampenerUpdatedEditor.Fire(this);

                return;
            }

            // Compute the total dampening factor.
            int count = inertialDampeners.Count;
            totalInertialDampeningFactor = 0;
            for (int index = 0; index < count; index++)
            {
                totalInertialDampeningFactor += inertialDampeners[index].inertialDampeningFactor;
            }

            if (HighLogic.LoadedSceneIsFlight)
                onDampenerUpdated.Fire(this, part.vessel);
            else if (HighLogic.LoadedSceneIsEditor)
                onDampenerUpdatedEditor.Fire(this);
        }

        void onValueModified(object obj)
        {
            getGenerators();
            updateDampeningFields();
        }

        void onVesselWasModified(Vessel modifiedVessel)
        {
            if (modifiedVessel != part.vessel)
                return;

            getGenerators();
            updateDampeningFields();
        }

        void onEditorPartEvent(ConstructionEventType eventType, Part modifiedPart)
        {
            getGenerators();
            updateDampeningFields();
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            getGenerators();
            updateDampeningFields();
        }

        void getGenerators()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                inertialDampeners = part.vessel.FindPartModulesImplementing<WBIInertialDampener>();
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                inertialDampeners = new List<WBIInertialDampener>();

                ShipConstruct ship = EditorLogic.fetch.ship;
                int count = ship.parts.Count;
                Part shipPart;
                List<WBIInertialDampener> attenuators;
                for (int index = 0; index < count; index++)
                {
                    shipPart = ship.parts[index];
                    attenuators = shipPart.FindModulesImplementing<WBIInertialDampener>();
                    if (attenuators.Count > 0)
                        inertialDampeners.AddRange(attenuators);
                }
            }
            else
            {
                inertialDampeners = new List<WBIInertialDampener>();
            }

            if (inertialDampeners.Count <= 0)
                inertialDampeners.Add(this);
        }
        #endregion
    }
}
