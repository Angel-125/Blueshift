using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

/*
Source code copyright 2020, by Michael Billard (Angel-125)
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
    /// Wild Blue Warp Engine that's designed for faster than light travel.
    /// </summary>
    [KSPModule("Warp Engine")]
    public class WBIWarpEngine: ModuleEnginesFX
    {
        #region constants
        protected float kLightSpeed = 299792458;
        protected float kMessageDuration = 3f;
        protected string kNeedsSpaceflight = "Needs to be in space";
        protected string kNeedsAltitude = "Needs higher altitude";
        protected string kNeedsWarpCapacity = "Needs more warp capacity";
        protected string kFlameOutGeneric = "Something went wrong";
        protected string kWarpReady = "Ready";
        protected string kTerrainWarning = "Warp halted to avoid collision with celestial body. Reduce speed to approach minimum warp altitude.";
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
        /// Minimum planetary radius needed to go to warp.
        /// </summary>
        [KSPField]
        public double minPlanetaryRadius = 1.0;

        /// <summary>
        /// Minimum altitude at which the engine can go to warp.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Minimum Warp altitude", guiUnits = "m", guiFormat = "n1")]
        public double minWarpAltitudeDisplay = 500000.0f;

        /// <summary>
        /// The velocity of the ship, adjusted for throttle setting and thrust limiter.
        /// </summary>
        [KSPField(guiActive = true, guiName = "Warp Speed", guiFormat = "n3", guiUnits = "C")]
        public float warpSpeed = 0;

        /// <summary>
        /// Limits top speed while in a planetary or munar SOI so we don't zoom past the celestial body.
        /// Out in interplanetary space we don't have a speed limit.
        /// </summary>
        [KSPField]
        public FloatCurve planetarySOISpeedCurve;

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
        /// The first number represents multiples of C, and the second number represents the required warpCapacity.
        /// You can apply any kind of warp curve you want, but the baseline uses the Fibonacci sequence * 10.
        /// It may seem steep, but in KSP's small scale, 1C is plenty fast.
        /// This curve is modified by the engine's displacementImpulse and current vessel mass.
        /// effectiveWarpCapacity = warpCapacity * (displacementImpulse / vessel mass)
        /// </summary>
        [KSPField]
        public FloatCurve warpCurve;

        [KSPField]
        public string textureModuleID = string.Empty;
        #endregion

        #region Housekeeping
        /// <summary>
        /// Flag to indicate that we're in space (orbiting, suborbital, or escaping)
        /// </summary>
        [KSPField]
        public bool isInSpace = false;

        /// <summary>
        /// Flag to indicate that the ship meets minimum warp altitude.
        /// </summary>
        [KSPField]
        public bool meetsWarpAltitude = false;

        /// <summary>
        /// Flag to indicate that the ship has sufficient warp capacity.
        /// </summary>
        [KSPField]
        public bool hasWarpCapacity = false;

        /// <summary>
        /// Name of optional bow shock transform.
        /// </summary>
        [KSPField]
        public string bowShockTransformName = string.Empty;

        // Only one active engine should apply the translation effects
        [KSPField]
        public bool applyWarpTranslation = true;
        [KSPField]
        public float averageDisplacementImpulse = 0;
        [KSPField]
        public float totalWarpCapacity = 0;
        [KSPField]
        public float effectiveWarpCapacity = 0;
        [KSPField(guiName = "Max Warp Speed", guiFormat = "n3", guiUnits = "C")]
        public float maxWarpSpeed = 0;
        [KSPField(guiName = "Distance per update", guiFormat = "n2", guiUnits = "m")]
        float warpDistance = 0;

        // Hit test stuff to make sure we don't run into planets.
        protected RaycastHit terrainHit;
        protected LayerMask layerMask = -1;

        protected List<WBIWarpEngine> warpEngines = null;
        protected List<WBIWarpCoil> warpCoils = null;
        protected List<WBIAnimatedTexture> warpEngineTextures = null;
        protected CelestialBody previousBody = null;
        protected Bounds bodyBounds;

        protected Transform bowShockTransform = null;

        // Due to the way engines work on FixedUpdate, the engine can determine that it is NOT flamed out if it meets its propellant requirements. Therefore, we keep track of our own flameout conditions.
        protected bool warpFlameout = false;
        #endregion

        #region Actions
        #endregion

        #region Overrides
        public void OnDestroy()
        {
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!EngineIgnited)
            {
                return;
            }
            if (!shouldApplyWarp())
            {
                return;
            }

            // Drive warp coil resource consumption and get total available warp capacity.
            getTotalWarpCapacity();

            // Get our precondition states
            isInSpace = IsInSpace();
            meetsWarpAltitude = MeetsWarpAltitude();
            hasWarpCapacity = HasWarpCapacity();

            // Now check for flameout
            if (IsFlamedOut())
            {
                warpDistance = 0;
                maxWarpSpeed = 0;
                warpSpeed = 0;
                effectiveWarpCapacity = 0;
                FlightInputHandler.state.mainThrottle = 0;
                return;
            }

            // Calculate effective warp capacity, and find the best warp curve to get maximum FTL speed.
            effectiveWarpCapacity = totalWarpCapacity * (averageDisplacementImpulse / this.part.vessel.GetTotalMass());
            calculateBestWarpSpeed();

            // Account for throttle setting and thrust limiter.
            float throttleSetting = FlightInputHandler.state.mainThrottle * (thrustPercentage / 100.0f);
            if (throttleSetting <= 0f)
                return;

            // Calculate offset position
            warpDistance = kLightSpeed * maxWarpSpeed * throttleSetting * TimeWarp.fixedDeltaTime;
            Transform refTransform = this.part.vessel.transform;
            Vector3 warpVector = refTransform.up * warpDistance;
            Vector3d offsetPosition = refTransform.position + warpVector;
            warpSpeed = maxWarpSpeed * throttleSetting;

            // Make sure that we won't run into a celestial body.
            if (previousBody != this.part.orbit.referenceBody)
            {
                previousBody = this.part.orbit.referenceBody;
                bodyBounds = previousBody.getBounds();
            }
            if (bodyBounds.Contains(offsetPosition))
            {
                ScreenMessages.PostScreenMessage(kTerrainWarning, 3.0f, ScreenMessageStyle.UPPER_CENTER);
                FlightInputHandler.state.mainThrottle = 0;
                return;
            }

            // Apply translation.
            FloatingOrigin.SetOutOfFrameOffset(offsetPosition);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!isOperational && !EngineIgnited)
            {
                return;
            }

            UpdateWarpStatus();
        }

        public override string GetInfo()
        {
            return base.GetInfo();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            loadCurve(warpCurve, "warpCurve");
            loadCurve(planetarySOISpeedCurve, "planetarySOISpeedCurve");
            warpEngines = new List<WBIWarpEngine>();
            warpCoils = new List<WBIWarpCoil>();
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
            Fields["averageDisplacementImpulse"].guiActive = debugEnabled;
            Fields["totalWarpCapacity"].guiActive = debugEnabled;
            Fields["minPlanetaryRadius"].guiActive = debugEnabled;
            Fields["effectiveWarpCapacity"].guiActive = debugEnabled;
            Fields["maxWarpSpeed"].guiActive = debugEnabled;
            Fields["warpDistance"].guiActive = debugEnabled;
        }

        public override void Flameout(string message, bool statusOnly = false, bool showFX = true)
        {
            base.Flameout(message, statusOnly, showFX);
            warpFlameout = true;
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

            // Warp effects
            if (bowShockTransform != null)
            {
                bowShockTransform.position = this.part.vessel.transform.position;
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
            }
        }
        #endregion

        #region Helpers
        public Vector3 getVesselDimensions()
        {
            Bounds bounds; 
            List<Bounds> boundsList = new List<Bounds>();
            Bounds[] renderBounds;
            int count = this.part.vessel.parts.Count;
            Part firstPart = this.part.vessel.parts[0];
            Part vesselPart;

            for (int index = 0; index < count; index++)
            {
                vesselPart = this.part.vessel.parts[index];
                renderBounds = PartGeometryUtil.GetPartRendererBounds(vesselPart);
                for (int boundsIndex = 0; boundsIndex < renderBounds.Length; boundsIndex++)
                {
                    bounds = renderBounds[boundsIndex];
                    bounds.size *= vesselPart.boundsMultiplier;
                    bounds.Expand(vesselPart.GetModuleSize(bounds.size, ModifierStagingSituation.CURRENT));
                    boundsList.Add(bounds);
                }
            }
            return PartGeometryUtil.MergeBounds(boundsList.ToArray(), firstPart.transform.root).size;
        }

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

        protected void calculateBestWarpSpeed()
        {
            int count = warpEngines.Count;

            float warpSpeed = 0;
            float bestWarpSpeed = 0;
            for (int index = 0; index < count; index++)
            {
                warpSpeed = warpEngines[index].warpCurve.Evaluate(effectiveWarpCapacity);
                if (warpSpeed > bestWarpSpeed)
                    bestWarpSpeed = warpSpeed;
            }
            maxWarpSpeed = bestWarpSpeed;

            // Limit speed if we're in a planetary SOI
            // based on this.part.vessel.mainBody.sphereOfInfluence?
            if (this.part.vessel.mainBody.scaledBody.GetComponentsInChildren<SunShaderController>(true).Length == 0)
            {
                float speedRatio = (float)(this.part.vessel.altitude / this.part.vessel.mainBody.sphereOfInfluence);
                maxWarpSpeed *= planetarySOISpeedCurve.Evaluate(speedRatio);
            }
        }

        protected void getTotalWarpCapacity()
        {
            List<WBIWarpCoil> coils = this.part.vessel.FindPartModulesImplementing<WBIWarpCoil>();
            int count = coils.Count;
            WBIWarpCoil warpCoil;

            totalWarpCapacity = 0;
            warpCoils.Clear();
            for (int index = 0; index < count; index++)
            {
                warpCoil = coils[index];
                if (warpCoil.isActivated && consumeCoilResources(warpCoil))
                {
                    totalWarpCapacity += warpCoil.warpCapacity;
                    warpCoils.Add(warpCoil);
                }
            }
        }

        protected bool consumeCoilResources(WBIWarpCoil warpCoil)
        {
            string errorStatus = string.Empty;
            int count = warpCoil.resHandler.inputResources.Count;

            if (!warpCoil.isActivated)
                return false;
            if (count == 0)
                return true;

            warpCoil.resHandler.UpdateModuleResourceInputs(ref errorStatus, 1.0, 0.1, true, true);
            for (int index = 0; index < count; index++)
            {
                if (!warpCoil.resHandler.inputResources[index].available)
                    return false;
            }

            return true;
        }

        protected bool shouldApplyWarp()
        {
            // Only one warp engine should handle warp speed. The rest just provide boost effects.
            List<WBIWarpEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIWarpEngine>();
            WBIWarpEngine engine, prevEngine;
            int count = engines.Count;
            float totalDisplacementImpulse = 0;

            warpEngines.Clear();
            averageDisplacementImpulse = 0;
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

            averageDisplacementImpulse = totalDisplacementImpulse / count;
            return applyWarpTranslation;
        }

        protected void loadCurve(FloatCurve curve, string curveNodeName)
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
            if (!engineNode.HasNode(curveNodeName))
                return;

            node = engineNode.GetNode(curveNodeName);
            curve.Load(node);
        }
        #endregion
    }
}
