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
    /// <summary>
    /// Circularization states for auto-circularization.
    /// </summary>
    public enum WBICircularizationStates
    {
        /// <summary>
        /// Don't circularize.
        /// </summary>
        doNotCircularize,

        /// <summary>
        /// Orbit needs to be circularized.
        /// </summary>
        needsCircularization,

        /// <summary>
        /// Orbit has been circularized
        /// </summary>
        hasBeenCircularized,

        /// <summary>
        /// Orbit can be circularized.
        /// </summary>
        canBeCircularized
    }

    #region Summary
    /// <summary>
    /// The Warp Engine is designed to propel a vessel faster than light. It requires WarpCapacity That is produced by WBIWarpCoil part modules. 
    /// 
    /// `
    /// MODULE
    /// {
    ///     name = WBIWarpEngine
    ///     ...Standard engine parameters here...
    ///     moduleDescription = Enables fater than light travel.
    ///     bowShockTransformName = bowShock
    ///     minPlanetaryRadius = 3.0
    ///     displacementImpulse = 100
    ///     
    ///     planetarySOISpeedCurve
    ///     {
    ///         key = 1 0.1
    ///         ...
    ///         key = 0.1 0.005
    ///     }
    ///     
    ///     warpCurve
    ///     {
    ///         key = 1 0
    ///         key = 10 1
    ///         ...
    ///         key = 1440 10
    ///     }
    ///     
    ///     waterfallEffectController = warpEffectController
    ///     waterfallWarpEffectsCurve
    ///     {
    ///         key = 0 0
    ///         ...
    ///         key = 1.5 1
    ///     }
    ///     
    ///     textureModuleID = WarpCore
    /// }
    /// `
    /// </summary>
    #endregion
    [KSPModule("Warp Engine")]
    public class WBIWarpEngine : ModuleEnginesFX
    {
        #region constants
        float kLightSpeed = 299792458;
        // How close do you have to be to a targeted vessel before you can rendezvous with it during auto-circularization.
        float kMinRendezvousDistance = 10000;
        // How close to the targed vessel should you end up at when you rendezvous with it during auto-circularization.
        float kRendezvousDistance = 100;
        float kMessageDuration = 3f;
        string kNeedsSpaceflight = "Needs to be in space";
        string kNeedsAltitude = "Needs higher altitude";
        string kNeedsWarpCapacity = "Needs more warp capacity";
        string kAddWarpCapacity = "Add warp capacity: {0:n2}/{1:n2}";
        string kCanGoFTL = "Can exceed light speed";
        string kFlameOutGeneric = "Something went wrong";
        string kWarpReady = "Ready";
        string kTerrainWarning = "Warp halted to avoid collision with celestial body. Reduce speed to approach minimum warp altitude.";
        string kOrbitCircularized = "Orbit circularized";
        string kRendezvousComplete = "Rendezvous completed";
        string kInterstellarEfficiencyModifier = "kInterstellarEfficiencyModifier";
        #endregion

        #region Fields
        [KSPField]
        public bool debugEnabled = false;

        /// <summary>
        /// Short description of the module as displayed in the editor.
        /// </summary>
        [KSPField]
        public string moduleDescription = string.Empty;

        /// <summary>
        /// Minimum planetary radius needed to go to warp. This is used to calculate the user-friendly minimum warp altitude display.
        /// </summary>
        [KSPField]
        public double minPlanetaryRadius = 3.0;

        /// <summary>
        /// Minimum altitude at which the engine can go to warp. The engine will flame-out unless this altitude requirement is met.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Minimum Warp altitude", guiUnits = "m", guiFormat = "n1")]
        public double minWarpAltitudeDisplay = 500000.0f;

        /// <summary>
        /// The FTL velocity of the ship, measured in C, that is adjusted for throttle setting and thrust limiter.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Warp Speed", guiFormat = "n3", guiUnits = "C")]
        public float warpSpeed = 0;

        /// <summary>
        /// (Debug visible) Maximum possible warp speed.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "Max Warp Speed", guiFormat = "n3", guiUnits = "C")]
        protected float maxWarpSpeed = 0;

        /// <summary>
        /// Pre-flight status check.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "FTL Check")]
        public string preflightCheck = string.Empty;

        /// <summary>
        /// Where we are in space.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Spatial Location")]
        public WBISpatialLocations spatialLocation = WBISpatialLocations.Unknown;

        /// <summary>
        /// The vessel's course- which is really just the selected target.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Course")]
        public string vesselCourse = string.Empty;

        /// <summary>
        /// Distance to the vessel's target
        /// </summary>
        [KSPField(guiActive = true, guiName = "Distance", guiFormat = "n3")]
        public double targetDistance = 0f;

        /// <summary>
        /// Limits top speed while in a planetary or munar SOI so we don't zoom past the celestial body.
        /// Out in interplanetary space we don't have a speed limit.
        /// The first number represents how close to the SOI edge the vessel is (1 = right at the edge, 0.1 = 10% of the distance to the SOI edge)
        /// The second number is the top speed multiplier.
        /// </summary>
        [KSPField]
        public FloatCurve planetarySOISpeedCurve;

        /// <summary>
        /// Whenever you cross into interstellar space, or are already in interstellar space and throttled down,
        /// then apply this acceleration curve. The warp speed will be max warp speed * curve's speed modifier.
        /// The first number represents the time since crossing the boundary/throttling up, and the second number is the multiplier.
        /// We don't apply this curve when going from interstellar to interplanetary space.
        /// </summary>
        [KSPField]
        public FloatCurve interstellarAccelerationCurve;

        /// <summary>
        /// Multiplies resource consumption and production rates by this multiplier when in interstellar space.
        /// Generators identified by warpPowerGeneratorID will be affected by this multiplier.
        /// Default multiplier is 10.
        /// </summary>
        [KSPField]
        public float interstellarPowerMultiplier = 10f;

        /// <summary>
        /// Warp engines can efficiently move a certain amount of mass to light speed and beyond without penalties.
        /// Going over this limit incurs performance penalties, but staying under this value provides benefits.
        /// The displacement value is rated in metric tons.
        /// </summary>
        [KSPField]
        public float displacementImpulse = 10;

        /// <summary>
        /// In addition to any specified PROPELLANT resources, warp engines require warpCapacity. Only parts with
        /// a WBIWarpCoil part module can generate warpCapacity.
        /// The warp curve controls how much warpCapacity is neeeded to go light speed or faster.
        /// The first number represents the available warpCapacity, while the second number gives multiples of C.
        /// You can apply any kind of warp curve you want, but the baseline uses the Fibonacci sequence * 10.
        /// It may seem steep, but in KSP's small scale, 1C is plenty fast.
        /// This curve is modified by the engine's displacementImpulse and current vessel mass.
        /// effectiveWarpCapacity = warpCapacity * (displacementImpulse / vessel mass)
        /// </summary>
        [KSPField]
        public FloatCurve warpCurve;

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;

        /// <summary>
        /// Waterfall Warp Effects Curve. This is used to control the Waterfall warp field effects based on the vessel's current warp speed.
        /// The first number represents multiples of C, and the second number represents the level at which to drive the warp effects.
        /// The effects value ranges from 0 to 1, while there's no upper limit to multiples of C, so keep that in mind.
        /// The default curve is:
        /// key = 0 0
        /// key = 1 0.5
        /// key = 1.5 1
        /// </summary>
        [KSPField]
        public FloatCurve waterfallWarpEffectsCurve;

        /// <summary>
        /// The name of the WBIAnimatedTexture to drive as part of the warp effects.
        /// </summary>
        [KSPField]
        public string textureModuleID = string.Empty;


        /// <summary>
        /// Engines can drive WBIModuleGeneratorFX that produce resources needed for warp travel if their moduleID matches this value.
        /// </summary>
        [KSPField]
        public string warpPowerGeneratorID = string.Empty;

        /// <summary>
        /// Optional effect to play when the vessel exceeds the speed of light.
        /// </summary>
        [KSPField]
        public string photonicBoomEffectName = string.Empty;
        #endregion

        #region Housekeeping
        /// <summary>
        /// (Debug visible) Flag to indicate that we're in space (orbiting, suborbital, or escaping)
        /// </summary>
        [KSPField]
        public bool isInSpace = false;

        /// <summary>
        /// (Debug visible) Flag to indicate that the ship meets minimum warp altitude.
        /// </summary>
        [KSPField]
        public bool meetsWarpAltitude = false;

        /// <summary>
        /// (Debug visible) Flag to indicate that the ship has sufficient warp capacity.
        /// </summary>
        [KSPField]
        public bool hasWarpCapacity = false;

        /// <summary>
        /// Name of optional bow shock transform.
        /// </summary>
        [KSPField]
        public string bowShockTransformName = string.Empty;

        /// <summary>
        /// (Debug visible) Flag to indicate that the engine should apply translation effects. Multiple engines can work together as long as each one's minimum requirements are met.
        /// </summary>
        [KSPField]
        protected bool applyWarpTranslation = true;

        /// <summary>
        /// (Debug visible) Total displacement impulse calculated from all active warp engines.
        /// </summary>
        [KSPField]
        protected float totalDisplacementImpulse = 0;

        /// <summary>
        /// (Debug visible) Total warp capacity calculated from all active warp engines.
        /// </summary>
        [KSPField]
        protected float totalWarpCapacity = 0;

        /// <summary>
        /// (Debug visible) Effective warp capacity after accounting for vessel mass
        /// </summary>
        [KSPField(guiName = "Effective Warp Capacity")]
        protected float effectiveWarpCapacity = 0;

        /// <summary>
        /// (Debug visible) Distance per physics update that the vessel will move.
        /// </summary>
        [KSPField(guiName = "Distance per update", guiFormat = "n2", guiUnits = "m")]
        protected float warpDistance = 0;

        /// <summary>
        /// (Debug visible) Current throttle level for the warp effects.
        /// </summary>
        [KSPField]
        protected float effectsThrottle = 0;

        /// <summary>
        /// Hit test stuff to make sure we don't run into planets.
        /// </summary>
        protected RaycastHit terrainHit;
        /// <summary>
        /// Layer mask used for the hit test
        /// </summary>
        protected LayerMask layerMask = -1;

        /// <summary>
        /// List of active warp engines
        /// </summary>
        protected List<WBIWarpEngine> warpEngines = null;
        /// <summary>
        /// List of enabled warp coils
        /// </summary>
        protected List<WBIWarpCoil> warpCoils = null;
        protected List<WBIModuleGeneratorFX> warpGenerators = null;
        /// <summary>
        /// List of animated textures driven by the warp engine
        /// </summary>
        protected List<WBIAnimatedTexture> warpEngineTextures = null;
        /// <summary>
        /// Previously visited celestial body
        /// </summary>
        protected CelestialBody previousBody = null;
        /// <summary>
        /// Bounds object of the celestial body
        /// </summary>
        protected Bounds bodyBounds;
        /// <summary>
        /// Current throttle level
        /// </summary>
        protected float throttleLevel = 0f;

        /// <summary>
        /// Optional bow shock effect transform.
        /// </summary>
        protected Transform bowShockTransform = null;

        /// <summary>
        /// Due to the way engines work on FixedUpdate, the engine can determine that it is NOT flamed out if it meets its propellant requirements. Therefore, we keep track of our own flameout conditions.
        /// </summary>
        protected bool warpFlameout = false;

        /// <summary>
        /// Optional (but highly recommended) Waterfall effects module
        /// </summary>
        protected WFModuleWaterfallFX waterfallFXModule = null;

        /// <summary>
        /// Flag to indicate whether or not the vessel has exceeded light speed.
        /// </summary>
        protected bool hasExceededLightSpeed = false;

        // These are used to auto-circularize the vessel's orbit
        private WBICircularizationStates circularizationState = WBICircularizationStates.doNotCircularize;
        private double circularizeStartTime = 0f;

        // Used for gradually accelerating to interstellar speed.
        private WBISpatialLocations prevSpatialLocation = WBISpatialLocations.Unknown;
        private double speedStartTime = 0f;
        private float prevInterstellarAcceleration = 0;
        #endregion

        #region Actions And Events
        /// <summary>
        /// Circularizes the ship's orbit
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "Auto-circularize orbit")]
        public void CircularizeOrbit()
        {
            if ( FlightInputHandler.state.mainThrottle <= 0)
            {
                // We don't circularize if the ship is in interstellar space.
                if (BlueshiftScenario.shared.IsInInterstellarSpace(this.part.vessel))
                {
                    circularizationState = WBICircularizationStates.doNotCircularize;
                    return;
                }
                // We don't circularize if the ship is in interplanetary space and the star has planets orbiting it.
                else if (BlueshiftScenario.shared.IsAStar(part.vessel.mainBody) && BlueshiftScenario.shared.HasPlanets(part.vessel.mainBody))
                {
                    circularizationState = WBICircularizationStates.doNotCircularize;
                    return;
                }

                // It costs resources to circularize. Make sure we can.
                if (BlueshiftScenario.circularizationResourceDef != null && BlueshiftScenario.circularizationCostPerTonne > 0)
                {
                    double amount, maxAmount = 0;
                    double resourceCost = (this.part.vessel.GetTotalMass() * BlueshiftScenario.circularizationCostPerTonne);

                    this.part.GetConnectedResourceTotals(BlueshiftScenario.circularizationResourceDef.id, out amount, out maxAmount);
                    if (amount < resourceCost)
                    {
                        circularizationState = WBICircularizationStates.doNotCircularize;
                        return;
                    }

                    amount = this.part.RequestResource(BlueshiftScenario.circularizationResourceDef.id, resourceCost);
                }

                // If we are targeting a vessel and we're near it (10km) then rendezvous with it instead.
                if (vessel.targetObject != null && vessel.targetObject.GetVessel() != null && targetDistance <= kMinRendezvousDistance)
                {
                    Vector3 position = UnityEngine.Random.onUnitSphere * kRendezvousDistance;
                    FlightGlobals.fetch.SetShipOrbitRendezvous(vessel.targetObject.GetVessel(), position, Vector3d.zero);
                    ScreenMessages.PostScreenMessage(kRendezvousComplete, kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                    circularizationState = WBICircularizationStates.hasBeenCircularized;
                    return;
                }

                // Get current orbit.
                Orbit orbit = this.part.vessel.orbitDriver.orbit;
                double inclination = orbit.inclination;
                double altitude = this.part.vessel.altitude;
                double planetRadius = this.part.vessel.mainBody.Radius;
                int planetIndex = this.part.vessel.mainBody.flightGlobalsIndex;
                double currentTime = Planetarium.GetUniversalTime();
                Orbit circlularOrbit = new Orbit(inclination, 0, planetRadius + altitude, 0, 0, 0, currentTime, this.part.vessel.mainBody);
                Orbit.State state;

                // Adjust the state vectors. This will shift the vessel's position, but the ship would've shifted around during normal gravity braking anyway.
                circlularOrbit.GetOrbitalStateVectorsAtUT(currentTime, out state);
                circlularOrbit.UpdateFromFixedVectors(orbit.pos, state.vel, this.part.vessel.mainBody.referenceBody, currentTime + 0.02);
                FlightGlobals.fetch.SetShipOrbit(planetIndex, 0, planetRadius + altitude, circlularOrbit.inclination, circlularOrbit.LAN, circlularOrbit.meanAnomaly, circlularOrbit.argumentOfPeriapsis, circlularOrbit.ObT);

                ScreenMessages.PostScreenMessage(kOrbitCircularized, kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                circularizationState = WBICircularizationStates.hasBeenCircularized;
            }
        }

        /// <summary>
        /// Action menu item to circularize the ship's orbit.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("Auto-circularize orbit")]
        public void CircularizeOrbitAction(KSPActionParam param)
        {
            CircularizeOrbit();
        }
        #endregion

        #region Overrides
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Update spatial location.
            spatialLocation = BlueshiftScenario.shared.GetSpatialLocation(part.vessel);

            // Vessel course - just the selected target.
            updateVesselCourse();

            // Make sure the engine is running and we should apply warp.
            if (!EngineIgnited)
                return;
            if (!shouldApplyWarp())
                return;

            // Drive warp coil resource consumption and get total available warp capacity.
            updateWarpPowerGenerators();
            getTotalWarpCapacity();

            // Calculate the best warp curve to get maximum FTL speed.
            calculateBestWarpSpeed();

            // Update our precondition states
            updatePreconditionStates();

            // Now check for flameout
            if (IsFlamedOut())
            {
                resetWarpParameters();
                return;
            }

            // Reset warp speed exceeded flag.
            if (warpSpeed < 1f)
                hasExceededLightSpeed = false;

            // Engage!
            travelAtWarp();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            getCoilsAndGenerators();
            updateFTLPreflightStatus();
            if (!isOperational && !EngineIgnited)
            {
                fadeOutEffects();
                return;
            }
            else if (flameout || warpFlameout)
            {
                fadeOutEffects();
            }

            UpdateWarpStatus();

            bool enableCircularizeOrbit = BlueshiftScenario.autoCircularize && (spatialLocation == WBISpatialLocations.Planetary || 
                (BlueshiftScenario.shared.IsAStar(vessel.mainBody) && BlueshiftScenario.shared.GetLastPlanet(vessel.mainBody) == null));
            Events["CircularizeOrbit"].active = enableCircularizeOrbit;
            Actions["CircularizeOrbitAction"].active = BlueshiftScenario.autoCircularize;
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.Append(base.GetInfo());
            info.AppendLine(string.Format("<b>Displacement Impulse:</b> {0:n1} t", displacementImpulse));
            return info.ToString();

        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            loadFloatCurves();

            warpEngines = new List<WBIWarpEngine>();
            warpCoils = new List<WBIWarpCoil>();
            warpGenerators = new List<WBIModuleGeneratorFX>();
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(this.part);
            getAnimatedWarpEngineTextures();

            // Optional bow shock transform.
            if (!string.IsNullOrEmpty(bowShockTransformName))
                bowShockTransform = this.part.FindModelTransform(bowShockTransformName);

            // GUI setup
            Fields["realIsp"].guiActive = false;
            Fields["finalThrust"].guiActive = false;
            Fields["fuelFlowGui"].guiActive = false;

            // Debug fields
            Fields["isInSpace"].guiActive = debugEnabled;
            Fields["meetsWarpAltitude"].guiActive = debugEnabled;
            Fields["hasWarpCapacity"].guiActive = debugEnabled;
            Fields["applyWarpTranslation"].guiActive = debugEnabled;
            Fields["totalDisplacementImpulse"].guiActive = debugEnabled;
            Fields["totalWarpCapacity"].guiActive = debugEnabled;
            Fields["minPlanetaryRadius"].guiActive = debugEnabled;
            Fields["effectiveWarpCapacity"].guiActive = debugEnabled;
            Fields["warpDistance"].guiActive = debugEnabled;
            Fields["effectsThrottle"].guiActive = debugEnabled;

            // Editor events
            if (HighLogic.LoadedSceneIsEditor)
            {
                Fields["effectiveWarpCapacity"].guiActiveEditor = true;
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
            }
        }

        public override void Flameout(string message, bool statusOnly = false, bool showFX = true)
        {
            base.Flameout(message, statusOnly, showFX);
            warpFlameout = true;
            hasExceededLightSpeed = false;
            spatialLocation = WBISpatialLocations.Unknown;
        }

        public override void UnFlameout(bool showFX = true)
        {
            if (warpFlameout)
                return;
            base.UnFlameout(showFX);
        }

        public override void Activate()
        {
            base.Activate();
            if (!staged)
                this.part.force_activate(true);

            Fields["warpSpeed"].guiActive = true;
            
            int count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = true;
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            hasExceededLightSpeed = false;
            spatialLocation = WBISpatialLocations.Unknown;

            Fields["warpSpeed"].guiActive = false;

            int count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = false;
            }
        }

        public override void FXUpdate()
        {
            base.FXUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (EngineIgnited && isEnabled && !flameout && !warpFlameout)
                this.part.Effect(runningEffectName, 1f);

            // Update our animated texture module, if any.
            float throttle = FlightInputHandler.state.mainThrottle;
            int count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = EngineIgnited;
                warpEngineTextures[index].animationThrottle = throttle > 0f ? throttle : 0.1f;
            }

            // If we aren't supposed to apply warp translation then there's nothing more to do.
            if (!applyWarpTranslation)
                return;

            // Update active warp coils
            count = warpCoils.Count;
            for (int index = 0; index < count; index++)
            {
                warpCoils[index].animationThrottle = !warpFlameout ? throttle : 0f;
            }

            // Update warp effects
            if (bowShockTransform != null)
                bowShockTransform.position = this.part.vessel.transform.position;

            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
            {
                float targetValue = 0f;
                if (throttleLevel > 0)
                    targetValue = waterfallWarpEffectsCurve.Evaluate(warpSpeed);

                effectsThrottle = Mathf.Lerp(effectsThrottle, targetValue, engineSpoolTime);

                if (effectsThrottle <= 0.001 && targetValue <= 0f)
                    effectsThrottle = 0;
                else if (targetValue > 0f && targetValue >= effectsThrottle && (effectsThrottle / targetValue >= 0.99f))
                    effectsThrottle = targetValue;
                else if (targetValue > 0f && effectsThrottle > targetValue && targetValue / effectsThrottle >= 0.99f)
                    effectsThrottle = targetValue;

                waterfallFXModule.SetControllerValue(waterfallEffectController, effectsThrottle);
            }

            if (!hasExceededLightSpeed && warpSpeed >= 1f)
            {
                hasExceededLightSpeed = true;
                this.part.Effect(photonicBoomEffectName, 1);
            }
        }
        #endregion

        #region API
        /// <summary>
        /// Determines whether or not the engine is ignited and operational.
        /// </summary>
        /// <returns>true if the engine is activated, false if not.</returns>
        public bool IsActivated()
        {
            return EngineIgnited && isOperational;
        }

        /// <summary>
        /// Checks flamout conditions including ensuring that the ship is in space, meets minimum warp altitude, and has sufficient warp capacity.
        /// </summary>
        /// <returns>true if the engine is flamed out, false if not.</returns>
        public bool IsFlamedOut()
        {
            // Must be in space, at or above orbital altitude, have a running engine, and not be flamed out.
            if (!isInSpace || !meetsWarpAltitude || !hasWarpCapacity && EngineIgnited && isOperational && !warpFlameout)
            {
                warpFlameout = true;
                if (!isInSpace)
                    Flameout(kNeedsSpaceflight);
                else if (!meetsWarpAltitude)
                    Flameout(kNeedsAltitude);
                else if (!hasWarpCapacity)
                    Flameout(kNeedsWarpCapacity);
                else
                    Flameout(kFlameOutGeneric);
                return true;
            }
            else if (isInSpace && meetsWarpAltitude && hasWarpCapacity && warpFlameout)
            {
                warpFlameout = false;
                UnFlameout();
                return false;
            }

            return flameout;
        }

        /// <summary>
        /// Determines whether or not the ship has sufficient warp capacity to go FTL.
        /// </summary>
        /// <returns>true if the ship has sufficient warp capacity, false if not.</returns>
        public bool HasWarpCapacity()
        {
            return totalWarpCapacity > 0;
        }

        /// <summary>
        /// Determines whether or the ship is in space. To be in space the ship must be sub-orbital, orbiting, or escaping.
        /// </summary>
        /// <returns>true if the ship is in space, false if not.</returns>
        public bool IsInSpace()
        {
            return this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL ||
                this.part.vessel.situation == Vessel.Situations.ORBITING ||
                this.part.vessel.situation == Vessel.Situations.ESCAPING;
        }

        /// <summary>
        /// Determines whether or not the ship meets the minimum required altitude to go to warp.
        /// </summary>
        /// <returns>true if the ship meets minimum altitude, false if not.</returns>
        public bool MeetsWarpAltitude()
        {
            return this.part.vessel.orbit.altitude >= this.part.vessel.orbit.referenceBody.Radius * minPlanetaryRadius;
        }

        /// <summary>
        /// Updates the warp status display
        /// </summary>
        public void UpdateWarpStatus()
        {
            if (isInSpace)
            {
                if (!meetsWarpAltitude)
                {
                    minWarpAltitudeDisplay = this.part.vessel.orbit.referenceBody.Radius * minPlanetaryRadius;
                    if (minWarpAltitudeDisplay > 1000000)
                    {
                        Fields["minWarpAltitudeDisplay"].guiUnits = "km";
                        minWarpAltitudeDisplay = minWarpAltitudeDisplay / 1000;
                    }
                    else
                    {
                        Fields["minWarpAltitudeDisplay"].guiUnits = "m";
                    }
                    Fields["minWarpAltitudeDisplay"].guiActive = true;
                }
                else if (!hasWarpCapacity)
                {
                }
                else
                {
                    minWarpAltitudeDisplay = double.NaN;
                    Fields["minWarpAltitudeDisplay"].guiActive = false;
                }
            }
            else
            {
                minWarpAltitudeDisplay = double.NaN;
                Fields["minWarpAltitudeDisplay"].guiActive = false;
                spatialLocation = WBISpatialLocations.Unknown;
            }
        }
        #endregion

        #region Helpers

        /*
         * Keep this for jump engines
        [KSPEvent(guiActive = true)]
        public void Test()
        {
            //WORKS DONT REMOVE   ->    FlightGlobals.fetch.SetShipOrbit(this.part.vessel.mainBody.flightGlobalsIndex, 0, this.part.vessel.altitude + this.part.vessel.mainBody.Radius, 0, 0, 0, 0, 0);

            Orbit orbit = this.part.vessel.orbit;
            double altitude = this.part.vessel.altitude;
            double planetRadius = this.part.vessel.mainBody.Radius;
            int planetIndex = this.part.vessel.mainBody.flightGlobalsIndex;
            double currentTime = Planetarium.GetUniversalTime();
            Orbit circlularOrbit = new Orbit(orbit.inclination, 0, planetRadius + altitude, 0, 0, 0, currentTime, this.part.vessel.mainBody);
            Orbit.State state;
            circlularOrbit.GetOrbitalStateVectorsAtUT(currentTime, out state);
            circlularOrbit.UpdateFromFixedVectors(orbit.pos, state.vel, this.part.vessel.mainBody.referenceBody, currentTime + 0.02);
            FlightGlobals.fetch.SetShipOrbit(planetIndex, 0, planetRadius + altitude, circlularOrbit.inclination, circlularOrbit.LAN, circlularOrbit.meanAnomaly, circlularOrbit.argumentOfPeriapsis, circlularOrbit.ObT);

//            This works, but the vessel will shift positions a bit.
//            Orbit orbit = this.part.vessel.orbit;
//            double altitude = this.part.vessel.altitude;
//            double planetRadius = this.part.vessel.mainBody.Radius;
//            int planetIndex = this.part.vessel.mainBody.flightGlobalsIndex;
//            FlightGlobals.fetch.SetShipOrbit(planetIndex, 0, planetRadius + altitude, orbit.inclination, orbit.LAN, orbit.meanAnomaly, orbit.argumentOfPeriapsis, orbit.ObT);

            // maybe update state.pos during timewarp?
            //            Orbit.State state;
            //            this.part.vessel.orbit.GetOrbitalStateVectorsAtUT(Planetarium.GetUniversalTime(), out state);
            //            this.part.vessel.orbit.UpdateFromFixedVectors(state.pos, state.vel, this.part.vessel.mainBody.referenceBody, Planetarium.GetUniversalTime());
        }
        */

        protected void updateFTLPreflightStatus()
        {
            // Update preflight check
            if (maxWarpSpeed < 1)
            {
                // Determinine minimum capacity to reach light speed or better.
                float FTLSpeed = 0;
                float warpCapacity = 0;
                for (int index = 0; index < warpCurve.Curve.keys.Length; index++)
                {
                    warpCapacity = warpCurve.Curve.keys[index].time;
                    FTLSpeed = warpCurve.Evaluate(warpCapacity);
                    if (FTLSpeed >= 1)
                        break;
                }

                preflightCheck = string.Format(kAddWarpCapacity, effectiveWarpCapacity, warpCapacity);
            }
            else
            {
                preflightCheck = kCanGoFTL;
            }
        }

        /// <summary>
        /// Fades out the warp effects
        /// </summary>
        protected void fadeOutEffects()
        {
            if (waterfallFXModule != null && effectsThrottle > 0f)
            {
                effectsThrottle = Mathf.Lerp(effectsThrottle, 0, engineSpoolTime);

                if (effectsThrottle <= 0.001f)
                    effectsThrottle = 0f;

                waterfallFXModule.SetControllerValue(waterfallEffectController, 0);
            }
        }

        /// <summary>
        /// Finds any animated textures that should be controlled by the warp engine
        /// </summary>
        protected void getAnimatedWarpEngineTextures()
        {
            warpEngineTextures = new List<WBIAnimatedTexture>();

            List<WBIAnimatedTexture> animatedTextures = this.part.FindModulesImplementing<WBIAnimatedTexture>();
            if (animatedTextures == null)
            {
                return;
            }

            int count = animatedTextures.Count;
            for (int index = 0; index < count; index++)
            {
                if (animatedTextures[index].moduleID == textureModuleID)
                    warpEngineTextures.Add(animatedTextures[index]);
            }
        }

        protected void onEditorShipModified(ShipConstruct ship)
        {
            // Get warp capacity and total displacement impulse
            int count = ship.parts.Count;
            WBIWarpCoil warpCoil;
            WBIWarpEngine engine;
            float coilCapacity;
            float vesselMass = ship.GetTotalMass();
            float warpDisplacementImpulse = 0;
            float warpCapacity = 0;
            for (int index = 0; index < count; index++)
            {
                warpCoil = ship.parts[index].FindModuleImplementing<WBIWarpCoil>();
                if (warpCoil != null)
                {
                    coilCapacity = warpCoil.warpCapacity * (warpCoil.displacementImpulse / vesselMass);
                    warpCapacity += coilCapacity;
                }

                engine = ship.parts[index].FindModuleImplementing<WBIWarpEngine>();
                if (engine != null)
                    warpDisplacementImpulse += engine.displacementImpulse;
            }

            // Get effective warp capacity
            effectiveWarpCapacity = warpCapacity * (warpDisplacementImpulse / vesselMass);

            // Now calculate max speed
            maxWarpSpeed = warpCurve.Evaluate(effectiveWarpCapacity);

            // FTL pre-flight status check.
            if (maxWarpSpeed < 1)
            {
                // Determinine minimum capacity to reach light speed or better.
                float FTLSpeed = 0;
                for (int index = 0; index < warpCurve.Curve.keys.Length; index++)
                {
                    warpCapacity = warpCurve.Curve.keys[index].time;
                    FTLSpeed = warpCurve.Evaluate(warpCapacity);
                    if (FTLSpeed >= 1)
                        break;
                }

                // Update status
                preflightCheck = string.Format(kAddWarpCapacity, effectiveWarpCapacity, warpCapacity);
            }
            else
            {
                preflightCheck = kCanGoFTL;
            }
        }

        /// <summary>
        /// Calculates the best possible warp speed from the vessel's active warp engines.
        /// </summary>
        protected void calculateBestWarpSpeed()
        {
            // First, calculate the effective warp capacity.
            effectiveWarpCapacity = totalWarpCapacity * (totalDisplacementImpulse / this.part.vessel.GetTotalMass());

            int count = warpEngines.Count;
            float bestWarpSpeed = -1f;
            float warpCurveSpeed = 0;
            for (int index = 0; index < count; index++)
            {
                warpCurveSpeed = warpEngines[index].warpCurve.Evaluate(effectiveWarpCapacity);
                if (warpCurveSpeed > bestWarpSpeed)
                    bestWarpSpeed = warpCurveSpeed;
            }
            maxWarpSpeed = bestWarpSpeed;

            // Adjust warp speed based on spatial location.
            switch (spatialLocation)
            {
                // If we're in interstellar space then we can increase our max warp speed.
                case WBISpatialLocations.Interstellar:
                    maxWarpSpeed *= BlueshiftScenario.interstellarWarpSpeedMultiplier;
                    break;

                // Limit speed if we're in a planetary SOI
                case WBISpatialLocations.Planetary:
                    float speedRatio = (float)(this.part.vessel.altitude / this.part.vessel.mainBody.sphereOfInfluence);
                    maxWarpSpeed *= planetarySOISpeedCurve.Evaluate(speedRatio);
                    break;

                // No speed adjustment while interplanetary.
                case WBISpatialLocations.Interplanetary:
                default:
                    break;
            }

            // Account for throttle setting and thrust limiter.
            throttleLevel = FlightInputHandler.state.mainThrottle * (thrustPercentage / 100.0f);
            if (throttleLevel <= 0)
            {
                warpSpeed = 0;
                prevInterstellarAcceleration = 0;
                if (spatialLocation == WBISpatialLocations.Interstellar)
                    speedStartTime = Planetarium.GetUniversalTime();
                return;
            }

            // If we've transitioned from interplanetary to interstellar or vice-versa, then transition to the appropriate speed.
            if (prevSpatialLocation != spatialLocation)
            {
                if (prevSpatialLocation == WBISpatialLocations.Interplanetary && spatialLocation == WBISpatialLocations.Interstellar)
                {
                    speedStartTime = Planetarium.GetUniversalTime();
                }

                else
                {
                    speedStartTime = 0;
                }

                prevSpatialLocation = spatialLocation;
            }

            // Still in same spatial location, but we may need to accelerate
            else if (speedStartTime > 0)
            {
                float elapsedTime = (float)(Planetarium.GetUniversalTime() - speedStartTime);
                float curveSpeed = Mathf.Abs(interstellarAccelerationCurve.Evaluate(elapsedTime));

                // For reasons unknown the float curve can go negative in its evaluation, so we skip any evaluations that are less than the previous evaluation.
                if (curveSpeed > prevInterstellarAcceleration)
                {
                    prevInterstellarAcceleration = curveSpeed;
                    warpSpeed = maxWarpSpeed * curveSpeed * throttleLevel;
                }

                // Stop accelerating if we've hit our end time.
                if (elapsedTime >= interstellarAccelerationCurve.maxTime)
                    speedStartTime = 0;      
            }

            // No acceleration, just cruising...
            else
            {
                warpSpeed = maxWarpSpeed * throttleLevel;
            }
        }

        /// <summary>
        /// Calulates the total warp capacity from the vessel's active warp coils. Each warp coil must successfully consume its required resources in order to be considered.
        /// </summary>
        protected void getTotalWarpCapacity()
        {
            int count = warpCoils.Count;
            WBIWarpCoil warpCoil;
            float coilCapacity;
            float vesselMass = part.vessel.GetTotalMass();

            totalWarpCapacity = 0;
            for (int index = 0; index < count; index++)
            {
                warpCoil = warpCoils[index];
                if (warpCoil.isActivated && consumeCoilResources(warpCoil))
                {
                    coilCapacity = warpCoil.warpCapacity * (warpCoil.displacementImpulse / vesselMass);
                    totalWarpCapacity += coilCapacity;
                }
            }
        }

        /// <summary>
        /// Updates the generators that provide warp power.
        /// </summary>
        protected void updateWarpPowerGenerators()
        {
            int count = warpGenerators.Count;
            WBIModuleGeneratorFX generator;

            for (int index = 0; index < count; index++)
            {
                generator = warpGenerators[index];
                if (spatialLocation == WBISpatialLocations.Interstellar)
                {
                    if (!generator.EfficiencyModifiers.ContainsKey(kInterstellarEfficiencyModifier))
                        generator.EfficiencyModifiers.Add(kInterstellarEfficiencyModifier, 0);
                    generator.EfficiencyModifiers[kInterstellarEfficiencyModifier] = Mathf.Clamp(interstellarPowerMultiplier * throttleLevel, 1, interstellarPowerMultiplier);
                    generator.TallyEfficiencyModifiers();
                }
                else if (spatialLocation != WBISpatialLocations.Interstellar && generator.EfficiencyModifiers.ContainsKey(kInterstellarEfficiencyModifier))
                {
                    generator.EfficiencyModifiers.Remove(kInterstellarEfficiencyModifier);
                    generator.TallyEfficiencyModifiers();
                }
            }
        }

        /// <summary>
        /// Consumes the warp coil's required resources.
        /// </summary>
        /// <param name="warpCoil">The WBIWarpCoil to check for required resources.</param>
        /// <returns>true if the coil successfully consumed its required resources, false if not.</returns>
        protected bool consumeCoilResources(WBIWarpCoil warpCoil)
        {
            string errorStatus = string.Empty;
            int count = warpCoil.resHandler.inputResources.Count;

            if (!warpCoil.isActivated)
                return false;
            if (count == 0)
                return true;

            warpCoil.resHandler.UpdateModuleResourceInputs(ref errorStatus, spatialLocation != WBISpatialLocations.Interstellar ? 1.0 : interstellarPowerMultiplier, 0.1, true, true);
            for (int index = 0; index < count; index++)
            {
                if (!warpCoil.resHandler.inputResources[index].available)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Looks for all the active warp engines in the vessel. From the list, only the top-most engine in the list of active engines should apply warp translation. All others
        /// simply provide support.
        /// </summary>
        /// <returns></returns>
        protected bool shouldApplyWarp()
        {
            // Only one warp engine should handle warp speed. The rest just provide boost effects.
            List<WBIWarpEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIWarpEngine>();
            WBIWarpEngine engine, prevEngine;
            int count = engines.Count;

            warpEngines.Clear();
            totalDisplacementImpulse = 0;
            for (int index = 0; index < count; index++)
            {
                engine = engines[index];
                if (engine.EngineIgnited && engine.isOperational)
                {
                    warpEngines.Add(engine);
                    totalDisplacementImpulse += engine.displacementImpulse;

                    //If the index is at the top of the list and we're the topmost engine then we should apply acceleartion.
                    if (engine == this && index == 0)
                    {
                        applyWarpTranslation = true;
                    }

                    //Check previous engine. If it is active and hovering then it will be applying translation, so we should not apply translation.
                    else if (engine == this)
                    {
                        prevEngine = engines[index - 1];
                        if (prevEngine.IsActivated())
                            applyWarpTranslation = false;
                        else
                            applyWarpTranslation = true;
                    }
                }
            }

            return applyWarpTranslation;
        }

        /// <summary>
        /// Loads the desired FloatCurve from the desired config node.
        /// </summary>
        /// <param name="curve">The FloatCurve to load</param>
        /// <param name="curveNodeName">The name of the curve to load</param>
        /// <param name="defaultCurve">An optional default curve to use in case the curve's node doesn't exist in the part module's config.</param>
        protected void loadCurve(FloatCurve curve, string curveNodeName, ConfigNode defaultCurve = null)
        {
            if (curve.Curve.length > 0)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode engineNode = null;
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
                        engineNode = node;
                        break;
                    }
                }
            }
            if (engineNode == null)
                return;

            if (engineNode.HasNode(curveNodeName))
            {
                node = engineNode.GetNode(curveNodeName);
                curve.Load(node);
            }
            else if (defaultCurve != null)
            {
                curve.Load(defaultCurve);
            }
        }

        private void loadFloatCurves()
        {
            ConfigNode defaultCurve = new ConfigNode("waterfallWarpEffectsCurve");
            loadCurve(warpCurve, "warpCurve");

            defaultCurve = new ConfigNode("planetarySOISpeedCurve");
            defaultCurve.AddValue("key", "1 0.1");
            defaultCurve.AddValue("key", "0.5 0.05");
            defaultCurve.AddValue("key", "0.25 0.01");
            defaultCurve.AddValue("key", "0.1 0.005");
            loadCurve(planetarySOISpeedCurve, "planetarySOISpeedCurve");

            defaultCurve = new ConfigNode("waterfallWarpEffectsCurve");
            defaultCurve.AddValue("key", "0 0");
            defaultCurve.AddValue("key", "0.001 0.1");
            defaultCurve.AddValue("key", "0.01 0.25");
            defaultCurve.AddValue("key", "0.1 0.25");
            defaultCurve.AddValue("key", "0.5 0.375");
            defaultCurve.AddValue("key", "1.0 0.5");
            defaultCurve.AddValue("key", "1.5 1");
            loadCurve(waterfallWarpEffectsCurve, "waterfallWarpEffectsCurve", defaultCurve);


            defaultCurve = new ConfigNode("interstellarAccelerationCurve");
            defaultCurve.AddValue("key", "0 0.001");
            defaultCurve.AddValue("key", "5 0.01");
            defaultCurve.AddValue("key", "7 0.1");
            defaultCurve.AddValue("key", "9 0.5");
            defaultCurve.AddValue("key", "10 1");
            loadCurve(interstellarAccelerationCurve, "interstellarAccelerationCurve", defaultCurve);
        }

        int vesselPartCount = 0;
        private void getCoilsAndGenerators()
        {
            if (vesselPartCount != part.vessel.parts.Count)
            {
                vesselPartCount = part.vessel.parts.Count;

                // Warp coils
                List<WBIWarpCoil> coils = part.vessel.FindPartModulesImplementing<WBIWarpCoil>();
                if (coils != null && coils.Count > 0)
                {
                    warpCoils.Clear();
                    warpCoils.AddRange(coils);
                }

                // Warp Power generators
                List<WBIModuleGeneratorFX> generators = part.vessel.FindPartModulesImplementing<WBIModuleGeneratorFX>();
                WBIModuleGeneratorFX generator;
                if (generators != null && generators.Count > 0)
                {
                    warpGenerators.Clear();
                    int count = generators.Count;
                    for (int index = 0; index < count; index++)
                    {
                        generator = generators[index];
                        if (generator.moduleID == warpPowerGeneratorID)
                        {
                            warpGenerators.Add(generator);
                        }
                    }
                }
            }
        }

        private void updatePreconditionStates()
        {
            isInSpace = IsInSpace();
            meetsWarpAltitude = MeetsWarpAltitude();
            hasWarpCapacity = HasWarpCapacity();

            if (throttleLevel <= 0f)
            {
                // Setup the auto-circularization timer if the autoCircularize game setting is enabled.
                if (BlueshiftScenario.autoCircularize &&
                    circularizationState != WBICircularizationStates.hasBeenCircularized &&
                    circularizationState != WBICircularizationStates.doNotCircularize &&
                    circularizationState != WBICircularizationStates.needsCircularization
                    )
                {
                    circularizationState = WBICircularizationStates.needsCircularization;
                    circularizeStartTime = Planetarium.GetUniversalTime();
                }
            }
            else
            {
                circularizationState = WBICircularizationStates.canBeCircularized;
            }
        }

        private void resetWarpParameters()
        {
            maxWarpSpeed = 0;
            warpSpeed = 0;
            speedStartTime = 0;
            warpDistance = 0;
            speedStartTime = 0f;
            effectiveWarpCapacity = 0;
            hasExceededLightSpeed = false;
            FlightInputHandler.state.mainThrottle = 0;
            circularizationState = WBICircularizationStates.doNotCircularize;
        }

        private void travelAtWarp()
        {
            if (throttleLevel <= 0f)
                return;

            // Calculate offset position
            warpDistance = kLightSpeed * warpSpeed * TimeWarp.fixedDeltaTime;
            Transform refTransform = part.vessel.ReferenceTransform;
            Vector3 warpVector = refTransform.up * warpDistance;
            Vector3d offsetPosition = refTransform.position + warpVector;

            // Make sure that we won't run into a celestial body.
            if (previousBody != part.orbit.referenceBody)
            {
                previousBody = part.orbit.referenceBody;
                bodyBounds = previousBody.getBounds();
            }
            if (bodyBounds.Contains(offsetPosition))
            {
                ScreenMessages.PostScreenMessage(kTerrainWarning, 3.0f, ScreenMessageStyle.UPPER_CENTER);
                FlightInputHandler.state.mainThrottle = 0;
                return;
            }

            // Apply translation.
            if (FlightGlobals.VesselsLoaded.Count > 1)
                part.vessel.SetPosition(offsetPosition);
            else
                FloatingOrigin.SetOutOfFrameOffset(offsetPosition);
        }

        private void updateVesselCourse()
        {
            string units = string.Empty;

            targetDistance = BlueshiftScenario.shared.GetDistanceToTarget(part.vessel, out units, out vesselCourse);

            if (targetDistance > 0)
            {
                Fields["targetDistance"].guiUnits = units;
            }
        }
        #endregion
    }
}
