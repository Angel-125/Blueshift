using System.Collections.Generic;
using HarmonyLib;
using KSP.Localization;
using Highlighting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blueshift
{
    /// <summary>
    /// Multiplies KSP's fully calculated EVA Construction limit only for the active
    /// EVA kerbal whose resonator currently has Heft applied.
    /// </summary>
    [HarmonyPatch(typeof(EVAConstructionModeEditor),
        nameof(EVAConstructionModeEditor.CombinedConstructionWeightLimit),
        MethodType.Getter)]
    internal static class WBIEVAHeftConstructionWeightPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref double __result)
        {
            float multiplier;
            if (WBIEVAGravitics.tryGetActiveHeftMultiplier(out multiplier))
                __result *= multiplier;
        }
    }

    internal struct GraviticsPartHighlight
    {
        public Part highlightedPart;
        public Part.HighlightType highlightType;
        public Color highlightColor;
    }

    internal class SuspensionFieldTarget
    {
        public Vessel vessel;
        public readonly List<GraviticsPartHighlight> highlights =
            new List<GraviticsPartHighlight>();
    }

    /// <summary>
    /// Provides the EVA abilities supplied by a KerbalGear Graviolium Field Resonator.
    /// The module is created dynamically while a carried wearable requests it.
    /// </summary>
    [KSPModule("#LOC_BLUESHIFT_evaGraviticsTitle")]
    public class WBIEVAGravitics : WBIPartModule
    {
        private enum GraviticsPower
        {
            None,
            Push,
            Pull,
            Shove,
            Launch
        }

        [System.Flags]
        private enum PowerSoundOwner
        {
            None = 0,
            Primary = 1,
            Lift = 2,
            SuspensionField = 4,
            Levitate = 8
        }

        #region Constants
        private const string ToggleEventName = "ToggleResonator";
        private const string EnableEventName = "#LOC_BLUESHIFT_evaGraviticsEnable";
        private const string DisableEventName = "#LOC_BLUESHIFT_evaGraviticsDisable";
        private const string GraviticsGroupName = "Gravitics";
        private const string GravioliumBeamFXNodeName = "GRAVIOLIUM_BEAM_FX";
        private const string GravioliumStarburstFXNodeName = "GRAVIOLIUM_STARBURST_FX";

        private static readonly Color ElectricBlue = new Color(0.0f, 0.68f, 1.0f, 1.0f);
        #endregion

        #region Fields
        /// <summary>
        /// Maximum distance, in meters, at which the resonator can acquire a target.
        /// </summary>
        [KSPField]
        public float targetingRange = 7.0f;

        /// <summary>
        /// Indicates whether the player has enabled the resonator.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool graviticsEnabled;

        /// <summary>
        /// Current gravitic power state shown in the EVA action window.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false,
            guiName = "#LOC_BLUESHIFT_evaGraviticsStatus",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        public string graviticsStatus = string.Empty;

        /// <summary>
        /// Scales the target speed of Lift, Suspension Field, and Levitate.
        /// At zero, those powers cancel vertical velocity and local gravity.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsForce",
            guiUnits = "%",
            guiFormat = "F0",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_FloatRange(stepIncrement = 1.0f, minValue = 0.0f, maxValue = 100.0f)]
        public float forcePercent = 100.0f;

        /// <summary>Looping sound played while a mouse-activated power is active.</summary>
        [KSPField]
        public string powerSoundPath =
            "WildBlueIndustries/Blueshift/FX/gravioliumBeam";

        /// <summary>Power sound volume before KSP's Ship volume is applied.</summary>
        [KSPField]
        public float powerSoundVolume = 1.0f;

        /// <summary>Playback pitch for the power sound.</summary>
        [KSPField]
        public float powerSoundPitch = 1.0f;

        /// <summary>
        /// Writes state-change and interaction diagnostics to KSP.log.
        /// </summary>
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// Brightness multiplier for the electric-blue pupil glow.
        /// </summary>
        [KSPField]
        public float pupilGlowIntensity = 1.5f;

        /// <summary>
        /// Rim falloff used to spread the glow across the pupil surface.
        /// </summary>
        [KSPField]
        public float pupilGlowFalloff = 0.1f;
        #endregion

        #region Power: Heft
        /// <summary>Increases this kerbal's stock EVA Construction lift limit.</summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerHeft",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerHeft;

        /// <summary>Multiplier applied to the calculated EVA Construction limit.</summary>
        [KSPField]
        public float heftMultiplier = 15.0f;

        /// <summary>Recovery time after Heft is disabled, in seconds.</summary>
        [KSPField]
        public float heftCooldown = 5.0f;

        /// <summary>Universal time at which Heft may be enabled again.</summary>
        [KSPField(isPersistant = true)]
        public double heftCooldownEndUT;

        private bool heftIsApplied;
        #endregion

        #region Power: Push
        /// <summary>
        /// Enables or disables the Push power.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerPush",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerPush;

        /// <summary>
        /// Continuous Push force in kilonewtons (KSP force units).
        /// </summary>
        [KSPField]
        public float pushForce = 0.5f;

        /// <summary>
        /// Base recovery time after Push, in seconds.
        /// </summary>
        [KSPField]
        public float pushBaseCooldown = 15.0f;

        /// <summary>
        /// Additional recovery time per second of continuous Push use.
        /// </summary>
        [KSPField]
        public float pushCooldownPerSecond = 2.0f;

        /// <summary>
        /// Universal time at which Push becomes available again.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double pushCooldownEndUT;

        private Part pushTarget;
        private bool pushIsActive;
        private double pushDuration;
        private float pushTargetMass;
        private double pushLocalGravity;
        private WBIGravioliumBeam gravioliumBeam;
        private readonly GravioliumBeamSettings pushBeamSettings =
            new GravioliumBeamSettings();
        private Part beamTarget;
        private Transform beamSourceTransform;
        private Transform leftPalmTransform;
        private Transform rightPalmTransform;
        private Transform beamSourceFallbackTransform;
        private Vector3 pushBeamTargetOffset;
        private Vector3 pushStarburstTargetOffset;
        private WBIGravioliumStarburst gravioliumStarburst;
        private readonly GravioliumStarburstSettings pushStarburstSettings =
            new GravioliumStarburstSettings();
        private GraviticsPower activePower;
        #endregion

        #region Power: Pull
        /// <summary>
        /// Enables or disables the Pull power.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerPull",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerPull;

        /// <summary>
        /// Continuous Pull force in kilonewtons (KSP force units).
        /// </summary>
        [KSPField]
        public float pullForce = 0.5f;

        /// <summary>
        /// Base recovery time after Pull, in seconds.
        /// </summary>
        [KSPField]
        public float pullBaseCooldown = 15.0f;

        /// <summary>
        /// Additional recovery time per second of continuous Pull use.
        /// </summary>
        [KSPField]
        public float pullCooldownPerSecond = 2.0f;

        /// <summary>
        /// Universal time at which Pull becomes available again.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double pullCooldownEndUT;
        #endregion

        #region Power: Levitate
        /// <summary>Gently levitates the active EVA kerbal while enabled.</summary>
        [KSPField(guiActive = false, guiActiveEditor = false,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerLevitate",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerLevitate;

        /// <summary>Target upward velocity, in meters per second.</summary>
        [KSPField]
        public float levitateTargetSpeed = 0.2f;

        /// <summary>Universal time at which Levitate becomes available again.</summary>
        [KSPField(isPersistant = true)]
        public double levitateCooldownEndUT;

        private bool levitateIsActive;
        private double levitateActiveDuration;
        private float levitateKerbalMass;
        private double levitateLocalGravity;
        private KerbalEVA levitateKerbalEVA;
        private readonly Dictionary<KerbalEVA, KFSMEvent> kerbalFloatEvents =
            new Dictionary<KerbalEVA, KFSMEvent>();
        private readonly Dictionary<KerbalEVA, KFSMEvent> kerbalLaunchEvents =
            new Dictionary<KerbalEVA, KFSMEvent>();
        #endregion

        #region Power: Lift
        /// <summary>
        /// Enables or disables the Lift power.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerLift",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerLift;

        /// <summary>Maximum vessel mass eligible for Lift, in tonnes.</summary>
        [KSPField]
        public float liftMaxMass = 5.0f;

        /// <summary>Maximum active Lift duration, in seconds.</summary>
        [KSPField]
        public float liftDuration = 10.0f;

        /// <summary>Target upward velocity, in meters per second.</summary>
        [KSPField]
        public float liftTargetSpeed = 0.1f;

        /// <summary>Time used to gently correct toward the target speed.</summary>
        [KSPField]
        public float liftVelocityResponseTime = 0.5f;

        /// <summary>Maximum speed-correction acceleration, in meters per second squared.</summary>
        [KSPField]
        public float liftMaxCorrectionAcceleration = 2.0f;

        /// <summary>Minimum recovery time after Lift, in seconds.</summary>
        [KSPField]
        public float liftBaseCooldown = 5.0f;

        /// <summary>Cooldown multiplier per active Lift second.</summary>
        [KSPField]
        public float liftCooldownPerSecond = 0.5f;

        /// <summary>Exponent applied to target mass in the Lift cooldown curve.</summary>
        [KSPField]
        public float liftCooldownMassExponent = 1.5f;

        /// <summary>Reference gravity used to normalize Lift cooldown calculations.</summary>
        [KSPField]
        public float liftCooldownReferenceGravity = 9.80665f;

        /// <summary>Universal time at which Lift becomes available again.</summary>
        [KSPField(isPersistant = true)]
        public double liftCooldownEndUT;

        private Part liftTarget;
        private bool liftIsActive;
        private double liftActiveDuration;
        private float liftTargetMass;
        private double liftLocalGravity;
        private WBIGravioliumBeam liftBeam;
        private Transform liftBeamSourceTransform;
        private Part liftBeamTarget;
        private Vector3 liftBeamTargetOffset;
        private bool primaryCooldownIncludesLift;
        #endregion

        #region Power: Shove
        /// <summary>
        /// Enables or disables the Shove power.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerShove",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerShove;

        /// <summary>
        /// Shove impulse in kilonewton-seconds (KSP impulse units).
        /// </summary>
        [KSPField]
        public float shoveImpulse = 1.0f;

        /// <summary>
        /// Short interval over which the Shove impulse is delivered, in seconds.
        /// </summary>
        [KSPField]
        public float shoveForceDuration = 0.1f;

        /// <summary>
        /// Length of time that Shove's beam and starburst remain visible, in seconds.
        /// </summary>
        [KSPField]
        public float shoveFXDuration = 1.0f;

        /// <summary>
        /// Base recovery time after Shove, in seconds.
        /// </summary>
        [KSPField]
        public float shoveBaseCooldown = 15.0f;

        /// <summary>
        /// Additional recovery time per second of Shove use.
        /// </summary>
        [KSPField]
        public float shoveCooldownPerSecond = 2.0f;

        /// <summary>
        /// Universal time at which Shove becomes available again.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double shoveCooldownEndUT;

        private Vector3 burstDirection;
        private Vector3 burstForcePerFrame;
        private int burstForceFramesRemaining;
        private bool burstTargetPrepared;
        #endregion

        #region Power: Launch
        /// <summary>
        /// Enables or disables the Launch power.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerLaunch",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerLaunch;

        /// <summary>
        /// Launch impulse in kilonewton-seconds (KSP impulse units).
        /// </summary>
        [KSPField]
        public float launchImpulse = 5.0f;

        /// <summary>
        /// Short interval over which the Launch impulse is delivered, in seconds.
        /// </summary>
        [KSPField]
        public float launchForceDuration = 0.1f;

        /// <summary>
        /// Length of time that Launch's beam and starburst remain visible, in seconds.
        /// </summary>
        [KSPField]
        public float launchFXDuration = 1.0f;

        /// <summary>
        /// Base recovery time after Launch, in seconds.
        /// </summary>
        [KSPField]
        public float launchBaseCooldown = 5.0f;

        /// <summary>
        /// Additional recovery time per second of Launch use.
        /// </summary>
        [KSPField]
        public float launchCooldownPerSecond = 1.5f;

        /// <summary>
        /// Universal time at which Launch becomes available again.
        /// </summary>
        [KSPField(isPersistant = true)]
        public double launchCooldownEndUT;
        #endregion

        #region Power: Suspension Field
        /// <summary>
        /// Enables a timed field that gently lifts all eligible nearby vessels.
        /// </summary>
        [KSPField(guiActive = false, guiActiveEditor = false,
            guiName = "#LOC_BLUESHIFT_evaGraviticsPowerSuspensionField",
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup")]
        [UI_Toggle(enabledText = "#LOC_BLUESHIFT_enabled",
            disabledText = "#LOC_BLUESHIFT_disabled")]
        public bool powerSuspensionField;

        /// <summary>Maximum acquisition radius, in meters.</summary>
        [KSPField]
        public float suspensionFieldRadius = 7.0f;

        /// <summary>Maximum mass of each eligible vessel, in tonnes.</summary>
        [KSPField]
        public float suspensionFieldMaxMass = 5.0f;

        /// <summary>Maximum active field duration, in seconds.</summary>
        [KSPField]
        public float suspensionFieldDuration = 10.0f;

        /// <summary>Target upward velocity, in meters per second.</summary>
        [KSPField]
        public float suspensionFieldTargetSpeed = 0.1f;

        /// <summary>Time used to gently correct each vessel toward the target speed.</summary>
        [KSPField]
        public float suspensionFieldVelocityResponseTime = 0.5f;

        /// <summary>Maximum speed-correction acceleration, in meters per second squared.</summary>
        [KSPField]
        public float suspensionFieldMaxCorrectionAcceleration = 2.0f;

        /// <summary>Minimum recovery time after using the field, in seconds.</summary>
        [KSPField]
        public float suspensionFieldBaseCooldown = 5.0f;

        /// <summary>Cooldown multiplier for accumulated mass/gravity work.</summary>
        [KSPField]
        public float suspensionFieldCooldownPerSecond = 0.5f;

        /// <summary>Exponent applied independently to every affected vessel's mass.</summary>
        [KSPField]
        public float suspensionFieldCooldownMassExponent = 1.5f;

        /// <summary>Reference gravity used to normalize cooldown calculations.</summary>
        [KSPField]
        public float suspensionFieldCooldownReferenceGravity = 9.80665f;

        /// <summary>Universal time at which Suspension Field becomes available again.</summary>
        [KSPField(isPersistant = true)]
        public double suspensionFieldCooldownEndUT;

        private readonly List<SuspensionFieldTarget> suspensionFieldTargets =
            new List<SuspensionFieldTarget>();
        private bool suspensionFieldIsActive;
        private double suspensionFieldActiveDuration;
        private double suspensionFieldCooldownWork;
        #endregion

        #region Housekeeping
        private bool kerbalGearIsActive;
        private bool gamePaused;
        private bool eventsSubscribed;
        private Part hoveredPart;
        private GraviticsPartHighlight hoveredPartHighlight;
        private GraviticsPartHighlight kerbalHighlight;
        private bool kerbalIsHighlighted;
        private string lastDiagnosticState = string.Empty;
        private string lastHighlighterDiagnosticState = string.Empty;
        private Part lastDiagnosticHoveredPart;
        private bool previousPowerPush;
        private bool previousPowerPull;
        private bool previousPowerShove;
        private bool previousPowerLaunch;
        private bool previousPowerLift;
        private bool previousPowerSuspensionField;
        private bool previousPowerLevitate;
        private bool previousPowerHeft;
        private int lastKerbalExperienceLevel = -1;
        private bool gravityPowerAvailabilityInitialized;
        private bool gravityPowersWereAvailable;
        private AudioSource powerAudioSource;
        private PowerSoundOwner powerSoundOwners;
        #endregion

        #region KSP Lifecycle
        /// <summary>
        /// Loads the reusable Graviolium visual settings nested in this module's config.
        /// </summary>
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (node == null)
                return;

            pushBeamSettings.Load(node.GetNode(GravioliumBeamFXNodeName));
            pushStarburstSettings.Load(node.GetNode(GravioliumStarburstFXNodeName));
        }

        /// <summary>
        /// Initializes the dynamically created EVA module.
        /// </summary>
        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            targetingRange = Mathf.Max(0.0f, targetingRange);
            forcePercent = Mathf.Clamp(forcePercent, 0.0f, 100.0f);
            powerSoundVolume = Mathf.Clamp01(powerSoundVolume);
            powerSoundPitch = Mathf.Max(0.01f, powerSoundPitch);
            pushForce = Mathf.Max(0.0f, pushForce);
            pushBaseCooldown = Mathf.Max(0.0f, pushBaseCooldown);
            pushCooldownPerSecond = Mathf.Max(0.0f, pushCooldownPerSecond);
            pullForce = Mathf.Max(0.0f, pullForce);
            pullBaseCooldown = Mathf.Max(0.0f, pullBaseCooldown);
            pullCooldownPerSecond = Mathf.Max(0.0f, pullCooldownPerSecond);
            shoveImpulse = Mathf.Max(0.0f, shoveImpulse);
            shoveForceDuration = Mathf.Max(0.01f, shoveForceDuration);
            shoveFXDuration = Mathf.Max(0.01f, shoveFXDuration);
            shoveBaseCooldown = Mathf.Max(0.0f, shoveBaseCooldown);
            shoveCooldownPerSecond = Mathf.Max(0.0f, shoveCooldownPerSecond);
            launchImpulse = Mathf.Max(0.0f, launchImpulse);
            launchForceDuration = Mathf.Max(0.01f, launchForceDuration);
            launchFXDuration = Mathf.Max(0.01f, launchFXDuration);
            launchBaseCooldown = Mathf.Max(0.0f, launchBaseCooldown);
            launchCooldownPerSecond = Mathf.Max(0.0f,
                launchCooldownPerSecond);
            liftMaxMass = Mathf.Max(0.0f, liftMaxMass);
            liftDuration = Mathf.Max(0.01f, liftDuration);
            liftTargetSpeed = Mathf.Max(0.0f, liftTargetSpeed);
            liftVelocityResponseTime = Mathf.Max(0.01f,
                liftVelocityResponseTime);
            liftMaxCorrectionAcceleration = Mathf.Max(0.0f,
                liftMaxCorrectionAcceleration);
            liftBaseCooldown = Mathf.Max(0.0f, liftBaseCooldown);
            liftCooldownPerSecond = Mathf.Max(0.0f, liftCooldownPerSecond);
            liftCooldownMassExponent = Mathf.Max(1.0f,
                liftCooldownMassExponent);
            liftCooldownReferenceGravity = Mathf.Max(0.01f,
                liftCooldownReferenceGravity);
            suspensionFieldRadius = Mathf.Max(0.0f, suspensionFieldRadius);
            suspensionFieldMaxMass = Mathf.Max(0.0f,
                suspensionFieldMaxMass);
            suspensionFieldDuration = Mathf.Max(0.01f,
                suspensionFieldDuration);
            suspensionFieldTargetSpeed = Mathf.Max(0.0f,
                suspensionFieldTargetSpeed);
            suspensionFieldVelocityResponseTime = Mathf.Max(0.01f,
                suspensionFieldVelocityResponseTime);
            suspensionFieldMaxCorrectionAcceleration = Mathf.Max(0.0f,
                suspensionFieldMaxCorrectionAcceleration);
            suspensionFieldBaseCooldown = Mathf.Max(0.0f,
                suspensionFieldBaseCooldown);
            suspensionFieldCooldownPerSecond = Mathf.Max(0.0f,
                suspensionFieldCooldownPerSecond);
            suspensionFieldCooldownMassExponent = Mathf.Max(1.0f,
                suspensionFieldCooldownMassExponent);
            suspensionFieldCooldownReferenceGravity = Mathf.Max(0.01f,
                suspensionFieldCooldownReferenceGravity);
            levitateTargetSpeed = Mathf.Max(0.0f, levitateTargetSpeed);
            heftMultiplier = Mathf.Max(1.0f, heftMultiplier);
            heftCooldown = Mathf.Max(0.0f, heftCooldown);
            pupilGlowIntensity = Mathf.Max(0.0f, pupilGlowIntensity);
            pupilGlowFalloff = Mathf.Clamp(pupilGlowFalloff, 0.01f, 5.0f);

            enforcePowerProgression(false);

            if (powerPush)
            {
                powerPull = false;
                powerShove = false;
                powerLaunch = false;
            }
            else if (powerPull)
            {
                powerShove = false;
                powerLaunch = false;
            }
            else if (powerShove)
                powerLaunch = false;
            if (powerLift && powerSuspensionField)
                powerSuspensionField = false;
            previousPowerPush = powerPush;
            previousPowerPull = powerPull;
            previousPowerShove = powerShove;
            previousPowerLaunch = powerLaunch;
            previousPowerLift = powerLift;
            previousPowerSuspensionField = powerSuspensionField;
            previousPowerLevitate = powerLevitate;
            previousPowerHeft = powerHeft;

            GameEvents.onGamePause.Add(onPaused);
            GameEvents.onGameUnpause.Add(onUnpaused);
            GameEvents.onCrewBoardVessel.Add(onCrewBoardVessel);
            GameEvents.onGameSceneLoadRequested.Add(onGameSceneLoadRequested);
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
            GameEvents.OnEVAConstructionMode.Add(onEVAConstructionMode);
            eventsSubscribed = true;

            debugLog("Started. range=" + targetingRange.ToString("F1") +
                "m, force=" + pushForce.ToString("F2") + "kN");
            updatePAW();
        }

        /// <summary>
        /// Makes the resonator available when KerbalGear activates this module.
        /// </summary>
        public override void OnActive()
        {
            base.OnActive();

            kerbalGearIsActive = true;
            enforcePowerProgression(false);
            if (powerHeft && graviticsEnabled && getHeftCooldownRemaining() <= 0.0)
                beginHeft();
            debugLog("Activated by KerbalGear.");
            updatePAW();
            refreshContextWindows();
        }

        /// <summary>
        /// Stops all interaction when the resonator wearable is removed.
        /// </summary>
        public override void OnInactive()
        {
            graviticsEnabled = false;
            kerbalGearIsActive = false;
            clearInteractionState();
            updatePAW();
            refreshContextWindows();

            base.OnInactive();
        }

        /// <summary>
        /// Updates target acquisition, highlights, and PAW status at frame cadence.
        /// </summary>
        public void Update()
        {
            enforcePowerProgression(true);
            enforceGravityRelativePowerAvailability();
            updatePowerSelection();
            Part rawHoveredPart = Mouse.HoveredPart;
            logDiagnosticState(rawHoveredPart);

            if (Input.GetMouseButtonDown(0))
            {
                debugLog("Left mouse down. pointer=" + (getPointerBlocker() ?? "ready") +
                    ", hovered=" + getPartName(rawHoveredPart) +
                    ", target=" + (getPushTargetInvalidReason(rawHoveredPart) ?? "valid") +
                    ", cooldown=" + getSelectedCooldownRemaining().ToString("F2") +
                    "s, liftCooldown=" + getLiftCooldownRemaining().ToString("F2") +
                    "s");
            }

            if (!canOperate())
            {
                finishPush("module blocked: " + getOperatingBlocker());
                finishLift("module blocked: " + getOperatingBlocker());
                finishSuspensionField("module blocked: " + getOperatingBlocker());
                finishLevitate("module blocked: " + getOperatingBlocker());
                clearHoveredPart();
                updateGraviticsStatus();
                return;
            }

            ensureKerbalHighlight();

            GraviticsPower selectedPower = getSelectedPower();
            if (selectedPower == GraviticsPower.None)
                finishPush("no power selected");
            if (!powerLift)
                finishLift("Lift power disabled");
            if (!powerSuspensionField)
                finishSuspensionField("Suspension Field disabled");
            if (!powerLevitate)
                finishLevitate("Levitate disabled");

            if (pushIsActive)
            {
                if (!canContinueActivePower())
                {
                    finishPush(getPushStopReason());
                }
                else
                    setHoveredPart(pushTarget);
            }

            bool canStartSelectedPower = selectedPower != GraviticsPower.None &&
                !pushIsActive && getSelectedCooldownRemaining() <= 0.0;
            bool canStartLift = powerLift && !liftIsActive &&
                getLiftCooldownRemaining() <= 0.0;
            Part currentHoveredPart = isPointerAvailable() ? rawHoveredPart : null;
            bool validForSelectedPower = canStartSelectedPower &&
                isValidPushTarget(currentHoveredPart);
            bool validForLift = canStartLift && isValidLiftTarget(currentHoveredPart);

            if (validForSelectedPower || validForLift)
            {
                setHoveredPart(currentHoveredPart);
            }
            else if (pushIsActive)
            {
                setHoveredPart(pushTarget);
            }
            else if (liftIsActive)
            {
                setHoveredPart(liftTarget);
            }
            else
            {
                clearHoveredPart();
            }

            if (Input.GetMouseButtonDown(0) && currentHoveredPart != null)
            {
                if (validForSelectedPower)
                {
                    disableAuxiliaryPowersForPrimaryActivation();
                    beginPush(currentHoveredPart);
                }
                if (validForLift && powerLift)
                    beginLift(currentHoveredPart);
                else if (canStartLift && powerLift)
                    showLiftRejection(currentHoveredPart);
            }

            updateGraviticsStatus();
        }

        /// <summary>
        /// Applies the selected gravitic power at physics cadence.
        /// </summary>
        public void FixedUpdate()
        {
            handleActivePowerPhysics();
            handleLiftPhysics();
            handleSuspensionFieldPhysics();
            handleLevitatePhysics();
        }

        /// <summary>
        /// Prevents a separately hosted beam from remaining visible if Unity disables this module.
        /// </summary>
        public void OnDisable()
        {
            finishPush("Unity behaviour disabled");
            finishLift("Unity behaviour disabled");
            finishSuspensionField("Unity behaviour disabled");
            finishLevitate("Unity behaviour disabled");
            finishHeft("Unity behaviour disabled");
            stopPowerSound();
        }

        private void handleActivePowerPhysics()
        {
            if (activePower == GraviticsPower.None || !pushIsActive)
            {
                finishPush(activePower == GraviticsPower.None
                    ? "active power missing"
                    : "power inactive");
                return;
            }

            if (activePower == GraviticsPower.Launch &&
                !gravityRelativePowersAreAvailable())
            {
                finishPush("Launch unavailable in zero gravity");
                return;
            }

            if (!canContinueActivePower())
            {
                finishPush(getPushStopReason());
                return;
            }

            Vector3 forceDirection;
            string directionFailure;
            if (!tryGetActiveForceDirection(pushTarget, out forceDirection,
                out directionFailure))
            {
                finishPush(directionFailure);
                return;
            }

            // A grounded KerbalEVA controller overwrites externally applied motion.
            // Move a burst target into KSP's floating state once, before delivering
            // the force. Do not repeat this during the longer FX window: doing so can
            // force a kerbal that has already landed back into floating state and
            // produce a second bounce followed by an artificial-looking settle.
            if (isBurstPower(activePower) && !burstTargetPrepared &&
                !prepareTargetKerbalForBurst(pushTarget.vessel, activePower))
            {
                finishPush("target Kerbal cannot enter burst physics state");
                return;
            }
            if (isBurstPower(activePower))
                burstTargetPrepared = true;

            if (pushDuration <= 0.0)
            {
                debugLog("Applying " + getPowerName(activePower) + ". target=" +
                    getPartName(pushTarget) +
                    ", force=" + getActiveForce().ToString("F3") +
                    (isBurstPower(activePower) ? "kN-s" : "kN") +
                    ", mass=" + pushTarget.vessel.GetTotalMass().ToString("F3") + "t" +
                    ", distance=" + Vector3.Distance(part.transform.position,
                        pushTarget.transform.position).ToString("F2") + "m" +
                    ", direction=" + forceDirection.ToString("F3") +
                    ", landed=" + pushTarget.vessel.LandedOrSplashed +
                    ", kinematic=" + pushTarget.rb.isKinematic +
                    ", sleeping=" + pushTarget.rb.IsSleeping() +
                    ", constraints=" + pushTarget.rb.constraints);
            }

            pushTarget.rb.WakeUp();
            if (isBurstPower(activePower))
            {
                if (burstForceFramesRemaining < 0)
                {
                    float physicsStep = Mathf.Max(TimeWarp.fixedDeltaTime, 0.0001f);
                    float forceDuration = getActiveBurstForceDuration();
                    burstForceFramesRemaining = Mathf.Max(1,
                        Mathf.CeilToInt(forceDuration / physicsStep));
                    burstForcePerFrame = forceDirection.normalized *
                        (getActiveForce() /
                        (burstForceFramesRemaining * physicsStep));
                    debugLog("Delivering " + getPowerName(activePower) +
                        " impulse as " + burstForcePerFrame.magnitude.ToString("F3") +
                        "kN over " + burstForceFramesRemaining +
                        " physics frames (" +
                        (burstForceFramesRemaining * physicsStep).ToString("F3") +
                        "s).");
                }

                if (burstForceFramesRemaining > 0)
                {
                    // A normal vessel receives the burst through its target rigidbody.
                    // A launched kerbal is an articulated ragdoll, so give every body
                    // the equivalent acceleration to preserve the configured impulse.
                    if (!applyLaunchForceToRagdoll(burstForcePerFrame))
                    {
                        pushTarget.rb.AddForce(burstForcePerFrame,
                            ForceMode.Force);
                    }
                    burstForceFramesRemaining--;
                }
            }
            else
            {
                pushTarget.rb.AddForce(forceDirection.normalized * getActiveForce(),
                    ForceMode.Force);
            }
            showPushBeam();
            showPushStarburst();
            pushDuration += Time.fixedDeltaTime;
            pushTargetMass = pushTarget.vessel.GetTotalMass();
            pushLocalGravity = part.vessel.graviticAcceleration.magnitude;

            if (isBurstPower(activePower) &&
                pushDuration >= Mathf.Max(getActiveBurstFXDuration(),
                    getActiveBurstForceDuration()))
            {
                finishPush("configured " + getPowerName(activePower) +
                    " FX duration complete");
            }
        }

        private void handleLiftPhysics()
        {
            if (!liftIsActive)
            {
                stopLiftBeam();
                return;
            }

            if (!gravityRelativePowersAreAvailable())
            {
                finishLift("Lift unavailable in zero gravity");
                return;
            }

            string invalidReason = getLiftTargetInvalidReason(liftTarget);
            if (!canOperate() || invalidReason != null)
            {
                finishLift(!canOperate()
                    ? "module blocked: " + getOperatingBlocker()
                    : "target invalid: " + invalidReason);
                return;
            }

            Vector3 liftDirection;
            if (!tryGetLaunchDirection(liftTarget, out liftDirection))
            {
                finishLift("unable to determine local up");
                return;
            }

            Rigidbody liftReferenceBody = getLiftReferenceRigidbody(
                liftTarget.vessel);
            if (liftReferenceBody == null)
            {
                finishLift("target vessel has no rigidbody");
                return;
            }

            if (!ensureTargetKerbalIsFloating(liftTarget.vessel, "Lift"))
            {
                finishLift("target Kerbal cannot enter floating mode");
                return;
            }

            float currentUpwardSpeed = getForceControlledUpwardSpeed(
                liftTarget.vessel, liftReferenceBody, liftDirection);
            float correctionAcceleration = calculateForceCorrectionAcceleration(
                liftTargetSpeed, currentUpwardSpeed, liftVelocityResponseTime,
                liftMaxCorrectionAcceleration);
            float gravityAcceleration = (float)
                liftTarget.vessel.graviticAcceleration.magnitude;
            float appliedAcceleration = calculateGravityCompensatedAcceleration(
                gravityAcceleration, correctionAcceleration);

            applyVesselAcceleration(liftTarget.vessel, liftDirection,
                appliedAcceleration);

            showLiftBeam();
            liftActiveDuration += Time.fixedDeltaTime;
            liftTargetMass = liftTarget.vessel.GetTotalMass();
            liftLocalGravity = gravityAcceleration;

            if (liftActiveDuration >= liftDuration)
                finishLift("configured Lift duration complete");
        }

        private void handleSuspensionFieldPhysics()
        {
            if (!suspensionFieldIsActive)
                return;

            if (!canOperate() || !powerSuspensionField ||
                !gravityRelativePowersAreAvailable())
            {
                finishSuspensionField(!canOperate()
                    ? "module blocked: " + getOperatingBlocker()
                    : !gravityRelativePowersAreAvailable()
                        ? "Suspension Field unavailable in zero gravity"
                        : "Suspension Field disabled");
                return;
            }

            float physicsStep = Time.fixedDeltaTime;
            int liftedVesselCount = 0;
            for (int index = suspensionFieldTargets.Count - 1; index >= 0;
                index--)
            {
                SuspensionFieldTarget fieldTarget = suspensionFieldTargets[index];
                Vessel targetVessel = fieldTarget.vessel;
                string invalidReason = getSuspensionFieldTargetInvalidReason(
                    targetVessel, false);
                if (invalidReason != null)
                {
                    debugLog("Suspension Field released " +
                        getVesselName(targetVessel) + ": " + invalidReason);
                    restoreSuspensionFieldTarget(fieldTarget);
                    suspensionFieldTargets.RemoveAt(index);
                    continue;
                }

                Rigidbody referenceBody = getLiftReferenceRigidbody(targetVessel);
                Vector3 liftDirection;
                if (referenceBody == null || !tryGetVesselUpDirection(targetVessel,
                    referenceBody, out liftDirection))
                {
                    restoreSuspensionFieldTarget(fieldTarget);
                    suspensionFieldTargets.RemoveAt(index);
                    continue;
                }

                if (!ensureTargetKerbalIsFloating(targetVessel,
                    "Suspension Field"))
                {
                    debugLog("Suspension Field released " +
                        getVesselName(targetVessel) +
                        ": target Kerbal cannot enter floating mode");
                    restoreSuspensionFieldTarget(fieldTarget);
                    suspensionFieldTargets.RemoveAt(index);
                    continue;
                }

                float currentUpwardSpeed = getForceControlledUpwardSpeed(
                    targetVessel, referenceBody, liftDirection);
                float correctionAcceleration = calculateForceCorrectionAcceleration(
                    suspensionFieldTargetSpeed, currentUpwardSpeed,
                    suspensionFieldVelocityResponseTime,
                    suspensionFieldMaxCorrectionAcceleration);
                float gravityAcceleration = (float)
                    targetVessel.graviticAcceleration.magnitude;
                float appliedAcceleration = calculateGravityCompensatedAcceleration(
                    gravityAcceleration, correctionAcceleration);
                applyVesselAcceleration(targetVessel, liftDirection,
                    appliedAcceleration);

                float targetMass = targetVessel.GetTotalMass();
                double massCost = System.Math.Pow(targetMass,
                    suspensionFieldCooldownMassExponent);
                double gravityFactor = gravityAcceleration /
                    suspensionFieldCooldownReferenceGravity;
                suspensionFieldCooldownWork += physicsStep * massCost *
                    System.Math.Max(0.0, gravityFactor);
                liftedVesselCount++;
            }

            suspensionFieldActiveDuration += physicsStep;
            if (liftedVesselCount == 0)
                finishSuspensionField("no eligible targets remain");
            else if (suspensionFieldActiveDuration >= suspensionFieldDuration)
                finishSuspensionField("configured Suspension Field duration complete");
        }

        private void beginSuspensionField()
        {
            double cooldownRemaining = getSuspensionFieldCooldownRemaining();
            if (!powerSuspensionField || suspensionFieldIsActive ||
                getKerbalExperienceLevel() < 5 || cooldownRemaining > 0.0 ||
                !canOperate())
            {
                powerSuspensionField = false;
                previousPowerSuspensionField = false;
                if (cooldownRemaining > 0.0)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format(
                        "#LOC_BLUESHIFT_evaGraviticsSuspensionCooldown",
                        cooldownRemaining.ToString("F1")), 3.0f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                return;
            }

            clearHoveredPart();
            suspensionFieldTargets.Clear();
            List<Vessel> loadedVessels = FlightGlobals.VesselsLoaded;
            for (int index = 0; index < loadedVessels.Count; index++)
            {
                Vessel targetVessel = loadedVessels[index];
                if (getSuspensionFieldTargetInvalidReason(targetVessel, true) != null)
                    continue;

                SuspensionFieldTarget fieldTarget = new SuspensionFieldTarget
                {
                    vessel = targetVessel
                };
                captureSuspensionFieldHighlights(fieldTarget);
                suspensionFieldTargets.Add(fieldTarget);
            }

            if (suspensionFieldTargets.Count == 0)
            {
                powerSuspensionField = false;
                previousPowerSuspensionField = false;
                ScreenMessages.PostScreenMessage(Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsSuspensionNoTargets",
                    suspensionFieldRadius.ToString("F1")), 3.0f,
                    ScreenMessageStyle.UPPER_CENTER);
                debugLog("Suspension Field rejected: no eligible vessels in range.");
                return;
            }

            suspensionFieldIsActive = true;
            suspensionFieldActiveDuration = 0.0;
            suspensionFieldCooldownWork = 0.0;
            playPowerSound(PowerSoundOwner.SuspensionField);
            debugLog("Suspension Field started with " +
                suspensionFieldTargets.Count + " vessel(s).");
        }

        private void finishSuspensionField(string reason = "interaction cleared")
        {
            stopPowerSound(PowerSoundOwner.SuspensionField);

            if (!suspensionFieldIsActive && suspensionFieldTargets.Count == 0)
                return;

            double cooldown = 0.0;
            if (suspensionFieldCooldownWork > 0.0)
            {
                cooldown = suspensionFieldBaseCooldown +
                    suspensionFieldCooldownPerSecond *
                    suspensionFieldCooldownWork;
                suspensionFieldCooldownEndUT = Planetarium.GetUniversalTime() +
                    System.Math.Max(0.0, cooldown);
            }

            for (int index = 0; index < suspensionFieldTargets.Count; index++)
                restoreSuspensionFieldTarget(suspensionFieldTargets[index]);
            suspensionFieldTargets.Clear();

            debugLog("Suspension Field stopped. reason=" + reason +
                ", duration=" + suspensionFieldActiveDuration.ToString("F3") +
                "s, cooldown=" + cooldown.ToString("F2") + "s");
            suspensionFieldIsActive = false;
            suspensionFieldActiveDuration = 0.0;
            suspensionFieldCooldownWork = 0.0;
            powerSuspensionField = false;
            previousPowerSuspensionField = false;
        }

        private string getSuspensionFieldTargetInvalidReason(Vessel targetVessel,
            bool checkRange)
        {
            if (targetVessel == null)
                return "vessel missing";
            if (part == null || part.vessel == null)
                return "EVA vessel missing";
            if (targetVessel == part.vessel)
                return "vessel is the EVA kerbal";
            if (!targetVessel.loaded)
                return "vessel unloaded";
            if (targetVessel.packed)
                return "vessel packed";
            if (targetVessel.state == Vessel.State.DEAD)
                return "vessel dead";
            if (targetVessel.parts == null || targetVessel.parts.Count == 0)
                return "vessel has no parts";
            if (getLiftReferenceRigidbody(targetVessel) == null)
                return "vessel has no rigidbody";

            float targetMass = targetVessel.GetTotalMass();
            if (targetMass > suspensionFieldMaxMass)
                return "vessel exceeds mass limit";
            if (checkRange)
            {
                double distance = Vector3d.Distance(part.vessel.GetWorldPos3D(),
                    targetVessel.GetWorldPos3D());
                if (distance > suspensionFieldRadius)
                    return "vessel outside field radius";
            }
            return null;
        }

        private static bool tryGetVesselUpDirection(Vessel targetVessel,
            Rigidbody referenceBody, out Vector3 liftDirection)
        {
            liftDirection = Vector3.zero;
            if (targetVessel == null || referenceBody == null)
                return false;

            Vector3 gravity = targetVessel.graviticAcceleration;
            if (gravity.sqrMagnitude > Mathf.Epsilon)
            {
                liftDirection = -gravity.normalized;
                return true;
            }

            if (targetVessel.mainBody != null)
            {
                liftDirection = referenceBody.worldCenterOfMass -
                    (Vector3)targetVessel.mainBody.position;
            }
            return liftDirection.sqrMagnitude > Mathf.Epsilon;
        }

        private static string getVesselName(Vessel targetVessel)
        {
            return targetVessel != null ? targetVessel.vesselName : "missing vessel";
        }

        private static void captureSuspensionFieldHighlights(
            SuspensionFieldTarget fieldTarget)
        {
            if (fieldTarget == null || fieldTarget.vessel == null ||
                fieldTarget.vessel.parts == null)
                return;

            List<Part> targetParts = fieldTarget.vessel.parts;
            for (int index = 0; index < targetParts.Count; index++)
            {
                Part targetPart = targetParts[index];
                if (targetPart == null)
                    continue;
                fieldTarget.highlights.Add(captureHighlight(targetPart));
                applyHighlight(targetPart);
            }
        }

        private static void restoreSuspensionFieldTarget(
            SuspensionFieldTarget fieldTarget)
        {
            if (fieldTarget == null)
                return;
            for (int index = 0; index < fieldTarget.highlights.Count; index++)
                restoreHighlight(fieldTarget.highlights[index]);
            fieldTarget.highlights.Clear();
        }

        private void applySuspensionFieldHighlights()
        {
            if (!suspensionFieldIsActive)
                return;
            for (int targetIndex = 0; targetIndex < suspensionFieldTargets.Count;
                targetIndex++)
            {
                SuspensionFieldTarget fieldTarget =
                    suspensionFieldTargets[targetIndex];
                for (int partIndex = 0; partIndex < fieldTarget.highlights.Count;
                    partIndex++)
                {
                    Part targetPart =
                        fieldTarget.highlights[partIndex].highlightedPart;
                    if (targetPart != null)
                        applyHighlight(targetPart);
                }
            }
        }

        private void handleLevitatePhysics()
        {
            if (!levitateIsActive)
                return;

            if (!canOperate() || !powerLevitate || part == null ||
                part.vessel == null || !gravityRelativePowersAreAvailable())
            {
                finishLevitate(!canOperate()
                    ? "module blocked: " + getOperatingBlocker()
                    : !gravityRelativePowersAreAvailable()
                        ? "Levitate unavailable in zero gravity"
                        : "Levitate disabled");
                return;
            }

            Vessel evaVessel = part.vessel;
            Rigidbody referenceBody = getLiftReferenceRigidbody(evaVessel);
            Vector3 liftDirection;
            if (referenceBody == null || !tryGetVesselUpDirection(evaVessel,
                referenceBody, out liftDirection))
            {
                finishLevitate("unable to determine local up");
                return;
            }

            if (!ensureKerbalIsFloatingForLevitation(levitateKerbalEVA,
                referenceBody, liftDirection))
            {
                finishLevitate("EVA state cannot enter floating mode");
                return;
            }

            float currentUpwardSpeed = getForceControlledUpwardSpeed(evaVessel,
                referenceBody, liftDirection);
            float correctionAcceleration = calculateForceCorrectionAcceleration(
                levitateTargetSpeed, currentUpwardSpeed,
                liftVelocityResponseTime, liftMaxCorrectionAcceleration);
            float gravityAcceleration = (float)
                evaVessel.graviticAcceleration.magnitude;
            float appliedAcceleration = calculateGravityCompensatedAcceleration(
                gravityAcceleration, correctionAcceleration);
            applyVesselAcceleration(evaVessel, liftDirection,
                appliedAcceleration);

            levitateActiveDuration += Time.fixedDeltaTime;
            levitateKerbalMass = evaVessel.GetTotalMass();
            levitateLocalGravity = gravityAcceleration;
        }

        private void beginLevitate()
        {
            double cooldownRemaining = getLevitateCooldownRemaining();
            if (!powerLevitate || levitateIsActive || cooldownRemaining > 0.0 ||
                getKerbalExperienceLevel() < 2 || !canOperate() || part == null ||
                part.vessel == null)
            {
                powerLevitate = false;
                previousPowerLevitate = false;
                if (cooldownRemaining > 0.0)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format(
                        "#LOC_BLUESHIFT_evaGraviticsLevitateCooldown",
                        cooldownRemaining.ToString("F1")), 3.0f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                return;
            }


            levitateKerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            Rigidbody referenceBody = getLiftReferenceRigidbody(part.vessel);
            Vector3 liftDirection;
            if (levitateKerbalEVA == null || referenceBody == null ||
                !tryGetVesselUpDirection(part.vessel, referenceBody,
                    out liftDirection) ||
                !ensureKerbalIsFloatingForLevitation(levitateKerbalEVA,
                    referenceBody, liftDirection))
            {
                powerLevitate = false;
                previousPowerLevitate = false;
                debugLog("Levitate rejected: EVA cannot enter floating mode.");
                return;
            }

            levitateIsActive = true;
            levitateActiveDuration = 0.0;
            levitateKerbalMass = part.vessel.GetTotalMass();
            levitateLocalGravity = part.vessel.graviticAcceleration.magnitude;
            playPowerSound(PowerSoundOwner.Levitate);
            debugLog("Levitate started. mass=" +
                levitateKerbalMass.ToString("F3") + "t");
        }

        private void finishLevitate(string reason = "interaction cleared")
        {
            stopPowerSound(PowerSoundOwner.Levitate);

            if (!levitateIsActive)
                return;

            double cooldown = 0.0;
            if (levitateActiveDuration > 0.0)
            {
                cooldown = calculateLiftCooldown(levitateActiveDuration,
                    levitateKerbalMass, levitateLocalGravity);
                levitateCooldownEndUT = Planetarium.GetUniversalTime() +
                    System.Math.Max(0.0, cooldown);
            }

            debugLog("Levitate stopped. reason=" + reason +
                ", duration=" + levitateActiveDuration.ToString("F3") +
                "s, cooldown=" + cooldown.ToString("F2") + "s");
            levitateIsActive = false;
            levitateActiveDuration = 0.0;
            levitateKerbalMass = 0.0f;
            levitateLocalGravity = 0.0;
            levitateKerbalEVA = null;
            powerLevitate = false;
            previousPowerLevitate = false;
        }

        private void beginHeft()
        {
            if (heftIsApplied)
                return;

            double cooldownRemaining = getHeftCooldownRemaining();
            if (!powerHeft || cooldownRemaining > 0.0 || !canOperate())
            {
                if (cooldownRemaining > 0.0)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format(
                        "#LOC_BLUESHIFT_evaGraviticsHeftCooldown",
                        cooldownRemaining.ToString("F1")), 3.0f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                powerHeft = false;
                previousPowerHeft = false;
                return;
            }

            heftIsApplied = true;
            GameEvents.OnCombinedConstructionWeightLimitChanged.Fire();
            debugLog("Heft started. multiplier=" + heftMultiplier.ToString("F2"));
        }

        private void finishHeft(string reason = "interaction cleared",
            bool startCooldown = true)
        {
            bool wasApplied = heftIsApplied;
            heftIsApplied = false;
            powerHeft = false;
            previousPowerHeft = false;

            if (!wasApplied)
                return;

            if (startCooldown)
            {
                heftCooldownEndUT = System.Math.Max(heftCooldownEndUT,
                    Planetarium.GetUniversalTime() + heftCooldown);
            }

            GameEvents.OnCombinedConstructionWeightLimitChanged.Fire();
            debugLog("Heft stopped. reason=" + reason + ", cooldown=" +
                (startCooldown ? heftCooldown.ToString("F2") : "0.00") + "s");
        }

        internal static bool tryGetActiveHeftMultiplier(out float multiplier)
        {
            multiplier = 1.0f;
            if (!HighLogic.LoadedSceneIsFlight || FlightGlobals.ActiveVessel == null ||
                FlightGlobals.ActiveVessel.parts == null)
            {
                return false;
            }

            List<Part> activeParts = FlightGlobals.ActiveVessel.parts;
            for (int index = 0; index < activeParts.Count; index++)
            {
                Part activePart = activeParts[index];
                if (activePart == null ||
                    activePart.FindModuleImplementing<KerbalEVA>() == null)
                {
                    continue;
                }

                WBIEVAGravitics gravitics =
                    activePart.FindModuleImplementing<WBIEVAGravitics>();
                if (gravitics == null || !gravitics.heftIsApplied ||
                    !gravitics.kerbalGearIsActive || !gravitics.graviticsEnabled ||
                    !gravitics.enabled || !gravitics.moduleIsEnabled)
                {
                    continue;
                }

                multiplier = Mathf.Max(1.0f, gravitics.heftMultiplier);
                return multiplier > 1.0f;
            }
            return false;
        }

        private bool ensureKerbalIsFloatingForLevitation(KerbalEVA kerbalEVA,
            Rigidbody referenceBody, Vector3 liftDirection)
        {
            if (kerbalEVA == null || kerbalEVA.fsm == null ||
                !kerbalEVA.fsm.Started || referenceBody == null)
                return false;

            KFSMState currentState = kerbalEVA.fsm.CurrentState;
            bool alreadyFloating = currentState == kerbalEVA.st_idle_fl ||
                currentState == kerbalEVA.st_bound_fl;
            if (!alreadyFloating)
            {
                if (!canTransitionKerbalToFloatingState(kerbalEVA, currentState))
                    return false;

                if (!transitionKerbalToFloatingState(kerbalEVA, currentState,
                    "Levitate"))
                    return false;
            }

            Vector3 normalizedUp = liftDirection.normalized;
            float currentUpwardSpeed = Vector3.Dot(referenceBody.velocity,
                normalizedUp);
            float targetSpeed = getForceAdjustedTargetSpeed(levitateTargetSpeed);
            if (targetSpeed > 0.0f && currentUpwardSpeed < targetSpeed)
            {
                // The grounded EVA state overwrites rigidbody velocity every physics
                // tick. Seed only the missing vertical component after changing to
                // KSP's floating state; subsequent motion remains force-controlled.
                referenceBody.velocity += normalizedUp *
                    (targetSpeed - currentUpwardSpeed);
            }
            return true;
        }

        private bool ensureTargetKerbalIsFloating(Vessel targetVessel,
            string powerName)
        {
            KerbalEVA targetKerbal = findKerbalEVA(targetVessel);
            if (targetKerbal == null)
                return true;
            if (targetKerbal.fsm == null || !targetKerbal.fsm.Started)
                return false;

            KFSMState currentState = targetKerbal.fsm.CurrentState;
            if (currentState == targetKerbal.st_idle_fl ||
                currentState == targetKerbal.st_bound_fl)
                return true;
            if (!canTransitionKerbalToFloatingState(targetKerbal, currentState))
                return false;

            return transitionKerbalToFloatingState(targetKerbal, currentState,
                powerName);
        }

        private bool prepareTargetKerbalForBurst(Vessel targetVessel,
            GraviticsPower power)
        {
            if (power != GraviticsPower.Launch)
                return ensureTargetKerbalIsFloating(targetVessel,
                    getPowerName(power));

            KerbalEVA targetKerbal = findKerbalEVA(targetVessel);
            if (targetKerbal == null)
                return true;
            if (targetKerbal.fsm == null || !targetKerbal.fsm.Started)
                return false;

            KFSMState currentState = targetKerbal.fsm.CurrentState;
            if (currentState == targetKerbal.st_ragdoll)
                return true;
            bool canEnterLaunchPhysics =
                currentState == targetKerbal.st_idle_fl ||
                currentState == targetKerbal.st_bound_fl ||
                canTransitionKerbalToFloatingState(targetKerbal, currentState);
            if (!canEnterLaunchPhysics)
                return false;

            return transitionKerbalToRagdollState(targetKerbal, currentState,
                "Launch");
        }

        private bool applyLaunchForceToRagdoll(Vector3 launchForce)
        {
            if (activePower != GraviticsPower.Launch || pushTarget == null ||
                pushTarget.vessel == null)
            {
                return false;
            }

            KerbalEVA targetKerbal = findKerbalEVA(pushTarget.vessel);
            if (targetKerbal == null || !targetKerbal.isRagdoll ||
                targetKerbal.ragdollNodes == null)
            {
                return false;
            }

            float targetMass = Mathf.Max(pushTarget.vessel.GetTotalMass(),
                0.0001f);
            Vector3 launchAcceleration = launchForce / targetMass;
            int bodiesAccelerated = 0;
            KerbalRagdollNode[] ragdollNodes = targetKerbal.ragdollNodes;
            for (int index = 0; index < ragdollNodes.Length; index++)
            {
                KerbalRagdollNode ragdollNode = ragdollNodes[index];
                if (ragdollNode == null || ragdollNode.rb == null ||
                    ragdollNode.go == null || !ragdollNode.go.activeInHierarchy ||
                    ragdollNode.rb.isKinematic)
                {
                    continue;
                }

                bool duplicateBody = false;
                for (int previousIndex = 0; previousIndex < index;
                    previousIndex++)
                {
                    KerbalRagdollNode previousNode =
                        ragdollNodes[previousIndex];
                    if (previousNode != null &&
                        previousNode.rb == ragdollNode.rb)
                    {
                        duplicateBody = true;
                        break;
                    }
                }
                if (duplicateBody)
                    continue;

                ragdollNode.rb.WakeUp();
                ragdollNode.rb.AddForce(launchAcceleration,
                    ForceMode.Acceleration);
                bodiesAccelerated++;
            }

            return bodiesAccelerated > 0;
        }

        private bool transitionKerbalToFloatingState(KerbalEVA kerbalEVA,
            KFSMState currentState, string powerName)
        {
            if (kerbalEVA == null || kerbalEVA.fsm == null ||
                currentState == null || kerbalEVA.st_idle_fl == null)
                return false;

            KFSMEvent floatingEvent;
            if (!kerbalFloatEvents.TryGetValue(kerbalEVA, out floatingEvent))
            {
                floatingEvent = new KFSMEvent("Graviolium Floating State");
                floatingEvent.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
                floatingEvent.GoToStateOnEvent = kerbalEVA.st_idle_fl;
                kerbalFloatEvents.Add(kerbalEVA, floatingEvent);
            }
            if (!currentState.StateEvents.Contains(floatingEvent))
                kerbalEVA.fsm.AddEvent(floatingEvent, currentState);

            kerbalEVA.fsm.RunEvent(floatingEvent);
            debugLog(powerName + " changed " + getVesselName(kerbalEVA.vessel) +
                " EVA state from " + currentState.name + " to " +
                kerbalEVA.st_idle_fl.name + ".");
            return true;
        }

        private bool transitionKerbalToRagdollState(KerbalEVA kerbalEVA,
            KFSMState currentState, string powerName)
        {
            if (kerbalEVA == null || kerbalEVA.fsm == null ||
                currentState == null || kerbalEVA.st_ragdoll == null)
                return false;

            KFSMEvent ragdollEvent;
            if (!kerbalLaunchEvents.TryGetValue(kerbalEVA, out ragdollEvent))
            {
                ragdollEvent = new KFSMEvent("Graviolium Launch Ragdoll State");
                ragdollEvent.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
                ragdollEvent.GoToStateOnEvent = kerbalEVA.st_ragdoll;
                kerbalLaunchEvents.Add(kerbalEVA, ragdollEvent);
            }
            if (!currentState.StateEvents.Contains(ragdollEvent))
                kerbalEVA.fsm.AddEvent(ragdollEvent, currentState);

            kerbalEVA.fsm.RunEvent(ragdollEvent);
            debugLog(powerName + " changed " +
                getVesselName(kerbalEVA.vessel) + " EVA state from " +
                currentState.name + " to " + kerbalEVA.st_ragdoll.name + ".");
            return true;
        }

        private static bool canTransitionKerbalToFloatingState(KerbalEVA kerbalEVA,
            KFSMState currentState)
        {
            return currentState != null &&
                (currentState == kerbalEVA.st_idle_gr ||
                currentState == kerbalEVA.st_idle_b_gr ||
                currentState == kerbalEVA.st_walk_acd ||
                currentState == kerbalEVA.st_walk_fps ||
                currentState == kerbalEVA.st_run_acd ||
                currentState == kerbalEVA.st_run_fps ||
                currentState == kerbalEVA.st_bound_gr_acd ||
                currentState == kerbalEVA.st_bound_gr_fps ||
                currentState == kerbalEVA.st_heading_acquire ||
                currentState == kerbalEVA.st_jump ||
                currentState == kerbalEVA.st_land);
        }

        private static KerbalEVA findKerbalEVA(Vessel targetVessel)
        {
            if (targetVessel == null || targetVessel.parts == null)
                return null;

            for (int index = 0; index < targetVessel.parts.Count; index++)
            {
                Part targetPart = targetVessel.parts[index];
                if (targetPart == null)
                    continue;

                KerbalEVA kerbalEVA = targetPart.FindModuleImplementing<KerbalEVA>();
                if (kerbalEVA != null)
                    return kerbalEVA;
            }
            return null;
        }

        private void playPowerSound(PowerSoundOwner owner)
        {
            if (owner == PowerSoundOwner.None || string.IsNullOrEmpty(powerSoundPath) ||
                part == null)
                return;

            if (powerAudioSource == null)
            {
                AudioClip powerClip = GameDatabase.Instance.GetAudioClip(powerSoundPath);
                if (powerClip == null)
                {
                    debugLog("Unable to load power sound: " + powerSoundPath);
                    return;
                }

                powerAudioSource = part.gameObject.AddComponent<AudioSource>();
                powerAudioSource.clip = powerClip;
                powerAudioSource.loop = true;
                powerAudioSource.playOnAwake = false;
                powerAudioSource.dopplerLevel = 0.0f;
                powerAudioSource.spatialBlend = 1.0f;
                powerAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                powerAudioSource.minDistance = 1.0f;
                powerAudioSource.maxDistance = 20.0f;
            }

            powerSoundOwners |= owner;
            powerAudioSource.pitch = powerSoundPitch;
            AudioFX.SetSourceVolume(powerAudioSource, powerSoundVolume,
                AudioFX.AudioChannel.Ship);
            if (!powerAudioSource.isPlaying)
                powerAudioSource.Play();
        }

        private void stopPowerSound(PowerSoundOwner owner)
        {
            powerSoundOwners &= ~owner;
            if (powerSoundOwners != PowerSoundOwner.None)
                return;

            if (powerAudioSource != null && powerAudioSource.isPlaying)
                powerAudioSource.Stop();
        }

        private void stopPowerSound()
        {
            if (powerAudioSource != null && powerAudioSource.isPlaying)
                powerAudioSource.Stop();
            powerSoundOwners = PowerSoundOwner.None;
        }

        private void destroyPowerAudioSource()
        {
            stopPowerSound();
            if (powerAudioSource != null)
                Object.Destroy(powerAudioSource);
            powerAudioSource = null;
        }

        private bool gravityRelativePowersAreAvailable()
        {
            if (part == null || part.vessel == null)
                return false;

            Vessel.Situations situation = part.vessel.situation;
            return situation != Vessel.Situations.ORBITING &&
                situation != Vessel.Situations.SUB_ORBITAL &&
                situation != Vessel.Situations.ESCAPING;
        }

        private int getKerbalExperienceLevel()
        {
            if (part == null || part.protoModuleCrew == null ||
                part.protoModuleCrew.Count == 0 ||
                part.protoModuleCrew[0] == null)
            {
                return 0;
            }

            return System.Math.Max(0, System.Math.Min(5,
                part.protoModuleCrew[0].experienceLevel));
        }

        private bool isPrimaryPowerUnlocked(GraviticsPower power)
        {
            int experienceLevel = getKerbalExperienceLevel();
            switch (power)
            {
                case GraviticsPower.Pull:
                    return experienceLevel >= 1;
                case GraviticsPower.Shove:
                    return experienceLevel >= 3;
                case GraviticsPower.Launch:
                    return experienceLevel >= 4;
                case GraviticsPower.Push:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Removes power selections that the current EVA kerbal has not earned yet.
        /// This also protects against locked selections restored from a save or set by
        /// another module without relying solely on PAW visibility.
        /// </summary>
        private void enforcePowerProgression(bool refreshUI)
        {
            int experienceLevel = getKerbalExperienceLevel();
            bool experienceChanged = experienceLevel != lastKerbalExperienceLevel;
            bool powerStateChanged = false;
            lastKerbalExperienceLevel = experienceLevel;

            if (experienceLevel < 1 &&
                (powerPull || activePower == GraviticsPower.Pull))
            {
                powerPull = false;
                if (activePower == GraviticsPower.Pull)
                    finishPush("Pull locked by experience level");
                powerStateChanged = true;
            }

            if (experienceLevel < 2)
            {
                if (powerLift || liftIsActive)
                {
                    finishLift("Lift locked by experience level");
                    powerLift = false;
                    powerStateChanged = true;
                }
                if (powerLevitate || levitateIsActive)
                {
                    finishLevitate("Levitate locked by experience level");
                    powerLevitate = false;
                    powerStateChanged = true;
                }
            }

            if (experienceLevel < 3 &&
                (powerShove || activePower == GraviticsPower.Shove))
            {
                powerShove = false;
                if (activePower == GraviticsPower.Shove)
                    finishPush("Shove locked by experience level");
                powerStateChanged = true;
            }

            if (experienceLevel < 4 &&
                (powerLaunch || activePower == GraviticsPower.Launch))
            {
                powerLaunch = false;
                if (activePower == GraviticsPower.Launch)
                    finishPush("Launch locked by experience level");
                powerStateChanged = true;
            }

            if (experienceLevel < 5 &&
                (powerSuspensionField || suspensionFieldIsActive))
            {
                finishSuspensionField(
                    "Suspension Field locked by experience level");
                powerSuspensionField = false;
                powerStateChanged = true;
            }

            if (debugMode && experienceChanged)
            {
                debugLog("Kerbal experience level is " + experienceLevel +
                    "; power availability refreshed.");
            }

            if (refreshUI && (experienceChanged || powerStateChanged))
            {
                updatePAW();
                refreshContextWindows();
            }
        }

        private void enforceGravityRelativePowerAvailability()
        {
            bool powersAreAvailable = gravityRelativePowersAreAvailable();
            bool environmentChanged = !gravityPowerAvailabilityInitialized ||
                powersAreAvailable != gravityPowersWereAvailable;
            gravityPowerAvailabilityInitialized = true;
            gravityPowersWereAvailable = powersAreAvailable;

            if (powersAreAvailable)
            {
                if (environmentChanged)
                {
                    updatePAW();
                    refreshContextWindows();
                }
                return;
            }

            bool powerStateChanged = powerLaunch || powerLift ||
                powerSuspensionField || powerLevitate ||
                activePower == GraviticsPower.Launch || liftIsActive ||
                suspensionFieldIsActive || levitateIsActive;
            if (!powerStateChanged && !environmentChanged)
                return;

            if (powerStateChanged)
            {
                powerLaunch = false;
                powerLift = false;
                powerSuspensionField = false;
                powerLevitate = false;
                previousPowerLaunch = false;
                previousPowerLift = false;
                previousPowerSuspensionField = false;
                previousPowerLevitate = false;

                if (activePower == GraviticsPower.Launch)
                    finishPush("Launch unavailable in zero gravity");
                finishLift("Lift unavailable in zero gravity");
                finishSuspensionField(
                    "Suspension Field unavailable in zero gravity");
                finishLevitate("Levitate unavailable in zero gravity");
                debugLog("Gravity-relative powers disabled for vessel situation " +
                    part.vessel.situation + ".");
            }
            updatePAW();
            refreshContextWindows();
        }

        private float calculateForceCorrectionAcceleration(float configuredTargetSpeed,
            float currentUpwardSpeed, float responseTime,
            float maximumCorrectionAcceleration)
        {
            float forceScale = Mathf.Clamp01(forcePercent / 100.0f);
            float targetSpeed = configuredTargetSpeed * forceScale;
            return Mathf.Clamp((targetSpeed - currentUpwardSpeed) / responseTime,
                -maximumCorrectionAcceleration, maximumCorrectionAcceleration);
        }

        private float getForceControlledUpwardSpeed(Vessel targetVessel,
            Rigidbody referenceBody, Vector3 liftDirection)
        {
            Vector3 normalizedUp = liftDirection.normalized;
            float currentUpwardSpeed = Vector3.Dot(referenceBody.velocity,
                normalizedUp);
            if (forcePercent > 0.0f || targetVessel == null ||
                targetVessel.parts == null)
            {
                return currentUpwardSpeed;
            }

            // Zero force is a true hover setting. Remove motion along local up from
            // every rigidbody in the vessel, while preserving horizontal movement.
            for (int index = 0; index < targetVessel.parts.Count; index++)
            {
                Rigidbody targetBody = targetVessel.parts[index].rb;
                if (targetBody == null)
                    continue;

                float bodyUpwardSpeed = Vector3.Dot(targetBody.velocity,
                    normalizedUp);
                targetBody.velocity -= normalizedUp * bodyUpwardSpeed;
            }
            return 0.0f;
        }

        private float getForceAdjustedTargetSpeed(float configuredTargetSpeed)
        {
            return configuredTargetSpeed * Mathf.Clamp01(forcePercent / 100.0f);
        }

        /// <summary>
        /// Calculates the upward acceleration needed to counter local gravity plus a
        /// bounded velocity correction. Future timed levitation powers can reuse this.
        /// </summary>
        protected virtual float calculateGravityCompensatedAcceleration(
            float gravityAcceleration, float velocityCorrectionAcceleration)
        {
            return Mathf.Max(0.0f,
                gravityAcceleration + velocityCorrectionAcceleration);
        }

        /// <summary>
        /// Applies a uniform acceleration to every physical part of a vessel.
        /// </summary>
        protected virtual void applyVesselAcceleration(Vessel targetVessel,
            Vector3 direction, float acceleration)
        {
            if (targetVessel == null || targetVessel.parts == null ||
                direction.sqrMagnitude <= Mathf.Epsilon || acceleration <= 0.0f)
            {
                return;
            }

            Vector3 normalizedDirection = direction.normalized;
            List<Part> targetParts = targetVessel.parts;
            for (int index = 0; index < targetParts.Count; index++)
            {
                Part targetVesselPart = targetParts[index];
                if (targetVesselPart == null || targetVesselPart.rb == null)
                    continue;

                targetVesselPart.rb.WakeUp();
                targetVesselPart.rb.AddForce(normalizedDirection *
                    (targetVesselPart.rb.mass * acceleration), ForceMode.Force);
            }
        }

        /// <summary>
        /// Keeps the custom colors authoritative over stock mouse-over highlighting.
        /// </summary>
        public void LateUpdate()
        {
            if (!canOperate())
                return;

            updatePushStarburstPose();
            if (kerbalIsHighlighted && part != null)
                applyHighlight(part);
            if (hoveredPart != null)
                applyHighlight(hoveredPart);
            applySuspensionFieldHighlights();
        }

        /// <summary>
        /// Restores transient visuals and unsubscribes if KSP destroys the dynamic module.
        /// </summary>
        public void OnDestroy()
        {
            clearInteractionState();
            destroyGravioliumBeam();
            destroyLiftBeam();
            destroyGravioliumStarburst();
            destroyPowerAudioSource();

            if (!eventsSubscribed)
                return;

            GameEvents.onGamePause.Remove(onPaused);
            GameEvents.onGameUnpause.Remove(onUnpaused);
            GameEvents.onCrewBoardVessel.Remove(onCrewBoardVessel);
            GameEvents.onGameSceneLoadRequested.Remove(onGameSceneLoadRequested);
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
            GameEvents.OnEVAConstructionMode.Remove(onEVAConstructionMode);
            eventsSubscribed = false;
        }

        /// <summary>
        /// Returns the localized module title displayed in the EVA action window.
        /// </summary>
        public override string GetModuleDisplayName()
        {
            return Localizer.Format("#LOC_BLUESHIFT_evaGraviticsTitle");
        }

        /// <summary>
        /// Returns the localized resonator description for part information panels.
        /// </summary>
        public override string GetInfo()
        {
            return Localizer.Format("#LOC_BLUESHIFT_evaGraviticsDesc");
        }
        #endregion

        #region Events
        /// <summary>
        /// Enables or disables the resonator's targeting mode.
        /// </summary>
        [KSPEvent(guiActive = true, guiActiveEditor = false,
            groupName = GraviticsGroupName,
            groupDisplayName = "#LOC_BLUESHIFT_evaGraviticsGroup",
            guiName = EnableEventName)]
        public void ToggleResonator()
        {
            if (!HighLogic.LoadedSceneIsFlight || !kerbalGearIsActive)
                return;

            graviticsEnabled = !graviticsEnabled;
            if (!graviticsEnabled)
                clearInteractionState();
            else
                ensureKerbalHighlight();

            updatePAW();
            refreshContextWindows();
        }
        #endregion

        #region Targeting and Cooldown Foundation
        /// <summary>
        /// Confirms that a target is still a loaded, unpacked, single-part vessel.
        /// This is called at the point of use so docking and construction changes are honored.
        /// </summary>
        private bool isValidPushTarget(Part targetPart)
        {
            return getPushTargetInvalidReason(targetPart) == null;
        }

        /// <summary>
        /// Calculates Push recovery time. Target mass and local gravity are accepted here so a
        /// tuned nonlinear curve can be added without changing input or physics code.
        /// </summary>
        protected virtual double calculatePushCooldown(double pushDuration,
            float targetMass, double localGravity)
        {
            return pushBaseCooldown + pushCooldownPerSecond * pushDuration;
        }

        /// <summary>
        /// Calculates Pull recovery time. Target mass and local gravity are accepted here so a
        /// tuned nonlinear curve can be added without changing input or physics code.
        /// </summary>
        protected virtual double calculatePullCooldown(double pullDuration,
            float targetMass, double localGravity)
        {
            return pullBaseCooldown + pullCooldownPerSecond * pullDuration;
        }

        /// <summary>
        /// Calculates Shove recovery time. Target mass and local gravity are accepted here so a
        /// tuned nonlinear curve can be added without changing input or physics code.
        /// </summary>
        protected virtual double calculateShoveCooldown(double activeDuration,
            float targetMass, double localGravity)
        {
            return shoveBaseCooldown + shoveCooldownPerSecond * activeDuration;
        }

        /// <summary>
        /// Calculates Launch recovery time. Target mass and local gravity are accepted here so a
        /// tuned nonlinear curve can be added without changing input or physics code.
        /// </summary>
        protected virtual double calculateLaunchCooldown(double activeDuration,
            float targetMass, double localGravity)
        {
            return launchBaseCooldown + launchCooldownPerSecond * activeDuration;
        }

        /// <summary>
        /// Calculates Lift recovery time from actual use, nonlinear target mass, and gravity.
        /// </summary>
        protected virtual double calculateLiftCooldown(double activeDuration,
            float targetMass, double localGravity)
        {
            double gravityFactor = System.Math.Max(0.0, localGravity) /
                liftCooldownReferenceGravity;
            double massCost = System.Math.Pow(System.Math.Max(0.0, targetMass),
                liftCooldownMassExponent);
            return liftBaseCooldown + liftCooldownPerSecond * activeDuration *
                massCost * gravityFactor;
        }

        private double calculateActivePowerCooldown()
        {
            switch (activePower)
            {
                case GraviticsPower.Pull:
                    return calculatePullCooldown(pushDuration, pushTargetMass,
                        pushLocalGravity);
                case GraviticsPower.Shove:
                    return calculateShoveCooldown(pushDuration, pushTargetMass,
                        pushLocalGravity);
                case GraviticsPower.Launch:
                    return calculateLaunchCooldown(pushDuration, pushTargetMass,
                        pushLocalGravity);
                default:
                    return calculatePushCooldown(pushDuration, pushTargetMass,
                        pushLocalGravity);
            }
        }

        private void beginPush(Part targetPart)
        {
            GraviticsPower selectedPower = getSelectedPower();
            double cooldownRemaining = getCooldownRemaining(selectedPower);
            string invalidReason = getPushTargetInvalidReason(targetPart);
            if (selectedPower == GraviticsPower.None ||
                !isPrimaryPowerUnlocked(selectedPower) ||
                cooldownRemaining > 0.0 || invalidReason != null)
            {
                debugLog(getPowerName(selectedPower) + " rejected. cooldown=" +
                    cooldownRemaining.ToString("F2") + "s, target=" + invalidReason);
                return;
            }

            if (isBurstPower(selectedPower) &&
                !tryGetBurstDirection(selectedPower, targetPart,
                    out burstDirection))
            {
                debugLog(getPowerName(selectedPower) +
                    " rejected. Unable to determine the force direction.");
                return;
            }

            activePower = selectedPower;
            burstForceFramesRemaining = isBurstPower(selectedPower)
                ? -1
                : 0;
            burstForcePerFrame = Vector3.zero;
            burstTargetPrepared = false;
            pushTarget = targetPart;
            pushIsActive = true;
            pushDuration = 0.0;
            pushTargetMass = targetPart.vessel.GetTotalMass();
            pushLocalGravity = part.vessel.graviticAcceleration.magnitude;
            setHoveredPart(targetPart);
            playPowerSound(PowerSoundOwner.Primary);
            debugLog(getPowerName(activePower) + " started. target=" +
                getPartName(targetPart));
        }

        private void finishPush(string reason = "interaction cleared")
        {
            stopPushBeam();
            stopPowerSound(PowerSoundOwner.Primary);

            if (!pushIsActive)
            {
                primaryCooldownIncludesLift = false;
                return;
            }

            double cooldown = 0.0;
            double liftCooldownContribution = 0.0;
            if (pushDuration > 0.0)
            {
                if (primaryCooldownIncludesLift)
                    liftCooldownContribution = getLiftCooldownRemaining();
                cooldown = calculateActivePowerCooldown() +
                    liftCooldownContribution;
                double cooldownEndUT = Planetarium.GetUniversalTime() +
                    System.Math.Max(0.0, cooldown);
                if (activePower == GraviticsPower.Pull)
                    pullCooldownEndUT = cooldownEndUT;
                else if (activePower == GraviticsPower.Shove)
                    shoveCooldownEndUT = cooldownEndUT;
                else if (activePower == GraviticsPower.Launch)
                    launchCooldownEndUT = cooldownEndUT;
                else
                    pushCooldownEndUT = cooldownEndUT;

                if (primaryCooldownIncludesLift)
                    liftCooldownEndUT = cooldownEndUT;
            }

            debugLog(getPowerName(activePower) + " stopped. reason=" + reason +
                ", duration=" + pushDuration.ToString("F3") + "s" +
                ", cooldown=" + cooldown.ToString("F2") + "s" +
                ", Lift contribution=" +
                liftCooldownContribution.ToString("F2") + "s");

            pushTarget = null;
            pushIsActive = false;
            activePower = GraviticsPower.None;
            burstDirection = Vector3.zero;
            burstForcePerFrame = Vector3.zero;
            burstForceFramesRemaining = 0;
            burstTargetPrepared = false;
            pushDuration = 0.0;
            pushTargetMass = 0.0f;
            pushLocalGravity = 0.0;
            primaryCooldownIncludesLift = false;
            clearHoveredPart();
        }

        private void beginLift(Part targetPart)
        {
            double cooldownRemaining = getLiftCooldownRemaining();
            string invalidReason = getLiftTargetInvalidReason(targetPart);
            if (!powerLift || getKerbalExperienceLevel() < 2 ||
                cooldownRemaining > 0.0 || invalidReason != null)
            {
                debugLog("Lift rejected. cooldown=" +
                    cooldownRemaining.ToString("F2") + "s, target=" +
                    invalidReason);
                return;
            }

            liftTarget = targetPart;
            liftIsActive = true;
            liftActiveDuration = 0.0;
            liftTargetMass = targetPart.vessel.GetTotalMass();
            liftLocalGravity = targetPart.vessel.graviticAcceleration.magnitude;
            setHoveredPart(targetPart);
            playPowerSound(PowerSoundOwner.Lift);
            debugLog("Lift started. target=" + getPartName(targetPart) +
                ", vesselParts=" + targetPart.vessel.parts.Count +
                ", mass=" + liftTargetMass.ToString("F3") + "t");
        }

        private void finishLift(string reason = "interaction cleared")
        {
            stopLiftBeam();
            stopPowerSound(PowerSoundOwner.Lift);

            if (!liftIsActive)
                return;

            double cooldown = 0.0;
            if (liftActiveDuration > 0.0)
            {
                cooldown = calculateLiftCooldown(liftActiveDuration,
                    liftTargetMass, liftLocalGravity);
                liftCooldownEndUT = Planetarium.GetUniversalTime() +
                    System.Math.Max(0.0, cooldown);
            }

            debugLog("Lift stopped. reason=" + reason +
                ", duration=" + liftActiveDuration.ToString("F3") + "s" +
                ", cooldown=" + cooldown.ToString("F2") + "s");

            liftTarget = null;
            liftIsActive = false;
            liftActiveDuration = 0.0;
            liftTargetMass = 0.0f;
            liftLocalGravity = 0.0;
            clearHoveredPart();
        }

        private double getPushCooldownRemaining()
        {
            return System.Math.Max(0.0, pushCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getPullCooldownRemaining()
        {
            return System.Math.Max(0.0, pullCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getShoveCooldownRemaining()
        {
            return System.Math.Max(0.0, shoveCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getLaunchCooldownRemaining()
        {
            return System.Math.Max(0.0, launchCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getCooldownRemaining(GraviticsPower power)
        {
            switch (power)
            {
                case GraviticsPower.Push:
                    return getPushCooldownRemaining();
                case GraviticsPower.Pull:
                    return getPullCooldownRemaining();
                case GraviticsPower.Shove:
                    return getShoveCooldownRemaining();
                case GraviticsPower.Launch:
                    return getLaunchCooldownRemaining();
                default:
                    return 0.0;
            }
        }

        private double getSelectedCooldownRemaining()
        {
            return getCooldownRemaining(getSelectedPower());
        }

        private double getLiftCooldownRemaining()
        {
            return System.Math.Max(0.0,
                liftCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getSuspensionFieldCooldownRemaining()
        {
            return System.Math.Max(0.0,
                suspensionFieldCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getLevitateCooldownRemaining()
        {
            return System.Math.Max(0.0,
                levitateCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getHeftCooldownRemaining()
        {
            return System.Math.Max(0.0,
                heftCooldownEndUT - Planetarium.GetUniversalTime());
        }

        private double getLongestCooldownRemaining()
        {
            return System.Math.Max(getHeftCooldownRemaining(),
                System.Math.Max(getLevitateCooldownRemaining(),
                System.Math.Max(getSuspensionFieldCooldownRemaining(),
                System.Math.Max(getLiftCooldownRemaining(),
                System.Math.Max(getLaunchCooldownRemaining(),
                System.Math.Max(getShoveCooldownRemaining(),
                System.Math.Max(getPullCooldownRemaining(),
                    getPushCooldownRemaining())))))));
        }

        private void showPushBeam()
        {
            if (pushTarget == null)
                return;

            ensureGravioliumBeam();
            if (gravioliumBeam == null)
                return;

            pushBeamTargetOffset = getVisibleCenterOffset(pushTarget);
            Vector3 targetPosition =
                pushTarget.transform.TransformPoint(pushBeamTargetOffset);
            Transform closestPalm = selectKerbalBeamSource(targetPosition);
            if (closestPalm == null)
                return;

            bool beamAlreadyPlaying = gravioliumBeam.IsPlaying &&
                beamTarget == pushTarget;
            bool sourceChanged = beamSourceTransform != closestPalm;
            beamSourceTransform = closestPalm;

            if (beamAlreadyPlaying && !sourceChanged)
                return;

            beamTarget = pushTarget;
            Vector3 sourcePosition = beamSourceTransform.position;
            pushStarburstTargetOffset = getBeamImpactOffset(pushTarget,
                sourcePosition, pushBeamTargetOffset);
            if (beamAlreadyPlaying)
            {
                gravioliumBeam.SetEndpoints(beamSourceTransform, Vector3.zero,
                    pushTarget.transform, pushBeamTargetOffset);
            }
            else
            {
                gravioliumBeam.Play(beamSourceTransform, Vector3.zero,
                    pushTarget.transform, pushBeamTargetOffset, pushBeamSettings);
            }
        }

        private void showPushStarburst()
        {
            if (!pushStarburstSettings.enabled || pushTarget == null ||
                beamSourceTransform == null)
                return;

            ensureGravioliumStarburst();
            if (gravioliumStarburst == null)
                return;

            Vector3 impactPosition =
                pushTarget.transform.TransformPoint(pushStarburstTargetOffset);
            Vector3 beamDirection = impactPosition - beamSourceTransform.position;
            if (beamDirection.sqrMagnitude <= Mathf.Epsilon)
                return;
            impactPosition -= beamDirection.normalized * 0.01f;

            if (gravioliumStarburst.IsPlaying)
                gravioliumStarburst.SetPose(impactPosition, beamDirection);
            else
                gravioliumStarburst.Play(impactPosition, beamDirection,
                    pushStarburstSettings);
        }

        private void stopPushBeam()
        {
            beamTarget = null;
            if (gravioliumBeam != null)
                gravioliumBeam.StopEffect();
            if (gravioliumStarburst != null)
                gravioliumStarburst.StopEffect();
        }

        private void showLiftBeam()
        {
            if (liftTarget == null)
                return;

            ensureLiftBeam();
            if (liftBeam == null)
                return;

            liftBeamTargetOffset = getVisibleCenterOffset(liftTarget);
            Vector3 targetPosition =
                liftTarget.transform.TransformPoint(liftBeamTargetOffset);
            Transform closestPalm = selectKerbalBeamSource(targetPosition);
            if (closestPalm == null)
                return;

            bool beamAlreadyPlaying = liftBeam.IsPlaying &&
                liftBeamTarget == liftTarget;
            bool sourceChanged = liftBeamSourceTransform != closestPalm;
            liftBeamSourceTransform = closestPalm;

            if (beamAlreadyPlaying && !sourceChanged)
                return;

            liftBeamTarget = liftTarget;
            if (beamAlreadyPlaying)
            {
                liftBeam.SetEndpoints(liftBeamSourceTransform, Vector3.zero,
                    liftTarget.transform, liftBeamTargetOffset);
            }
            else
            {
                liftBeam.Play(liftBeamSourceTransform, Vector3.zero,
                    liftTarget.transform, liftBeamTargetOffset,
                    pushBeamSettings);
            }
        }

        private void stopLiftBeam()
        {
            liftBeamTarget = null;
            liftBeamSourceTransform = null;
            if (liftBeam != null)
                liftBeam.StopEffect();
        }
        #endregion

        #region Highlighting
        private void ensureKerbalHighlight()
        {
            if (kerbalIsHighlighted || part == null)
                return;

            kerbalHighlight = captureHighlight(part);

            // VideoSettings.ApplySettings uses this property to enable every active
            // highlighting compositor. FlightCamera.CycleCameraHighlighter only visits
            // the camera array owned by FlightCamera and can miss another active camera.
            logHighlighterState("before stock compositor enable", true);
            enableStockHighlightingSystem();

            // Reset the Part's cached color so Highlight(Color) cannot return early after
            // a compositor has just been enabled.
            part.SetHighlight(false, false);
            part.RefreshHighlighter();

            applyHighlight(part);
            kerbalIsHighlighted = true;
            debugLog("Kerbal stock highlight enabled. highlightFX=" +
                GameSettings.HIGHLIGHT_FX + ", inFlightHighlight=" +
                GameSettings.INFLIGHT_HIGHLIGHT);
            logHighlighterState("after stock compositor enable", true);
        }

        private void setHoveredPart(Part targetPart)
        {
            if (hoveredPart == targetPart)
                return;

            clearHoveredPart();
            if (targetPart == null)
                return;

            hoveredPart = targetPart;
            hoveredPartHighlight = captureHighlight(targetPart);
            applyHighlight(targetPart);
        }

        private void clearHoveredPart()
        {
            if (hoveredPart != null)
                restoreHighlight(hoveredPartHighlight);
            hoveredPart = null;
        }

        private void clearInteractionState()
        {
            finishPush();
            finishLift();
            finishSuspensionField();
            finishLevitate();
            finishHeft();
            clearHoveredPart();
            if (kerbalIsHighlighted)
            {
                restoreHighlight(kerbalHighlight);
                kerbalIsHighlighted = false;
            }
        }

        private static GraviticsPartHighlight captureHighlight(Part highlightedPart)
        {
            return new GraviticsPartHighlight
            {
                highlightedPart = highlightedPart,
                highlightType = highlightedPart.highlightType,
                highlightColor = highlightedPart.highlightColor
            };
        }

        private static void applyHighlight(Part highlightedPart)
        {
            highlightedPart.SetHighlightColor(ElectricBlue);
            highlightedPart.SetHighlightType(Part.HighlightType.AlwaysOn);
            highlightedPart.SetHighlight(true, false);
        }

        private static void restoreHighlight(GraviticsPartHighlight savedHighlight)
        {
            if (savedHighlight.highlightedPart == null)
                return;

            savedHighlight.highlightedPart.SetHighlight(false, false);
            savedHighlight.highlightedPart.SetHighlightColor(savedHighlight.highlightColor);
            savedHighlight.highlightedPart.SetHighlightType(savedHighlight.highlightType);
        }
        #endregion

        #region Helpers
        private bool canOperate()
        {
            return getOperatingBlocker() == null;
        }

        private static bool isPointerAvailable()
        {
            return getPointerBlocker() == null;
        }

        private bool tryGetCursorPushDirection(Part targetPart, out Vector3 pushDirection)
        {
            pushDirection = Vector3.zero;
            if (FlightCamera.fetch == null || FlightCamera.fetch.mainCamera == null)
                return false;

            Ray cursorRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
            pushDirection = cursorRay.direction.normalized;

            // A camera ray from an overhead view points into the ground. For a landed
            // target that would only increase its contact force and never produce visible
            // motion, so retain the cursor heading while projecting it onto the local
            // surface plane. Free-flight targets keep the full three-dimensional ray.
            if (targetPart != null && targetPart.vessel != null &&
                targetPart.vessel.LandedOrSplashed)
            {
                Vector3 gravityDirection =
                    ((Vector3)targetPart.vessel.graviticAcceleration).normalized;
                if (gravityDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    Vector3 surfaceDirection = Vector3.ProjectOnPlane(pushDirection,
                        gravityDirection);
                    if (surfaceDirection.sqrMagnitude <= 0.0001f && part != null)
                    {
                        Vector3 sourcePosition = part.rb != null
                            ? part.rb.worldCenterOfMass
                            : part.transform.position;
                        Vector3 targetPosition = targetPart.rb != null
                            ? targetPart.rb.worldCenterOfMass
                            : targetPart.transform.position;
                        surfaceDirection = Vector3.ProjectOnPlane(
                            targetPosition - sourcePosition, gravityDirection);
                    }

                    if (surfaceDirection.sqrMagnitude > Mathf.Epsilon)
                        pushDirection = surfaceDirection.normalized;
                }
            }

            return pushDirection.sqrMagnitude > Mathf.Epsilon;
        }

        private bool tryGetPullDirection(Part targetPart, out Vector3 pullDirection)
        {
            pullDirection = Vector3.zero;
            if (part == null || targetPart == null || targetPart.rb == null)
                return false;

            Vector3 kerbalPosition = part.rb != null
                ? part.rb.worldCenterOfMass
                : part.transform.position;
            pullDirection = kerbalPosition - targetPart.rb.worldCenterOfMass;
            return pullDirection.sqrMagnitude > Mathf.Epsilon;
        }

        private bool tryGetShoveDirection(Part targetPart, out Vector3 shoveVector)
        {
            shoveVector = Vector3.zero;
            if (part == null || targetPart == null || targetPart.rb == null)
                return false;

            Vector3 kerbalPosition = part.rb != null
                ? part.rb.worldCenterOfMass
                : part.transform.position;
            shoveVector = targetPart.rb.worldCenterOfMass - kerbalPosition;
            return shoveVector.sqrMagnitude > Mathf.Epsilon;
        }

        private bool tryGetLaunchDirection(Part targetPart, out Vector3 launchVector)
        {
            launchVector = Vector3.zero;
            if (targetPart == null || targetPart.vessel == null)
                return false;

            Rigidbody referenceBody = targetPart.rb != null
                ? targetPart.rb
                : getLiftReferenceRigidbody(targetPart.vessel);
            if (referenceBody == null)
                return false;

            Vector3 gravity = targetPart.vessel.graviticAcceleration;
            if (gravity.sqrMagnitude > Mathf.Epsilon)
            {
                launchVector = -gravity.normalized;
                return true;
            }

            CelestialBody mainBody = targetPart.vessel.mainBody;
            if (mainBody != null)
            {
                launchVector = referenceBody.worldCenterOfMass -
                    (Vector3)mainBody.position;
            }

            if (launchVector.sqrMagnitude <= Mathf.Epsilon)
                launchVector = targetPart.transform.up;
            return launchVector.sqrMagnitude > Mathf.Epsilon;
        }

        private bool tryGetBurstDirection(GraviticsPower power, Part targetPart,
            out Vector3 direction)
        {
            return power == GraviticsPower.Launch
                ? tryGetLaunchDirection(targetPart, out direction)
                : tryGetShoveDirection(targetPart, out direction);
        }

        private bool tryGetActiveForceDirection(Part targetPart,
            out Vector3 direction, out string failureReason)
        {
            direction = Vector3.zero;
            failureReason = null;

            if (activePower == GraviticsPower.Pull)
            {
                if (tryGetPullDirection(targetPart, out direction))
                    return true;
                failureReason = "kerbal and target centers overlap";
                return false;
            }

            if (isBurstPower(activePower))
            {
                direction = burstDirection;
                if (direction.sqrMagnitude > Mathf.Epsilon)
                    return true;
                failureReason = "captured " + getPowerName(activePower) +
                    " direction is invalid";
                return false;
            }

            if (activePower == GraviticsPower.Push &&
                tryGetCursorPushDirection(targetPart, out direction))
            {
                return true;
            }

            failureReason = "flight camera unavailable";
            return false;
        }

        private GraviticsPower getSelectedPower()
        {
            if (powerLaunch)
                return GraviticsPower.Launch;
            if (powerShove)
                return GraviticsPower.Shove;
            if (powerPull)
                return GraviticsPower.Pull;
            if (powerPush)
                return GraviticsPower.Push;
            return GraviticsPower.None;
        }

        private float getActiveForce()
        {
            if (activePower == GraviticsPower.Pull)
                return pullForce;
            if (activePower == GraviticsPower.Shove)
                return shoveImpulse;
            if (activePower == GraviticsPower.Launch)
                return launchImpulse;
            return pushForce;
        }

        private float getActiveBurstForceDuration()
        {
            return activePower == GraviticsPower.Launch
                ? launchForceDuration
                : shoveForceDuration;
        }

        private float getActiveBurstFXDuration()
        {
            return activePower == GraviticsPower.Launch
                ? launchFXDuration
                : shoveFXDuration;
        }

        private static bool isBurstPower(GraviticsPower power)
        {
            return power == GraviticsPower.Shove || power == GraviticsPower.Launch;
        }

        private static string getPowerName(GraviticsPower power)
        {
            switch (power)
            {
                case GraviticsPower.Push:
                    return "Push";
                case GraviticsPower.Pull:
                    return "Pull";
                case GraviticsPower.Shove:
                    return "Shove";
                case GraviticsPower.Launch:
                    return "Launch";
                default:
                    return "Power";
            }
        }

        private void updatePowerSelection()
        {
            bool primarySelectionChanged = powerPush != previousPowerPush ||
                powerPull != previousPowerPull ||
                powerShove != previousPowerShove ||
                powerLaunch != previousPowerLaunch;
            bool liftSelectionChanged = powerLift != previousPowerLift;
            bool suspensionSelectionChanged = powerSuspensionField !=
                previousPowerSuspensionField;
            bool levitateSelectionChanged = powerLevitate !=
                previousPowerLevitate;
            bool heftSelectionChanged = powerHeft != previousPowerHeft;
            if (!primarySelectionChanged && !liftSelectionChanged &&
                !suspensionSelectionChanged && !levitateSelectionChanged &&
                !heftSelectionChanged)
                return;

            if (primarySelectionChanged && powerLaunch && !previousPowerLaunch)
            {
                powerPush = false;
                powerPull = false;
                powerShove = false;
            }
            else if (primarySelectionChanged && powerShove && !previousPowerShove)
            {
                powerPush = false;
                powerPull = false;
                powerLaunch = false;
            }
            else if (primarySelectionChanged && powerPull && !previousPowerPull)
            {
                powerPush = false;
                powerShove = false;
                powerLaunch = false;
            }
            else if (primarySelectionChanged && powerPush && !previousPowerPush)
            {
                powerPull = false;
                powerShove = false;
                powerLaunch = false;
            }
            else if (powerPush)
            {
                powerPull = false;
                powerShove = false;
                powerLaunch = false;
            }
            else if (powerPull)
            {
                powerShove = false;
                powerLaunch = false;
            }
            else if (powerShove)
                powerLaunch = false;

            if (suspensionSelectionChanged && powerSuspensionField &&
                !previousPowerSuspensionField)
            {
                powerLift = false;
                finishLift("Suspension Field selected");
                beginSuspensionField();
            }
            else if (liftSelectionChanged && powerLift && !previousPowerLift)
            {
                powerSuspensionField = false;
                finishSuspensionField("Lift selected");
            }
            else if (suspensionSelectionChanged && !powerSuspensionField)
            {
                finishSuspensionField("Suspension Field disabled from PAW");
            }

            if (levitateSelectionChanged && powerLevitate &&
                !previousPowerLevitate)
            {
                beginLevitate();
            }
            else if (levitateSelectionChanged && !powerLevitate)
            {
                finishLevitate("Levitate disabled from PAW");
            }

            if (heftSelectionChanged && powerHeft && !previousPowerHeft)
            {
                beginHeft();
            }
            else if (heftSelectionChanged && !powerHeft)
            {
                finishHeft("Heft disabled from PAW");
            }

            if (pushIsActive && getSelectedPower() != activePower)
                finishPush("power selection changed");
            if (liftIsActive && !powerLift)
                finishLift("Lift power disabled");

            previousPowerPush = powerPush;
            previousPowerPull = powerPull;
            previousPowerShove = powerShove;
            previousPowerLaunch = powerLaunch;
            previousPowerLift = powerLift;
            previousPowerSuspensionField = powerSuspensionField;
            previousPowerLevitate = powerLevitate;
            previousPowerHeft = powerHeft;
            debugLog("Selected primary power: " +
                getPowerName(getSelectedPower()) + ", Lift=" + powerLift +
                ", Suspension Field=" + powerSuspensionField +
                ", Levitate=" + powerLevitate + ", Heft=" + powerHeft);
            updatePAW();
            refreshContextWindows();
        }

        /// <summary>
        /// Disables powers that may remain armed alongside Push, Pull, Shove, or Launch
        /// but must yield when a primary power actually activates on a valid target.
        /// Future auxiliary powers can share this activation boundary by joining here.
        /// </summary>
        private void disableAuxiliaryPowersForPrimaryActivation()
        {
            primaryCooldownIncludesLift = false;
            bool powerWasDisabled = false;
            if (powerLift)
            {
                powerLift = false;
                finishLift("primary power activated");
                primaryCooldownIncludesLift =
                    getLiftCooldownRemaining() > 0.0;
                powerWasDisabled = true;
            }

            if (!powerWasDisabled)
                return;

            previousPowerLift = powerLift;
            debugLog("Auxiliary powers disabled for " +
                getPowerName(getSelectedPower()) + " activation.");
            updatePAW();
            refreshContextWindows();
        }

        private void ensureGravioliumBeam()
        {
            if (gravioliumBeam != null || part == null)
                return;

            GameObject beamObject = new GameObject("Graviolium Field Beam");
            beamObject.transform.SetParent(part.transform, false);
            beamObject.layer = part.gameObject.layer;
            gravioliumBeam = beamObject.AddComponent<WBIGravioliumBeam>();
            findKerbalBeamSources();
        }

        private void ensureLiftBeam()
        {
            if (liftBeam != null || part == null)
                return;

            GameObject beamObject = new GameObject("Graviolium Lift Beam");
            beamObject.transform.SetParent(part.transform, false);
            beamObject.layer = part.gameObject.layer;
            liftBeam = beamObject.AddComponent<WBIGravioliumBeam>();
            findKerbalBeamSources();
        }

        private void ensureGravioliumStarburst()
        {
            if (gravioliumStarburst != null || part == null)
                return;

            GameObject starburstObject = new GameObject("Graviolium Starburst");
            starburstObject.transform.SetParent(part.transform, false);
            starburstObject.layer = part.gameObject.layer;
            gravioliumStarburst =
                starburstObject.AddComponent<WBIGravioliumStarburst>();
        }

        private void updatePushStarburstPose()
        {
            if (!pushIsActive || pushTarget == null || beamSourceTransform == null ||
                gravioliumStarburst == null || !gravioliumStarburst.IsPlaying)
            {
                return;
            }

            Vector3 impactPosition =
                pushTarget.transform.TransformPoint(pushStarburstTargetOffset);
            Vector3 beamDirection = impactPosition - beamSourceTransform.position;
            if (beamDirection.sqrMagnitude > Mathf.Epsilon)
            {
                impactPosition -= beamDirection.normalized * 0.01f;
                gravioliumStarburst.SetPose(impactPosition, beamDirection);
            }
        }

        private void findKerbalBeamSources()
        {
            Transform leftWrist = null;
            Transform rightWrist = null;
            Transform[] transforms = part.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                switch (transforms[index].name)
                {
                    case "bn_l_mid_a01":
                        leftPalmTransform = transforms[index];
                        break;
                    case "bn_r_mid_a01":
                        rightPalmTransform = transforms[index];
                        break;
                    case "bn_l_wrist01":
                        leftWrist = transforms[index];
                        break;
                    case "bn_r_wrist01":
                        rightWrist = transforms[index];
                        break;
                }
            }

            if (leftPalmTransform == null)
                leftPalmTransform = leftWrist;
            if (rightPalmTransform == null)
                rightPalmTransform = rightWrist;

            KerbalEVA kerbalEVA = part.FindModuleImplementing<KerbalEVA>();
            if (kerbalEVA != null && kerbalEVA.BackpackTransform != null)
                beamSourceFallbackTransform = kerbalEVA.BackpackTransform;
            else
                beamSourceFallbackTransform = part.transform;
        }

        private Transform selectKerbalBeamSource(Vector3 targetPosition)
        {
            if (leftPalmTransform == null && rightPalmTransform == null)
                return beamSourceFallbackTransform;
            if (leftPalmTransform == null)
                return rightPalmTransform;
            if (rightPalmTransform == null)
                return leftPalmTransform;

            float leftDistance =
                (leftPalmTransform.position - targetPosition).sqrMagnitude;
            float rightDistance =
                (rightPalmTransform.position - targetPosition).sqrMagnitude;
            return leftDistance <= rightDistance
                ? leftPalmTransform
                : rightPalmTransform;
        }

        private static Vector3 getVisibleCenterOffset(Part targetPart)
        {
            Renderer[] renderers = targetPart.GetComponentsInChildren<Renderer>(false);
            Bounds visibleBounds = new Bounds();
            bool hasVisibleBounds = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                Renderer targetRenderer = renderers[index];
                if (targetRenderer == null || !targetRenderer.enabled ||
                    targetRenderer.GetType().Name == "ParticleSystemRenderer" ||
                    targetRenderer is LineRenderer)
                {
                    continue;
                }

                if (hasVisibleBounds)
                    visibleBounds.Encapsulate(targetRenderer.bounds);
                else
                {
                    visibleBounds = targetRenderer.bounds;
                    hasVisibleBounds = true;
                }
            }

            Vector3 visibleCenter = hasVisibleBounds
                ? visibleBounds.center
                : targetPart.rb.worldCenterOfMass;
            return targetPart.transform.InverseTransformPoint(visibleCenter);
        }

        private static Vector3 getBeamImpactOffset(Part targetPart,
            Vector3 sourcePosition, Vector3 targetCenterOffset)
        {
            Vector3 targetCenter = targetPart.transform.TransformPoint(targetCenterOffset);
            Vector3 beamVector = targetCenter - sourcePosition;
            float beamLength = beamVector.magnitude;
            if (beamLength <= Mathf.Epsilon)
                return targetCenterOffset;

            Ray beamRay = new Ray(sourcePosition, beamVector / beamLength);
            Collider[] targetColliders =
                targetPart.GetComponentsInChildren<Collider>(false);
            bool foundImpact = false;
            float nearestDistance = float.MaxValue;
            Vector3 impactPosition = targetCenter;
            for (int index = 0; index < targetColliders.Length; index++)
            {
                Collider targetCollider = targetColliders[index];
                if (targetCollider == null || !targetCollider.enabled ||
                    targetCollider.isTrigger)
                {
                    continue;
                }

                RaycastHit hit;
                if (targetCollider.Raycast(beamRay, out hit, beamLength + 0.5f) &&
                    hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    impactPosition = hit.point;
                    foundImpact = true;
                }
            }

            return targetPart.transform.InverseTransformPoint(
                foundImpact ? impactPosition : targetCenter);
        }

        private void destroyGravioliumBeam()
        {
            if (gravioliumBeam == null)
                return;

            GameObject beamObject = gravioliumBeam.gameObject;
            gravioliumBeam.StopEffect();
            gravioliumBeam = null;
            beamTarget = null;
            beamSourceTransform = null;
            leftPalmTransform = null;
            rightPalmTransform = null;
            beamSourceFallbackTransform = null;
            Destroy(beamObject);
        }

        private void destroyLiftBeam()
        {
            if (liftBeam == null)
                return;

            GameObject beamObject = liftBeam.gameObject;
            liftBeam.StopEffect();
            liftBeam = null;
            liftBeamTarget = null;
            liftBeamSourceTransform = null;
            Destroy(beamObject);
        }

        private void destroyGravioliumStarburst()
        {
            if (gravioliumStarburst == null)
                return;

            GameObject starburstObject = gravioliumStarburst.gameObject;
            gravioliumStarburst.StopEffect();
            gravioliumStarburst = null;
            Destroy(starburstObject);
        }

        private string getOperatingBlocker()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return "not in flight";
            if (!kerbalGearIsActive)
                return "KerbalGear module inactive";
            if (!graviticsEnabled)
                return "gravitics disabled";
            if (!enabled)
                return "Unity behaviour disabled";
            if (!moduleIsEnabled)
                return "part module disabled";
            if (gamePaused)
                return "game paused";
            if (MapView.MapIsEnabled)
                return "map view enabled";
            if (part == null)
                return "EVA part missing";
            if (part.vessel == null)
                return "EVA vessel missing";
            if (part.vessel != FlightGlobals.ActiveVessel)
                return "EVA vessel is not active";
            if (part.FindModuleImplementing<KerbalEVA>() == null)
                return "KerbalEVA module missing";
            return null;
        }

        private bool canContinueActivePower()
        {
            if (!canOperate() || !isValidPushTarget(pushTarget))
                return false;

            // Shove is triggered by one click and then runs autonomously for its
            // configured duration. Push and Pull remain hold-to-use powers.
            return isBurstPower(activePower) ||
                (Input.GetMouseButton(0) && isPointerAvailable());
        }

        private static string getPointerBlocker()
        {
            if (!InputLockManager.IsUnlocked(ControlTypes.UI_MAIN))
                return "UI_MAIN locked";
            if (!InputLockManager.IsUnlocked(ControlTypes.UI_DIALOGS))
                return "UI_DIALOGS locked";
            if (!InputLockManager.IsUnlocked(ControlTypes.EVA_INPUT))
                return "EVA_INPUT locked";
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return "pointer over UI";
            return null;
        }

        private string getPushTargetInvalidReason(Part targetPart)
        {
            return getTargetInvalidReason(targetPart, true);
        }

        private string getTargetInvalidReason(Part targetPart,
            bool requireTargetRigidbody)
        {
            if (targetPart == null)
                return "no hovered part";
            if (targetPart == part)
                return "hovered part is the EVA kerbal";
            if (targetPart.vessel == null)
                return "target vessel missing";
            if (part == null || targetPart.vessel == part.vessel)
                return "target belongs to the EVA vessel";
            if (!targetPart.vessel.loaded)
                return "target vessel unloaded";
            if (targetPart.vessel.packed)
                return "target vessel packed";
            if (targetPart.vessel.state == Vessel.State.DEAD)
                return "target vessel dead";
            if (targetPart.vessel.parts == null)
                return "target part list missing";
            if (targetPart.vessel.parts.Count == 0)
                return "target vessel has no parts";
            if (requireTargetRigidbody && targetPart.rb == null)
                return "target rigidbody missing";

            float distance = Vector3.Distance(part.transform.position,
                targetPart.transform.position);
            if (distance > targetingRange)
                return "target is " + distance.ToString("F2") + "m away (range " +
                    targetingRange.ToString("F2") + "m)";
            return null;
        }

        private bool isValidLiftTarget(Part targetPart)
        {
            return getLiftTargetInvalidReason(targetPart) == null;
        }

        private string getLiftTargetInvalidReason(Part targetPart)
        {
            string invalidReason = getTargetInvalidReason(targetPart, false);
            if (invalidReason != null)
                return invalidReason;

            if (getLiftReferenceRigidbody(targetPart.vessel) == null)
                return "target vessel has no rigidbody";

            float targetMass = targetPart.vessel.GetTotalMass();
            if (targetMass > liftMaxMass)
            {
                return "target mass " + targetMass.ToString("F2") +
                    "t exceeds Lift limit " + liftMaxMass.ToString("F2") + "t";
            }

            return null;
        }

        private static Rigidbody getLiftReferenceRigidbody(Vessel targetVessel)
        {
            if (targetVessel == null || targetVessel.parts == null)
                return null;

            List<Part> targetParts = targetVessel.parts;
            for (int index = 0; index < targetParts.Count; index++)
            {
                if (targetParts[index] != null && targetParts[index].rb != null)
                    return targetParts[index].rb;
            }
            return null;
        }

        private void showLiftRejection(Part targetPart)
        {
            string invalidReason = getLiftTargetInvalidReason(targetPart);
            if (invalidReason == null)
                return;

            if (targetPart != null && targetPart.vessel != null &&
                targetPart.vessel.GetTotalMass() > liftMaxMass)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsLiftTooHeavy",
                    targetPart.vessel.GetTotalMass().ToString("F2"),
                    liftMaxMass.ToString("F2")), 3.0f,
                    ScreenMessageStyle.UPPER_CENTER);
            }
            else
            {
                ScreenMessages.PostScreenMessage(Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsLiftInvalid", invalidReason),
                    3.0f, ScreenMessageStyle.UPPER_CENTER);
            }
            debugLog("Lift rejected. " + invalidReason);
        }

        private string getPushStopReason()
        {
            string operatingBlocker = getOperatingBlocker();
            if (operatingBlocker != null)
                return "module blocked: " + operatingBlocker;
            if (!isBurstPower(activePower))
            {
                if (!Input.GetMouseButton(0))
                    return "left mouse released";

                string pointerBlocker = getPointerBlocker();
                if (pointerBlocker != null)
                    return "input blocked: " + pointerBlocker;
            }

            string targetReason = getPushTargetInvalidReason(pushTarget);
            return targetReason != null ? "target invalid: " + targetReason : "unknown";
        }

        private void logDiagnosticState(Part rawHoveredPart)
        {
            if (!debugMode)
                return;

            string operatingBlocker = getOperatingBlocker();
            string pointerBlocker = getPointerBlocker();
            string diagnosticState = "gravitics=" + graviticsEnabled +
                ", powerPush=" + powerPush +
                ", powerPull=" + powerPull +
                ", powerShove=" + powerShove +
                ", powerLaunch=" + powerLaunch +
                ", powerLift=" + powerLift +
                ", powerSuspensionField=" + powerSuspensionField +
                ", powerLevitate=" + powerLevitate +
                ", operating=" + (operatingBlocker ?? "ready") +
                ", pointer=" + (pointerBlocker ?? "ready") +
                ", cooldown=" + (getSelectedCooldownRemaining() > 0.0) +
                ", activePower=" + getPowerName(activePower) +
                ", applyingForce=" + pushIsActive +
                ", lifting=" + liftIsActive +
                ", liftCooldown=" + getLiftCooldownRemaining().ToString("F2") +
                "s, suspending=" + suspensionFieldIsActive +
                ", suspensionTargets=" + suspensionFieldTargets.Count +
                ", suspensionCooldown=" +
                getSuspensionFieldCooldownRemaining().ToString("F2") +
                "s, levitating=" + levitateIsActive +
                ", levitateCooldown=" +
                getLevitateCooldownRemaining().ToString("F2") +
                "s";
            if (diagnosticState != lastDiagnosticState)
            {
                lastDiagnosticState = diagnosticState;
                debugLog("State: " + diagnosticState);
            }

            logHighlighterState("runtime", false);

            if (rawHoveredPart == lastDiagnosticHoveredPart)
                return;

            lastDiagnosticHoveredPart = rawHoveredPart;
            debugLog("Hover changed. part=" + getPartName(rawHoveredPart) +
                ", target=" + (getPushTargetInvalidReason(rawHoveredPart) ?? "valid"));
        }

        private static string getPartName(Part targetPart)
        {
            if (targetPart == null)
                return "none";
            return targetPart.partInfo != null ? targetPart.partInfo.name : targetPart.name;
        }

        private void debugLog(string message)
        {
            if (debugMode)
                Debug.Log("[Blueshift:WBIEVAGravitics] " + message);
        }

        private void logHighlighterState(string reason, bool force)
        {
            if (!debugMode)
                return;

            Highlighter partHighlighter = part != null ? part.highlighter : null;
            HighlightingSystem[] systems = Object.FindObjectsOfType<HighlightingSystem>();
            string state = "partHighlighter=" + (partHighlighter != null) +
                ", componentEnabled=" + (partHighlighter != null && partHighlighter.enabled) +
                ", highlighted=" + (partHighlighter != null && partHighlighter.highlighted) +
                ", highlightActive=" + (part != null && part.HighlightActive) +
                ", rendererCount=" + (part != null && part.HighlightRenderer != null
                    ? part.HighlightRenderer.Count : 0) +
                ", configuredAA=" + GameSettings.ANTI_ALIASING +
                ", runtimeAA=" + QualitySettings.antiAliasing +
                ", systems=" + systems.Length;

            for (int index = 0; index < systems.Length; index++)
            {
                HighlightingSystem system = systems[index];
                Camera camera = system.GetComponent<Camera>();
                state += ", [" + index + ":" +
                    (camera != null ? camera.name : system.gameObject.name) +
                    " enabled=" + system.enabled +
                    " active=" + system.gameObject.activeInHierarchy +
                    " supported=" + system.IsSupported + "]";
            }

            if (!force && state == lastHighlighterDiagnosticState)
                return;

            lastHighlighterDiagnosticState = state;
            debugLog("Highlight state (" + reason + "): " + state);
        }

        private void enableStockHighlightingSystem()
        {
            if (!GameSettings.HIGHLIGHT_FX)
                return;

            // KSP's edge highlighter requires multisample anti-aliasing. A graphics
            // component can change Unity's live quality level after GameSettings has
            // loaded, leaving runtime AA at zero until Video Settings is applied. Restore
            // the player's saved nonzero AA value before asking KSP to enable the effect.
            if (QualitySettings.antiAliasing == 0 && GameSettings.ANTI_ALIASING > 0)
            {
                debugLog("Restoring runtime anti-aliasing from 0 to configured value " +
                    GameSettings.ANTI_ALIASING + " for stock highlighting.");
                QualitySettings.antiAliasing = GameSettings.ANTI_ALIASING;
            }

            HighlightingSystem.FxEnabled = true;
        }

        private void updatePAW()
        {
            if (Events != null && Events.Contains(ToggleEventName))
            {
                BaseEvent toggleEvent = Events[ToggleEventName];
                bool eventIsAvailable = HighLogic.LoadedSceneIsFlight && kerbalGearIsActive;
                toggleEvent.active = eventIsAvailable;
                toggleEvent.guiActive = eventIsAvailable;
                toggleEvent.guiName = Localizer.Format(graviticsEnabled
                    ? DisableEventName
                    : EnableEventName);
            }

            if (Fields != null)
            {
                bool gravityPowersAvailable = gravityRelativePowersAreAvailable();
                int experienceLevel = getKerbalExperienceLevel();
                Fields["graviticsStatus"].guiActive = kerbalGearIsActive;
                Fields["powerPush"].guiActive = kerbalGearIsActive && graviticsEnabled;
                Fields["powerPull"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 1;
                Fields["powerShove"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 3;
                Fields["powerLaunch"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 4 &&
                    gravityPowersAvailable;
                Fields["powerLift"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 2 &&
                    gravityPowersAvailable;
                Fields["powerSuspensionField"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 5 &&
                    gravityPowersAvailable;
                Fields["powerLevitate"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && experienceLevel >= 2 &&
                    gravityPowersAvailable;
                Fields["powerHeft"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled;
                Fields["forcePercent"].guiActive = kerbalGearIsActive &&
                    graviticsEnabled && gravityPowersAvailable &&
                    (powerLift || powerSuspensionField || powerLevitate);
            }

            updateGraviticsStatus();
        }

        private void updateGraviticsStatus()
        {
            if (Fields == null)
                return;

            BaseField statusField = Fields["graviticsStatus"];
            if (!graviticsEnabled)
            {
                statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsStatus");
                double disabledCooldownRemaining = getLongestCooldownRemaining();
                graviticsStatus = disabledCooldownRemaining > 0.0
                    ? Localizer.Format("#LOC_BLUESHIFT_evaGraviticsCooldown",
                        disabledCooldownRemaining.ToString("F1"))
                    : Localizer.Format("#LOC_BLUESHIFT_disabled");
                return;
            }

            GraviticsPower selectedPower = getSelectedPower();
            if (pushIsActive)
            {
                string activeStatusName = "#LOC_BLUESHIFT_evaGraviticsPowerPush";
                string activeStatus = "#LOC_BLUESHIFT_evaGraviticsPushing";
                if (activePower == GraviticsPower.Pull)
                {
                    activeStatusName = "#LOC_BLUESHIFT_evaGraviticsPowerPull";
                    activeStatus = "#LOC_BLUESHIFT_evaGraviticsPulling";
                }
                else if (activePower == GraviticsPower.Shove)
                {
                    activeStatusName = "#LOC_BLUESHIFT_evaGraviticsPowerShove";
                    activeStatus = "#LOC_BLUESHIFT_evaGraviticsShoving";
                }
                else if (activePower == GraviticsPower.Launch)
                {
                    activeStatusName = "#LOC_BLUESHIFT_evaGraviticsPowerLaunch";
                    activeStatus = "#LOC_BLUESHIFT_evaGraviticsLaunching";
                }
                statusField.guiName = Localizer.Format(activeStatusName);
                graviticsStatus = Localizer.Format(activeStatus);
                return;
            }

            if (liftIsActive)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerLift");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsLifting");
                return;
            }

            if (suspensionFieldIsActive)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerSuspensionField");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsSuspending");
                return;
            }

            if (levitateIsActive)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerLevitate");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsLevitating");
                return;
            }

            if (heftIsApplied)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerHeft");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsHeftActive");
                return;
            }

            // Cooldowns belong to the resonator session, not just the power that is
            // currently selected. Keep the longest outstanding timer visible when the
            // player switches powers so recovery never disappears from the PAW.
            double cooldownRemaining = getLongestCooldownRemaining();
            if (cooldownRemaining > 0.0)
            {
                statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsStatus");
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsCooldown",
                    cooldownRemaining.ToString("F1"));
                return;
            }

            if (selectedPower == GraviticsPower.None && powerLift)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerLift");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            if (selectedPower == GraviticsPower.None && powerSuspensionField)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerSuspensionField");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            if (selectedPower == GraviticsPower.None && powerLevitate)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerLevitate");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            if (selectedPower == GraviticsPower.None && powerHeft)
            {
                statusField.guiName = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsPowerHeft");
                graviticsStatus = Localizer.Format(
                    "#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            if (selectedPower == GraviticsPower.None)
            {
                statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsStatus");
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            string statusName = "#LOC_BLUESHIFT_evaGraviticsPowerPush";
            if (selectedPower == GraviticsPower.Pull)
                statusName = "#LOC_BLUESHIFT_evaGraviticsPowerPull";
            else if (selectedPower == GraviticsPower.Shove)
                statusName = "#LOC_BLUESHIFT_evaGraviticsPowerShove";
            else if (selectedPower == GraviticsPower.Launch)
                statusName = "#LOC_BLUESHIFT_evaGraviticsPowerLaunch";
            statusField.guiName = Localizer.Format(statusName);
            graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsReady");
        }

        private void refreshContextWindows()
        {
            if (part != null)
                MonoUtilities.RefreshContextWindows(part);
        }

        private void onPaused()
        {
            gamePaused = true;
            finishPush();
            finishLift();
            finishSuspensionField();
            finishLevitate();
            clearHoveredPart();
        }

        private void onUnpaused()
        {
            gamePaused = false;
        }

        private void onCrewBoardVessel(
            GameEvents.FromToAction<Part, Part> boardingAction)
        {
            if (boardingAction.from != part)
                return;

            finishHeft("kerbal boarded vessel");
        }

        private void onGameSceneLoadRequested(GameScenes requestedScene)
        {
            if (requestedScene != GameScenes.FLIGHT)
                finishHeft("leaving flight scene");
        }

        private void onGameSettingsApplied()
        {
            if (powerAudioSource != null)
            {
                AudioFX.SetSourceVolume(powerAudioSource, powerSoundVolume,
                    AudioFX.AudioChannel.Ship);
            }
            debugLog("Game settings applied. highlightFX=" + GameSettings.HIGHLIGHT_FX +
                ", inFlightHighlight=" + GameSettings.INFLIGHT_HIGHLIGHT +
                ", qualityLevel=" + QualitySettings.GetQualityLevel() +
                ", antiAliasing=" + QualitySettings.antiAliasing);
            logHighlighterState("game settings applied", true);
        }

        private void onEVAConstructionMode(bool constructionModeEnabled)
        {
            if (heftIsApplied)
                GameEvents.OnCombinedConstructionWeightLimitChanged.Fire();

            if (!graviticsEnabled || !kerbalGearIsActive || part == null ||
                part.vessel == null || part.vessel != FlightGlobals.ActiveVessel)
                return;

            EVAConstructionModeController constructionController =
                EVAConstructionModeController.Instance;
            EVAConstructionModeController.PanelMode previousPanelMode =
                EVAConstructionModeController.PanelMode.Cargo;
            bool temporarilyChangedPanelMode = constructionModeEnabled &&
                constructionController != null && constructionController.IsOpen &&
                constructionController.panelMode ==
                    EVAConstructionModeController.PanelMode.Construction;

            // HighlightingSystem.OnEnable refuses to initialize while the construction
            // panel reports Construction mode. The stock controller has already finished
            // entering the mode when this event fires, so briefly expose its non-blocking
            // panel state while the compositor is rebuilt, then immediately restore it.
            try
            {
                if (temporarilyChangedPanelMode)
                {
                    previousPanelMode = constructionController.panelMode;
                    constructionController.panelMode =
                        EVAConstructionModeController.PanelMode.Cargo;
                }

                enableStockHighlightingSystem();
            }
            finally
            {
                if (temporarilyChangedPanelMode)
                    constructionController.panelMode = previousPanelMode;
            }

            part.SetHighlight(false, false);
            part.RefreshHighlighter();
            applyHighlight(part);
            kerbalIsHighlighted = true;
            debugLog("Kerbal highlight restored after EVA Construction mode " +
                (constructionModeEnabled ? "entered." : "exited."));
            logHighlighterState("EVA Construction mode transition", true);
        }
        #endregion
    }
}
