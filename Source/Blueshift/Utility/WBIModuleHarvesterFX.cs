using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

/*
Source code copyright 2021, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Blueshift
{
    /// <summary>
    /// This resource harvester add the ability to drive Effects, animated textures, and Waterfall.
    /// </summary>
    public class WBIModuleHarvesterFX: ModuleResourceHarvester
    {
        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// The module's title/display name.
        /// </summary>
        [KSPField]
        public string moduleTitle = "Resource Harvester";

        /// <summary>
        /// The module's description.
        /// </summary>
        [KSPField]
        public string moduleDescription = "Gathers resources.";

        /// <summary>
        /// The ID of the part module. Since parts can have multiple harvesters, this field helps identify them.
        /// </summary>
        [KSPField]
        public string moduleID = string.Empty;

        /// <summary>
        /// Toggles visibility of the GUI.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// Harvesters can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
        /// </summary>
        [KSPField]
        public string textureModuleID = string.Empty;

        /// <summary>
        /// A throttle control to vary the animation speed of a controlled WBIAnimatedTexture
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Animation Throttle")]
        [UI_FloatRange(stepIncrement = 0.01f, maxValue = 1f, minValue = 0f)]
        public float animationThrottle = 1.0f;

        /// <summary>
        /// Harvesters can play a start effect when the generator is activated.
        /// </summary>
        [KSPField]
        public string startEffect = string.Empty;

        /// <summary>
        /// Harvesters can play a stop effect when the generator is deactivated.
        /// </summary>
        [KSPField]
        public string stopEffect = string.Empty;

        /// <summary>
        /// Harvesters can play a running effect while the generator is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;

        #region Maintenance
        /// <summary>
        /// Flag to indicate that the part needs maintenance in order to function.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool needsMaintenance = false;

        /// <summary>
        /// In hours, how long until the part needs maintenance in order to function. Default is 600.
        /// </summary>
        [KSPField]
        public double mtbf = 600;

        /// <summary>
        /// In seconds, the current time remaining until the part needs maintenance in order to function.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double currentMTBF = 600 * 3600;

        /// <summary>
        /// The skill required to perform repairs. Default is "RepairSkill" (Engineers have this).
        /// </summary>
        [KSPField]
        public string repairSkill = "RepairSkill";

        /// <summary>
        /// The minimum skill level required to perform repairs. Default is 1.
        /// </summary>
        [KSPField]
        public int minimumSkillLevel = 1;

        /// <summary>
        /// The part name that is consumed during repairs. Default is "evaRepairKit" (the stock EVA Repair Kit).
        /// </summary>
        [KSPField]
        public string repairKitName = "evaRepairKit";

        /// <summary>
        /// The number of repair kits required to repair the part. Default is 1.
        /// </summary>
        [KSPField]
        public int repairKitsRequired = 1;
        #endregion

        #endregion

        #region Housekeeping
        WBIAnimatedTexture[] animatedTextures = null;
        WFModuleWaterfallFX waterfallFXModule = null;
        IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;
        string currentBiome = string.Empty;
        List<ResourceRatio> resourceRatios;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return ConverterName;
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return string.Empty;
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            string info = base.GetInfo();

            StringBuilder builder = new StringBuilder();

            info = info.Replace(ConverterName, moduleDescription);
            builder.AppendLine(info);
            builder.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoMaintenance"));
            builder.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoMTB", new string[1] { string.Format("{0:n1}", mtbf) }));
            builder.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoRepairSkill", new string[1] { repairSkill }));
            builder.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoRepairRating", new string[1] { minimumSkillLevel.ToString() }));
            builder.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoKitsRequired", new string[1] { repairKitsRequired.ToString() }));

            return info;
        }
        #endregion

        #region API
        /// <summary>
        /// Performs maintenance on the part.
        /// </summary>
        [KSPEvent(guiName = "#LOC_BLUESHIFT_repairPart", guiActiveUnfocused = true, unfocusedRange = 5)]
        public void RepairPart()
        {
            if (BlueshiftScenario.shared.CanRepairPart(repairSkill, minimumSkillLevel, repairKitName, repairKitsRequired))
            {
                BlueshiftScenario.shared.ConsumeRepairKits(FlightGlobals.ActiveVessel, repairKitName, repairKitsRequired);
                needsMaintenance = false;
                currentMTBF = mtbf * 3600;
                string message = Localizer.Format("#LOC_BLUESHIFT_partRepaired", new string[1] { part.partInfo.title });
                ScreenMessages.PostScreenMessage(message, BlueshiftScenario.messageDuration, ScreenMessageStyle.UPPER_CENTER);
                Events["RepairPart"].active = false;
            }
        }

        /// <summary>
        /// Debug event to break the part.
        /// </summary>
        [KSPEvent(guiName = "(Debug) break part", guiActive = true)]
        public void DebugBreakPart()
        {
            needsMaintenance = false;
            currentMTBF = 1f;
        }
        #endregion

        #region Overrides

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsEditor)
                this.DisableModule();
            else if (HighLogic.LoadedSceneIsFlight)
            {
                this.EnableModule();
                rebuildResourceRatios();
            }

            // Setup GUI
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;
            Events["DebugBreakPart"].active = debugMode;
            Events["DebugBreakPart"].guiName = "Break " + ClassName;
            Events["RepairPart"].active = needsMaintenance;
            Events["RepairPart"].guiName = Localizer.Format("#LOC_BLUESHIFT_repairPart", new string[1] { moduleTitle });
            if (needsMaintenance)
                status = Localizer.Format("#LOC_BLUESHIFT_needsMaintenance");

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();

            this.part.Effect(startEffect, 1.0f);

            updateTextureModules();
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();

            this.part.Effect(runningEffect, 0.0f);
            this.part.Effect(stopEffect, 1.0f);

            updateTextureModules();

            if (waterfallFXModule != null)
            {
                waterfallFXModule.SetControllerValue(waterfallEffectController, 0);
            }
        }

        public override void OnInactive()
        {
            base.OnInactive();
            StopResourceConverter();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!moduleIsEnabled)
                return;
            if (!IsActivated && !AlwaysActive)
            {
                return;
            }

            //Get the current biome
            currentBiome = getCurrentBiomeName(this.part.vessel);

            // Play running effect if needed.
            this.part.Effect(runningEffect, animationThrottle);

            // Update animated textures
            updateTextureModules();

            // Update Waterfall
            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
            {
                waterfallFXModule.SetControllerValue(waterfallEffectController, animationThrottle);
            }
        }

        protected override void LoadRecipe(double harvestRate)
        {
            base.LoadRecipe(harvestRate);
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!IsActivated)
                return;

            //No abundance summary? Then we're done.
            if (resourceRatios.Count == 0)
                return;

            // Check maintenance
            if (BlueshiftScenario.maintenanceEnabled)
            {
                if (needsMaintenance)
                {
                    return;
                }
                else
                {
                    currentMTBF -= TimeWarp.fixedDeltaTime;

                    if (currentMTBF <= 0)
                    {
                        status = Localizer.Format("#LOC_BLUESHIFT_needsMaintenance");
                        Events["RepairPart"].active = true;
                        needsMaintenance = true;
                        string message = Localizer.Format("#LOC_BLUESHIFT_partNeedsMaintenance", new string[1] { part.partInfo.title });
                        ScreenMessages.PostScreenMessage(message, BlueshiftScenario.messageDuration, ScreenMessageStyle.UPPER_LEFT);
                        return;
                    }
                }
            }

            //Set dump excess flag
            ResourceRatio ratio;
            int ratioCount = this.recipe.Outputs.Count;
            for (int index = 0; index < ratioCount; index++)
            {
                ratio = this.recipe.Outputs[index];
                ratio.DumpExcess = true;
                this.recipe.Outputs[index] = ratio;
            }

            //Add our resource ratios to the output list
            ratioCount = resourceRatios.Count;
            double amount;
            double maxAmount;
            PartResourceDefinition resourceDef = null;
            for (int index = 0; index < ratioCount; index++)
            {
                resourceDef = definitionForResource(resourceRatios[index].ResourceName);
                part.vessel.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                if (amount + resourceRatios[index].Ratio > maxAmount)
                    continue;
                this.recipe.Outputs.Add(resourceRatios[index]);
            }
        }
        #endregion

        #region Helpers
        protected void updateTextureModules()
        {
            if (animatedTextures == null)
                return;

            for (int index = 0; index < animatedTextures.Length; index++)
            {
                animatedTextures[index].isActivated = IsActivated;
                animatedTextures[index].animationThrottle = animationThrottle;
            }
        }

        protected WBIAnimatedTexture[] getAnimatedTextureModules()
        {
            List<WBIAnimatedTexture> textureModules = this.part.FindModulesImplementing<WBIAnimatedTexture>();
            List<WBIAnimatedTexture> animationModules = new List<WBIAnimatedTexture>();
            int count = textureModules.Count;

            for (int index = 0; index < count; index++)
            {
                if (textureModules[index].moduleID == textureModuleID)
                    animationModules.Add(textureModules[index]);
            }

            return animationModules.ToArray();
        }

        protected void rebuildResourceRatios()
        {
            PartResourceDefinition resourceDef = null;
            float abundance = 0f;
            float harvestEfficiency;
            ResourceRatio ratio;

            if (!ResourceMap.Instance.IsPlanetScanned(this.part.vessel.mainBody.flightGlobalsIndex) && !ResourceMap.Instance.IsBiomeUnlocked(this.part.vessel.mainBody.flightGlobalsIndex, currentBiome))
                return;

            abundanceCache = getAbundances(this.part.vessel, (HarvestTypes)HarvesterType);

            debugLog("Rebuilding resource ratios... ");
            debugLog("abundanceCache count: " + abundanceCache.ToArray().Length);
            resourceRatios.Clear();
            this.recipe.Outputs.Clear();
            foreach (ResourceCache.AbundanceSummary summary in abundanceCache)
            {
                //Get the resource definition
                debugLog("Getting abundance for " + summary.ResourceName);
                resourceDef = definitionForResource(summary.ResourceName);
                if (resourceDef == null)
                {
                    debugLog("No definition found!");
                    continue;
                }

                //Get the abundance
                abundance = ResourceMap.Instance.GetAbundance(new AbundanceRequest()
                {
                    Altitude = this.vessel.altitude,
                    BodyId = FlightGlobals.currentMainBody.flightGlobalsIndex,
                    CheckForLock = true,
                    Latitude = this.vessel.latitude,
                    Longitude = this.vessel.longitude,
                    ResourceType = (HarvestTypes) HarvesterType,
                    ResourceName = summary.ResourceName
                });
                if (abundance < HarvestThreshold || abundance < 0.001f)
                {
                    debugLog("Abundance is below HarvestThreshold or minimum abundance (0.001)");
                    continue;
                }

                //Now determine the harvest efficiency
                harvestEfficiency = abundance * Efficiency;

                //Setup the resource ratio
                ratio = new ResourceRatio();
                ratio.ResourceName = summary.ResourceName;
                ratio.Ratio = harvestEfficiency;
                ratio.DumpExcess = true;
                ratio.FlowMode = ResourceFlowMode.NULL;

                resourceRatios.Add(ratio);
                debugLog("Added resource ratio for " + summary.ResourceName + " abundance: " + abundance);
            }
            debugLog("Found abundances for " + resourceRatios.Count + " resources");
        }

        protected void debugLog(string message)
        {
            if (debugMode)
                Debug.Log("[" + this.ClassName + "] - " + message);
        }

        protected PartResourceDefinition definitionForResource(string resourceName)
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            if (definitions.Contains(resourceName))
                return definitions[resourceName];

            return null;
        }

        protected CBAttributeMapSO.MapAttribute getCurrentBiome(Vessel vessel)
        {
            CelestialBody celestialBody = vessel.mainBody;
            double lattitude = ResourceUtilities.Deg2Rad(vessel.latitude);
            double longitude = ResourceUtilities.Deg2Rad(vessel.longitude);
            CBAttributeMapSO.MapAttribute biome = ResourceUtilities.GetBiome(lattitude, longitude, FlightGlobals.currentMainBody);

            return biome;
        }

        protected string getCurrentBiomeName(Vessel vessel)
        {
            CBAttributeMapSO.MapAttribute biome = getCurrentBiome(vessel);

            if (biome != null)
                return biome.name;

            switch (vessel.situation)
            {
                case Vessel.Situations.FLYING:
                    return "FLYING";

                case Vessel.Situations.ORBITING:
                case Vessel.Situations.ESCAPING:
                case Vessel.Situations.SUB_ORBITAL:
                    return "ORBITING";

                case Vessel.Situations.SPLASHED:
                    return "SPLASHED";

                default:
                    return "LANDED";
            }
        }

        protected IEnumerable<ResourceCache.AbundanceSummary> getAbundances(Vessel vessel, HarvestTypes harvestType)
        {
            string biomeName = getCurrentBiomeName(vessel);
            int flightGlobalsIndex = vessel.mainBody.flightGlobalsIndex;
            IEnumerable<ResourceCache.AbundanceSummary> abundanceCache;

            //First, try getting from the current biome.
            abundanceCache = ResourceCache.Instance.AbundanceCache.
                Where(a => a.HarvestType == harvestType && a.BodyId == flightGlobalsIndex && a.BiomeName == biomeName);

            //No worky? Try using vessel situation.
            if (abundanceCache.Count() == 0)
            {
                switch (harvestType)
                {
                    case HarvestTypes.Atmospheric:
                        biomeName = "FLYING";
                        break;

                    case HarvestTypes.Oceanic:
                        biomeName = "SPLASHED";
                        break;

                    case HarvestTypes.Exospheric:
                        biomeName = "ORBITING";
                        break;

                    default:
                        biomeName = "Planetary";
                        break;
                }

                //Give it another shot.
                abundanceCache = ResourceCache.Instance.AbundanceCache.
                    Where(a => a.HarvestType == harvestType && a.BodyId == flightGlobalsIndex && a.BiomeName == biomeName);
            }

            return abundanceCache;
        }
        #endregion
    }
}
