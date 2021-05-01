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
        public WBIAnimatedTexture[] animatedTextures = null;

        /// <summary>
        /// Optional (but highly recommended) Waterfall effects module
        /// </summary>
        protected WFModuleWaterfallFX waterfallFXModule = null;
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
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoMaintenance"));
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_warpCapacity", new string[1] { string.Format("{0:n1}", warpCapacity) }));
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoRepairSkill", new string[1] { repairSkill }));
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoRepairRating", new string[1] { minimumSkillLevel.ToString() }));
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_infoKitsRequired", new string[1] { repairKitsRequired.ToString() }));
            info.AppendLine(resHandler.PrintModuleResources());

            return info.ToString();
        }
        #endregion

        #region Events
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
                statusDisplay = Localizer.Format("#LOC_BLUESHIFT_statusOK");
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

        #region API
        /// <summary>
        /// Determines whether or not the warp coil has enough resources to operate.
        /// </summary>
        /// <param name="rateMultiplier">The resource consumption rate multiplier</param>
        /// <returns>True if the vessel has enough resources to power the warp coil, false if not.</returns>
        public bool HasEnoughResources(double rateMultiplier)
        {
            int count = resHandler.inputResources.Count;
            double amount, maxAmount = 0;

            for (int index = 0; index < count; index++)
            {
                vessel.GetConnectedResourceTotals(resHandler.inputResources[index].id, out amount, out maxAmount);

                if (amount < resHandler.inputResources[index].rate * rateMultiplier * TimeWarp.fixedDeltaTime)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Updates the mean time between failures
        /// </summary>
        /// <param name="rateMultiplier">The rate multiplier to reduce the current MTBF by.</param>
        public void UpdateMTBF(double rateMultiplier)
        {
            if (!BlueshiftScenario.maintenanceEnabled)
                return;

            currentMTBF -= (rateMultiplier * TimeWarp.fixedDeltaTime);
            if (currentMTBF <= 0)
            {
                needsMaintenance = true;
                statusDisplay = Localizer.Format("#LOC_BLUESHIFT_needsMaintenance");
                Events["RepairPart"].active = true;
                string message = Localizer.Format("#LOC_BLUESHIFT_partNeedsMaintenance", new string[1] { part.partInfo.title });
                ScreenMessages.PostScreenMessage(message, BlueshiftScenario.messageDuration, ScreenMessageStyle.UPPER_LEFT);
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
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;
            statusDisplay = needsMaintenance ? Localizer.Format("#LOC_BLUESHIFT_needsMaintenance") : Localizer.Format("#LOC_BLUESHIFT_statusOK");
            Events["DebugBreakPart"].active = debugMode;
            Events["DebugBreakPart"].guiName = "Break " + ClassName;
            Events["RepairPart"].active = needsMaintenance;
            Events["RepairPart"].guiName = Localizer.Format("#LOC_BLUESHIFT_repairPart", new string[1] { Localizer.Format("#LOC_BLUESHIFT_warpCoilTitle") });

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);
        }
        #endregion

        #region Helpers
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
