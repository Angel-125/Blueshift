            
The Warp Engine is designed to propel a vessel faster than light. It requires WarpCapacity That is produced by WBIWarpCoil part modules. ``` MODULE { name = WBIWarpEngine ...Standard engine parameters here... moduleDescription = Enables fater than light travel. bowShockTransformName = bowShock minPlanetaryRadius = 3.0 displacementImpulse = 100 planetarySOISpeedCurve { key = 1 0.1 ... key = 0.1 0.005 } warpCurve { key = 1 0 key = 10 1 ... key = 1440 10 } waterfallEffectController = warpEffectController waterfallWarpEffectsCurve { key = 0 0 ... key = 1.5 1 } textureModuleID = WarpCore } ```
        
## Fields

### onWarpEffectsUpdated
Game event signifying when warp engine effects have been updated.
### onWarpEngineStart
Game event signifying when the warp engine starts.
### onWarpEngineShutdown
Game event signifying when the warp engine shuts down.
### onWarpEngineFlameout
Game event signifying when the warp engine flames out.
### onWarpEngineUnFlameout
Game event signifying when the warp engine un-flames out.
### moduleDescription
Short description of the module as displayed in the editor.
### minPlanetaryRadius
Minimum planetary radius needed to go to warp. This is used to calculate the user-friendly minimum warp altitude display.
### minWarpAltitudeDisplay
Minimum altitude at which the engine can go to warp. The engine will flame-out unless this altitude requirement is met.
### warpSpeedDisplay
The FTL display velocity of the ship, measured in C, that is adjusted for throttle setting and thrust limiter.
### maxWarpSpeedDisplay
(Debug visible) Maximum possible warp speed.
### preflightCheck
Pre-flight status check.
### spatialLocation
Where we are in space.
### vesselCourse
The vessel's course- which is really just the selected target.
### targetDistance
Distance to the vessel's target
### planetarySOISpeedCurve
Limits top speed while in a planetary or munar SOI so we don't zoom past the celestial body. Out in interplanetary space we don't have a speed limit. The first number represents how close to the SOI edge the vessel is (1 = right at the edge, 0.1 = 10% of the distance to the SOI edge) The second number is the top speed multiplier.
### interstellarAccelerationCurve
Whenever you cross into interstellar space, or are already in interstellar space and throttled down, then apply this acceleration curve. The warp speed will be max warp speed * curve's speed modifier. The first number represents the time since crossing the boundary/throttling up, and the second number is the multiplier. We don't apply this curve when going from interstellar to interplanetary space.
### warpCurve
In addition to any specified PROPELLANT resources, warp engines require warpCapacity. Only parts with a WBIWarpCoil part module can generate warpCapacity. The warp curve controls how much warpCapacity is neeeded to go light speed or faster. The first number represents the available warpCapacity, while the second number gives multiples of C. You can apply any kind of warp curve you want, but the baseline uses the Fibonacci sequence * 10. It may seem steep, but in KSP's small scale, 1C is plenty fast. This curve is modified by the engine's displacementImpulse and current vessel mass. effectiveWarpCapacity = warpCapacity * (displacementImpulse / vessel mass)
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### waterfallWarpEffectsCurve
Waterfall Warp Effects Curve. This is used to control the Waterfall warp field effects based on the vessel's current warp speed. The first number represents multiples of C, and the second number represents the level at which to drive the warp effects. The effects value ranges from 0 to 1, while there's no upper limit to multiples of C, so keep that in mind. The default curve is: key = 0 0 key = 1 0.5 key = 1.5 1
### textureModuleID
The name of the WBIAnimatedTexture to drive as part of the warp effects.
### warpPowerGeneratorID
Engines can drive WBIModuleGeneratorFX that produce resources needed for warp travel if their moduleID matches this value.
### photonicBoomEffectName
Optional effect to play when the vessel exceeds the speed of light.
### warpSimulationResource
Used when calculating the max warp speed in the editor, this is the resource that is common between the warp engine, gravitic generator, and warp coil. This resource should be the limiting resource in the trio (the one that runs out the fastest).
### powerMultiplier
The ratio between the amount of power produced for the warp coils to the amount of power consumed by the warp coils.
### displacementMultiplier
The ratio between the total mass displaced by the warp coils to the vessel's total mass.
### warpIgnitionThreshold
When the powerMultiplier drops below this value, the engine will flame out.
### planetarySpeedBrakeEnabled
Planetary Speed Brake
### interstellarResourceConsumptionModifier
Consumption modifier to apply to resource consumption rates when warping in interstellar space. This is a percentage value between 0 and 99.999. Anything outside this range will be ignored. Default is 10%, which reduces resource consumption by 10% while in interstellar space.
### warpEngineerSkill
The skill required to improve warp speed. Default is "ConverterSkill" (Engineers have this)
### warpSpeedBoostRank
The skill rank required to improve warp speed.
### warpSpeedSkillMultiplier
Per skill rank, the multiplier to multiply warp speed by.
### isInSpace
(Debug visible) Flag to indicate that we're in space (orbiting, suborbital, or escaping)
### meetsWarpAltitude
(Debug visible) Flag to indicate that the ship meets minimum warp altitude.
### hasWarpCapacity
(Debug visible) Flag to indicate that the ship has sufficient warp capacity.
### bowShockTransformName
Name of optional bow shock transform.
### applyWarpTranslation
(Debug visible) Flag to indicate that the engine should apply translation effects. Multiple engines can work together as long as each one's minimum requirements are met.
### totalDisplacementImpulse
(Debug visible) Total displacement impulse calculated from all active warp engines.
### totalWarpCapacity
(Debug visible) Total warp capacity calculated from all active warp engines.
### effectiveWarpCapacity
(Debug visible) Effective warp capacity after accounting for vessel mass
### warpDistance
(Debug visible) Distance per physics update that the vessel will move.
### waterfallEffectsLevel
(Debug visible) Current throttle level for the warp effects.
### warpResourceProduced
(Debug visible) amount of simulation resource produced.
### warpResourceRequired
(Debug visible) amount of simulation resource consumed.
### terrainHit
Hit test stuff to make sure we don't run into planets.
### layerMask
Layer mask used for the hit test
### warpEngines
List of active warp engines
### warpCoils
List of enabled warp coils
### warpGenerators
List of warp generators
### warpEngineTextures
List of animated textures driven by the warp engine
### previousBody
Previously visited celestial body
### bodyBounds
Bounds object of the celestial body
### throttleLevel
Current throttle level
### bowShockTransform
Optional bow shock effect transform.
### warpFlameout
Due to the way engines work on FixedUpdate, the engine can determine that it is NOT flamed out if it meets its propellant requirements. Therefore, we keep track of our own flameout conditions.
### waterfallFXModule
Optional (but highly recommended) Waterfall effects module
### hasExceededLightSpeed
Flag to indicate whether or not the vessel has exceeded light speed.
### warpSpeed
Current speed of the ship in terms of C.
### consumptionMultiplier
Current multiplier used for the consumption of resources.
## Methods


### CircularizeOrbit
Circularizes the ship's orbit

### CircularizeOrbitAction(KSPActionParam)
Action menu item to circularize the ship's orbit.
> #### Parameters
> **param:** 


### IsActivated
Determines whether or not the engine is ignited and operational.
> #### Return value
> true if the engine is activated, false if not.

### IsFlamedOut
Checks flamout conditions including ensuring that the ship is in space, meets minimum warp altitude, and has sufficient warp capacity.
> #### Return value
> true if the engine is flamed out, false if not.

### HasWarpCapacity
Determines whether or not the ship has sufficient warp capacity to go FTL.
> #### Return value
> true if the ship has sufficient warp capacity, false if not.

### IsInSpace
Determines whether or the ship is in space. To be in space the ship must be sub-orbital, orbiting, or escaping.
> #### Return value
> true if the ship is in space, false if not.

### MeetsWarpAltitude
Determines whether or not the ship meets the minimum required altitude to go to warp.
> #### Return value
> true if the ship meets minimum altitude, false if not.

### UpdateWarpStatus
Updates the warp status display

### fadeOutEffects
Fades out the warp effects

### getAnimatedWarpEngineTextures
Finds any animated textures that should be controlled by the warp engine

### calculateBestWarpSpeed
Calculates the best possible warp speed from the vessel's active warp engines.

### getTotalWarpCapacity
Calulates the total warp capacity from the vessel's active warp coils. Each warp coil must successfully consume its required resources in order to be considered.

### updateWarpPowerGenerators
Updates the generators that provide warp power.

### disableGeneratorBypass
In order to synchronize the converter's process with the active warp engine, we enable a generator bypass. The moment that we no longer need to do that, such as when the engine is shut down, or it flames out, we want to disable the bypass.

### shouldApplyWarp
Looks for all the active warp engines in the vessel. From the list, only the top-most engine in the list of active engines should apply warp translation. All others simply provide support.
> #### Return value
> 

### loadCurve(FloatCurve,System.String,ConfigNode)
Loads the desired FloatCurve from the desired config node.
> #### Parameters
> **curve:** The FloatCurve to load

> **curveNodeName:** The name of the curve to load

> **defaultCurve:** An optional default curve to use in case the curve's node doesn't exist in the part module's config.


