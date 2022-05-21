using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift.Utility
{
    [KSPModule("Resource Vent")]
    public class WBIResourceVent : WBIPartModule
    {
        #region Strings
        string statusMissing = "Missing: ";
        string statusReady = "Ready";
        string statusDischarging = "Discharging";
        #endregion

        #region Fields
        [KSPField]
        public string dischargedResources = string.Empty;

        [KSPField]
        public float landedDischargeRate = 0.01f;

        [KSPField]
        public string landedResourcesRequired = string.Empty;

        [KSPField]
        public double splashedDischargeRate = 0.01f;

        [KSPField]
        public string spashedResourcesRequired = string.Empty;

        [KSPField]
        public double atmosphericDischargeRate = 0.001f;

        [KSPField]
        public string atmosphereResourcesRequired = string.Empty;

        [KSPField]
        public double vacuumDischargeRate = 0.001f;

        [KSPField]
        public string vacuumResourcesRequired = string.Empty;

        [KSPField]
        public bool canSyncWithWarpEngine = false;

        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_resourceVentStatus")]
        public string status;

        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_resourceVentDischargeRate", guiFormat = "f3", guiUnits = "u/sec")]
        public double dischargeRate;

        [KSPField(isPersistant = true)]
        public double lastUpdateTime = 0f;
        #endregion

        #region Housekeeping
        public double dischargeMultiplier = 1.0f;
        public string[] resourcesToDump;
        public ResourceRatio[] landedResourceInputs;
        public ResourceRatio[] splashedResourceInputs;
        public ResourceRatio[] atmoResourceInputs;
        public ResourceRatio[] vaccResourceInputs;
        ModuleAnimateGeneric animation;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (animation != null)
            {
                if (animation.Events["Toggle"].guiName == animation.startEventGUIName)
                {
                    dischargeRate = 0f;
                    status = statusReady;
                    return;
                }
            }

            //Timekeeping
            double currentTime = Planetarium.GetUniversalTime();
            double elapsedTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            //Do we have resources to discharge?
            if (!hasResourcesToDump())
            {
                dischargeRate = 0f;
                status = statusReady;
                return;
            }

            //Consume inputs based upon vehicle situation
            switch (this.part.vessel.situation)
            {
                //Use atmospheric rate and resource consumption if in atmosphere
                //otherwise use vacuum rate and consumption
                default:
                    if (this.part.vessel.dynamicPressurekPa > 0f)
                    {
                        dischargeRate = atmosphericDischargeRate * dischargeMultiplier;
                        if (atmoResourceInputs != null)
                        {
                            if (!consumeInputs(atmoResourceInputs, elapsedTime))
                                return;
                        }
                    }

                    else //Vacuum
                    {
                        dischargeRate = vacuumDischargeRate * dischargeMultiplier;

                        if (vaccResourceInputs != null)
                        {
                            dischargeRate = vacuumDischargeRate;
                            if (!consumeInputs(vaccResourceInputs, elapsedTime))
                                return;
                        }
                    }
                    break;

                //Use landed rate and consumption
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    dischargeRate = landedDischargeRate * dischargeMultiplier;
                    if (landedResourceInputs != null)
                    {
                        if (!consumeInputs(landedResourceInputs, elapsedTime))
                            return;
                    }
                    break;

                //Use splashed rate and consumption
                case Vessel.Situations.SPLASHED:
                    dischargeRate = splashedDischargeRate * dischargeMultiplier;
                    if (splashedResourceInputs != null)
                    {
                        if (!consumeInputs(splashedResourceInputs, elapsedTime))
                            return;
                    }
                    break;
            }

            //If we're still good, then discharge the desired resources
            status = statusDischarging;
            double dischargeRatePerFrame = dischargeRate * TimeWarp.fixedDeltaTime;

            //Account for catchup time
            if (elapsedTime >= 1.0f)
                dischargeRatePerFrame = dischargeRate * (float)elapsedTime;

            for (int index = 0; index < resourcesToDump.Length; index++)
            {
                part.RequestResource(resourcesToDump[index], dischargeRatePerFrame);
            }
        }

        public override string GetInfo()
        {
            getInputs();
            StringBuilder info = new StringBuilder();

            //List of resources to dump
            string discharges = Localizer.Format("#LOC_BLUESHIFT_resourceVentDischarges");
            if (resourcesToDump.Length > 1)
            {
                info.Append(discharges + ": ");
                for (int index = 0; index < resourcesToDump.Length - 1; index++)
                    info.Append(resourcesToDump[index] + ", ");
                info.AppendLine(" and " + resourcesToDump[resourcesToDump.Length - 1]);
            }
            else
            {
                info.AppendLine(discharges + " " + dischargedResources);
            }

            //Discharge rates
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_resourceVentDischargeRates"));

            //Landed
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_resourceVentDischargeLanded") + formatRate(landedDischargeRate));
            if (landedResourceInputs != null)
                info.AppendLine(formatConsumedResources(landedResourceInputs));
            info.AppendLine(" ");

            //Splashed
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_resourceVentDischargeSplashed") + formatRate(splashedDischargeRate));
            if (splashedResourceInputs != null)
                info.AppendLine(formatConsumedResources(splashedResourceInputs));
            info.AppendLine(" ");

            //Atmo
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_resourceVentDischargeAtmo") + formatRate(atmosphericDischargeRate));
            if (atmoResourceInputs != null)
                info.AppendLine(formatConsumedResources(atmoResourceInputs));
            info.AppendLine(" ");

            //Vacuum
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_resourceVentDischargeVacuum") + formatRate(vacuumDischargeRate));
            if (vaccResourceInputs != null)
                info.AppendLine(formatConsumedResources(vaccResourceInputs));

            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            WBIWarpEngine.onWarpEffectsUpdated.Add(onWarpEffectsUpdated);
            WBIWarpEngine.onWarpEngineStart.Add(onWarpEngineStart);
            WBIWarpEngine.onWarpEngineShutdown.Add(onWarpEngineShutdown);
            WBIWarpEngine.onWarpEngineFlameout.Add(onWarpEngineFlameout);
            WBIWarpEngine.onWarpEngineUnFlameout.Add(onWarpEngineUnFlameout);

            statusDischarging = Localizer.Format("#LOC_BLUESHIFT_resourceVentStatusDischarging");
            statusMissing = Localizer.Format("#LOC_BLUESHIFT_resourceVentStatusMissing");
            statusReady = Localizer.Format("#LOC_BLUESHIFT_resourceVentStatusReady");

            //Animation
            animation = this.part.FindModuleImplementing<ModuleAnimateGeneric>();
            if (animation != null)
            {
                animation.Fields["status"].guiName = Localizer.Format("#LOC_BLUESHIFT_resourceVentEmitterState");
            }

            //Get the inputs
            getInputs();

            //Timekeeping
            if (lastUpdateTime == 0f)
                lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public void Destroy()
        {
            WBIWarpEngine.onWarpEffectsUpdated.Remove(onWarpEffectsUpdated);
            WBIWarpEngine.onWarpEngineStart.Remove(onWarpEngineStart);
            WBIWarpEngine.onWarpEngineShutdown.Remove(onWarpEngineShutdown);
            WBIWarpEngine.onWarpEngineFlameout.Remove(onWarpEngineFlameout);
            WBIWarpEngine.onWarpEngineUnFlameout.Remove(onWarpEngineUnFlameout);
        }
        #endregion

        #region WarpEngine events
        void onWarpEffectsUpdated(Vessel warpShip, WBIWarpEngine warpEngine, float throttle)
        {
            if (part.vessel != warpShip || !canSyncWithWarpEngine)
                return;

            dischargeMultiplier = warpEngine.consumptionMultiplier;
        }

        void onWarpEngineStart(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip || !canSyncWithWarpEngine)
                return;

            dischargeMultiplier = 1f;
        }

        void onWarpEngineShutdown(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip || !canSyncWithWarpEngine)
                return;

            dischargeMultiplier = 1f;
        }

        void onWarpEngineFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip || !canSyncWithWarpEngine)
                return;

            dischargeMultiplier = 1f;
        }

        void onWarpEngineUnFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip || !canSyncWithWarpEngine)
                return;

            dischargeMultiplier = 1f;
        }
        #endregion

        #region Helpers
        string formatConsumedResources(ResourceRatio[] inputs)
        {
            StringBuilder info = new StringBuilder();

            info.Append("Consumes ");
            for (int index = 0; index < inputs.Length; index++)
                info.Append(formatResource(inputs[index].ResourceName, inputs[index].Ratio));
            info.AppendLine(" ");

            string consumedResources = info.ToString().TrimEnd(new char[] { ',' });
            return consumedResources;
        }

        string formatResource(string resourceName, double ratio)
        {
            if (ratio < 0.0001)
                return resourceName + string.Format(": {0:f2}/day", ratio * (double)KSPUtil.dateTimeFormatter.Day);
            else if (ratio < 0.01)
                return resourceName + string.Format(": {0:f2}/hr", ratio * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return resourceName + string.Format(": {0:f2}/sec", ratio);
        }

        string formatRate(double rate)
        {
            if (rate < 0.0001)
                return string.Format("{0:f2}/day", rate * (double)KSPUtil.dateTimeFormatter.Day);
            else if (rate < 0.01)
                return string.Format("{0:f2}/hr", rate * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return string.Format("{0:f2}/sec", rate);
        }

        void getInputs()
        {
            char[] semicolonSeparator = new char[] { ';' };

            //Get the outputs
            if (string.IsNullOrEmpty(dischargedResources))
                return;
            resourcesToDump = dischargedResources.Split(semicolonSeparator);

            //Get inputs
            if (!string.IsNullOrEmpty(landedResourcesRequired))
            {
                Debug.Log("[" + this.ClassName + "] - Adding required resources for landed state");
                landedResourceInputs = getInputs(landedResourcesRequired);
            }
            if (!string.IsNullOrEmpty(spashedResourcesRequired))
            {
                Debug.Log("[" + this.ClassName + "] - Adding required resources for splashed state");
                splashedResourceInputs = getInputs(spashedResourcesRequired);
            }
            if (!string.IsNullOrEmpty(atmosphereResourcesRequired))
            {
                Debug.Log("[" + this.ClassName + "] - Adding required resources for atmosphere state");
                atmoResourceInputs = getInputs(atmosphereResourcesRequired);
            }
            if (!string.IsNullOrEmpty(vacuumResourcesRequired))
            {
                Debug.Log("[" + this.ClassName + "] - Adding required resources for vacuum state");
                vaccResourceInputs = getInputs(vacuumResourcesRequired);
            }
        }

        bool hasResourcesToDump()
        {
            string resourceName;
            double amount, maxAmount;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            for (int index = 0; index < resourcesToDump.Length; index++)
            {
                resourceName = resourcesToDump[index];

                //Get resource definition
                if (definitions.Contains(resourceName))
                    resourceDef = definitions[resourceName];

                //Check resource amount
                this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                if (amount >= 0.0001f)
                    return true;
            }

            return false;
        }

        bool consumeInputs(ResourceRatio[] inputs, double elapsedTime)
        {
            double amountObtained = 0f;
            double demand;
            for (int index = 0; index < inputs.Length; index++)
            {
                demand = inputs[index].Ratio;
                if (elapsedTime >= 1.0f)
                    demand *= elapsedTime;

                amountObtained = this.part.RequestResource(inputs[index].ResourceName, demand);
                if (amountObtained / demand < 0.9999f)
                {
                    PartResourceDefinition resourceDef = null;
                    PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                    resourceDef = definitions[inputs[index].ResourceName];

                    status = statusMissing + resourceDef.displayName;
                    dischargeRate = 0f;
                    return false;
                }
            }
            return true;
        }

        ResourceRatio[] getInputs(string inputResources)
        {
            char[] semicolonSeparator = new char[] { ';' };
            char[] commaSeparator = new char[] { ',' };
            string[] inputs;
            string[] parameters;
            List<ResourceRatio> inputResourceRatios = new List<ResourceRatio>();
            ResourceRatio resourceRatio;
            float amount;

            inputs = inputResources.Split(semicolonSeparator);
            for (int index = 0; index < inputs.Length; index++)
            {
                parameters = inputs[index].Split(commaSeparator);
                if (parameters.Length != 2)
                    continue;

                //index 0: resource name
                resourceRatio = new ResourceRatio();
                resourceRatio.ResourceName = parameters[0];

                //index 1: amount
                if (!float.TryParse(parameters[1], out amount))
                    continue;
                resourceRatio.Ratio = amount;

                inputResourceRatios.Add(resourceRatio);
                Debug.Log("[" + this.ClassName + "] - added input resource: " + resourceRatio.ResourceName + " amount: " + resourceRatio.Ratio);
            }

            return inputResourceRatios.ToArray();
        }
        #endregion
    }
}
