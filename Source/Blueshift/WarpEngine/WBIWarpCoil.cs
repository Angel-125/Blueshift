using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;
using Blueshift.EVARepairs;

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
    /// Warp coils produce the warp capacity needed for vessels to go faster than light. Warp capacity is a fixed resource, but the resources needed to produce it are entirely optional.
    /// 
    /// `
    /// MODULE
    /// {
    ///     name = WBIWarpCoil
    ///     textureModuleID = WarpCoil
    ///     warpCapacity = 10
    ///     RESOURCE
    ///     {
    ///         name = GravityWaves
    ///         rate = 200
    ///         FlowMode = STAGE_PRIORITY_FLOW
    ///     }
    /// }
    /// `
    /// </summary>
    public class WBIWarpCoil: WBIPartModule, IModuleInfo
    {
        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// Warp coils can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
        /// </summary>
        [KSPField]
        public string textureModuleID = string.Empty;

        /// <summary>
        /// Warp coils can play a running effect while the generator is running.
        /// </summary>
        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;

        /// <summary>
        /// The amount of warp capacity that the coil can produce.
        /// </summary>
        [KSPField]
        public float warpCapacity = 1;

        /// <summary>
        /// The activation switch. When not running, the animations won't be animated.
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Warp Coil", guiActive = true)]
        [UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
        public bool isActivated = true;

        /// <summary>
        /// Display string for the warp coil status.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_warpCoilStatus")]
        public string statusDisplay = Localizer.Format("#LOC_BLUESHIFT_statusOK");

        /// <summary>
        /// A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Animation Throttle")]
        [UI_FloatRange(stepIncrement = 0.01f, maxValue = 1f, minValue = 0f)]
        public float animationThrottle = 0f;

        /// <summary>
        /// Warp coils can efficiently move a certain amount of mass to light speed and beyond without penalties.
        /// Going over this limit incurs performance penalties, but staying under this value provides benefits.
        /// The displacement value is rated in metric tons.
        /// </summary>
        [KSPField]
        public float displacementImpulse = 10;

        /// <summary>
        /// Flag to indicate that the part needs maintenance in order to function.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool needsMaintenance = false;
        #endregion

        #region Housekeeping
        public WBIAnimatedTexture[] animatedTextures = null;

        /// <summary>
        /// Optional (but highly recommended) Waterfall effects module
        /// </summary>
        protected WFModuleWaterfallFX waterfallFXModule = null;
        BSModuleEVARepairs evaRepairs = null;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return Localizer.Format("#LOC_BLUESHIFT_warpCoilTitle");
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            StringBuilder info = new StringBuilder();
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_warpCapacity", new string[1] { string.Format("{0:n1}", warpCapacity) }));
            return info.ToString();
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_warpCoilInfoDesc"));
            info.AppendLine(resHandler.PrintModuleResources());

            return info.ToString();
        }
        #endregion

        #region API
        /// <summary>
        /// Updates the MTBF rate multiplier with the new rate.
        /// </summary>
        /// <param name="rateMultiplier">A double containing the new multiplier.</param>
        public void UpdateMTBFRateMultiplier(double rateMultiplier)
        {
            if (evaRepairs == null)
                return;

            evaRepairs.SetRateMultiplier(rateMultiplier);
        }

        /// <summary>
        /// Updates the warp core's EVA Repairs' MTBF, if any.
        /// </summary>
        public void UpdateMTBF(double elapsedTime)
        {
            if (evaRepairs == null)
                return;

            evaRepairs.UpdateMTBF(elapsedTime);
        }

        /// <summary>
        /// Determines whether or not the warp coil has enough resources to operate.
        /// </summary>
        /// <param name="rateMultiplier">The resource consumption rate multiplier</param>
        /// <returns>True if the vessel has enough resources to power the warp coil, false if not.</returns>
        public bool HasEnoughResources(double rateMultiplier)
        {
            int count = resHandler.inputResources.Count;
            double amount, maxAmount, consumptionRate = 0;

            for (int index = 0; index < count; index++)
            {
                vessel.GetConnectedResourceTotals(resHandler.inputResources[index].id, out amount, out maxAmount);
                consumptionRate = resHandler.inputResources[index].rate * rateMultiplier * TimeWarp.fixedDeltaTime;

                if (amount < consumptionRate)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the amount of resource required per second.
        /// </summary>
        /// <param name="resourceName">A string containing the name of the resource.</param>
        /// <returns>A double containing the amount of required resource if it can be found, or 0 if not.</returns>
        public double GetAmountRequired(string resourceName)
        {
            int count = resHandler.inputResources.Count;
            for (int index = 0; index < count; index++)
            {
                if (resHandler.inputResources[index].name == resourceName)
                    return resHandler.inputResources[index].rate;
            }

            return 0;
        }

        public bool ConsumeResources(double rateMultiplier, bool isTimewarping)
        {
            // If we're not timewarping then let the resource handler, uh, handle it.
            if (!isTimewarping)
            {
                string errorStatus = string.Empty;
                int count = resHandler.inputResources.Count;

                resHandler.UpdateModuleResourceInputs(ref errorStatus, rateMultiplier, 0.1, true, true);
                for (int index = 0; index < count; index++)
                {
                    if (!resHandler.inputResources[index].available)
                        return false;
                }

                return true;
            }

            // We are timewarping. Manually consume the resources.
            // We need to manually consume resources to avoid an issue where high timewarp will attempt to pull far more resources than exist.
            else
            {
                int count = resHandler.inputResources.Count;
                double amount = 0;
                double maxAmount = 0;
                double demand = 0;

                for (int index = 0; index < count; index++)
                {
                    // Get the resource's current and max amounts.
                    part.GetConnectedResourceTotals(resHandler.inputResources[index].id, out amount, out maxAmount);

                    // Determine how many units per second we require.
                    demand = resHandler.inputResources[index].rate;

                    // Cap demand to maxAmount if the timewarp-adjusted demand exceeds the maxAmount.
                    if (demand * TimeWarp.fixedDeltaTime > maxAmount)
                        demand = maxAmount;
                    else
                        demand *= TimeWarp.fixedDeltaTime;

                    // Pull the resource if we have enough. Otherwise, we can't consume the resouce and we're done.
                    if (amount >= demand)
                    {
                        part.RequestResource(resHandler.inputResources[index].id, demand);
                    }
                    else
                    {
                        return false;
                    }
                }

                // A-Ok
                return true;
            }
        }
        #endregion

        #region Overrides
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Setup GUI
            debugMode = BlueshiftScenario.debugMode;
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;
            statusDisplay = needsMaintenance ? Localizer.Format("#LOC_BLUESHIFT_needsMaintenance") : Localizer.Format("#LOC_BLUESHIFT_statusOK");

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);

            // Get EVA Repairs module (if any)
            evaRepairs = BSModuleEVARepairs.GetPartModule(part);

            // Game events
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onPartRepaired.Add(onPartRepaired);
                GameEvents.onPartFailure.Add(onPartFailure);
            }
        }

        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onPartRepaired.Remove(onPartRepaired);
                GameEvents.onPartFailure.Remove(onPartFailure);
            }
        }
        #endregion

        #region Helpers
        void onPartFailure(Part failedPart)
        {
            if (failedPart != part)
                return;

            isActivated = false;
            animationThrottle = 0;

            OnUpdate();
        }

        void onPartRepaired(Part repairedPart)
        {
        }

        protected void updateTextureModules()
        {
            if (animatedTextures == null)
                return;

            for (int index = 0; index < animatedTextures.Length; index++)
            {
                animatedTextures[index].isActivated = isActivated;
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
        #endregion
    }
}
