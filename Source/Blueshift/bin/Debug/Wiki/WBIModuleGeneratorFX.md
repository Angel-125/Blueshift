            
An enhanced version of the stock ModuleGenerator that supports playing effects. Supports Waterfall.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### moduleTitle
The module's title/display name.
### moduleDescription
The module's description.
### moduleID
The ID of the part module. Since parts can have multiple generators, this field helps identify them.
### guiVisible
Toggles visibility of the GUI.
### textureModuleID
Generators can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
### animationThrottle
A throttle control to vary the animation speed of a controlled WBIAnimatedTexture
### startEffect
Generators can play a start effect when the generator is activated.
### stopEffect
Generators can play a stop effect when the generator is deactivated.
### runningEffect
Generators can play a running effect while the generator is running.
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### isMissingResources
Flag indicating whether or not we're missing resources needed to produce outputs.
### bypassRunCycle
This flag lets an external part module bypass the converter's run cycle which is triggered by FixedUpdate. When this flag is set to true, then the base class's FixedUpdate won't be called. Without the base class' FixedUpdate getting called, no resources will be converted. The external part module is expected to call RunGeneratorCycle manually. This system was put in place to get around timing issues where gravitic generators should produce enough resources for warp coils to consume each time tick, but due to timing issues, the resources aren't produced in time for the warp engine to handle resource consumption. To get around that problem, the active warp engine handles resource conversion during its fixed update.
### resourceConsumptionModifier
Multiplier to adjust consumption of input resources.
## Methods


### RunGeneratorCycle
This is a helper function to avoid issues where a warp engine needs a certain amount of resources in order to operate, the system should have them, but due to timing in the game, the resources aren't produced when they should be.

### GetAmountProduced(System.String)
Returns the amount of the supplied resource that is produced per second.
> #### Parameters
> **resourceName:** A string containing the name of the resource to look for.

> #### Return value
> A double containing the amount of the resource produced, or 0 if the resource can't be found.

