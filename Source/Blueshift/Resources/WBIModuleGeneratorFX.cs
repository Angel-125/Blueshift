using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
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
    /// An enhanced version of the stock ModuleGenerator that supports playing effects. Supports Waterfall.
    /// </summary>
    public class WBIModuleGeneratorFX : ModuleResourceConverter, IModuleInfo
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
        public string moduleTitle = "Generator";

        /// <summary>
        /// The module's description.
        /// </summary>
        [KSPField]
        public string moduleDescription = "Produces and consumes resources";

        /// <summary>
        /// The ID of the part module. Since parts can have multiple generators, this field helps identify them.
        /// </summary>
        [KSPField]
        public string moduleID = string.Empty;

        /// <summary>
        /// Toggles visibility of the GUI.
        /// </summary>
        [KSPField]
        public bool guiVisible = true;

        /// <summary>
        /// Generators can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
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
        /// Generators can play a start effect when the generator is activated.
        /// </summary>
        [KSPField]
        public string startEffect = string.Empty;

        /// <summary>
        /// Generators can play a stop effect when the generator is deactivated.
        /// </summary>
        [KSPField]
        public string stopEffect = string.Empty;

        /// <summary>
        /// Generators can play a running effect while the generator is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;

        /// <summary>
        /// Name of the group for the UI controls.
        /// </summary>
        [KSPField]
        public string groupName = string.Empty;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Flag indicating whether or not we're missing resources needed to produce outputs.
        /// </summary>
        public bool isMissingResources = false;

        /// <summary>
        /// This flag lets an external part module bypass the converter's run cycle which is triggered by FixedUpdate. When this flag is set to true, then the base class's FixedUpdate won't be called.
        /// Without the base class' FixedUpdate getting called, no resources will be converted. The external part module is expected to call RunGeneratorCycle manually.
        /// This system was put in place to get around timing issues where gravitic generators should produce enough resources for warp coils to consume each time tick, but due to timing issues, 
        /// the resources aren't produced in time for the warp engine to handle resource consumption. To get around that problem, the active warp engine handles resource conversion during its fixed update.
        /// </summary>
        public bool bypassRunCycle = false;

        /// <summary>
        /// Multiplier to adjust consumption of input resources.
        /// </summary>
        public double resourceConsumptionModifier = 1.0f;

        WBIAnimatedTexture[] animatedTextures = null;
        List<ResourceRatio> drainedResources = new List<ResourceRatio>();
        WFModuleWaterfallFX waterfallFXModule = null;
        bool drainedResourceProduced = false;
        string resourcesDrainedHash = string.Empty;
        Dictionary<string, double> baseInputRatios = null;
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
            StringBuilder builder = new StringBuilder();
            ConversionRecipe conversionRecipe = this.LoadRecipe();

            // Description
            builder.Append(this.moduleDescription);

            // Inputs
            buildResourceSection("#autoLOC_259572", builder, conversionRecipe.Inputs);

            // Outputs
            buildResourceSection("#autoLOC_259594", builder, conversionRecipe.Outputs);

            // Required
            buildResourceSection("#autoLOC_259620", builder, conversionRecipe.Requirements);

            // Drained
            loadDrainedResources();
            buildResourceSection("#LOC_BLUESHIFT_moduleGeneratorDrainedResources", builder, drainedResources);

            return builder.ToString();
        }

        private void buildResourceSection(string sectionTitle, StringBuilder builder, List<ResourceRatio> resourceRatios)
        {
            int count = resourceRatios.Count;
            if (count <= 0)
                return;

            PartResourceDefinition definition;
            ResourceRatio resourceRatio;
            string resourceDisplayName;
            double ratio = 0f;
            string unitsDisplay = string.Empty;

            // Section title
            builder.Append(Localizer.Format(sectionTitle));

            for (int index = 0; index < count; index++)
            {
                resourceRatio = resourceRatios[index];

                // Resource name
                definition = PartResourceLibrary.Instance.GetDefinition(resourceRatio.ResourceName.GetHashCode());
                resourceDisplayName = definition != null ? definition.displayName : resourceRatio.ResourceName;
                builder.Append("\n - ");
                builder.Append(resourceDisplayName);
                builder.Append(": ");

                // Ratio
                ratio = resourceRatio.Ratio;
                // Per Day
                if (ratio < 0.0001)
                {
                    unitsDisplay = "#autoLOC_6001057";
                    ratio *= (double)KSPUtil.dateTimeFormatter.Day;
                }
                // Per Hour
                else if (ratio < 0.0275) // Shows up to 99 units per hour
                {
                    unitsDisplay = "#autoLOC_6001058";
                    ratio *= (double)KSPUtil.dateTimeFormatter.Hour;
                }
                // Per Min
                else if (ratio < 1.0f)
                {
                    unitsDisplay = "#LOC_BLUESHIFT_unitsPerMin";
                    ratio *= (double)KSPUtil.dateTimeFormatter.Minute;
                }
                // Per second
                else
                {
                    unitsDisplay = "#autoLOC_6001059";
                }

                // Account for EfficiencyBonus
                ratio *= EfficiencyBonus;

                // Build the string.
                builder.Append(Localizer.Format(unitsDisplay, new string[1] { string.Format("{0:n2}", ratio) }));
            }
        }
        #endregion

        #region Overrides
        public void OnDestroy()
        {
            GameEvents.OnResourceConverterOutput.Remove(onResourceConverterOutput);
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            loadDrainedResources();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.OnResourceConverterOutput.Add(onResourceConverterOutput);
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);

            // Fix for action groups in editor
            if (HighLogic.LoadedSceneIsEditor)
                EnableModule();

            // Setup GUI
            debugMode = BlueshiftScenario.debugMode;
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;
            if (!string.IsNullOrEmpty(groupName))
            {
                Events["StartResourceConverter"].group.name = groupName;
                Events["StartResourceConverter"].group.displayName = groupName;
                Events["StopResourceConverter"].group.name = groupName;
                Events["StopResourceConverter"].group.displayName = groupName;
                Fields["status"].group.name = groupName;
                Fields["status"].group.displayName = groupName;
            }

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);

            int count = outputList.Count;
            for (int index = 0; index < count; index++)
            {
                resourcesDrainedHash += outputList[index].ResourceName;
            }

            baseInputRatios = new Dictionary<string, double>();
            count = inputList.Count;
            for (int index = 0; index < count; index++)
            {
                baseInputRatios.Add(inputList[index].ResourceName, inputList[index].Ratio);
            }
        }

        public override void StartResourceConverter()
        {
            bypassRunCycle = false;
            base.StartResourceConverter();

            this.part.Effect(startEffect, 1.0f);

            updateTextureModules();
        }

        public override void StopResourceConverter()
        {
            bypassRunCycle = false;
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
                // Drain any resources that we need to deplete.
                drainResources();
                drainedResourceProduced = false;
                return;
            }

            // Set animationThrottle
            if (isMissingResources)
            {
                drainResources();
                drainedResourceProduced = false;
                animationThrottle = 0f;
            }

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

        public override void FixedUpdate()
        {
            // Don't do the fixed update if the run cycle has been bypassed.
            if (bypassRunCycle)
                return;

            base.FixedUpdate();
        }

        protected override ConversionRecipe PrepareRecipe(double deltatime)
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return base.PrepareRecipe(deltatime);

            //Check resources
            int count = inputList.Count;
            double currentAmount, maxAmount;
            PartResourceDefinition resourceDefinition;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            for (int index = 0; index < count; index++)
            {
                if (definitions.Contains(inputList[index].ResourceName))
                    resourceDefinition = definitions[inputList[index].ResourceName];
                else
                    continue;

                this.part.vessel.resourcePartSet.GetConnectedResourceTotals(resourceDefinition.id, out currentAmount, out maxAmount, true);

                if (currentAmount <= 0)
                {
                    status = "Missing " + resourceDefinition.displayName;
                    isMissingResources = true;
                    return null;
                }
            }

            isMissingResources = false;
            status = "Nominal";
            ConversionRecipe recipe = base.PrepareRecipe(deltatime);
            if (recipe == null)
                return null;

            // Apply input modifiers to the recipe
            count = recipe.Inputs.Count;
            ResourceRatio recipeInput;
            for (int index = 0; index < count; index++)
            {
                recipeInput = recipe.Inputs[index];

                // Since recipes are reused, we need to compute the ratio based on the original input ratios before factoring in the resourceConsumptionModifier.
                if (baseInputRatios.ContainsKey(recipeInput.ResourceName))
                {
                    recipeInput.Ratio = baseInputRatios[recipeInput.ResourceName] * resourceConsumptionModifier;
                    recipe.Inputs[index] = recipeInput;
                    if (debugMode)
                    {
                        Debug.Log("[WWBIModuleGeneratorFX] - " + recipeInput.ResourceName + " consumes " + recipeInput.Ratio);
                    }
                }
            }

            return recipe;
        }
        #endregion

        #region API
        /// <summary>
        /// This is a helper function to avoid issues where a warp engine needs a certain amount of resources in order to operate, the system should have them,
        /// but due to timing in the game, the resources aren't produced when they should be.
        /// </summary>
        public void RunGeneratorCycle()
        {
            base.FixedUpdate();
        }

        /// <summary>
        /// Returns the amount of the supplied resource that is produced per second.
        /// </summary>
        /// <param name="resourceName">A string containing the name of the resource to look for.</param>
        /// <returns>A double containing the amount of the resource produced, or 0 if the resource can't be found.</returns>
        public double GetAmountProduced(string resourceName)
        {
            int count = outputList.Count;

            for (int index = 0; index < count; index++)
            {
                if (outputList[index].ResourceName == resourceName)
                    return outputList[index].Ratio;
            }

            return 0;
        }
        #endregion

        #region Helpers

        private void onResourceConverterOutput(PartModule converter, string resourceName, double amount)
        {
            int count = drainedResources.Count;
            for (int index = 0; index < count; index++)
            {
                if (drainedResources[index].ResourceName == resourceName)
                {
                    drainedResourceProduced = true;
                    return;
                }
            }
        }

        protected void drainResources()
        {
            if (drainedResourceProduced)
                return;
            int count = drainedResources.Count;
            ResourceRatio resource;

            for (int index = 0; index < count; index++)
            {
                resource = drainedResources[index];

                this.part.RequestResource(resource.ResourceName, resource.Ratio, resource.FlowMode);
            }
        }

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

        private ConfigNode getPartConfigNode()
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (this.part.partInfo.partConfig == null)
                return null;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        partConfigNode = node;
                        break;
                    }
                }
            }

            return partConfigNode;
        }

        private void onGameSettingsApplied()
        {
            debugMode = BlueshiftSettings.DebugModeEnabled;

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(part);
        }

        private void loadDrainedResources()
        {
            if (drainedResources == null)
                drainedResources = new List<ResourceRatio>();
            else if (drainedResources.Count > 0)
            {
                if (debugMode)
                    Debug.Log("[WBIModuleGeneratorFX] - " + ConverterName + " has no drained resources.");
                return;
            }

            ConfigNode configNode = getPartConfigNode();
            if (configNode == null)
            {
                if (debugMode)
                {
                    if (part.partInfo != null)
                        Debug.Log("[WBIModuleGeneratorFX] - For part " + part.partInfo.name);
                    else if (!string.IsNullOrEmpty(part.partName))
                        Debug.Log("[WBIModuleGeneratorFX] - For part " + part.partName);
                    Debug.Log("[WBIModuleGeneratorFX] - Cannot find part config node for " + ConverterName);
                }
                return;
            }

            if (configNode.HasNode("DRAINED_RESOURCE"))
            {
                ConfigNode[] nodes = configNode.GetNodes("DRAINED_RESOURCE");
                double ratio = 0;
                bool dumpExcess = true;
                ResourceFlowMode flowMode = ResourceFlowMode.ALL_VESSEL;
                for (int index = 0; index < nodes.Length; index++)
                {
                    configNode = nodes[index];
                    if (!configNode.HasValue("ResourceName") && !configNode.HasValue("Ratio"))
                        continue;

                    ratio = 0;
                    dumpExcess = true;
                    double.TryParse(configNode.GetValue("Ratio"), out ratio);
                    bool.TryParse(configNode.GetValue("DumpExcess"), out dumpExcess);

                    ResourceRatio resource = new ResourceRatio(configNode.GetValue("ResourceName"), ratio, dumpExcess);

                    flowMode = ResourceFlowMode.ALL_VESSEL;
                    if (configNode.HasValue("FlowMode"))
                    {
                        flowMode = (ResourceFlowMode)Enum.Parse(typeof(ResourceFlowMode), configNode.GetValue("FlowMode"));
                        resource.FlowMode = flowMode;
                    }
                    else
                    {
                        resource.FlowMode = ResourceFlowMode.ALL_VESSEL;
                    }

                    drainedResources.Add(resource);
                }
            }
        }
        #endregion
    }
}
