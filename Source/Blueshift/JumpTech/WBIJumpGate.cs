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
    #region Resource Toll
    public enum ResourcePriceTiers
    {
        /// <summary>
        /// Planetary price tier
        /// </summary>
        Planetary,

        /// <summary>
        /// Interplanetary price tier
        /// </summary>
        Interplanetary,

        /// <summary>
        /// Interstellar price tier
        /// </summary>
        Interstellar
    }

    /// <summary>
    /// Defines a resource that must be paid in order to reach the desired destination. If defined, then the default mechanics are overridden.
    /// </summary>
    public struct ResourceToll
    {
        /// <summary>
        /// Name of the resource toll.
        /// </summary>
        public string name;

        /// <summary>
        /// Price tier- one of: planetary, interplanetary, interstellar
        /// </summary>
        public ResourcePriceTiers priceTier;

        /// <summary>
        /// Name of the resource required to pay the jump toll.
        /// </summary>
        public string resourceName;

        /// <summary>
        /// Amount of resource per metric tonne mass of the traveler
        /// </summary>
        public double amountPerTonne;

        /// <summary>
        /// Resource is paid by the traveler that is initiating the jump
        /// </summary>
        public bool paidByTraveler;
    }
    #endregion

    public class WBIJumpGate: WBIPartModule
    {
        #region Constants
        const float kMessageDuration = 3f;
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
        /// Since KSP's vessel measurements are so wacked when in flight, we'll use a maximum jump mass instead.
        /// Set to -1 (the default value) for unlimited mass.
        /// </summary>
        [KSPField]
        public double jumpMaxMass = -1f;

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
        /// Specifies the rendezvous distance. Default is 50 meters away from the gate's vessel transform. Set to -1 (the default) to use the value from Blueshift settings.
        /// </summary>
        [KSPField]
        public float rendezvousDistance = -1f;

        /// <summary>
        /// Flag to automatically activate the jumpgate. It requires two gates in the network.
        /// </summary>
        [KSPField]
        public bool autoActivate = false;

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
        bool errorMessageShown = false;
        List<Vessel> jumpgates;
        JumpgateSelector jumpgateSelector;
        bool playEffects = false;
        Transform portalTrigger = null;
        List<ResourceToll> resourceTolls = null;
        ResourcePriceTiers destinationPriceTier = ResourcePriceTiers.Interstellar;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return Localizer.Format("#LOC_BLUESHIFT_jumpGateStatus");
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

            info.AppendLine("");
            if (resHandler.inputResources.Count > 0)
            {
                info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_jumpGateDesc1"));
                info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_jumpGateDesc2"));
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

            // Make sure that the vessel can fit. Originally I intended to use dimensions but that didn't work out. Vessel.UpdateVesselSize()
            // Nuses ShipConstruction.CalculateCraftSize, which has DIFFERENT sizes in editor vs flight. Thus, we can't trust the in-flight values.
            // It appears to be related to the part transform's relative position.
            float vesselToTeleportMass = vesselToTeleport.GetTotalMass();
            if (jumpMaxMass > 0 && vesselToTeleportMass > jumpMaxMass)
            {
                if (!errorMessageShown)
                {
                    errorMessageShown = true;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_jumpGateMassToMuch", new string[] { string.Format("{0:n2", vesselToTeleportMass), string.Format("{0:n2", jumpMaxMass) }), kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                }
                return;
            }

            // Pay the jump toll.
            string statusMessage = string.Empty;
            if (!payJumpToll(vesselToTeleport, out statusMessage))
            {
                if (!errorMessageShown)
                {
                    errorMessageShown = true;
                    ScreenMessages.PostScreenMessage(statusMessage, kMessageDuration, ScreenMessageStyle.UPPER_CENTER);
                }
                return;
            }

            // Good to go.
            errorMessageShown = false;

            // Save the source and destination gates. We need this so we can set focus back to the source and jump another vessel.
            BlueshiftScenario.shared.jumpGateSourceId = part.vessel.id.ToString();
            BlueshiftScenario.shared.destinationGateId = destinationVessel.id.ToString();

            // If the destination is in space then rendezvous with its orbit. Otherwise, land next to it.
            // We can use size here because, while inaccurate, it is overestimated, and we just want to get in the vicinity
            vesselToTeleport.UpdateVesselSize();
            Vector3 size = vesselToTeleport.vesselSize;
            double distance = Math.Round(Mathf.Max(size.x, size.y, size.z), 2) / 2 + 5.0;
            if (BlueshiftScenario.shared.IsInSpace(destinationVessel))
            {
                Vector3 position = destinationVessel.transform.up.normalized * (rendezvousDistance + (float)distance);
                FlightGlobals.fetch.SetShipOrbitRendezvous(destinationVessel, position, Vector3d.zero);
            }

            // Land the vessel next to the jumpgate.
            else if (BlueshiftScenario.shared.IsLandedOrSplashed(destinationVessel))
            {
                // Get destination's location.
                double inclination = destinationVessel.srfRelRotation.Pitch();
                double heading = destinationVessel.srfRelRotation.Yaw();
                Vector3d worldPos = destinationVessel.GetWorldPos3D();

                // Now, calculate how far to offset the teleporting vessel relative to the destination
                Transform refTransform = destinationVessel.ReferenceTransform;
                Vector3 translateVector = refTransform.up * (float)distance;
                Vector3d offsetPosition = worldPos + translateVector;

                // Get the offset latitude and longitute
                Vector2d latLong = destinationVessel.mainBody.GetLatitudeAndLongitude(offsetPosition);
                double latitude = latLong.x;
                double longitude = latLong.y;

                // Off we go
                FlightGlobals.fetch.SetVesselPosition(destinationVessel.mainBody.flightGlobalsIndex, latitude, longitude, distance, inclination, heading, true, true);
                FloatingOrigin.ResetTerrainShaderOffset();
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            errorMessageShown = false;
        }
        #endregion

        #region Events
        [KSPEvent(active = true, guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 500, guiName = "#LOC_BLUESHIFT_jumpGateSelectGate")]
        public void SelectGate()
        {
            setupJumpNetwork();
            if (jumpgates.Count >= 1)
            {
                jumpgateSelector.jumpgates = jumpgates;
                jumpgateSelector.SetVisible(true);
            }
        }

        [KSPEvent(active = true, guiActive = true, guiActiveUncommand = true, guiActiveUnfocused = true, externalToEVAOnly = false, unfocusedRange = 500, guiName = "#LOC_BLUESHIFT_jumpGateSwitchToSource")]
        public void SwitchToSource()
        {
            Guid guid = new Guid(BlueshiftScenario.shared.jumpGateSourceId);
            Vessel sourceGate = FlightGlobals.FindVessel(guid);

            BlueshiftScenario.shared.jumpGateSourceId = string.Empty;
            if (sourceGate != null)
                FlightGlobals.ForceSetActiveVessel(sourceGate);
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
                effectsThrottle = 0;
                destinationVessel = null;
                isActivated = false;
            }
        }
        #endregion

        #region Overrides
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onPartRepaired.Remove(onPartRepaired);
                GameEvents.onPartFailure.Remove(onPartFailure);
            }
        }

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
            debugMode = BlueshiftScenario.debugMode;
            Fields["effectsThrottle"].guiActive = debugMode;
            Fields["effectsThrottle"].guiActiveEditor = debugMode;

            // Enable event to return back to source gate.
            if (HighLogic.LoadedSceneIsFlight)
            {
                string vesselId = part.vessel.id.ToString();
                string jumpGateSourceId = BlueshiftScenario.shared.jumpGateSourceId;
                string destinationGateId = BlueshiftScenario.shared.destinationGateId;
                if (!string.IsNullOrEmpty(jumpGateSourceId) && !string.IsNullOrEmpty(destinationGateId) && destinationGateId == vesselId)
                {
                    Guid guid = new Guid(BlueshiftScenario.shared.jumpGateSourceId);
                    Vessel sourceGate = FlightGlobals.FindVessel(guid);
                    if (sourceGate != null)
                    {
                        Events["SwitchToSource"].active = true;
                        Events["SwitchToSource"].guiName = Localizer.Format("#LOC_BLUESHIFT_jumpGateSwitchToSource") + " " + sourceGate.vesselName;
                    }
                    else
                    {
                        Events["SwitchToSource"].active = false;
                    }
                }
                else
                {
                    Events["SwitchToSource"].active = false;
                }
            }

            // Get portal trigger, if any.
            if (!string.IsNullOrEmpty(portalTriggerTransform))
            {
                portalTrigger = part.FindModelTransform(portalTriggerTransform);
                loadCurve(triggerStartupScaleCurve, "triggerStartupScaleCurve");
                if (HighLogic.LoadedSceneIsEditor)
                {
                    disablePortalTrigger();
                }
            }

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Setup dialog
            jumpgateSelector = new JumpgateSelector();
            jumpgateSelector.gateSelectedDelegate = jumpgateSelected;

            // Get animated textures
            animatedTextures = getAnimatedTextureModules();

            // Get Waterfall module (if any)
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);

            // Setup anomaly-specific parameters if needed.
            setupAnomalyFields();

            // Setup network
            setupJumpNetwork();

            // Game events
            if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onPartRepaired.Add(onPartRepaired);
                GameEvents.onPartFailure.Add(onPartFailure);
            }

            // Rendezvous distance
            if (rendezvousDistance < 0)
                rendezvousDistance = BlueshiftScenario.rendezvousDistance;

            // Resource tolls
            loadResourceTolls();
        }
        #endregion

        #region Helpers
        bool payJumpToll(Vessel vesselToTeleport, out string statusMessage)
        {
            // If we have a resource toll then use that instead of the standard toll.
            if (resourceTolls.Count > 0)
                return payTieredJumpToll(vesselToTeleport, out statusMessage);

            // Pay standard jump toll
            else
                return payStandardJumpToll(vesselToTeleport, out statusMessage);
        }

        bool payTieredJumpToll(Vessel vesselToTeleport, out string statusMessage)
        {
            statusMessage = "";
            List<ResourceToll> filteredTolls = new List<ResourceToll>();

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
            float vesselMass = vesselToTeleport.GetTotalMass();
            for (int index = 0; index < count; index++)
            {
                resourceToll = filteredTolls[index];

                if (!definitions.Contains(resourceToll.resourceName))
                    continue;
                definition = definitions[resourceToll.resourceName];

                // Check the amount of resource required
                if (resourceToll.paidByTraveler)
                    vesselToTeleport.GetConnectedResourceTotals(definition.id, out amount, out maxAmount);
                else
                    part.GetConnectedResourceTotals(definition.id, out amount, out maxAmount);

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
                if (resourceToll.paidByTraveler)
                    vesselToTeleport.RequestResource(vesselToTeleport.rootPart, definition.id, resourceToll.amountPerTonne * vesselMass, true);
                else
                    part.vessel.RequestResource(part.vessel.rootPart, definition.id, resourceToll.amountPerTonne * vesselMass, true);
            }

            return true;
        }

        bool payStandardJumpToll(Vessel vesselToTeleport, out string statusMessage)
        {
            statusMessage = "";

            // Pay the toll if needed. Cost per resources is rate * vessel mass.
            if (resHandler.inputResources.Count > 0 && !CheatOptions.InfinitePropellant)
            {
                float vesselMass = vesselToTeleport.GetTotalMass();
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
                        statusMessage = Localizer.Format("#LOC_BLUESHIFT_jumpGateInsufficentResources") + definition.displayName;
                        return false;
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
            return true;
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
                    catch(Exception ex)
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

        void onPartFailure(Part failedPart)
        {
            if (failedPart != part)
                return;

            isActivated = false;
            effectsThrottle = 0;

            OnUpdate();
        }

        void onPartRepaired(Part repairedPart)
        {
        }

        private void disablePortalTrigger()
        {
            portalTrigger.gameObject.SetActive(false);
            Collider collider = portalTrigger.gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }

        private void jumpgateSelected(Vessel destinationGate)
        {
            destinationVessel = destinationGate;

            // Start updating the FX.
            effectsThrottle = 0;
            playEffects = true;
            isActivated = false;

            // Determine destination's price tier
            CelestialBody sourceGateBody = part.vessel.mainBody;
            CelestialBody sourceGateParentBody = part.vessel.mainBody.referenceBody;
            CelestialBody destinationGateBody = destinationGate.mainBody;
            CelestialBody destinationGateParentBody = destinationGate.mainBody.referenceBody;
            CelestialBody sourceGateStar = BlueshiftScenario.shared.GetParentStar(sourceGateBody);
            CelestialBody destinationGateStar = BlueshiftScenario.shared.GetParentStar(destinationGateBody);
            bool sourceGateBodyisAStar = BlueshiftScenario.shared.IsAStar(sourceGateBody);
            bool sourceGateParentBodyisAStar = BlueshiftScenario.shared.IsAStar(sourceGateParentBody);
            bool destinationGateBodyIsAStar = BlueshiftScenario.shared.IsAStar(destinationGateBody);
            bool destinationGateParentBodyIsAStar = BlueshiftScenario.shared.IsAStar(destinationGateParentBody);

            // Source gate's body is a planet.
            if (!sourceGateBodyisAStar)
            {
                // Same planet
                if (sourceGateBody == destinationGateBody)
                    destinationPriceTier = ResourcePriceTiers.Planetary;

                // If the destination gate's body is a moon of the source, or vice-versa, then we're planetary.
                else if (destinationGateParentBody == sourceGateBody || (sourceGateParentBody == destinationGateBody && !destinationGateBodyIsAStar))
                    destinationPriceTier = ResourcePriceTiers.Planetary;

                // If the destination gate's parent body is the same as the source gate's parent body, then we're planetary.
                else if (sourceGateParentBody == destinationGateParentBody && !sourceGateParentBodyisAStar)
                    destinationPriceTier = ResourcePriceTiers.Planetary;

                // If the destination gate is in the same solar system then we're interplanetary.
                else if ((sourceGateStar == destinationGateStar && sourceGateStar != null) || sourceGateStar == destinationGateBody)
                    destinationPriceTier = ResourcePriceTiers.Interplanetary;

                // We're interstellar
                else
                    destinationPriceTier = ResourcePriceTiers.Interstellar;
            }

            // Source gate body is a star.
            else
            {
                // If the source and destination bodys are the same then we're interplanetary.
                if (sourceGateBody == destinationGateBody)
                    destinationPriceTier = ResourcePriceTiers.Interplanetary;

                // If the destination gate's parent star is the source gate's star, then we're interplanetary.
                else if (destinationGateStar == sourceGateBody)
                    destinationPriceTier = ResourcePriceTiers.Interplanetary;

                else
                    destinationPriceTier = ResourcePriceTiers.Interstellar;
            }
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
            if (jumpgates.Count == 1 && !isActivated && autoActivate)
            {
                Events["SelectGate"].active = false;
                effectsThrottle = 1.0f;
                isActivated = true;
                destinationVessel = jumpgates[0];
            }

            // Enable the gate selector if we have more than one gate.
            else if (jumpgates.Count >= 1)
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
