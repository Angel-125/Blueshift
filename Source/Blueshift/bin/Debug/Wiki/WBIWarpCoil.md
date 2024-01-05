            
Warp coils produce the warp capacity needed for vessels to go faster than light. Warp capacity is a fixed resource, but the resources needed to produce it are entirely optional. ` MODULE { name = WBIWarpCoil textureModuleID = WarpCoil warpCapacity = 10 RESOURCE { name = GravityWaves rate = 200 FlowMode = STAGE_PRIORITY_FLOW } } `
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### textureModuleID
Warp coils can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
### runningEffect
Warp coils can play a running effect while the generator is running.
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### warpCapacity
The amount of warp capacity that the coil can produce.
### isActivated
The activation switch. When not running, the animations won't be animated.
### statusDisplay
Display string for the warp coil status.
### animationThrottle
A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### displacementImpulse
Warp coils can efficiently move a certain amount of mass to light speed and beyond without penalties. Going over this limit incurs performance penalties, but staying under this value provides benefits. The displacement value is rated in metric tons.
### needsMaintenance
Flag to indicate that the part needs maintenance in order to function.
### waterfallFXModule
Optional (but highly recommended) Waterfall effects module
## Properties

### totalWarpCapacity
Returns the total modified warp capacity, accounting for multi-coil segment variants
## Methods


### UpdateMTBFRateMultiplier(System.Double)
Updates the MTBF rate multiplier with the new rate.
> #### Parameters
> **rateMultiplier:** A double containing the new multiplier.


### UpdateMTBF(System.Double)
Updates the warp core's EVA Repairs' MTBF, if any.

### HasEnoughResources(System.Double)
Determines whether or not the warp coil has enough resources to operate.
> #### Parameters
> **rateMultiplier:** The resource consumption rate multiplier

> #### Return value
> True if the vessel has enough resources to power the warp coil, false if not.

### GetAmountRequired(System.String)
Returns the amount of resource required per second.
> #### Parameters
> **resourceName:** A string containing the name of the resource.

> #### Return value
> A double containing the amount of required resource if it can be found, or 0 if not.

