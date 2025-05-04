using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// This small part module soaks up the input resource(s)
    /// </summary>
    public class WBIModuleResourceSponge: WBIPartModule, IModuleInfo
    {
        #region Fields
        #endregion

        #region Housekeeping
        /// <summary>
        /// Timestamp of when it was last updated.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double lastUpdateTime = -1;

        ModuleResourceConverter converter;
        Dictionary<string, List<PartResource>> resourceProviders;
        int partCount = -1;
        double timePerCycle = 3600;
        bool flightIsReady = false;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !flightIsReady)
                return;
            
            // Get resources to collect
            getResourceProviders();

            // Soak up the resource(s)
            double elapsedTime = Planetarium.GetUniversalTime() - lastUpdateTime;
            if (elapsedTime <= timePerCycle)
            {
                soakResources(elapsedTime);
            }
            else
            {
                while (elapsedTime > timePerCycle && timePerCycle > 0)
                {
                    soakResources(timePerCycle);
                    elapsedTime -= timePerCycle;
                }
                if (elapsedTime < 0)
                    elapsedTime = Math.Abs(elapsedTime);
                soakResources(elapsedTime);
            }
            lastUpdateTime = Planetarium.GetUniversalTime();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.onFlightReady.Add(onFlightReady);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (lastUpdateTime < 0)
                lastUpdateTime = Planetarium.GetUniversalTime();

            converter = part.FindModuleImplementing<ModuleResourceConverter>();

            resourceProviders = new Dictionary<string, List<PartResource>>();
            getResourceProviders();
        }

        public void OnDestroy()
        {
            GameEvents.onFlightReady.Remove(onFlightReady);
        }
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return Localizer.Format("#LOC_BLUESHIFT_spongeTitle");
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return "";
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_spongeDesc"));
            info.AppendLine(resHandler.PrintModuleResources());

            return info.ToString();
        }
        #endregion

        #region Helpers
        void onFlightReady()
        {
            flightIsReady = true;
        }

        void soakResources(double elapsedTime)
        {
            int count = resHandler.inputResources.Count;
            ModuleResource inputResource;
            List<PartResource> partResources;
            int partResourceCount;
            double amountRequested;
            double adjustedAmount;
            PartResource resource;
            PartResource providerResource;

            for (int index = 0; index < count; index++)
            {
                inputResource = resHandler.inputResources[index];
                amountRequested = inputResource.rate * elapsedTime;

                // Part must have the resource in order to soak.
                if (part.Resources.Contains(inputResource.name) == false)
                    continue;

                // Make sure that we're not full.
                resource = part.Resources[inputResource.name];
                if (resource.amount.Equals(resource.maxAmount) || resource.flowState == false)
                    continue;

                // Go through the list and get as much as we can.
                partResources = resourceProviders[inputResource.name];
                partResourceCount = partResources.Count;
                for (int resourceIndex = 0; resourceIndex < partResourceCount; resourceIndex++)
                {
                    providerResource = partResources[resourceIndex];
                    if (providerResource.amount <= 0)
                        continue;

                    if (providerResource.amount >= amountRequested)
                    {
                        if (resource.amount + amountRequested <= resource.maxAmount)
                        {
                            // We've got enough room to store the resource
                            resource.amount += amountRequested;
                            if (resource.amount >= resource.maxAmount)
                                resource.amount = resource.maxAmount;

                            providerResource.amount -= amountRequested;
                            if (providerResource.amount <= 0)
                                providerResource.amount = 0;
                        }
                        else
                        {
                            // We don't have enough room so store what we can and then skip to the next resource.
                            adjustedAmount = resource.amount + amountRequested - resource.maxAmount;
                            resource.amount = resource.maxAmount;
                            providerResource.amount -= adjustedAmount;
                            if (providerResource.amount <= 0)
                                providerResource.amount = 0;
                            break;
                        }
                    }
                    else
                    {
                        // Provider doesn't have enough so we'll store what we can.
                        adjustedAmount = providerResource.amount;
                        if (resource.amount + adjustedAmount <= resource.maxAmount)
                        {
                            resource.amount += adjustedAmount;
                            if (resource.amount >= resource.maxAmount)
                                resource.amount = resource.maxAmount;

                            providerResource.amount = 0;
                        }
                        else
                        {
                            // We don't have enough room so store what we can and skip to the next resource.
                            adjustedAmount = resource.amount + adjustedAmount - resource.maxAmount;
                            resource.amount = resource.maxAmount;
                            providerResource.amount -= adjustedAmount;
                            if (providerResource.amount <= 0)
                                providerResource.amount = 0;
                            break;
                        }
                    }
                }
            }
        }

        void getResourceProviders()
        {
            if (partCount != part.vessel.parts.Count)
                partCount = part.vessel.parts.Count;
            else
                return;
            Debug.Log("[Blueshift] - getResourceProviders called.");
            // Get the resource names that we soak.
            int inputResourceCount = resHandler.inputResources.Count;
            if (inputResourceCount <= 0)
                return;

            // Create initial providers map
            resourceProviders = new Dictionary<string, List<PartResource>>();
            double cycleTime;
            for (int index = 0; index < inputResourceCount; index++)
            {
                // Calcuate the cycle time for high timewarp
                if (part.Resources.Contains(resHandler.inputResources[index].name))
                {
                    cycleTime = resHandler.inputResources[index].rate * part.Resources[resHandler.inputResources[index].name].amount;
                    if (cycleTime < timePerCycle)
                        timePerCycle = cycleTime;
                }

                resourceProviders.Add(resHandler.inputResources[index].name, new List<PartResource>());
            }

            // Go through the part list and map the resources that we soak.
            PartResource resource;
            Part vesselPart;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                vesselPart = part.vessel.parts[partIndex];

                // Skip the part if it's us or another sponge
                if (vesselPart == this || vesselPart.HasModuleImplementing<WBIModuleResourceSponge>())
                    continue;

                // If the part has a resource that we're interested in then add it to the list.
                for (int index = 0; index < inputResourceCount; index++)
                {
                    if (vesselPart.Resources.Contains(resHandler.inputResources[index].name))
                    {
                        resource = vesselPart.Resources[resHandler.inputResources[index].name];
                        resourceProviders[resource.resourceName].Add(resource);
                    }
                }
            }
        }
        #endregion
    }
}