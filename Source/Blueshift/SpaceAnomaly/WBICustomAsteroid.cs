using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// A customized version of ModuleAsteroid to allow for standard asteroid functionality while avoiding the procedural mesh generation.
    /// This is helpful for custom asteroid anomalies like Oumuamua.
    /// </summary>
    public class WBICustomAsteroid: ModuleAsteroid
    {
        #region Fields
        /// <summary>
        /// Flag indicating that a sample of the asteroid has been acquired.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool sampleAcquired;
        #endregion

        #region Housekeeping
        /// <summary>
        /// The science experiment to run.
        /// </summary>
        protected ScienceExperiment scienceExperiment;

        /// <summary>
        /// Tracker for the asteroid's center of mass.
        /// </summary>
        protected FlightCoMTracker flightCoMTracker = null;
        #endregion

        #region Overrides
        /// <summary>
        /// Overrides the start method to avoid generating a procedural asteroid.
        /// 
        /// </summary>
        /// <param name="state"></param>
        public override void OnStart(StartState state)
        {
            // Setup random seed
            if (seed == -1)
            {
                seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                SetAsteroidMass(part.mass);
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Determine unfocused range
            part.vessel.UpdateVesselSize();
            float unfocusedRange = part.vessel.vesselSize.magnitude;
            Events["RenameAsteroidEvent"].unfocusedRange = unfocusedRange;
            Events["RunExperiment"].unfocusedRange = unfocusedRange;
            Events["TargetCoM"].unfocusedRange = unfocusedRange;

            // Hide the base class's experiment sample event. We can't access whatever is used to setup the base class's experiment so we'll make our own.
            Events["TakeSampleEVAEvent"].active = false;

            // Hide the base class's MakeTarget event. We can't access whatever is used to target center of mass so we'll make our own.
            Events["MakeTarget"].active = false;

            // Setup the experiment event. If we've already acquired a sample then we're done.
            Events["RunExperiment"].active = !sampleAcquired;
            if (sampleAcquired)
                return;

            // If we have no experiment ID then we're done.
            if (string.IsNullOrEmpty(sampleExperimentId))
            {
                Events["RunExperiment"].active = false;
                return;
            }

            // Get the experiment. If we can't then we're done.
            scienceExperiment = ResearchAndDevelopment.GetExperiment(sampleExperimentId);
            if (scienceExperiment == null)
            {
                Events["RunExperiment"].active = false;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Replacement event for the asteroid's sample return experiment.
        /// </summary>
        [KSPEvent(externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001850", unfocusedRange = 1f)]
        public void RunExperiment()
        {
            string message = string.Empty;

            // Make sure the experitment is usable.
            if (!ScienceUtil.RequiredUsageExternalAvailable(part.vessel, FlightGlobals.ActiveVessel, (ExperimentUsageReqs)experimentUsageMask, scienceExperiment, ref message))
                return;

            // Make sure the situation is valid.
            ExperimentSituations experimentSituation = ScienceUtil.GetExperimentSituation(this.vessel);
            if (!scienceExperiment.IsAvailableWhile(experimentSituation, part.vessel.mainBody))
                return;

            // Get the biome name and biome display name.
            string biomeName = string.Empty;
            string biomeDisplayName = string.Empty;
            if (scienceExperiment.BiomeIsRelevantWhile(experimentSituation))
            {
                if (!string.IsNullOrEmpty(part.vessel.landedAt))
                {
                    biomeName = Vessel.GetLandedAtString(part.vessel.landedAt);
                    biomeDisplayName = Localizer.Format(part.vessel.displaylandedAt);
                }
                else
                {
                    biomeName = ScienceUtil.GetExperimentBiome(part.vessel.mainBody, part.vessel.latitude, this.vessel.longitude);
                    biomeDisplayName = ScienceUtil.GetBiomedisplayName(part.vessel.mainBody, biomeName);
                }
                if (string.IsNullOrEmpty(biomeDisplayName))
                    biomeDisplayName = biomeName;
            }

            // Run the experiment. If we can't then hide the event.
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(scienceExperiment, ScienceUtil.GetExperimentSituation(part.vessel), part.vessel.mainBody, biomeName, biomeDisplayName);
            if (subject == null)
            {
                // Something went wrong so hide the event.
                Events["RunExperiment"].active = false;
                return;
            }

            float amount = scienceExperiment.baseValue * scienceExperiment.dataScale;
            ScienceData data = new ScienceData(amount, sampleExperimentXmitScalar, 1f, subject.id, subject.title);

            // Find a container that can store the experiment.
            List<ModuleScienceContainer> scienceContainers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            ModuleScienceContainer scienceContainer = null;
            int count = scienceContainers.Count;
            for (int index = 0; index < count; index++)
            {
                if (!scienceContainers[index].HasData(data))
                {
                    scienceContainer = scienceContainers[index];
                    break;
                }
            }
            if (scienceContainer != null)
            {
                scienceContainer.AddData(data);
                scienceContainer.ReviewData();
            }
            else
            {
                // Inform user that we can't find a container that doesn't already have the experiment data.
                message = Localizer.Format("#LOC_BLUESHIFT_cannotStoreAsteroidExperiment", new string[1] { scienceExperiment.experimentTitle });
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_LEFT);
            }
        }

        /// <summary>
        /// Replacement event for ModuleAsteroid's event to target the asteroid's center of mass.
        /// </summary>
        [KSPEvent(externalToEVAOnly = false, guiActive = true, guiActiveUnfocused = true, guiName = "#autoLOC_6001849", unfocusedRange = 500f)]
        public void TargetCoM()
        {
            if (flightCoMTracker == null)
                flightCoMTracker = FlightCoMTracker.Create(this, true);

            flightCoMTracker.MakeTarget();
        }
        #endregion

        #region Helpers
        #endregion
    }
}
