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
    /// An enhanced version of the stock ModuleGenerator that supports playing effects.
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
        #endregion

        #region Housekeeping
        /// <summary>
        /// Flag indicating whether or not we're missing resources needed to produce outputs.
        /// </summary>
        public bool isMissingResources = false;

        WBIAnimatedTexture[] animatedTextures = null;
        List<ResourceRatio> drainedResources = new List<ResourceRatio>();
        WFModuleWaterfallFX waterfallFXModule = null;
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
            return info;
        }
        #endregion

        #region Overrides
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            ConfigNode configNode = getPartConfigNode();
            if (configNode == null)
                return;

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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (HighLogic.LoadedSceneIsEditor)
                this.DisableModule();
            else if (HighLogic.LoadedSceneIsFlight)
                this.EnableModule();

            // Setup GUI
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;

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
                // Drain any resources that we need to deplete.
                drainResources();
                return;
            }

            // Set animationThrottle
            if (isMissingResources)
            {
                drainResources();
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
            return base.PrepareRecipe(deltatime);
        }
        #endregion

        #region API
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
        protected void drainResources()
        {
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
        #endregion
    }
}
