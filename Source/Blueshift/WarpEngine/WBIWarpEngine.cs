using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;
using KSP.UI.Screens;
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
    /// ```
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
    /// ```
    /// </summary>
    #endregion
    [KSPModule("#LOC_BLUESHIFT_warpEngineTitle")]
    public class WBIWarpEngine : ModuleEnginesFX
    {
        #region constants
        float kLightSpeed = 299792458;
        float kMessageDuration = 3f;
        string kInterstellarEfficiencyModifier = "kInterstellarEfficiencyModifier";
        double kFrameSkipTime = 0.1f;
        #endregion

        #region GameEvents
        /// <summary>
        /// Game event signifying when warp engine effects have been updated.
        /// </summary>
        public static EventData<Vessel, WBIWarpEngine, float> onWarpEffectsUpdated = new EventData<Vessel, WBIWarpEngine, float>("onWarpEffectsUpdated");
        /// <summary>
        /// Game event signifying when the warp engine starts.
        /// </summary>
        public static EventData<Vessel, WBIWarpEngine> onWarpEngineStart = new EventData<Vessel, WBIWarpEngine>("onWarpEngineStart");
        /// <summary>
        /// Game event signifying when the warp engine shuts down.
        /// </summary>
        public static EventData<Vessel, WBIWarpEngine> onWarpEngineShutdown = new EventData<Vessel, WBIWarpEngine>("onWarpEngineShutdown");
        /// <summary>
        /// Game event signifying when the warp engine flames out.
        /// </summary>
        public static EventData<Vessel, WBIWarpEngine> onWarpEngineFlameout = new EventData<Vessel, WBIWarpEngine>("onWarpEngineFlameout");
        /// <summary>
        /// Game event signifying when the warp engine un-flames out.
        /// </summary>
        public static EventData<Vessel, WBIWarpEngine> onWarpEngineUnFlameout = new EventData<Vessel, WBIWarpEngine>("onWarpEngineUnFlameout");
        #endregion

        #region Fields
        [KSPField]
        public bool debugMode = false;

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
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_minWarpAltitude", guiUnits = "m", guiFormat = "n1")]
        public double minWarpAltitudeDisplay = 500000.0f;

        /// <summary>
        /// The FTL display velocity of the ship, measured in C, that is adjusted for throttle setting and thrust limiter.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_warpSpeed", guiFormat = "n3", guiUnits = "C")]
        protected float warpSpeedDisplay = 0;

        /// <summary>
        /// (Debug visible) Maximum possible warp speed.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_maxWarpSpeed", guiFormat = "n3", guiUnits = "C")]
        protected float maxWarpSpeedDisplay = 0;

        /// <summary>
        /// Pre-flight status check.
        /// </summary>
        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_BLUESHIFT_preflightCheck")]
        public string preflightCheck = string.Empty;

        /// <summary>
        /// Where we are in space.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_spatialLocation")]
        public WBISpatialLocations spatialLocation = WBISpatialLocations.Unknown;

        /// <summary>
        /// The vessel's course- which is really just the selected target.
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_vesselCourse")]
        public string vesselCourse = string.Empty;

        /// <summary>
        /// Distance to the vessel's target
        /// </summary>
        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_targetDistance", guiFormat = "n3")]
        public double targetDistance = 0f;

        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_orbitInclination", guiFormat = "n0", guiUnits = "deg")]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 180f, minValue = 0f, stepIncrement = 5f)]
        float autoCircularizeInclination = 0f;

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

        /// <summary>
        /// Used when calculating the max warp speed in the editor, this is the resource that is common between the warp engine, gravitic generator, and warp coil.
        /// This resource should be the limiting resource in the trio (the one that runs out the fastest).
        /// </summary>
        [KSPField]
        public string warpSimulationResource = "GravityWaves";

        /// <summary>
        /// The ratio between the amount of power produced for the warp coils to the amount of power consumed by the warp coils.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "n3", guiName = "#LOC_BLUESHIFT_powerMultiplier")]
        public float powerMultiplier = 0;

        /// <summary>
        /// The ratio between the total mass displaced by the warp coils to the vessel's total mass.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = true, guiFormat = "n3", guiName = "#LOC_BLUESHIFT_displacementMultiplier")]
        public float displacementMultiplier = 0;

        /// <summary>
        /// When the powerMultiplier drops below this value, the engine will flame out.
        /// </summary>
        [KSPField]
        public float warpIgnitionThreshold = 0.25f;

        [KSPField(guiActive = true, guiName = "#LOC_BLUESHIFT_superchargerMultiplier", guiFormat = "n0", guiUnits = "%", isPersistant = true)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, minValue = 0f, maxValue = 100f, stepIncrement = 5f)]
        float superchargerMultiplier = 0f;

        /// <summary>
        /// Planetary Speed Brake
        /// </summary>
        [KSPField(guiName = "#LOC_BLUESHIFT_speedBrake", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_stateOn", disabledText = "#LOC_BLUESHIFT_stateOff")]
        public bool planetarySpeedBrakeEnabled = true;

        /// <summary>
        /// Consumption modifier to apply to resource consumption rates when warping in interstellar space.
        /// This is a percentage value between 0 and 99.999. Anything outside this range will be ignored.
        /// Default is 10%, which reduces resource consumption by 10% while in interstellar space.
        /// </summary>
        [KSPField]
        public float interstellarResourceConsumptionModifier = -1f;

        #region SkillBoost
        /// <summary>
        /// The skill required to improve warp speed. Default is "ConverterSkill" (Engineers have this)
        /// </summary>
        [KSPField]
        public string warpEngineerSkill;

        /// <summary>
        /// The skill rank required to improve warp speed.
        /// </summary>
        [KSPField]
        public int warpSpeedBoostRank = -1;

        /// <summary>
        /// Per skill rank, the multiplier to multiply warp speed by.
        /// </summary>
        [KSPField]
        public float warpSpeedSkillMultiplier = -1f;
        #endregion

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
        [KSPField(guiName = "Effective Warp Capacity", guiFormat = "n3", guiUnits = "Ko")]
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
        protected float waterfallEffectsLevel = 0;

        /// <summary>
        /// (Debug visible) amount of simulation resource produced.
        /// </summary>
        [KSPField(guiActive = true, guiFormat = "n3", guiUnits = "u/s")]
        double warpResourceProduced = 0;

        /// <summary>
        /// (Debug visible) amount of simulation resource consumed.
        /// </summary>
        [KSPField(guiActive = true, guiFormat = "n3", guiUnits = "u/s")]
        double warpResourceRequired = 0;

        [KSPField(guiActive = true, guiFormat = "n3", guiUnits = "u")]
        double warpResourceAmount = 0;

        [KSPField(guiActive = true, guiFormat = "n3", guiUnits = "u")]
        double warpResourceMaxAmount = 0;

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
        /// <summary>
        /// List of warp generators
        /// </summary>
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

        /// <summary>
        /// Current speed of the ship in terms of C.
        /// </summary>
        public float warpSpeed = 0;

        /// <summary>
        /// Current multiplier used for the consumption of resources.
        /// </summary>
        public double consumptionMultiplier = 1f;

        float prevThrottle = -1f;
        float maxWarpSpeed = 0;
        float prevWarpSpeed = 0;
        float prevMaxWarpSpeed = 0;
        bool wentInterstellar = false;
        string targetDistanceUnits = string.Empty;
        bool lockedCourseAndSpeed = false;
        bool isTimewarping = false;
        Vector3d warpCruiseVector;
        Vector3d preCruiseVelocity;
        bool needsVelocityUpdate = false;
        double resumeUpdateTimestamp = -1f;
        #endregion

        #region Actions And Events
        /// <summary>
        /// Circularizes the ship's orbit
        /// </summary>
        [KSPEvent(guiActive = true, guiName = "#LOC_BLUESHIFT_circularizeOrbit")]
        public void CircularizeOrbit()
        {
            if ( FlightInputHandler.state.mainThrottle <= 0)
            {
                // We don't circularize if the ship is in interstellar space.
                if (BlueshiftScenario.shared.IsInInterstellarSpace(this.part.vessel))
                {
                    circularizationState = WBICircularizationStates.doNotCircularize;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_needsSolarOrbit"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                    return;
                }

                // The ship needs to be at or above minimum planetary radius.
                if (!MeetsWarpAltitude())
                {
                    circularizationState = WBICircularizationStates.doNotCircularize;
                    ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_needsAltitude"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
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
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_needsResources"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                        circularizationState = WBICircularizationStates.doNotCircularize;
                        return;
                    }

                    amount = this.part.RequestResource(BlueshiftScenario.circularizationResourceDef.id, resourceCost);
                }

                // If we are targeting a vessel and we're near it (10km) then rendezvous with it instead.
                Vessel targetVessel = null;
                if (vessel.targetObject != null)
                    targetVessel = vessel.targetObject.GetVessel();
                if (targetVessel != null)
                {
                    // Check SOI
                    if (vessel.mainBody != targetVessel.mainBody)
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_needsSameSOI"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                        circularizationState = WBICircularizationStates.doNotCircularize;
                        return;
                    }

                    double distanceMeters = Math.Abs((part.vessel.GetWorldPos3D() - targetVessel.GetWorldPos3D()).magnitude);
                    double minRendezvousDistance = BlueshiftScenario.minRendezvousDistancePlanetary;
                    if (spatialLocation == WBISpatialLocations.Interplanetary)
                        minRendezvousDistance = BlueshiftScenario.minRendezvousDistanceInterplanetary;

                    // Check rendezvous distance
                    if (distanceMeters <= minRendezvousDistance)
                    {
                        Vector3 position = UnityEngine.Random.onUnitSphere * BlueshiftScenario.rendezvousDistance;
                        FlightGlobals.fetch.SetShipOrbitRendezvous(vessel.targetObject.GetVessel(), position, Vector3d.zero);
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_rendezvousComplete"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                        circularizationState = WBICircularizationStates.hasBeenCircularized;
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_needsMinDistance"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                        circularizationState = WBICircularizationStates.doNotCircularize;
                    }
                    return;
                }

                // Get current orbit.
                Orbit orbit = this.part.vessel.orbitDriver.orbit;
                double inclination = autoCircularizeInclination; // orbit.inclination;
                double altitude = this.part.vessel.altitude;
                double planetRadius = this.part.vessel.mainBody.Radius;
                int planetIndex = this.part.vessel.mainBody.flightGlobalsIndex;
                double currentTime = Planetarium.GetUniversalTime();
                Orbit circlularOrbit = new Orbit(inclination, 0, planetRadius + altitude, 0, 0, 0, currentTime, this.part.vessel.mainBody);
                Orbit.State state;

                // Adjust the state vectors. This will shift the vessel's position, but the ship would've shifted around during normal gravity braking anyway.
                circlularOrbit.GetOrbitalStateVectorsAtUT(currentTime, out state);
                circlularOrbit.UpdateFromFixedVectors(orbit.pos, state.vel, this.part.vessel.mainBody.referenceBody, currentTime + 0.02);
                FlightGlobals.fetch.SetShipOrbit(planetIndex, 0, planetRadius + altitude, inclination, circlularOrbit.LAN, circlularOrbit.meanAnomaly, circlularOrbit.argumentOfPeriapsis, circlularOrbit.ObT);

                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_orbitCircularized"), kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                circularizationState = WBICircularizationStates.hasBeenCircularized;
            }
        }

        /// <summary>
        /// Action menu item to circularize the ship's orbit.
        /// </summary>
        /// <param name="param"></param>
        [KSPAction("#LOC_BLUESHIFT_circularizeOrbit")]
        public void CircularizeOrbitAction(KSPActionParam param)
        {
            CircularizeOrbit();
        }
        #endregion

        #region Overrides
        public void OnDestroy()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselSOIChanged.Remove(onVesselSOIChanged);
            }

            disableGeneratorBypass();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Get timewarping state.
            isTimewarping = TimeWarping();

            // Update spatial location.
            spatialLocation = BlueshiftScenario.shared.GetSpatialLocation(part.vessel);
            if (prevSpatialLocation == WBISpatialLocations.Unknown)
                prevSpatialLocation = spatialLocation;

            // Update throttle level
            throttleLevel = FlightInputHandler.state.mainThrottle * (thrustPercentage / 100.0f);

            // If ship's course and speed are locked, then we're timewarping and we need to:
            // Update throttleLevel based solely on thrustPercentage.
            // Update the engine FX.
            // If player hits the cut throttle key, then drop out of timewarp.
            if (lockedCourseAndSpeed)
            {

                throttleLevel = thrustPercentage / 100.0f;

                if (this.EngineIgnited)
                    this.UpdatePropellantStatus(true);

                requestedThrottle = throttleLevel;
                currentThrottle = throttleLevel;
                UpdateThrottle();
                if (EngineIgnited)
                    UpdatePropellantStatus(true);
                ThrustUpdate();
                FXUpdate();

                // Kill timewarp if user taps on cut throttle.
                if (UnityEngine.Input.GetKeyDown(GameSettings.THROTTLE_CUTOFF.primary.code) || 
                    UnityEngine.Input.GetKeyDown(GameSettings.THROTTLE_CUTOFF.secondary.code) || 
                    UnityEngine.Input.GetKeyDown(GameSettings.BRAKES.primary.code) || 
                    UnityEngine.Input.GetKeyDown(GameSettings.BRAKES.secondary.code)
                    )
                { 
                    TimeWarp.SetRate(0, false);
                }
            }

            // If we're not timewapring and the user taps on the brakes then cut the throttle.
            else if (!isTimewarping && (UnityEngine.Input.GetKeyDown(GameSettings.BRAKES.primary.code) || UnityEngine.Input.GetKeyDown(GameSettings.BRAKES.secondary.code)))
            {
                FlightInputHandler.state.mainThrottle = 0f;
                throttleLevel = 0f;
            }

            // Make sure the engine is running
            // Make sure that we should apply warp.
            if (!EngineIgnited || !shouldApplyWarp())
                return;

            // Drive warp coil resource consumption.
            updateWarpPowerGenerators();

            // Update our total warp capacity and warp speed.
            updateWarpCapacityAndSpeed();

            // Update our precondition states
            updatePreconditionStates();

            // Now check for flameout
            if (IsFlamedOut())
            {
                resetWarpParameters();

                if (lockedCourseAndSpeed)
                {
                    cancelWarpCruiseVelocity();
                    lockedCourseAndSpeed = false;
                    TimeWarp.SetRate(0, false);
                }

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

            // Update warp spedometer
            updateWarpSpedometer();

            if (!HighLogic.LoadedSceneIsFlight)
                return;
            getCoilsAndGenerators();

            // Vessel course - just the selected target.
            updateVesselCourseUI();

            // Update preflight status
            updateFTLPreflightStatus();

            if (!isOperational && !EngineIgnited)
            {
                fadeOutEffects();
                spatialLocation = BlueshiftScenario.shared.GetSpatialLocation(part.vessel);
                return;
            }
            else if (flameout || warpFlameout)
            {
                fadeOutEffects();
            }

            // Update warp engine status
            UpdateWarpStatus();

            // Update auto-circularization display
            bool enableCircularizeOrbit = BlueshiftScenario.autoCircularize && spatialLocation != WBISpatialLocations.Interstellar && throttleLevel <= 0;
            Events["CircularizeOrbit"].active = enableCircularizeOrbit && isOperational;
            Fields["autoCircularizeInclination"].guiActive = enableCircularizeOrbit && isOperational;
            Actions["CircularizeOrbitAction"].active = enableCircularizeOrbit && isOperational;
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();
            info.Append(base.GetInfo());
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_warpEngineDesc"));
            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            loadFloatCurves();

            // Use global warp performance improvement skills if the part doesn't define any.
            if (string.IsNullOrEmpty(warpEngineerSkill))
                warpEngineerSkill = BlueshiftScenario.warpEngineerSkill;
            if (warpSpeedBoostRank <= 0)
                warpSpeedBoostRank = BlueshiftScenario.warpSpeedBoostRank;
            if (warpSpeedSkillMultiplier <= 0)
                warpSpeedSkillMultiplier = BlueshiftScenario.warpSpeedSkillMultiplier;

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
            debugMode = BlueshiftScenario.debugMode;
            Fields["isInSpace"].guiActive = debugMode;
            Fields["meetsWarpAltitude"].guiActive = debugMode;
            Fields["hasWarpCapacity"].guiActive = debugMode;
            Fields["applyWarpTranslation"].guiActive = debugMode;
            Fields["totalDisplacementImpulse"].guiActive = debugMode;
            Fields["totalWarpCapacity"].guiActive = debugMode;
            Fields["minPlanetaryRadius"].guiActive = debugMode;
            Fields["effectiveWarpCapacity"].guiActive = debugMode;
            Fields["warpDistance"].guiActive = debugMode;
            Fields["waterfallEffectsLevel"].guiActive = debugMode;
            Fields["warpResourceProduced"].guiActive = debugMode;
            Fields["warpResourceRequired"].guiActive = debugMode;
            Fields["warpResourceAmount"].guiActive = debugMode;
            Fields["warpResourceMaxAmount"].guiActive = debugMode;

            // Editor events
            if (HighLogic.LoadedSceneIsEditor)
            {
                Fields["effectiveWarpCapacity"].guiActiveEditor = true;
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                if (EditorLogic.fetch != null && EditorLogic.fetch.ship != null)
                    onEditorShipModified(EditorLogic.fetch.ship);

                Fields["superchargerMultiplier"].uiControlEditor.onFieldChanged += new Callback<BaseField, object>(onSuperchargerFieldChanged);
                Fields["superchargerMultiplier"].uiControlEditor.onSymmetryFieldChanged += new Callback<BaseField, object>(onSuperchargerFieldChanged);
                Fields["thrustPercentage"].uiControlEditor.onFieldChanged += new Callback<BaseField, object>(onSuperchargerFieldChanged);
                Fields["thrustPercentage"].uiControlEditor.onSymmetryFieldChanged += new Callback<BaseField, object>(onSuperchargerFieldChanged);
            }

            // In flight events
            else if (HighLogic.LoadedSceneIsFlight)
            {
                GameEvents.onVesselSOIChanged.Add(onVesselSOIChanged);
            }

            // Other
            if (interstellarResourceConsumptionModifier < 0)
            {
                interstellarResourceConsumptionModifier = BlueshiftScenario.interstellarResourceConsumptionModifier;
            }
        }

        public override void Flameout(string message, bool statusOnly = false, bool showFX = true)
        {
            base.Flameout(message, statusOnly, showFX);
            warpFlameout = true;
            hasExceededLightSpeed = false;
            spatialLocation = WBISpatialLocations.Unknown;
            disableGeneratorBypass();
            onWarpEngineFlameout.Fire(part.vessel, this);
        }

        public override void UnFlameout(bool showFX = true)
        {
            if (warpFlameout)
                return;
            base.UnFlameout(showFX);
            onWarpEngineUnFlameout.Fire(part.vessel, this);
        }

        public override void Activate()
        {
            base.Activate();
            if (HighLogic.LoadedSceneIsEditor)
                return;
            if (!staged)
                part.force_activate();

            Fields["warpSpeedDisplay"].guiActive = true;
            Fields["maxWarpSpeedDisplay"].guiActive = true;
            Fields["preflightCheck"].guiActive = true;

            int count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = true;
            }

            // Auto-start any inactive converters that aren't broken. Because people are too damn lazy...
            List<ModuleResourceConverter> powerPlants = part.FindModulesImplementing<ModuleResourceConverter>();
            ModuleResourceConverter powerPlant;
            count = powerPlants.Count;
            for (int index = 0; index < count; index++)
            {
                powerPlant = powerPlants[index];
                if (!powerPlant.enabled)
                    continue;
                powerPlant.StartResourceConverter();
            }

            // Auto-start any integrated warp coils
            List<WBIWarpCoil> coils = part.FindModulesImplementing<WBIWarpCoil>();
            count = coils.Count;
            for (int index = 0; index < count; index++)
                coils[index].isActivated = true;

            onWarpEngineStart.Fire(part.vessel, this);
            spatialLocation = BlueshiftScenario.shared.GetSpatialLocation(part.vessel);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            if (HighLogic.LoadedSceneIsEditor)
                return;
            hasExceededLightSpeed = false;
            spatialLocation = WBISpatialLocations.Unknown;

            Fields["warpSpeedDisplay"].guiActive = false;
            Fields["maxWarpSpeedDisplay"].guiActive = false;
            Fields["preflightCheck"].guiActive = false;
            Events["CircularizeOrbit"].active = false;

            int count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = false;
            }

            disableGeneratorBypass();

            // Auto-stop any converters that aren't broken
            List<ModuleResourceConverter> powerPlants = part.FindModulesImplementing<ModuleResourceConverter>();
            ModuleResourceConverter powerPlant;
            count = powerPlants.Count;
            for (int index = 0; index < count; index++)
            {
                powerPlant = powerPlants[index];
                if (!powerPlant.enabled)
                    continue;
                powerPlant.StopResourceConverter();
            }

            // Auto-stop any integrated warp coils
            List<WBIWarpCoil> coils = part.FindModulesImplementing<WBIWarpCoil>();
            count = coils.Count;
            for (int index = 0; index < count; index++)
                coils[index].isActivated = false;

            onWarpEngineShutdown.Fire(part.vessel, this);
            spatialLocation = BlueshiftScenario.shared.GetSpatialLocation(part.vessel);
            resetWarpParameters();
        }

        public override void DeactivatePowerFX()
        {
            if (!lockedCourseAndSpeed)
                base.DeactivatePowerFX();
        }

        public override void DeactivateRunningFX()
        {
            if (!lockedCourseAndSpeed)
                base.DeactivateRunningFX();
        }

        public override void FXUpdate()
        {
            base.FXUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            // Set operational status
            if (EngineIgnited && isEnabled && !flameout && !warpFlameout)
            {
                part.Effect(runningEffectName, 1f);
            }

            // Setup throttle
            int count = 0;
            float effectsPowerLevel = FlightInputHandler.state.mainThrottle;
            if (lockedCourseAndSpeed)
            {
                effectsPowerLevel = 1f;

                // Manually drive the FX, UpdateFX will disable the effects during timewarp.
                part.Effect(powerEffectName, effectsPowerLevel);
            }
            else if (!isOperational)
            {
                effectsPowerLevel = 0;
            }

            // Update our animated texture module, if any.
            count = warpEngineTextures.Count;
            for (int index = 0; index < count; index++)
            {
                warpEngineTextures[index].isActivated = EngineIgnited;
                warpEngineTextures[index].animationThrottle = effectsPowerLevel > 0f ? effectsPowerLevel : 0.1f;
            }

            // If we aren't supposed to apply warp translation then there's nothing more to do.
            if (!applyWarpTranslation)
                return;

            // Update active warp coils
            count = warpCoils.Count;
            for (int index = 0; index < count; index++)
            {
                warpCoils[index].animationThrottle = !warpFlameout ? effectsPowerLevel : 0f;
            }

            // Update warp effects
            if (bowShockTransform != null)
            {
                if (part.vessel.vesselSize == Vector3.zero)
                    part.vessel.UpdateVesselSize();
                Vector3 localPosition = new Vector3(0, part.vessel.vesselSize.y, 0);
                bowShockTransform.localPosition = localPosition;
            }

            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
            {
                float targetValue = 0f;
                if (throttleLevel > 0)
                    targetValue = waterfallWarpEffectsCurve.Evaluate(warpSpeed);

                waterfallEffectsLevel = Mathf.Lerp(waterfallEffectsLevel, targetValue, engineSpoolTime);

               if (waterfallEffectsLevel <= 0.001 && targetValue <= 0f)
                    waterfallEffectsLevel = 0;
                else if (targetValue > 0f && targetValue >= waterfallEffectsLevel && (waterfallEffectsLevel / targetValue >= 0.99f))
                    waterfallEffectsLevel = targetValue;
                else if (targetValue > 0f && waterfallEffectsLevel > targetValue && targetValue / waterfallEffectsLevel >= 0.99f)
                    waterfallEffectsLevel = targetValue;

                waterfallFXModule.SetControllerValue(waterfallEffectController, waterfallEffectsLevel);
            }

            if (!hasExceededLightSpeed && warpSpeed >= 1f)
            {
                hasExceededLightSpeed = true;
                this.part.Effect(photonicBoomEffectName, 1);
            }

            onWarpEffectsUpdated.Fire(part.vessel, this, effectsPowerLevel);
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
            // If our power multiplier is below the ignition threshold then we are flamed out.
            if (powerMultiplier < warpIgnitionThreshold)
            {
                warpFlameout = true;
                Flameout(Localizer.Format("#LOC_BLUESHIFT_needsMorePower"));
                return true;
            }

            // Must be in space, at or above orbital altitude, have a running engine, and not be flamed out.
            else if (!isInSpace || !meetsWarpAltitude || !hasWarpCapacity && EngineIgnited && !flameout && !warpFlameout)
            {
                warpFlameout = true;
                if (!isInSpace)
                    Flameout(Localizer.Format("#LOC_BLUESHIFT_needsSpaceflight"));
                else if (!meetsWarpAltitude)
                    Flameout(Localizer.Format("#LOC_BLUESHIFT_needsAltitude"));
                else if (!hasWarpCapacity)
                    Flameout(Localizer.Format("#LOC_BLUESHIFT_needsWarpCapacity"));
                else
                    Flameout(Localizer.Format("#LOC_BLUESHIFT_flameoutGeneric"));
                return true;
            }

            else if (warpFlameout && isInSpace && meetsWarpAltitude && hasWarpCapacity && powerMultiplier >= warpIgnitionThreshold)
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
            if (EngineIgnited && throttleLevel <= 0)
                return true;

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
        protected void updateFTLPreflightStatus()
        {
            if (powerMultiplier < warpIgnitionThreshold)
            {
                preflightCheck = Localizer.Format("#LOC_BLUESHIFT_needsMorePower");
            }

            else if (maxWarpSpeed < 1)
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

                // If effective warp capacity > calculated warp capaciy, then we actually can go faster than light but we just don't want to.
                if (effectiveWarpCapacity > warpCapacity)
                {
                    preflightCheck = spatialLocation != WBISpatialLocations.Planetary ? Localizer.Format("#LOC_BLUESHIFT_IncreaseThrottle") : Localizer.Format("#LOC_BLUESHIFT_planetarySpeedLimit");
                }
                else
                {
                    preflightCheck = Localizer.Format("#LOC_BLUESHIFT_addWarpCapacity", new string[2] { string.Format("{0:n2}", effectiveWarpCapacity), string.Format("{0:n2}", warpCapacity) });
                }
            }

            else if (throttleLevel <= 0 && effectiveWarpCapacity > 0)
            {
                if (HighLogic.LoadedSceneIsFlight)
                    preflightCheck = Localizer.Format("#LOC_BLUESHIFT_warpZeroThrottleReady");
                else if (HighLogic.LoadedSceneIsEditor)
                    preflightCheck = Localizer.Format("#LOC_BLUESHIFT_IncreaseThrottle");
                else
                    preflightCheck = "Um, whut?";
            }

            else
            {
                preflightCheck = Localizer.Format("#LOC_BLUESHIFT_canGoFTL");
            }
        }

        /// <summary>
        /// Fades out the warp effects
        /// </summary>
        protected void fadeOutEffects()
        {
            if (waterfallFXModule != null && waterfallEffectsLevel > 0f)
            {
                waterfallEffectsLevel = Mathf.Lerp(waterfallEffectsLevel, 0, engineSpoolTime);

                if (waterfallEffectsLevel <= 0.001f)
                    waterfallEffectsLevel = 0f;

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

        private void onSuperchargerFieldChanged(BaseField baseField, object obj)
        {
            vesselPartCount = -1;
            onEditorShipModified(EditorLogic.fetch.ship);
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            int count = ship.parts.Count;
            if (vesselPartCount == count)
                return;
            else
                vesselPartCount = count;

            WBIWarpCoil warpCoil;
            WBIModuleGeneratorFX generator;
            WBIWarpEngine warpEngine;

            warpGenerators.Clear();
            warpCoils.Clear();
            warpEngines.Clear();

            for (int index = 0; index < count; index++)
            {
                // Find generators
                generator = ship.parts[index].FindModuleImplementing<WBIModuleGeneratorFX>();
                if (generator != null)
                    warpGenerators.Add(generator);

                // Find warp coils
                warpCoil = ship.parts[index].FindModuleImplementing<WBIWarpCoil>();
                if (warpCoil != null)
                    warpCoils.Add(warpCoil);

                // Find engines
                warpEngine = ship.parts[index].FindModuleImplementing<WBIWarpEngine>();
                if (warpEngine != null)
                    warpEngines.Add(warpEngine);
            }

            if (warpEngines.Count <= 0)
                return;

            // Get total warp capacity
            getTotalWarpCapacity();

            // Calculate best warp speed
            spatialLocation = WBISpatialLocations.Interplanetary;
            throttleLevel = thrustPercentage / 100.0f;
            wentInterstellar = false;
            speedStartTime = 0f;
            warpFlameout = false;
            calculateBestWarpSpeed();

            // Update display
            updateFTLPreflightStatus();

            updateWarpSpedometer();

            //Dirty the GUI
            MonoUtilities.RefreshContextWindows(this.part);
        }

        void updateWarpSpedometer()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                warpSpeedDisplay = (warpSpeed + prevWarpSpeed) / 2f;
                maxWarpSpeedDisplay = (maxWarpSpeed + prevMaxWarpSpeed) / 2f;
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                warpSpeedDisplay = warpSpeed;
                maxWarpSpeedDisplay = maxWarpSpeed;
            }

            prevMaxWarpSpeed = maxWarpSpeed;
            prevWarpSpeed = warpSpeed;
        }

        void updateWarpCapacityAndSpeed()
        {
            // Give generators a chance to build up a charge whenever we change the throttle or cross a spatial boundary or the ship is nearly out of gravity waves.
            float diff = Mathf.Abs(prevThrottle * 0.0001f);
            bool throttlesEqual = Mathf.Abs(prevThrottle - throttleLevel) <= diff;

            // If we crossed a spatial boundary then skip frames.
            bool crossedSpatialBoundary = prevSpatialLocation != spatialLocation;
            if (crossedSpatialBoundary)
            {
                wentInterstellar = spatialLocation == WBISpatialLocations.Interstellar;

                prevSpatialLocation = spatialLocation;

                if (isTimewarping && lockedCourseAndSpeed && !needsVelocityUpdate)
                    needsVelocityUpdate = true;

                resumeUpdateTimestamp = Planetarium.GetUniversalTime() + kFrameSkipTime;
            }

            // If throttle changed then skip frames.
            else if (!throttlesEqual)
            {
                resumeUpdateTimestamp = Planetarium.GetUniversalTime() + kFrameSkipTime;

                // If we were throttled down and we had no warp capacity, then set a small amount of warp capacity to prevent flameout.
                if (prevThrottle <= 0 && totalWarpCapacity <= 0)
                    totalWarpCapacity = 0.0001f;

                prevThrottle = throttleLevel;
            }

            // Calculate the best warp curve to get maximum FTL speed.
            else if (Planetarium.GetUniversalTime() >= resumeUpdateTimestamp || resumeUpdateTimestamp <= 0)
            {
                getTotalWarpCapacity();
                calculateBestWarpSpeed();
            }
        }

        /// <summary>
        /// Calculates the best possible warp speed from the vessel's active warp engines.
        /// </summary>
        protected void calculateBestWarpSpeed()
        {
            maxWarpSpeed = 0;
            warpSpeed = 0;

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
            if (maxWarpSpeed < 0)
                maxWarpSpeed = 0;

            // In the editor, max warp speed is limited by throttleLevel.
            if (HighLogic.LoadedSceneIsEditor)
            {
                maxWarpSpeed = maxWarpSpeed * throttleLevel;
            }

            // Adjust warp speed based on spatial location.
            switch (spatialLocation)
            {
                // If we're in interstellar space then we can increase our max warp speed.
                case WBISpatialLocations.Interstellar:
                    maxWarpSpeed *= BlueshiftScenario.interstellarWarpSpeedMultiplier;
                    break;

                // Limit speed if we're in a planetary SOI
                case WBISpatialLocations.Planetary:
                    if (planetarySpeedBrakeEnabled)
                    {
                        float speedRatio = (float)(this.part.vessel.altitude / this.part.vessel.mainBody.sphereOfInfluence);
                        maxWarpSpeed *= planetarySOISpeedCurve.Evaluate(speedRatio);

                        // Avoid snail's pace. We should have a minimum of 0.001C in planetary space.
                        if (maxWarpSpeed < 0.001f && bestWarpSpeed > 0.001f)
                            maxWarpSpeed = 0.001f;
                    }
                    break;

                // No speed adjustment while interplanetary.
                case WBISpatialLocations.Interplanetary:
                default:
                    break;
            }

            // Account for miracle workers
            int highestRank = 0;
            ProtoCrewMember astronaut;
            if (HighLogic.LoadedSceneIsFlight)
            {
                highestRank = BlueshiftScenario.shared.GetHighestRank(vessel, warpEngineerSkill, out astronaut);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                ShipConstruct ship = EditorLogic.fetch.ship;
                highestRank = BlueshiftScenario.shared.GetHighestRank(ship, warpEngineerSkill, out astronaut);
            }
            float skillMultiplier = 0f;
            if (highestRank >= warpSpeedBoostRank)
            {
                skillMultiplier = 1.0f + (warpSpeedSkillMultiplier * highestRank);
                maxWarpSpeed *= skillMultiplier;
            }

            // Account for throttle setting and thrust limiter.
            if (throttleLevel <= 0 || maxWarpSpeed <= 0)
            {
                warpSpeed = 0;
                if (spatialLocation == WBISpatialLocations.Interstellar)
                    speedStartTime = Planetarium.GetUniversalTime();
                return;
            }

            // Finalize warp speed.
            warpSpeed = maxWarpSpeed * throttleLevel;
        }

        /// <summary>
        /// Calulates the total warp capacity from the vessel's active warp coils. Each warp coil must successfully consume its required resources in order to be considered.
        /// </summary>
        protected void getTotalWarpCapacity()
        {
            int count = warpCoils.Count;
            WBIWarpCoil warpCoil;
            WBIModuleGeneratorFX generator;
            float vesselMass = 0;
            double totalResourceRequired = 0;
            double totalResourceProduced = 0;
            float totalDisplacement = 0;

            totalWarpCapacity = 0;
            totalDisplacementImpulse = 0;
            powerMultiplier = 0;
            displacementMultiplier = 0;
            effectiveWarpCapacity = 0;
            warpResourceProduced = 0;
            warpResourceRequired = 0;
            consumptionMultiplier = 0;

            // Get ship mass
            if (HighLogic.LoadedSceneIsFlight)
                vesselMass = part.vessel.GetTotalMass();
            else if (HighLogic.LoadedSceneIsEditor)
                vesselMass = EditorLogic.fetch.ship.GetTotalMass();

            // Get the ideal amount of resource produced by the generators.
            count = warpGenerators.Count;
            for (int index = 0; index < count; index++)
            {
                generator = warpGenerators[index];
                if (HighLogic.LoadedSceneIsFlight && (!generator.isEnabled || !generator.IsActivated || generator.isMissingResources))
                    continue;
                totalResourceProduced += generator.GetAmountProduced(warpSimulationResource);
            }

            // Get the ideal amount of resource required by the coils
            count = warpCoils.Count;
            for (int index = 0; index < count; index++)
            {
                warpCoil = warpCoils[index];
                if (HighLogic.LoadedSceneIsFlight && (!warpCoil.isEnabled || !warpCoil.isActivated))
                    continue;
                totalResourceRequired += warpCoil.GetAmountRequired(warpSimulationResource);
            }

            // Update warp resource statistics
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            resourceDef = definitions[warpSimulationResource];

            if (HighLogic.LoadedSceneIsFlight)
            {
                vessel.GetConnectedResourceTotals(resourceDef.id, out warpResourceAmount, out warpResourceMaxAmount);
            }
            else if (HighLogic.LoadedSceneIsEditor)
            {
                EditorLogic.fetch.ship.GetConnectedResourceTotals(resourceDef.id, true, out warpResourceAmount, out warpResourceMaxAmount);
                warpResourceAmount = warpResourceMaxAmount;
            }

            warpResourceProduced = totalResourceProduced;
            warpResourceRequired = totalResourceRequired;

            // Calculate the power multiplier
            if (totalResourceRequired > 0)
                powerMultiplier = (float)(totalResourceProduced / totalResourceRequired);
            else
                powerMultiplier = 0.0001f;

            // If the supercharger is off then set the powerMultiplier to 1. Otherwise adust the power multiplier.
            if (powerMultiplier > 1)
            {
                if (superchargerMultiplier <= 0)
                    powerMultiplier = 1f;
                else
                    powerMultiplier = 1f + ((powerMultiplier - 1f) * (superchargerMultiplier / 100f));
            }

            // Check for flamout
            if (powerMultiplier < warpIgnitionThreshold)
            {
                warpFlameout = true;
                return;
            }

            // Calculate the consumption multiplier and update the EVA Repairs module (if any)
            consumptionMultiplier = powerMultiplier;

            // Calculate displacement impulse and warp capacity for the active coils that are powered up.
            count = warpCoils.Count;
            for (int index = 0; index < count; index++)
            {
                warpCoil = warpCoils[index];
                if (HighLogic.LoadedSceneIsFlight && (!warpCoil.isActivated || warpCoil.needsMaintenance))
                    continue;

                if (consumeCoilResources(warpCoil, consumptionMultiplier))
                {
                    totalWarpCapacity += warpCoil.totalWarpCapacity;
                    totalDisplacement += warpCoil.displacementImpulse;
                }
            }

            // Calculate displacement multiplier
            displacementMultiplier = totalDisplacement / vesselMass;

            // Get effective warp capacity
            effectiveWarpCapacity = totalWarpCapacity * displacementMultiplier * powerMultiplier;
        }

        void clampMinimumSuperchageLevel()
        {

        }

        bool consumeCoilResources(WBIWarpCoil warpCoil, double rateMultiplier)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return true;

            string errorStatus = string.Empty;
            int count = warpCoil.resHandler.inputResources.Count;

            if (!warpCoil.isActivated || warpCoil.needsMaintenance)
                return false;
            if (count == 0)
                return true;

            // Update the rate multiplier and MTBF
            warpCoil.UpdateMTBFRateMultiplier(rateMultiplier);
            if (warpCoil.part != part)
            {
                warpCoil.UpdateMTBF(TimeWarp.fixedDeltaTime);
            }

            // Consume resource
            return warpCoil.ConsumeResources(rateMultiplier, isTimewarping);
        }

        /// <summary>
        /// Updates the generators that provide warp power.
        /// </summary>
        protected void updateWarpPowerGenerators()
        {
            int count = warpGenerators.Count;
            WBIModuleGeneratorFX generator;
            double resourceModifier = 1.0f;

            // Calculate interstellar resource consumption modifier
            resourceModifier = 1f - (Mathf.Clamp(interstellarResourceConsumptionModifier, 0f, 99.999f) / 100.0f);

            // Run cycle for each generator
            for (int index = 0; index < count; index++)
            {
                generator = warpGenerators[index];
                generator.bypassRunCycle = true;
                generator.resourceConsumptionModifier = spatialLocation == WBISpatialLocations.Interstellar ? resourceModifier : 1.0f;
                generator.RunGeneratorCycle();
            }
        }

        /// <summary>
        /// In order to synchronize the converter's process with the active warp engine, we enable a generator bypass. The moment that we no longer need to do that, such as when
        /// the engine is shut down, or it flames out, we want to disable the bypass.
        /// </summary>
        protected void disableGeneratorBypass()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            try
            {
                if (applyWarpTranslation)
                {
                    warpGenerators = part.vessel.FindPartModulesImplementing<WBIModuleGeneratorFX>();
                    int count = warpGenerators.Count;
                    for (int index = 0; index < count; index++)
                    {
                        warpGenerators[index].bypassRunCycle = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log(ex);
            }
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
            for (int index = 0; index < count; index++)
            {
                engine = engines[index];
                if (engine.EngineIgnited && engine.isOperational)
                {
                    warpEngines.Add(engine);

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
            prevMaxWarpSpeed = 0;
            prevWarpSpeed = 0;
            warpSpeedDisplay = 0;
            maxWarpSpeedDisplay = 0;
            speedStartTime = 0;
            warpDistance = 0;
            speedStartTime = 0f;
            effectiveWarpCapacity = 0;
            hasExceededLightSpeed = false;
            FlightInputHandler.state.mainThrottle = 0;
            circularizationState = WBICircularizationStates.doNotCircularize;
        }

        private void cancelWarpCruiseVelocity()
        {
            // Concept courtesy of KSPIE.
            // Create a reverse vector to slow down.
            Vector3 warpVector = new Vector3d(-warpCruiseVector.x, -warpCruiseVector.y, -warpCruiseVector.z);

            // Account for pre-cruise velocity.
            warpVector += new Vector3d(-preCruiseVelocity.x, -preCruiseVelocity.y, -preCruiseVelocity.z);

            // Account for current body's velocity
            if (vessel.mainBody.orbit != null)
                warpVector += vessel.mainBody.orbit.GetFrameVel();

            // Now update the vessel's velocity.
            part.vessel.IgnoreGForces(2);

            if (!vessel.packed)
                vessel.GoOnRails();

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + warpVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!vessel.packed)
                vessel.GoOffRails();
        }

        private void addWarpCruiseVelocity()
        {
            Transform refTransform = part.vessel.ReferenceTransform;

            // Concept courtesy of KSPIE.
            // Get the current velocity, we'll need to restore it when we're done with timewarp.
            preCruiseVelocity = vessel.orbit.GetFrameVel();

            // Calculate the cruise velocity. We're about to go very fast.
            warpCruiseVector = new Vector3d(refTransform.up.x, refTransform.up.z, refTransform.up.y) * warpSpeed * kLightSpeed;

            // Now adjust the orbit with the new velocity.
            part.vessel.IgnoreGForces(2);
            if (!vessel.packed)
                vessel.GoOnRails();

            vessel.orbit.UpdateFromStateVectors(vessel.orbit.pos, vessel.orbit.vel + warpCruiseVector, vessel.orbit.referenceBody, Planetarium.GetUniversalTime());

            if (!vessel.packed)
                vessel.GoOffRails();
        }

        private void updateVesselPosition()
        {
            // First, calculate offset position.
            warpDistance = kLightSpeed * warpSpeed * TimeWarp.fixedDeltaTime;
            Transform refTransform = part.vessel.ReferenceTransform;
            Vector3 warpVector = refTransform.up * warpDistance;
            Vector3d offsetPosition = refTransform.position + warpVector;

            // Next, make sure that we won't run into a celestial body.
            if (previousBody != part.orbit.referenceBody)
            {
                previousBody = part.orbit.referenceBody;
                bodyBounds = previousBody.getBounds();
            }

            if (bodyBounds.Contains(offsetPosition))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_terrainWarning"), 3.0f, ScreenMessageStyle.UPPER_CENTER);
                FlightInputHandler.state.mainThrottle = 0;
                return;
            }

            // We aren't going to run into anything, so update the vessel's position.
            if (FlightGlobals.VesselsLoaded.Count > 1)
                part.vessel.SetPosition(offsetPosition);
            else
                FloatingOrigin.SetOutOfFrameOffset(offsetPosition);
        }

        private void travelAtWarp()
        {
            if (Planetarium.GetUniversalTime() < resumeUpdateTimestamp)
                return;

            // If we're not timewarping but our course and speed was locked, then cancel our warp cruise velocity.
            if (!isTimewarping && lockedCourseAndSpeed)
            {
                // Unlock course and speed.
                lockedCourseAndSpeed = false;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_courseUnlocked"), 3.0f, ScreenMessageStyle.UPPER_CENTER);

                // Cancel the warp cruise velocity
                cancelWarpCruiseVelocity();

                // Give the game time to catch up
                resumeUpdateTimestamp = Planetarium.GetUniversalTime() + kFrameSkipTime;
            }

            // If we're timewarping and we haven't locked our course and speed, then do so now.
            else if (isTimewarping && !lockedCourseAndSpeed && throttleLevel > 0)
            {
                // Lock the course and speed.
                lockedCourseAndSpeed = true;
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_BLUESHIFT_courseLocked"), 3.0f, ScreenMessageStyle.UPPER_CENTER);

                // Add the timewarp cruise velocity
                addWarpCruiseVelocity();

                // Give the game time to catch up
                resumeUpdateTimestamp = Planetarium.GetUniversalTime() + kFrameSkipTime;
            }

            // If we've locked our course and speed, we're timewarping, and we need a velocity update, then redo our warp cruise velocity.
            else if (isTimewarping && lockedCourseAndSpeed && needsVelocityUpdate)
            {
                // Reset the flag
                needsVelocityUpdate = false;

                // Cancel current cruise velocity
                cancelWarpCruiseVelocity();

                // Unlock course and speed. This will force the warp engine to recalcuate and reset cruising velocity.
                lockedCourseAndSpeed = false;

                // Give the game time to catch up
                resumeUpdateTimestamp = Planetarium.GetUniversalTime() + kFrameSkipTime;
            }

            // If we aren't timewarping and our throttle level > 0, then update the vessel's position.
            else if (!isTimewarping && throttleLevel > 0)
            {
                updateVesselPosition();
            }
        }

        private void updateVesselCourseUI()
        {
            string units = string.Empty;

            targetDistance = BlueshiftScenario.shared.GetDistanceToTarget(part.vessel, out units, out vesselCourse);
            targetDistanceUnits = units;

            if (targetDistance > 0)
            {
                Fields["vesselCourse"].guiActive = true;
                Fields["targetDistance"].guiActive = true;
                Fields["targetDistance"].guiUnits = units;
            }
            else
            {
                Fields["vesselCourse"].guiActive = false;
                Fields["targetDistance"].guiActive = false;
            }
        }

        private void onVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> vesselCelestialBody)
        {
            if (vesselCelestialBody.host != part.vessel || !lockedCourseAndSpeed)
                return;

            // If we went from interstellar space to non-interstellar space then kill timewarp.
            if (prevSpatialLocation == WBISpatialLocations.Interstellar && !BlueshiftScenario.shared.IsInInterstellarSpace(part.vessel))
                TimeWarp.SetRate(0, false);
        }
        #endregion
    }
}
