using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    /// <summary>
    /// Counters the pull of gravity up to a maximum amount of gravitic acceleration.
    /// </summary>
    public class WBIFlexGravGeneratorAnimated: WBIFlexGravGenerator
    {
        public enum WBIGeneratorStates
        {
            Shutdown,
            Starting,
            ShuttingDown,
            Running,
            Flameout
        }

        #region Fields
        [KSPField(guiName = "#LOC_BLUESHIFT_flexGravSingularity", guiActive = true, isPersistant = true)]
        public WBIGeneratorStates generatorState = WBIGeneratorStates.Shutdown;

        #region Animation
        protected const int kDefaultAnimationLayer = 2;

        [KSPField]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField]
        public float startupTime = 2.0f;

        [KSPField]
        public float shutdownTime = 1.0f;

        [KSPField]
        public string gravRingTransformName = string.Empty;

        [KSPField]
        public string gravRingSpinAxis = "0,0,1";

        [KSPField]
        public float spinRateRPMMin = 3.0f;

        [KSPField]
        public float spinRateRPMMax = 12.0f;

        [KSPField]
        public float runningPowerMin = 0.05f;
        #endregion

        #region Effects
        /// <summary>
        /// EFFECTS group played when the generator starts. Falls back to startEffect.
        /// </summary>
        [KSPField]
        public string engageEffectName = string.Empty;

        /// <summary>
        /// EFFECTS group played when the generator stops. Falls back to stopEffect.
        /// </summary>
        [KSPField]
        public string disengageEffectName = string.Empty;

        /// <summary>
        /// EFFECTS group played when the generator loses required resources.
        /// </summary>
        [KSPField]
        public string flameoutEffectName = string.Empty;

        /// <summary>
        /// EFFECTS group driven by generator output.
        /// </summary>
        [KSPField]
        public string powerEffectName = string.Empty;

        /// <summary>
        /// EFFECTS group played continuously while the generator is operating. Falls back to runningEffect.
        /// </summary>
        [KSPField]
        public string runningEffectName = string.Empty;

        /// <summary>
        /// EFFECTS group driven by the forward component of gravitic acceleration.
        /// </summary>
        [KSPField]
        public string fwdThrustEffect = string.Empty;

        /// <summary>
        /// EFFECTS group driven by the reverse component of gravitic acceleration.
        /// </summary>
        [KSPField]
        public string revThrustEffect = string.Empty;

        /// <summary>
        /// EFFECTS group driven by the vertical component of gravitic acceleration.
        /// </summary>
        [KSPField]
        public string vtolThrustEffect = string.Empty;

        /// <summary>
        /// Optional EFFECTS group driven by positive Translate Y input.
        /// </summary>
        [KSPField]
        public string verticalUpEffectName = string.Empty;

        /// <summary>
        /// Optional EFFECTS group driven by negative Translate Y input.
        /// </summary>
        [KSPField]
        public string verticalDownEffectName = string.Empty;

        /// <summary>
        /// Legacy generic horizontal thrust EFFECTS group.
        /// </summary>
        [KSPField]
        protected string thrustEffect = string.Empty;
        #endregion

        #endregion

        #region Housekeeping
        protected float rotationPerFrame = 0;
        protected float rotationPerFrameMin = 0;
        protected float rotationPerFrameMax = 0;
        protected float currentStartStopLerp = 0.0f;
        protected AnimationState animationState;
        protected Transform gravRingTransform = null;
        protected Transform thrustTransform = null;
        protected Transform reverseThrustTransform = null;
        protected Transform vtolThrustTransform = null;
        protected Transform vtolFXTransform = null;
        protected Vector3 gravSpinAxis = Vector3.zero;
        protected Animation animation = null;
        protected bool flamedOut;
        protected Dictionary<string, float> effectLevels = new Dictionary<string, float>();

        Light[] lights;
        #endregion

        #region Overrides
        protected override bool managesEffectsInternally
        {
            get { return true; }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!string.IsNullOrEmpty(groupName))
            {
                Fields["generatorState"].group.name = groupName;
                Fields["generatorState"].group.displayName = groupName;
            }

            setupAnimations();
            getGravRing();
            setupLights();

            if (IsActivated)
            {
                generatorState = WBIGeneratorStates.Running;
                currentStartStopLerp = 1.0f;
            }
            else
            {
                generatorState = WBIGeneratorStates.Shutdown;
                currentStartStopLerp = 0.0f;
                stopContinuousEffects();
            }
        }

        public override void OnUpdate()
        {
            updateFxState();
            base.OnUpdate();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // statusPercent <= 0 means the converter is stuck
            // If we're running or starting, then flameout and set state to stopping
            if ((isMissingResources || statusPercent <= 1E-09) && !flamedOut && (generatorState == WBIGeneratorStates.Running || generatorState == WBIGeneratorStates.Starting))
            {
                flamedOut = true;
                generatorState = WBIGeneratorStates.Flameout;
                playAnimation(true);
                playEffect(flameoutEffectName);
            }

            // If we're flamed out but no longer missing resources & status percent > 0, then un-flameout.
            else if (flamedOut && !isMissingResources && statusPercent > 1E-09)
            {
                flamedOut = false;
                generatorState = WBIGeneratorStates.Starting;
                playAnimation();
                currentStartStopLerp = 0.0f;
                setEffect(flameoutEffectName, 0.0f);
                playEffect(getEngageEffectName());
            }
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
            flamedOut = false;
            generatorState = WBIGeneratorStates.Starting;
            currentStartStopLerp = 0.0f;
            setEffect(flameoutEffectName, 0.0f);
            playEffect(getEngageEffectName());
            playAnimation();
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            generatorState = WBIGeneratorStates.ShuttingDown;
            flamedOut = false;
            currentStartStopLerp = Mathf.Max(currentStartStopLerp, animationThrottle);
            setEffect(flameoutEffectName, 0.0f);
            playEffect(getDisengageEffectName());
            playAnimation(true);
        }

        protected override void ApplyAccelerationVector(Vector3d accelerationVector)
        {
            if (generatorState == WBIGeneratorStates.Running)
            {
                base.ApplyAccelerationVector(accelerationVector);
            }
        }
        #endregion

        #region Helpers
        protected void Log(string mesage)
        {
            if (BlueshiftScenario.debugMode)
                Debug.Log("[WBIFlexGravAnimated] - " + mesage);
        }

        protected void setupLights()
        {
            //Find the lights
            lights = this.part.gameObject.GetComponentsInChildren<Light>();
            Log("THERE! ARE! " + lights.Length + " LIGHTS!");

            //Turn off lights if any
            if (lights != null)
            {
                for (int index = 0; index < lights.Length; index++)
                    lights[index].intensity = IsActivated ? 1.0f : 0.0f;
            }
        }

        protected virtual void getGravRing()
        {
            //Get the gravity ring transform
            gravRingTransform = this.part.FindModelTransform(gravRingTransformName);

            //Get the rotation axis
            if (gravRingTransform != null)
            {
                if (string.IsNullOrEmpty(gravRingSpinAxis) == false)
                {
                    string[] axisValues = gravRingSpinAxis.Split(',');
                    float value;
                    if (axisValues.Length == 3)
                    {
                        if (float.TryParse(axisValues[0], out value))
                            gravSpinAxis.x = value;
                        if (float.TryParse(axisValues[1], out value))
                            gravSpinAxis.y = value;
                        if (float.TryParse(axisValues[2], out value))
                            gravSpinAxis.z = value;
                    }
                }

                //Rotations per frame
                rotationPerFrameMax = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime);
                rotationPerFrameMin = ((spinRateRPMMin * 60.0f) * TimeWarp.fixedDeltaTime);
            }
        }

        protected virtual void setupAnimations()
        {
            Log("SetupAnimations called.");

            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Log("No animations found.");
                return;
            }

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            if (IsActivated)
            {
                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        protected virtual void playAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName) || gravRingTransform == null)
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            if (playInReverse)
            {
                anim[animationName].time = anim[animationName].length;
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }

            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }
        }

        protected virtual void updateFxState()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel == null)
                return;

            float powerLevel = vessel.ctrlState.mainThrottle;
            if (!throttleControlEnabled)
                powerLevel = flexGravManualOutput / 100f;
            powerLevel = Mathf.Clamp01(powerLevel);

            float frameDeltaTime = Time.deltaTime;
            if (frameDeltaTime <= 0)
                frameDeltaTime = TimeWarp.fixedDeltaTime;

            switch (generatorState)
            {
                default:
                case WBIGeneratorStates.Shutdown:
                    if (lights != null && lights.Length > 0)
                    {
                        for (int index = 0; index < lights.Length; index++)
                            lights[index].intensity = 0f;
                    }
                    animationThrottle = 0;
                    stopContinuousEffects();
                    break;

                case WBIGeneratorStates.Running:
                    if (powerLevel < runningPowerMin)
                        powerLevel = runningPowerMin;
                    animationThrottle = powerLevel;
                    updateContinuousEffects(1.0f, powerLevel);

                    //Spin the grav ring
                    if (gravRingTransform != null)
                    {
                        rotationPerFrame = ((spinRateRPMMax * 60.0f) * frameDeltaTime) * powerLevel;
                        if (rotationPerFrame < rotationPerFrameMin)
                            rotationPerFrame = rotationPerFrameMin;
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame);
                    }
                    break;

                case WBIGeneratorStates.Starting:
                    if (powerLevel < runningPowerMin)
                        powerLevel = runningPowerMin;

                    currentStartStopLerp = moveEffectLevel(currentStartStopLerp, 1.0f, startupTime, frameDeltaTime);
                    animationThrottle = powerLevel * currentStartStopLerp;
                    updateContinuousEffects(currentStartStopLerp, powerLevel);

                    if (gravRingTransform != null)
                    {
                        rotationPerFrame = ((spinRateRPMMax * 60.0f) * frameDeltaTime) * powerLevel;
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame * currentStartStopLerp);
                    }

                    if (currentStartStopLerp >= 0.99f)
                    {
                        generatorState = WBIGeneratorStates.Running;
                        currentStartStopLerp = 1.0f;
                    }

                    if (lights != null && lights.Length > 0)
                    {
                        for (int index = 0; index < lights.Length; index++)
                            lights[index].intensity = currentStartStopLerp;
                    }
                    break;

                case WBIGeneratorStates.ShuttingDown:
                case WBIGeneratorStates.Flameout:
                    currentStartStopLerp = moveEffectLevel(currentStartStopLerp, 0.0f, shutdownTime, frameDeltaTime);
                    animationThrottle = powerLevel * currentStartStopLerp;
                    updateContinuousEffects(currentStartStopLerp, powerLevel);

                    if (gravRingTransform != null)
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame * currentStartStopLerp);

                    if (currentStartStopLerp <= 0.01f)
                    {
                        generatorState = WBIGeneratorStates.Shutdown;
                        currentStartStopLerp = 0f;
                        animationThrottle = 0f;
                        stopContinuousEffects();
                    }

                    if (lights != null && lights.Length > 0)
                    {
                        for (int index = 0; index < lights.Length; index++)
                            lights[index].intensity = currentStartStopLerp;
                    }
                    break;
            }
        }

        /// <summary>
        /// Drives all continuous stock EFFECTS groups from the generator's current state.
        /// </summary>
        protected virtual void updateContinuousEffects(float spoolLevel, float powerLevel)
        {
            float effectPower = Mathf.Clamp01(spoolLevel * powerLevel);
            float verticalFactor = Mathf.Clamp01(verticalLiftAngle / 90.0f);
            float horizontalFactor = 1.0f - verticalFactor;
            float verticalInput = Mathf.Clamp(translateUpDn, -1.0f, 1.0f);

            setEffect(getRunningEffectName(), Mathf.Clamp01(spoolLevel));
            setEffect(powerEffectName, effectPower);
            setEffect(thrustEffect, effectPower * horizontalFactor);
            setEffectIfDistinct(fwdThrustEffect, thrustEffect, useForwardVector ? effectPower * horizontalFactor : 0.0f);
            setEffectIfDistinct(revThrustEffect, thrustEffect, useForwardVector ? 0.0f : effectPower * horizontalFactor);
            setEffect(vtolThrustEffect, effectPower * verticalFactor);
            setEffect(verticalUpEffectName, verticalInput > 0 ? effectPower * verticalInput : 0.0f);
            setEffect(verticalDownEffectName, verticalInput < 0 ? effectPower * -verticalInput : 0.0f);
        }

        /// <summary>
        /// Turns off effects that can be looped or continuously emitted.
        /// </summary>
        protected virtual void stopContinuousEffects()
        {
            setEffect(getRunningEffectName(), 0.0f);
            setEffect(powerEffectName, 0.0f);
            setEffect(thrustEffect, 0.0f);
            setEffectIfDistinct(fwdThrustEffect, thrustEffect, 0.0f);
            setEffectIfDistinct(revThrustEffect, thrustEffect, 0.0f);
            setEffect(vtolThrustEffect, 0.0f);
            setEffect(verticalUpEffectName, 0.0f);
            setEffect(verticalDownEffectName, 0.0f);
        }

        protected virtual float moveEffectLevel(float currentLevel, float targetLevel, float duration, float deltaTime)
        {
            if (duration <= 0)
                return targetLevel;

            return Mathf.MoveTowards(currentLevel, targetLevel, deltaTime / duration);
        }

        protected virtual string getEngageEffectName()
        {
            return string.IsNullOrEmpty(engageEffectName) ? startEffect : engageEffectName;
        }

        protected virtual string getDisengageEffectName()
        {
            return string.IsNullOrEmpty(disengageEffectName) ? stopEffect : disengageEffectName;
        }

        protected virtual string getRunningEffectName()
        {
            return string.IsNullOrEmpty(runningEffectName) ? runningEffect : runningEffectName;
        }

        protected virtual void setEffect(string effectName, float powerLevel)
        {
            if (string.IsNullOrEmpty(effectName))
                return;

            powerLevel = Mathf.Clamp01(powerLevel);

            float previousLevel;
            if (effectLevels.TryGetValue(effectName, out previousLevel) && Mathf.Approximately(previousLevel, powerLevel))
                return;

            effectLevels[effectName] = powerLevel;
            part.Effect(effectName, powerLevel);
        }

        protected virtual void setEffectIfDistinct(string effectName, string otherEffectName, float powerLevel)
        {
            if (effectName != otherEffectName)
                setEffect(effectName, powerLevel);
        }

        /// <summary>
        /// Plays a transition effect. Transition groups are intentionally not cached so one-shot audio can replay.
        /// </summary>
        protected virtual void playEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
                return;

            part.Effect(effectName, 1.0f);
            effectLevels.Remove(effectName);
        }
        #endregion
    }
}
