using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift
{
    internal enum AnimationSyncStates
    {
        manual,
        warpEngine,
        converter
    }

    public class WBIModuleAnimation: WBIPartModule
    {
        #region Constants
        protected const int kDefaultAnimationLayer = 2;
        #endregion

        #region Fields
        [KSPField]
        public bool debugMode = false;

        [KSPField()]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField]
        public float spoolTime = 0.02f;

        [KSPField]
        public float minEngineSpool = 0.5f;

        [KSPField()]
        public string startEventGUIName;

        [KSPField()]
        public string endEventGUIName;

        [KSPField()]
        public string actionGUIName;

        [KSPField]
        public string playDirectionForwardGUIName;

        [KSPField]
        public string playDirectionReverseGUIName;

        [KSPField(isPersistant = true)]
        public bool guiIsVisible = true;

        [KSPField]
        public KSPActionGroup defaultActionGroup;

        [KSPField]
        public bool playAnimationLooped = false;

        [KSPField]
        public float minAnimationSlowdownSpeed = 0f;

        [KSPField]
        public bool canSyncWithConverter = false;

        [KSPField]
        public bool canSyncWithWarpEngine = false;

        [KSPField]
        public string runningEffect = string.Empty;

        /// <summary>
        /// Name of the Waterfall effects controller that controls the warp effects (if any).
        /// </summary>
        [KSPField]
        public string waterfallEffectController = string.Empty;
        #endregion

        #region Housekeeping
        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField(isPersistant = true)]
        bool isMoving = false;

        bool isLooping = false;
        Animation animation = null;
        AnimationState animationState;
        List<Material> materials = null;
        float currentSpoolTime = 0f;
        List<BaseConverter> converters = null;
        Light[] lights = null;
        bool harvesterWasActivated = false;
        AnimationSyncStates syncState = AnimationSyncStates.manual;
        float engineThrottle = 0f;
        bool playInReverse = false;
        WFModuleWaterfallFX waterfallFXModule = null;
        SDModuleSpaceDustHarvester harvester = null;
        #endregion

        #region Events
        [KSPEvent(guiName = "ToggleAnimation")]
        public virtual void ToggleAnimation()
        {
            //Play animation for current state, but skip if we are currently looping the animation.
            //This will ensure that when we stop looping the animation, its cycle will complete without playing in reverse.
            if (!isLooping)
                playAnimation(playInReverse);

            //Toggle state
            isDeployed = !isDeployed;

            // Update sync state
            syncState = AnimationSyncStates.manual;
            updateSyncState();

            // Update UI
            updateAnimationUI();
        }

        [KSPEvent(guiActiveEditor = true)]
        public virtual void ToggleAnimationDirection()
        {
            playInReverse = !playInReverse;
            updateAnimationUI();
        }
        #endregion

        #region Actions
        [KSPAction("ToggleAnimation")]
        public virtual void ToggleAnimationAction(KSPActionParam param)
        {
            ToggleAnimation();
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (syncState == AnimationSyncStates.manual)
            {
                updateAnimation();
            }
            else if ((syncState == AnimationSyncStates.converter) || (syncState == AnimationSyncStates.warpEngine && engineThrottle <= 0 && canSyncWithConverter))
            {
                updateConverterState();
                updateAnimation();
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            setupAnimations();
            findEmissiveRenderers();
            converters = part.FindModulesImplementing<BaseConverter>();
            lights = this.part.gameObject.GetComponentsInChildren<Light>();
            waterfallFXModule = WFModuleWaterfallFX.GetWaterfallModule(part);
            harvester = SDModuleSpaceDustHarvester.GetHarvesterModule(part);

            WBIWarpEngine.onWarpEffectsUpdated.Add(onWarpEffectsUpdated);
            WBIWarpEngine.onWarpEngineStart.Add(onWarpEngineStart);
            WBIWarpEngine.onWarpEngineShutdown.Add(onWarpEngineShutdown);
            WBIWarpEngine.onWarpEngineFlameout.Add(onWarpEngineFlameout);
            WBIWarpEngine.onWarpEngineUnFlameout.Add(onWarpEngineUnFlameout);

            debugMode = BlueshiftScenario.debugMode;
            showGui(guiIsVisible || debugMode);
            Events["ToggleAnimationDirection"].active = animation != null;
            Events["ToggleAnimationDirection"].guiActive = debugMode || guiIsVisible;
            Actions["ToggleAnimationAction"].guiName = actionGUIName;
            if (defaultActionGroup > 0)
                Actions["ToggleAnimationAction"].actionGroup = defaultActionGroup;

            updateAnimationUI();
            if (isDeployed)
            {
                playAnimation(playInReverse);
            }
        }

        public void Destory()
        {
            WBIWarpEngine.onWarpEffectsUpdated.Remove(onWarpEffectsUpdated);
            WBIWarpEngine.onWarpEngineStart.Remove(onWarpEngineStart);
            WBIWarpEngine.onWarpEngineShutdown.Remove(onWarpEngineShutdown);
            WBIWarpEngine.onWarpEngineFlameout.Remove(onWarpEngineFlameout);
            WBIWarpEngine.onWarpEngineUnFlameout.Remove(onWarpEngineUnFlameout);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasValue("playInReverse"))
                bool.TryParse(node.GetValue("playInReverse"), out playInReverse);
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            node.AddValue("playInReverse", playInReverse);
        }
        #endregion

        #region Helpers
        void onWarpEffectsUpdated(Vessel warpShip, WBIWarpEngine warpEngine, float throttle)
        {
            if (!canSyncWithWarpEngine || part.vessel != warpShip)
                return;

            syncState = AnimationSyncStates.warpEngine;
            engineThrottle = throttle;

            if ((!isDeployed && engineThrottle > 0) || (isDeployed && engineThrottle <= 0 && (!canSyncWithConverter || (canSyncWithConverter && !harvesterWasActivated))))
                ToggleAnimation();

            updateAnimation();
        }

        void onWarpEngineStart(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (!canSyncWithWarpEngine || part.vessel != warpShip)
                return;
            syncState = AnimationSyncStates.warpEngine;
        }

        void onWarpEngineShutdown(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (!canSyncWithWarpEngine || part.vessel != warpShip)
                return;
            syncState = AnimationSyncStates.manual;
            updateSyncState();
        }

        void onWarpEngineFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (!canSyncWithWarpEngine || part.vessel != warpShip)
                return;
            syncState = AnimationSyncStates.manual;
            updateSyncState();
        }

        void onWarpEngineUnFlameout(Vessel warpShip, WBIWarpEngine warpEngine)
        {
            if (!canSyncWithWarpEngine || part.vessel != warpShip)
                return;
            syncState = AnimationSyncStates.warpEngine;
        }

        void showGui(bool isVisible)
        {
            guiIsVisible = isVisible;
            Events["ToggleAnimation"].guiActive = isVisible || debugMode;
        }

        void setupAnimations()
        {
            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Debug.Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Debug.Log("No animations found.");
                return;
            }

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            if (isDeployed)
            {
                Events["ToggleAnimation"].guiName = endEventGUIName;

                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;

                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        void playAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;
            if (animation == null)
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;

            if (HighLogic.LoadedSceneIsFlight)
                animation[animationName].speed = animationSpeed;
            else
                animation[animationName].speed = animationSpeed * 1000;

            if (playInReverse)
                animation[animationName].time = animation[animationName].length;

            animation.Play(animationName);

            isMoving = true;
        }

        void findEmissiveRenderers()
        {
            materials = new List<Material>();

            ConfigNode node = getPartConfigNode();
            if (node == null || !node.HasValue("emissiveTransform"))
                return;
            string[] transformNames = node.GetValues("emissiveTransform");

            Transform target;
            Renderer renderer;
            for (int index = 0; index < transformNames.Length; index++)
            {
                target = part.FindModelTransform(transformNames[index]);
                if (target == null)
                    continue;

                renderer = target.GetComponent<Renderer>();
                if (renderer == null)
                    continue;

                materials.Add(renderer.material);
            }
        }

        void updateSyncState()
        {
            if (!isDeployed && canSyncWithWarpEngine)
            {
                syncState = AnimationSyncStates.warpEngine;
            }
            else if (!isDeployed && canSyncWithConverter)
            {
                syncState = AnimationSyncStates.converter;
            }
        }

        void updateAnimation()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            updateCurrentSpoolTime();
            updateEmissives();
            updateAnimationSpeed();

            if (isLooping && !isDeployed && currentSpoolTime <= minAnimationSlowdownSpeed)
                isLooping = false;

            if (animation != null)
            {
                if (!animation.isPlaying && isMoving)
                {
                    if (!playAnimationLooped || !isLooping)
                    {
                        isMoving = false;
                    }
                    else if (isLooping)
                    {
                        isMoving = true;
                        playAnimation(playInReverse);
                    }
                }
            }
        }

        void updateCurrentSpoolTime()
        {
            if (syncState == AnimationSyncStates.warpEngine && engineThrottle > 0)
            {
                // if engineThrottle > 0 but < minSpool then set engineThrottle to minSpool.
                if (engineThrottle > 0 && engineThrottle < minEngineSpool)
                    engineThrottle = minEngineSpool;

                // Lerp up
                if (currentSpoolTime < engineThrottle)
                {
                    currentSpoolTime = Mathf.Lerp(currentSpoolTime, engineThrottle, TimeWarp.fixedDeltaTime / spoolTime);
                    if ((currentSpoolTime / engineThrottle) > 0.995f)
                        currentSpoolTime = engineThrottle;

                }

                // Lerp down
                else
                {
                    currentSpoolTime = Mathf.Lerp(currentSpoolTime, engineThrottle, TimeWarp.fixedDeltaTime / spoolTime);
                    if ((currentSpoolTime / engineThrottle) <= 0.002f)
                        currentSpoolTime = engineThrottle;
                }
            }

            // Lerp up
            else if (isDeployed)
            {
                currentSpoolTime = Mathf.Lerp(currentSpoolTime, 1.0f, TimeWarp.fixedDeltaTime / spoolTime);
                if (currentSpoolTime > 0.995f)
                    currentSpoolTime = 1.0f;
            }

            // Lerp down
            else
            {
                currentSpoolTime = Mathf.Lerp(currentSpoolTime, 0f, TimeWarp.fixedDeltaTime / spoolTime);
                if (currentSpoolTime <= 0.002f)
                    currentSpoolTime = 0f;
            }
        }

        void updateAnimationSpeed()
        {
            if (string.IsNullOrEmpty(animationName) || animation == null || !animation.isPlaying || !isMoving)
                return;

            float animationSpeed = (playInReverse == false ? 1.0f : -1.0f) * currentSpoolTime;
            if (!isDeployed && currentSpoolTime <= minAnimationSlowdownSpeed)
                animationSpeed = minAnimationSlowdownSpeed;
            animation[animationName].speed = animationSpeed;
        }

        void updateEmissives()
        {
            int count = materials.Count;
            if (count <= 0)
                return;

            Color color = new Color(1, 1, 1, currentSpoolTime);
            for (int index = 0; index < count; index++)
            {
                materials[index].SetColor("_EmissiveColor", color);
            }

            for (int index = 0; index < lights.Length; index++)
                lights[index].intensity = currentSpoolTime;

            // Play effect
            part.Effect(runningEffect, currentSpoolTime);

            // Update Waterfall
            if (waterfallFXModule != null && !string.IsNullOrEmpty(waterfallEffectController))
            {
                waterfallFXModule.SetControllerValue(waterfallEffectController, currentSpoolTime);
            }
        }

        void updateConverterState()
        {
            if (!canSyncWithConverter)
                return;
            bool harvesterIsActivated = false;

            // Check for SpaceDust harvester
            if (harvester != null)
            {
                harvesterIsActivated = harvester.Enabled;
            }

            // Check for stock converter
            else if (converters != null)
            {
                int count = converters.Count;
                if (count == 0)
                    return;

                for (int index = 0; index < count; index++)
                {
                    if (converters[index].IsActivated)
                    {
                        harvesterIsActivated = true;
                        break;
                    }
                    else if (!converters[index].Events["StartResourceConverter"].active)
                    {
                        converters[index].Events["StartResourceConverter"].active = true;
                    }
                }
            }

            if (harvesterIsActivated != harvesterWasActivated)
            {
                harvesterWasActivated = harvesterIsActivated;
                isDeployed = harvesterIsActivated;
                if (!isLooping)
                    playAnimation(playInReverse);
                updateAnimationUI();
            }
        }

        void updateAnimationUI()
        {
            if (isDeployed)
            {
                if (playAnimationLooped)
                    isLooping = true;
                Events["ToggleAnimation"].guiName = endEventGUIName;
            }
            else
            {
                Events["ToggleAnimation"].guiName = startEventGUIName;
            }

            Events["ToggleAnimationDirection"].guiName = playInReverse ? playDirectionForwardGUIName : playDirectionReverseGUIName;

            MonoUtilities.RefreshContextWindows(this.part);
        }
        #endregion
    }
}
