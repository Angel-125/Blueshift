using System.Collections.Generic;
using KSP.Localization;
using Highlighting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Blueshift
{
    internal struct GraviticsPartHighlight
    {
        public Part highlightedPart;
        public Part.HighlightType highlightType;
        public Color highlightColor;
    }

    /// <summary>
    /// Provides the EVA abilities supplied by a KerbalGear Graviolium Field Resonator.
    /// The module is created dynamically while a carried wearable requests it.
    /// </summary>
    [KSPModule("#LOC_BLUESHIFT_evaGraviticsTitle")]
    public class WBIEVAGravitics : WBIPartModule
    {
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
            pushForce = Mathf.Max(0.0f, pushForce);
            pushBaseCooldown = Mathf.Max(0.0f, pushBaseCooldown);
            pushCooldownPerSecond = Mathf.Max(0.0f, pushCooldownPerSecond);
            pupilGlowIntensity = Mathf.Max(0.0f, pupilGlowIntensity);
            pupilGlowFalloff = Mathf.Clamp(pupilGlowFalloff, 0.01f, 5.0f);

            GameEvents.onGamePause.Add(onPaused);
            GameEvents.onGameUnpause.Add(onUnpaused);
            GameEvents.OnGameSettingsApplied.Add(onGameSettingsApplied);
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
            Part rawHoveredPart = Mouse.HoveredPart;
            logDiagnosticState(rawHoveredPart);

            if (Input.GetMouseButtonDown(0))
            {
                debugLog("Left mouse down. pointer=" + (getPointerBlocker() ?? "ready") +
                    ", hovered=" + getPartName(rawHoveredPart) +
                    ", target=" + (getPushTargetInvalidReason(rawHoveredPart) ?? "valid") +
                    ", cooldown=" + getPushCooldownRemaining().ToString("F2") + "s");
            }

            if (!canOperate())
            {
                finishPush("module blocked: " + getOperatingBlocker());
                clearHoveredPart();
                updateGraviticsStatus();
                return;
            }

            ensureKerbalHighlight();

            if (!powerPush)
            {
                finishPush("Push power disabled");
                clearHoveredPart();
                updateGraviticsStatus();
                return;
            }

            if (pushIsActive)
            {
                if (!Input.GetMouseButton(0) || !isPointerAvailable() ||
                    !isValidPushTarget(pushTarget))
                {
                    finishPush(getPushStopReason());
                }
                else
                {
                    setHoveredPart(pushTarget);
                }

                updateGraviticsStatus();
                return;
            }

            if (getPushCooldownRemaining() > 0.0)
            {
                clearHoveredPart();
                updateGraviticsStatus();
                return;
            }

            Part currentHoveredPart = isPointerAvailable() ? rawHoveredPart : null;
            if (!isValidPushTarget(currentHoveredPart))
                currentHoveredPart = null;
            setHoveredPart(currentHoveredPart);

            if (currentHoveredPart != null && Input.GetMouseButtonDown(0))
                beginPush(currentHoveredPart);

            updateGraviticsStatus();
        }

        /// <summary>
        /// Applies continuous Push force at physics cadence.
        /// </summary>
        public void FixedUpdate()
        {
            handlePushPhysics();
        }

        /// <summary>
        /// Prevents a separately hosted beam from remaining visible if Unity disables this module.
        /// </summary>
        public void OnDisable()
        {
            finishPush("Unity behaviour disabled");
        }

        private void handlePushPhysics()
        {
            if (!powerPush || !pushIsActive)
            {
                finishPush(!powerPush ? "Push power disabled" : "Push inactive");
                return;
            }

            if (!canOperate() || !Input.GetMouseButton(0) || !isPointerAvailable() ||
                !isValidPushTarget(pushTarget))
            {
                finishPush(getPushStopReason());
                return;
            }

            Vector3 pushDirection;
            if (!tryGetCursorPushDirection(pushTarget, out pushDirection))
            {
                finishPush("flight camera unavailable");
                return;
            }

            if (pushDuration <= 0.0)
            {
                debugLog("Applying Push. target=" + getPartName(pushTarget) +
                    ", force=" + pushForce.ToString("F3") + "kN" +
                    ", mass=" + pushTarget.vessel.GetTotalMass().ToString("F3") + "t" +
                    ", distance=" + Vector3.Distance(part.transform.position,
                        pushTarget.transform.position).ToString("F2") + "m" +
                    ", direction=" + pushDirection.ToString("F3") +
                    ", landed=" + pushTarget.vessel.LandedOrSplashed +
                    ", kinematic=" + pushTarget.rb.isKinematic +
                    ", sleeping=" + pushTarget.rb.IsSleeping() +
                    ", constraints=" + pushTarget.rb.constraints);
            }

            pushTarget.rb.WakeUp();
            pushTarget.rb.AddForce(pushDirection.normalized * pushForce, ForceMode.Force);
            showPushBeam();
            showPushStarburst();
            pushDuration += Time.fixedDeltaTime;
            pushTargetMass = pushTarget.vessel.GetTotalMass();
            pushLocalGravity = part.vessel.graviticAcceleration.magnitude;
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
        }

        /// <summary>
        /// Restores transient visuals and unsubscribes if KSP destroys the dynamic module.
        /// </summary>
        public void OnDestroy()
        {
            clearInteractionState();
            destroyGravioliumBeam();
            destroyGravioliumStarburst();

            if (!eventsSubscribed)
                return;

            GameEvents.onGamePause.Remove(onPaused);
            GameEvents.onGameUnpause.Remove(onUnpaused);
            GameEvents.OnGameSettingsApplied.Remove(onGameSettingsApplied);
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

        private void beginPush(Part targetPart)
        {
            double cooldownRemaining = getPushCooldownRemaining();
            string invalidReason = getPushTargetInvalidReason(targetPart);
            if (cooldownRemaining > 0.0 || invalidReason != null)
            {
                debugLog("Push rejected. cooldown=" + cooldownRemaining.ToString("F2") +
                    "s, target=" + invalidReason);
                return;
            }

            pushTarget = targetPart;
            pushIsActive = true;
            pushDuration = 0.0;
            pushTargetMass = targetPart.vessel.GetTotalMass();
            pushLocalGravity = part.vessel.graviticAcceleration.magnitude;
            setHoveredPart(targetPart);
            debugLog("Push started. target=" + getPartName(targetPart));
        }

        private void finishPush(string reason = "interaction cleared")
        {
            stopPushBeam();

            if (!pushIsActive)
                return;

            double cooldown = 0.0;
            if (pushDuration > 0.0)
            {
                cooldown = calculatePushCooldown(pushDuration,
                    pushTargetMass, pushLocalGravity);
                pushCooldownEndUT = Planetarium.GetUniversalTime() +
                    System.Math.Max(0.0, cooldown);
            }

            debugLog("Push stopped. reason=" + reason +
                ", duration=" + pushDuration.ToString("F3") + "s" +
                ", cooldown=" + cooldown.ToString("F2") + "s");

            pushTarget = null;
            pushIsActive = false;
            pushDuration = 0.0;
            pushTargetMass = 0.0f;
            pushLocalGravity = 0.0;
            clearHoveredPart();
        }

        private double getPushCooldownRemaining()
        {
            return System.Math.Max(0.0, pushCooldownEndUT - Planetarium.GetUniversalTime());
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
            if (targetPart.rb == null)
                return "target rigidbody missing";

            float distance = Vector3.Distance(part.transform.position,
                targetPart.transform.position);
            if (distance > targetingRange)
                return "target is " + distance.ToString("F2") + "m away (range " +
                    targetingRange.ToString("F2") + "m)";
            return null;
        }

        private string getPushStopReason()
        {
            string operatingBlocker = getOperatingBlocker();
            if (operatingBlocker != null)
                return "module blocked: " + operatingBlocker;
            if (!Input.GetMouseButton(0))
                return "left mouse released";

            string pointerBlocker = getPointerBlocker();
            if (pointerBlocker != null)
                return "input blocked: " + pointerBlocker;

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
                ", operating=" + (operatingBlocker ?? "ready") +
                ", pointer=" + (pointerBlocker ?? "ready") +
                ", cooldown=" + (getPushCooldownRemaining() > 0.0) +
                ", pushing=" + pushIsActive;
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
                Fields["graviticsStatus"].guiActive = kerbalGearIsActive;
                Fields["powerPush"].guiActive = kerbalGearIsActive && graviticsEnabled;
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
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_disabled");
                return;
            }

            double cooldownRemaining = getPushCooldownRemaining();
            if (cooldownRemaining > 0.0)
            {
                statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsStatus");
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsCooldown",
                    cooldownRemaining.ToString("F1"));
                return;
            }

            if (!powerPush)
            {
                statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsStatus");
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsReady");
                return;
            }

            statusField.guiName = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsPowerPush");
            if (pushIsActive)
            {
                graviticsStatus = Localizer.Format("#LOC_BLUESHIFT_evaGraviticsPushing");
                return;
            }

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
            clearHoveredPart();
        }

        private void onUnpaused()
        {
            gamePaused = false;
        }

        private void onGameSettingsApplied()
        {
            debugLog("Game settings applied. highlightFX=" + GameSettings.HIGHLIGHT_FX +
                ", inFlightHighlight=" + GameSettings.INFLIGHT_HIGHLIGHT +
                ", qualityLevel=" + QualitySettings.GetQualityLevel() +
                ", antiAliasing=" + QualitySettings.antiAliasing);
            logHighlighterState("game settings applied", true);
        }
        #endregion
    }
}
