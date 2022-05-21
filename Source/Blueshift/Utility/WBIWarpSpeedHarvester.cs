using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    public class WBIWarpSpeedHarvester: WBIPartModule
    {
        #region ResourceDistribution
        internal class ResourceDistribution
        {
            public string starName;
            public string resourceName;
            public double minAbundance;
            public double maxAbundance;
        }
        #endregion

        #region Constants
        const string kGlobalInterplanetary = "GlobalInterplanetary";
        const string kInterstellar = "Interstellar";
        #endregion

        #region Fields
        [KSPField]
        public bool debugMode = false;

        [KSPField]
        public bool useSpecialistBonus = true;

        [KSPField]
        public float specialistEfficiencyFactor = 0.2f;

        [KSPField]
        public float specialistBonusBase = 0.05f;

        [KSPField]
        public string experienceEffect = "DrillSkill";

        [KSPField]
        public float efficiency = 1f;
        #endregion

        #region Housekeeping
        Dictionary<string, List<ResourceDistribution>> resourceDistributions;
        float engineThrottle = 0f;
        bool isActivated = false;
        WBIWarpEngine warpEngine;
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || !isActivated || warpEngine ==  null || warpEngine.spatialLocation == WBISpatialLocations.Planetary || warpEngine.spatialLocation == WBISpatialLocations.Unknown || engineThrottle <= 0)
                return;

            // Get resource distributions
            List<ResourceDistribution> distributions = null;
            switch (warpEngine.spatialLocation)
            {
                case WBISpatialLocations.Interstellar:
                    if (resourceDistributions.ContainsKey(kInterstellar))
                        distributions = resourceDistributions[kInterstellar];
                    else
                        return;
                    break;

                case WBISpatialLocations.Interplanetary:
                    if (resourceDistributions.ContainsKey(part.vessel.mainBody.name))
                        distributions = resourceDistributions[part.vessel.mainBody.name];
                    else if (resourceDistributions.ContainsKey(kGlobalInterplanetary))
                        distributions = resourceDistributions[kGlobalInterplanetary];
                    else
                        return;
                    break;

                default:
                    break;
            }

            // Compute abundance
            float abundance = 0f;
            double demand = 0f;
            int count = distributions.Count;
            ResourceDistribution distribution = null;
            for (int index = 0; index < count; index++)
            {
                distribution = distributions[index];

                // Calculate abundance.
                abundance = UnityEngine.Random.Range((float)distribution.minAbundance, (float)distribution.maxAbundance);

                // Account for miracle workers
                float crewEfficiency = 1.0f;
                if (useSpecialistBonus)
                {
                    ProtoCrewMember astronaut;
                    int highestRank = BlueshiftScenario.shared.GetHighestRank(part.vessel, experienceEffect, out astronaut);
                    if (highestRank > 0)
                        crewEfficiency = specialistBonusBase + 1.0f * highestRank * specialistEfficiencyFactor;
                }

                // Calculate demand
                demand = abundance * crewEfficiency * warpEngine.warpSpeed;

                // Now generate the resources
                part.RequestResource(distribution.resourceName, -demand);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            WBIWarpEngine.onWarpEffectsUpdated.Add(onWarpEffectsUpdated);
            WBIWarpEngine.onWarpEngineStart.Add(onWarpEngineStart);
            WBIWarpEngine.onWarpEngineShutdown.Add(onWarpEngineShutdown);
            WBIWarpEngine.onWarpEngineFlameout.Add(onWarpEngineFlameout);
            WBIWarpEngine.onWarpEngineUnFlameout.Add(onWarpEngineUnFlameout);

            loadDistributions();
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
            if (part.vessel != warpShip)
                return;

            this.warpEngine = warpEngine;
            engineThrottle = throttle;
            isActivated = true;
        }

        void onWarpEngineStart(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip)
                return;

            this.warpEngine = warpEngine;
            isActivated = true;
        }

        void onWarpEngineShutdown(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip)
                return;

            isActivated = false;
            engineThrottle = 0f;
            this.warpEngine = null;
        }

        void onWarpEngineFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip)
                return;

            isActivated = false;
            engineThrottle = 0f;
            this.warpEngine = null;
        }

        void onWarpEngineUnFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (part.vessel != warpShip)
                return;

            isActivated = true;
            this.warpEngine = warpEngine;
        }
        #endregion

        #region Helpers
        void loadDistributions()
        {
            resourceDistributions = new Dictionary<string, List<ResourceDistribution>>();

            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("INTERSTELLAR_DISTRIBUTION");
            loadDistributions(nodes, kInterstellar);

            nodes = GameDatabase.Instance.GetConfigNodes("GLOBAL_INTERPLANETARY_DISTRIBUTION");
            loadDistributions(nodes, kGlobalInterplanetary);

            nodes = GameDatabase.Instance.GetConfigNodes("INTERPLANETARY_DISTRIBUTION");
            loadDistributions(nodes, string.Empty, true);
        }

        void loadDistributions(ConfigNode[] nodes, string key, bool useStarName = false)
        {
            if (nodes.Length <= 0)
                return;

            ResourceDistribution distribution;
            List<ResourceDistribution> distributions = null;

            for (int index = 0; index < nodes.Length; index++)
            {
                distribution = getDistribution(nodes[index]);
                if (distribution == null)
                    continue;

                if (useStarName && !string.IsNullOrEmpty(distribution.starName))
                    key = distribution.starName;

                if (!resourceDistributions.ContainsKey(key))
                {
                    distributions = new List<ResourceDistribution>();
                    resourceDistributions.Add(key, distributions);
                }

                distributions = resourceDistributions[key];
                distributions.Add(distribution);

                resourceDistributions[key] = distributions;
            }
        }

        ResourceDistribution getDistribution(ConfigNode node)
        {
            ResourceDistribution distribution = new ResourceDistribution();
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            if (!node.HasValue("resourceName") || !node.HasValue("minAbundance") || !node.HasValue("maxAbundance"))
            {
                return null;
            }

            distribution.resourceName = node.GetValue("resourceName");
            if (!definitions.Contains(distribution.resourceName))
                return null;

            if (!double.TryParse(node.GetValue("minAbundance"), out distribution.minAbundance))
                return null;

            if (!double.TryParse(node.GetValue("maxAbundance"), out distribution.maxAbundance))
                return null;

            if (node.HasValue("starName"))
                distribution.starName = node.GetValue("starName");

            return distribution;
        }
        #endregion
    }
}
