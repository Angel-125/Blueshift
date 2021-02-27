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
namespace Blueshift
{
    public class WBIJumpGate: WBIPartModule
    {
        #region Constants
        const float kRendezvousDistance = 50f;
        const float kMessageDuration = 3f;
        string kJumpDimensionsExceeded = "Cannot jump. Vessel's dimensions exceed the JUMPMAX dimensions.";
        string kInsufficientResources = "Cannot jump. Vessel needs more ";
        #endregion

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
        /// Only gates with matching network IDs can connect to each other. Leave blank if the gate connects to any network.
        /// If there are only two gates in the network then there is no need to select the other gate from the list.
        /// You can add additional networks by adding a semicolon character in between network IDs.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string networkID = string.Empty;

        /// <summary>
        /// For paired gates, the address of the gate. This should be set using JUMPGATE_ANOMALY.
        /// Default is an empty address.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string gateAddress = string.Empty;

        /// <summary>
        /// For paired gates, the address of the paired gate. This should be set using JUMPGATE_ANOMALY.
        /// Default is an empty address.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string pairedGateAddress = string.Empty;

        /// <summary>
        /// If the gate has a limited jump range, then only those gates that are in the network and within range can be selected.
        /// The exception is a network of two gates; max range is ignored.
        /// Set to -1 (the default) for unlimited jump range.
        /// Units are in light-years (9460700000000000 meters)
        /// </summary>
        [KSPField]
        public float maxJumpRange = -1f;

        /// <summary>
        /// Maximum width and height of the vessel that the gate can support.
        /// </summary>
        [KSPField]
        public string jumpMaxDimensions = string.Empty;

        /// <summary>
        /// Range at which players can interact with the gate's PAW. Default is 500 meters.
        /// </summary>
        [KSPField]
        public float interactionRange = 500f;

        /// <summary>
        /// Name of the portal trigger transform. The trigger is a collider set to Is Trigger in Unity.
        /// </summary>
        [KSPField]
        public string portalTriggerTransform = string.Empty;

        /// <summary>
        /// Scale curve to use during startup. This should follow the Waterfall effect (if any).
        /// During the startup sequence the Z-axis will be scaled according to this curve. Any vessel or vessel parts caught
        /// by the portal trigger during startup will get vaporized unless "Jumpgates: desctructive startup" in Game Difficulty is disabled.
        /// </summary>
        [KSPField]
        public FloatCurve triggerStartupScaleCurve;

        /// <summary>
        /// Specifies the rendezvous distance. Default is 50 meters away from the gate's vessel transform.
        /// </summary>
        [KSPField]
        public float rendezvousDistance = kRendezvousDistance;
        #endregion

        #region Housekeeping
        WBIAnimatedTexture[] animatedTextures = null;

        /// <summary>
        /// Optional (but highly recommended) Waterfall effects module
        /// </summary>
        WFModuleWaterfallFX waterfallFXModule = null;

        /// <summary>
        /// The ID of the vessel when it was first created.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string vesselID = string.Empty;

        bool isActivated = false;
        Vessel destinationVessel = null;
        Vector2 jumpDimensions = Vector2.zero;
        bool dimensionsExceededMsgShown = false;
        bool insufficientResourcesMsgShown = false;
        List<Vessel> jumpgates;
        JumpgateSelector jumpgateSelector;
        bool playEffects = false;
        Transform portalTrigger = null;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return "Jump Gate";
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return "";
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine("<color=white>Provides instantaneous travel between jump gates.</color>");
            if (resHandler.inputResources.Count > 0)
            {
                info.AppendLine("Use of the gate costs resources.");
                info.AppendLine("Resource costs per tonne of mass:");
                info.AppendLine(resHandler.PrintModuleResources());
            }
            return info.ToString();
        }
        #endregion

        #region Trigger handling
        public void OnTriggerEnter(Collider collider)
        {
            if (collider.attachedRigidbody == null || !collider.CompareTag("Untagged") || destinationVessel == null)
                return;

            //Get the vessel that collided with the trigger
            Part collidedPart = collider.attachedRigidbody.GetComponent<Part>();
            if (collidedPart == null)
                return;
            Vessel vesselToTeleport = collidedPart.vessel;

            // Parts stuck in the startup wash go boom.
            if (!isActivated && playEffects && collidedPart != this.part)
            {
                if (BlueshiftSettings.JumpgateStartupIsDestructive)
                {
                    collidedPart.explosionPotential = 0.1f;
                    collidedPart.explode();
                    if (vesselToTeleport.parts.Count == 0)
                        FlightGlobals.ForceSetActiveVessel(part.vessel);
                }
                return;
            }

            // Make sure that the vessel can fit.
            vesselToTeleport.UpdateVesselSize();
            Vector3 size = vesselToTeleport.vesselSize;
            if (jumpDimensions.magnitude > 0 && (size.x > jumpDimensions.x || size.z > jumpDimensions.y))
            {
                if (!dimensionsExceededMsgShown)
                {
                    dimensionsExceededMsgShown = true;
                    ScreenMessages.PostScreenMessage(kJumpDimensionsExceeded, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                }
                return;
            }

            // Pay the toll if needed. Cost per resources is rate * vessel mass.
            if (resHandler.inputResources.Count > 0 && !CheatOptions.InfinitePropellant)
            {
                float vesselMass = vesselToTeleport.GetTotalMass();
                string errorStatus = string.Empty;
                int count = resHandler.inputResources.Count;
                PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                PartResourceDefinition definition;
                ModuleResource resource;
                double amount = 0;
                double maxAmount = 0;

                // First make sure that the ship can pay the toll.
                for (int index = 0; index < count; index++)
                {
                    resource = resHandler.inputResources[index];
                    if (!definitions.Contains(resource.name))
                        continue;
                    definition = definitions[resource.name];

                    vesselToTeleport.GetConnectedResourceTotals(definition.id, out amount, out maxAmount);
                    if (amount < (resource.rate * vesselMass))
                    {
                        if (!insufficientResourcesMsgShown)
                        {
                            insufficientResourcesMsgShown = true;
                            ScreenMessages.PostScreenMessage(kInsufficientResources + definition.displayName, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                        }
                        return;
                    }
                }

                // Now pay the toll.
                for (int index = 0; index < count; index++)
                {
                    resource = resHandler.inputResources[index];
                    if (!definitions.Contains(resource.name))
                        continue;
                    definition = definitions[resource.name];

                    vesselToTeleport.RequestResource(vesselToTeleport.rootPart, definition.id, resource.rate * vesselMass, true);
                }
            }

            // Good to go.
            dimensionsExceededMsgShown = false;
            insufficientResourcesMsgShown = false;
            Vector3 position = destinationVessel.transform.up.normalized * (rendezvousDistance + size.y);
            FlightGlobals.fetch.SetShipOrbitRendezvous(destinationVessel, position, Vector3d.zero);
        }

        public void OnTriggerExit(Collider collider)
        {
            dimensionsExceededMsgShown = false;
            insufficientResourcesMsgShown = false;
        }
        #endregion

        #region Events
        [KSPEvent(active = true, guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 500, guiName = "Select Destination Gate")]
        public void SelectGate()
        {
            if (jumpgates.Count > 1)
            {
                jumpgateSelector.jumpgates = jumpgates;
                jumpgateSelector.SetVisible(true);
            }
        }

        /// <summary>
        /// Enables/disables the jumpgate.
        /// </summary>
        /// <param name="isEnabled">A flag that sets the gate enabled/disabled.</param>
        public void SetGateEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                if (!isActivated && destinationVessel == null)
                    updateJumpgatePAW();
            }
            else
            {
                Events["SelectGate"].active = false;
            }
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!isActivated && playEffects)
            {
                // Update the effects throttle
                effectsThrottle += (effectSpoolTime * TimeWarp.fixedDeltaTime);

                // Scale the portal trigger
                if (portalTrigger != null && triggerStartupScaleCurve != null)
                {
                    float scale = triggerStartupScaleCurve.Evaluate(effectsThrottle);
                    portalTrigger.localScale = new Vector3(1, 1, scale);
                }

                // See if we're done.
                if (effectsThrottle >= 1)
                {
                    isActivated = true;
                    playEffects = false;
                    effectsThrottle = 1;
                    if (portalTrigger != null)
                        portalTrigger.localScale = Vector3.one;
                }
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Play running effect if needed.
            if (!string.IsNullOrEmpty(runningEffect))
                this.part.Effect(runningEffect, effectsThrottle);

            // Update animated textures
            updateTextureModules();

            // Update Waterfall
            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
            {
                waterfallFXModule.SetControllerValue(waterfallEffectController, effectsThrottle);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            // Setup GUI
            Fields["effectsThrottle"].guiActive = debugMode;
            Fields["effectsThrottle"].guiActiveEditor = debugMode;

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Setup dialog
            jumpgateSelector = new JumpgateSelector();
            jumpgateSelector.gateSelectedDelegate = jumpgateSelected;

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);

            // Get jump dimensions
            if (!string.IsNullOrEmpty(jumpMaxDimensions))
            {
                string[] dimensions = jumpMaxDimensions.Split(new char[] {','});
                float.TryParse(dimensions[0], out jumpDimensions.x);
                float.TryParse(dimensions[1], out jumpDimensions.y);
            }

            // Get portal trigger, if any.
            if (!string.IsNullOrEmpty(portalTriggerTransform))
            {
                portalTrigger = part.FindModelTransform(portalTriggerTransform);
                loadCurve(triggerStartupScaleCurve, "triggerStartupScaleCurve");
            }

            // Setup anomaly-specific parameters if needed.
            setupAnomalyFields();

            // Setup network
            setupJumpNetwork();
        }
        #endregion

        #region Helpers
        private void jumpgateSelected(Vessel destinationGate)
        {
            destinationVessel = destinationGate;

            // Start updating the FX.
            effectsThrottle = 0;
            playEffects = true;
            isActivated = false;
        }

        private void setupJumpNetwork()
        {
            BlueshiftScenario.shared.AddJumpgateToNetwork(vessel.id.ToString(), networkID);

            // Get list of possible destinations within our network.
            jumpgates = BlueshiftScenario.shared.GetDestinationGates(networkID, vessel.GetWorldPos3D(), maxJumpRange);
            if (jumpgates == null)
                jumpgates = new List<Vessel>();

            updateJumpgatePAW();
        }

        private void updateJumpgatePAW()
        {
            if (jumpgates.Count == 1)
            {
                Events["SelectGate"].active = false;
                effectsThrottle = 1.0f;
                isActivated = true;
                destinationVessel = jumpgates[0];
            }

            // Enable the gate selector if we have more than one gate.
            else if (jumpgates.Count > 1)
            {
                Events["SelectGate"].active = true;
                Events["SelectGate"].unfocusedRange = interactionRange;
                isActivated = false;
                effectsThrottle = 0;
                destinationVessel = null;
            }

            // No other gates in the network.
            else
            {
                Events["SelectGate"].active = false;
                isActivated = false;
                effectsThrottle = 0;
                destinationVessel = null;
            }
        }

        private void setupAnomalyFields()
        {
            vesselID = part.vessel.id.ToString();
            WBISpaceAnomaly anomaly = BlueshiftScenario.shared.GetAnomaly(vesselID);
            if (anomaly != null)
            {
                networkID = anomaly.networkID;

                if (anomaly.rendezvousDistance > 0)
                    rendezvousDistance = anomaly.rendezvousDistance;
            }
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
