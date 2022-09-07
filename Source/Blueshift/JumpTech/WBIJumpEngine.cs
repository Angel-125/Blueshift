using System;
using System.Collections.Generic;
using System.Text;
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

namespace Blueshift.JumpTech
{
    public class WBIJumpEngine: WBIPartModule
    {
        #region Fields
        /// <summary>
        /// A flag to enable/disable debug mode.
        /// </summary>
        [KSPField]
        public bool debugMode = false;

        /// <summary>
        /// This field tells the module which WBIAnimatedTexture to control.
        /// </summary>
        [KSPField]
        public string textureModuleID = string.Empty;

        /// <summary>
        /// Animation to play before playing the portal effect.
        /// </summary>
        [KSPField]
        public string startupAnimation = string.Empty;

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
        /// In seconds, how quickly to throttle up the waterfall effect from 0 to 1.
        /// </summary>
        public float effectSpoolTime = 0.5f;

        /// <summary>
        /// A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
        /// </summary>
        [KSPField(isPersistant = true, guiName = "Effects Throttle")]
        [UI_FloatRange(stepIncrement = 0.01f, maxValue = 1f, minValue = 0f)]
        public float effectsThrottle = 0f;

        /// <summary>
        /// The is the maxium range of the jump engine. Units are in light-years (9460700000000000 meters)
        /// </summary>
        [KSPField]
        public float maxJumpRange = 1f;

        /// <summary>
        /// The amount of time per metric ton of vessel mass that it takes for the engine to cool down before it can make another jump.
        /// Units are in seconds.
        /// </summary>
        [KSPField]
        public float cooldownTime = 3600f;

        /// <summary>
        /// Timestamp of when the cooldown is completed.
        /// </summary>
        [KSPField(isPersistant = true)]
        protected double cooldownEndTime = -1f;
        #endregion

        #region Events
        [KSPEvent(active = true, guiActive = true, guiName = "#LOC_BLUESHIFT_jumpEngineInitiateJump")]
        public void ShowJumpUI()
        {
            if (!canJumpShip())
                return;

            // Show the jump UI

            // WARP!
            displayJumpMessage();

            // Displace the vessel.
            displaceVessel();
        }
        #endregion

        #region Housekeeping
        List<ResourceToll> resourceTolls = null;
        ResourcePriceTiers destinationPriceTier = ResourcePriceTiers.Interstellar;
        WBIAnimatedTexture[] animatedTextures = null;
        WFModuleWaterfallFX waterfallFXModule = null;
        List<string> jumpQuotes = null;
        #endregion

        #region Actions
        /// Displays the jumpgate selector
        /// </summary>
        /// <param name="param">A KSPActionParam containing the action parameters.</param>
        [KSPAction("#LOC_BLUESHIFT_jumpEngineInitiateJump")]
        public void ActionJumpShip(KSPActionParam param)
        {
            ShowJumpUI();
        }
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Game events
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onPartRepaired.Add(onPartRepaired);
                GameEvents.onPartFailure.Add(onPartFailure);
            }

            // Resource tolls
            loadResourceTolls();
            loadJumpQuotes();

            // Init Cooldown
            if (cooldownEndTime < 0)
                cooldownEndTime = Planetarium.GetUniversalTime();

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);
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
        void displaceVessel()
        {
            float jumpDistance = maxJumpRange * (float)BlueshiftScenario.shared.kLightYear;
            Transform refTransform = part.vessel.ReferenceTransform;
            Vector3 warpVector = refTransform.up * jumpDistance;
            Vector3d offsetPosition = refTransform.position + warpVector;

            // Apply translation.
            if (FlightGlobals.VesselsLoaded.Count > 1)
                part.vessel.SetPosition(offsetPosition);
            else
                FloatingOrigin.SetOutOfFrameOffset(offsetPosition);
        }

        void displayJumpMessage()
        {
            // Pick a random jump message to display.
        }

        bool canJumpShip()
        {
            // Has the engine passed the cooldown period?
            double currentTime = Planetarium.GetUniversalTime();
            string message = string.Empty;
            if (cooldownEndTime > 0 && cooldownEndTime < currentTime)
            {
                // Show cooldown required message.
                double secondsRemaining = cooldownEndTime - currentTime;
                message = Localizer.Format("LOC_BLUESHIFT_jumpEngineNeedsCooldown", new string[] { part.partInfo.title, string.Format("{0:n2}", secondsRemaining) });
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            // Does the ship have enough graviolium?
            if (!payJumpToll(out message))
            {
                ScreenMessages.PostScreenMessage(message, 5f, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            return true;
        }

        double getCooldownTime()
        {
            double vesselMass = part.vessel.GetTotalMass();
            return vesselMass * cooldownTime;
        }

        void onPartFailure(Part failedPart)
        {
            if (failedPart != part)
                return;

            effectsThrottle = 0;

            OnUpdate();
        }

        void onPartRepaired(Part repairedPart)
        {
        }

        void loadJumpQuotes()
        {
            jumpQuotes = new List<string>();
            ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("JUMP_MESSAGES");
            ConfigNode node;
            string[] quotes;

            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (!node.HasValue("message"))
                    continue;
                quotes = node.GetValues("message");
                jumpQuotes.AddRange(quotes);
            }
        }

        void loadResourceTolls()
        {
            resourceTolls = new List<ResourceToll>();
            ConfigNode node = getPartConfigNode();
            if (node == null)
                return;
            if (!node.HasNode("RESOURCE_TOLL"))
                return;

            ConfigNode[] nodes = node.GetNodes("RESOURCE_TOLL");
            ResourceToll resourceToll;
            ResourcePriceTiers priceTier = ResourcePriceTiers.Interstellar;
            double amount = 0;
            bool paidByTraveler = true;
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                resourceToll = new ResourceToll();

                if (node.HasValue("name"))
                    resourceToll.name = node.GetValue("name");

                if (node.HasValue("priceTier"))
                {
                    try
                    {
                        resourceToll.priceTier = (ResourcePriceTiers)Enum.Parse(typeof(ResourcePriceTiers), node.GetValue("priceTier"), true);
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                        resourceToll.priceTier = ResourcePriceTiers.Interstellar;
                    }
                }
                else
                {
                    resourceToll.priceTier = ResourcePriceTiers.Interstellar;
                }

                if (node.HasValue("resourceName"))
                    resourceToll.resourceName = node.GetValue("resourceName");
                else
                    resourceToll.resourceName = resHandler.inputResources[0].name;

                if (node.HasValue("amountPerTonne"))
                {
                    if (double.TryParse(node.GetValue("amountPerTonne"), out amount))
                        resourceToll.amountPerTonne = amount;
                    else
                        resourceToll.amountPerTonne = resHandler.inputResources[0].rate;
                }

                if (node.HasValue("paidByTraveler"))
                {
                    if (bool.TryParse(node.GetValue("paidByTraveler"), out paidByTraveler))
                        resourceToll.paidByTraveler = paidByTraveler;
                    else
                        resourceToll.paidByTraveler = true;
                }

                resourceTolls.Add(resourceToll);
            }
        }

        void calculatePriceTier()
        {
            // Price tier depends upon how far the ship is jumping.
            destinationPriceTier = ResourcePriceTiers.Interstellar;
        }

        bool payJumpToll(out string statusMessage)
        {
            statusMessage = "";
            List<ResourceToll> filteredTolls = new List<ResourceToll>();

            calculatePriceTier();

            // Filter the tolls by the destination price tier
            int count = resourceTolls.Count;
            ResourceToll resourceToll;
            for (int index = 0; index < count; index++)
            {
                resourceToll = resourceTolls[index];
                if (resourceToll.priceTier == destinationPriceTier)
                    filteredTolls.Add(resourceToll);
            }

            // Now make sure that the toll can be paid.
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition definition;
            double amount = 0;
            double maxAmount = 0;
            count = filteredTolls.Count;
            float vesselMass = part.vessel.GetTotalMass();
            for (int index = 0; index < count; index++)
            {
                resourceToll = filteredTolls[index];

                if (!definitions.Contains(resourceToll.resourceName))
                    continue;
                definition = definitions[resourceToll.resourceName];

                // Check the amount of resource required
                part.vessel.GetConnectedResourceTotals(definition.id, out amount, out maxAmount);

                // If we don't have enough then we're done.
                if (amount < (resourceToll.amountPerTonne * vesselMass))
                {
                    statusMessage = Localizer.Format("#LOC_BLUESHIFT_jumpGateInsufficentResources") + definition.displayName;
                    return false;
                }
            }

            // We know we have enough, now pay the toll.
            for (int index = 0; index < count; index++)
            {
                resourceToll = filteredTolls[index];

                if (!definitions.Contains(resourceToll.resourceName))
                    continue;
                definition = definitions[resourceToll.resourceName];

                // Check the amount of resource required
                part.vessel.RequestResource(part.vessel.rootPart, definition.id, resourceToll.amountPerTonne * vesselMass, true);
            }

            return true;
        }

        protected void updateTextureModules()
        {
            if (animatedTextures == null)
                return;

            for (int index = 0; index < animatedTextures.Length; index++)
            {
                animatedTextures[index].isActivated = effectsThrottle > 0;
                animatedTextures[index].animationThrottle = effectsThrottle;
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
