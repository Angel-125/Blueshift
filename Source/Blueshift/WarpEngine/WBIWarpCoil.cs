using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

/*
Source code copyright 2020, by Michael Billard (Angel-125)
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

        #endregion

        #region Housekeeping
        public WBIAnimatedTexture[] animatedTextures = null;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return "Warp Coil";
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return string.Format("<b>Warp Capacity:</b> {0:n1}", warpCapacity);
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("<color=white>Generates warp capacity for faster than light travel.</color>");
            info.AppendLine(" ");
            info.AppendLine(string.Format("<color=white><b>Warp Capacity:</b> {0:n1}</color>", warpCapacity));
            info.AppendLine(resHandler.PrintModuleResources());

            return info.ToString();
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
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Setup GUI
            Fields["animationThrottle"].guiActive = debugMode;
            Fields["animationThrottle"].guiActiveEditor = debugMode;

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();
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
