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
        [KSPField]
        public string powerEffectName;
        [KSPField]
        public string runningEffectName;
        [KSPField]
        public string vtolThrustEffect = string.Empty;
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

        Light[] lights;
        #endregion

        #region Overrides
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
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            updateFxState();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            // statusPercent <= 0 means the converter is stuck
            // If we're running or starting, then flameout and set state to stopping
            if ((isMissingResources || statusPercent <= 1E-09) && !flamedOut && (generatorState == WBIGeneratorStates.Running || generatorState == WBIGeneratorStates.Starting))
            {
                flamedOut = true;
                generatorState = WBIGeneratorStates.ShuttingDown;
                playAnimation(true);

                if (!string.IsNullOrEmpty(runningEffect))
                    this.part.Effect(runningEffect, 0.0f);
                if (!string.IsNullOrEmpty(stopEffect))
                    this.part.Effect(stopEffect, 1.0f);
            }

            // If we're flamed out but no longer missing resources & status percent > 0, then un-flameout.
            else if (flamedOut && !isMissingResources && statusPercent > 1E-09)
            {
                flamedOut = false;
                generatorState = WBIGeneratorStates.Starting;
                playAnimation();

                if (!string.IsNullOrEmpty(startEffect))
                    this.part.Effect(startEffect, 1.0f);
                if (!string.IsNullOrEmpty(runningEffect))
                    this.part.Effect(runningEffect, 1.0f);
            }
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
            generatorState = WBIGeneratorStates.Starting;
            playAnimation();
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            generatorState = WBIGeneratorStates.ShuttingDown;
            flamedOut = false;
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
            float powerLevel = vessel.ctrlState.mainThrottle;
            if (!throttleControlEnabled)
                powerLevel = flexGravManualOutput / 100f;

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
                    break;

                case WBIGeneratorStates.Running:
                    if (powerLevel < 0.1f)
                        powerLevel = 0.1f;
                    animationThrottle = powerLevel;

                    //Spin the grav ring
                    if (gravRingTransform != null)
                    {
                        rotationPerFrame = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime) * powerLevel;
                        if (rotationPerFrame < rotationPerFrameMin)
                            rotationPerFrame = rotationPerFrameMin;
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame);
                    }
                    break;

                case WBIGeneratorStates.Starting:
                    if (powerLevel < 0.1f)
                        powerLevel = 0.1f;
                    animationThrottle = powerLevel;

                    currentStartStopLerp = Mathf.Lerp(currentStartStopLerp, 1.0f, TimeWarp.fixedDeltaTime / startupTime);
                    this.part.Effect(runningEffect, currentStartStopLerp);

                    if (gravRingTransform != null)
                    {
                        rotationPerFrame = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime) * powerLevel;
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
                    currentStartStopLerp = Mathf.Lerp(currentStartStopLerp, 0.0f, TimeWarp.fixedDeltaTime / shutdownTime);
                    animationThrottle = currentStartStopLerp;
                    this.part.Effect(runningEffect, currentStartStopLerp);

                    if (gravRingTransform != null)
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame * currentStartStopLerp);

                    if (currentStartStopLerp <= 0.01f)
                    {
                        generatorState = WBIGeneratorStates.Shutdown;
                        currentStartStopLerp = 0f;
                        animationThrottle = 0f;
                    }

                    if (lights != null && lights.Length > 0)
                    {
                        for (int index = 0; index < lights.Length; index++)
                            lights[index].intensity = currentStartStopLerp;
                    }
                    break;
            }
        }
        #endregion
    }
}
