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
    /// This part module is designed to unlock random nodes in a tech tree. It can also drive Waterfall effects.
    /// </summary>
    public class WBITechUnlock: WBIPartModule
    {
        #region Constants
        const float kMessageDuration = 3.0f;
        const string kUnlockTechNodeMsg = " unlocked.";
        #endregion

        #region Fields
        /// <summary>
        /// Maximum RNG value
        /// </summary>
        public int dieRoll = 100;

        /// <summary>
        /// Target number to unlock a tech tree node
        /// </summary>
        public int unlockTargetNumber = 97;

        /// <summary>
        /// Tech unlock message
        /// </summary>
        public string unlockMessage = "The mesmerizing object offers the gift of knowledge...";

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;

        /// <summary>
        /// A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Animation Throttle")]
        [UI_FloatRange(stepIncrement = 0.01f, maxValue = 1f, minValue = 0f)]
        public float animationThrottle = 1f;

        /// <summary>
        /// Flag to indicate whether or not the part has been visited.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool hasBeenVisited = false;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Optional (but highly recommended) Waterfall effects module
        /// </summary>
        protected WFModuleWaterfallFX waterfallFXModule = null;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);

            // Handle visitation
            if (!hasBeenVisited)
            {
                hasBeenVisited = true;
                unlockTechNode();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Update Waterfall
            if (waterfallFXModule != null)
            {
                waterfallFXModule.SetControllerValue(waterfallEffectController, animationThrottle);
            }
        }
        #endregion

        #region Helpers
        protected virtual void unlockTechNode()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER && HighLogic.CurrentGame.Mode != Game.Modes.SCIENCE_SANDBOX)
            {
                return;
            }

            int unlockRoll = UnityEngine.Random.Range(1, dieRoll);
            if (unlockRoll < unlockTargetNumber)
                return;

            //Get the list of unavailable nodes and their tech IDs
            List<ProtoTechNode> unavailableNodes = AssetBase.RnDTechTree.GetNextUnavailableNodes();
            if (unavailableNodes.Count <= 0)
                return;
            ProtoTechNode node;
            int index = 0;
            Dictionary<string, int> techNodes = new Dictionary<string, int>();
            for (index = 0; index < unavailableNodes.Count; index++)
            {
                if (techNodes.ContainsKey(unavailableNodes[index].techID) == false)
                    techNodes.Add(unavailableNodes[index].techID, index);
            }

            index = UnityEngine.Random.Range(0, unavailableNodes.Count);
            node = unavailableNodes[index];
            ResearchAndDevelopment.Instance.UnlockProtoTechNode(node);
            ResearchAndDevelopment.RefreshTechTreeUI();

            ScreenMessages.PostScreenMessage(unlockMessage, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
            ScreenMessages.PostScreenMessage(ResearchAndDevelopment.GetTechnologyTitle(node.techID) + kUnlockTechNodeMsg, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
        }
        #endregion
    }
}
