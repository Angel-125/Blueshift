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
        public int unlockTargetNumber = 1;

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
            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
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

            // Get our config node
            ConfigNode partNode = getPartConfigNode();
            string[] unlockedNodes = new string[0];
            ProtoTechNode techNode = null;

            // Get the list of unlocked tech nodes (if any).
            if (partNode != null && partNode.HasValue("unlockedTechNode"))
            {
                unlockedNodes = partNode.GetValues("unlockedTechNode");

                // Cull nodes that have already been unlocked.
                List<string> candidates = new List<string>();
                for (int index = 0; index < unlockedNodes.Length; index++)
                {
                    techNode = AssetBase.RnDTechTree.FindTech(unlockedNodes[index]);
                    if (techNode == null || techNode.state == RDTech.State.Available)
                        continue;

                    candidates.Add(unlockedNodes[index]);
                }
                unlockedNodes = candidates.ToArray();
            }
            else
            {
                // No tech nodes to specifically unlock. Make a random roll to see if we should unlock a random tech node.
                int unlockRoll = UnityEngine.Random.Range(1, dieRoll);
                if (unlockRoll < unlockTargetNumber)
                    return;
            }

            // If we have a specific list of nodes to unlock, then do so now.
            ProtoTechNode node;
            if (unlockedNodes.Length > 0)
            {
                ScreenMessages.PostScreenMessage(unlockMessage, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);

                for (int nodeIndex = 0; nodeIndex < unlockedNodes.Length; nodeIndex++)
                {
                    techNode = AssetBase.RnDTechTree.FindTech(unlockedNodes[nodeIndex]);
                    unlockTechNode(techNode);
                }
            }
            else
            {
                //Get the list of unavailable nodes and their tech IDs
                ProtoTechNode[] unlockCandidates = getTechUnlockCandidates();
                if (unlockCandidates.Length <= 0)
                    return;

                ScreenMessages.PostScreenMessage(unlockMessage, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);

                // Unlock a random node.
                int index = UnityEngine.Random.Range(0, unlockedNodes.Length);
                node = unlockCandidates[index];
                unlockTechNode(node);
            }
        }

        protected void unlockTechNode(ProtoTechNode node)
        {
            ResearchAndDevelopment.Instance.UnlockProtoTechNode(node);
            ResearchAndDevelopment.RefreshTechTreeUI();

            ScreenMessages.PostScreenMessage(ResearchAndDevelopment.GetTechnologyTitle(node.techID) + kUnlockTechNodeMsg, kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
        }

        ProtoTechNode[] getTechUnlockCandidates()
        {
            ProtoTechNode[] unavailableNodes = AssetBase.RnDTechTree.GetTreeTechs();
            Dictionary<string, ProtoTechNode> techNodesMap = new Dictionary<string, ProtoTechNode>();
            for (int index = 0; index < unavailableNodes.Length; index++)
            {
                if (unavailableNodes[index].state == RDTech.State.Unavailable)
                    techNodesMap.Add(unavailableNodes[index].techID, unavailableNodes[index]);
            }

            // Get the parts list.
            List<ProtoTechNode> unlockCandidates = new List<ProtoTechNode>();
            List<AvailablePart> partsList = null;
            if (PartLoader.Instance)
                partsList = PartLoader.LoadedPartsList;
            if (partsList == null)
                return unavailableNodes;

            int count = partsList.Count;
            AvailablePart availablePart;
            for (int index = 0; index < count; index++)
            {
                // Get the available part
                availablePart = partsList[index];

                // Skip if the part is hidden
                if (availablePart.TechHidden)
                    continue;

                // Skip if the tech required isn't in our map.
                if (!(techNodesMap.ContainsKey(availablePart.TechRequired)))
                    continue;

                // If we haven't already added the tech node to our list, add it now.
                if (!unlockCandidates.Contains(techNodesMap[availablePart.TechRequired]))
                    unlockCandidates.Add(techNodesMap[availablePart.TechRequired]);
            }

            return unlockCandidates.ToArray();
        }
        #endregion
    }
}
