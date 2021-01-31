            
This resource harvester add the ability to drive Effects, animated textures, and Waterfall.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### moduleTitle
The module's title/display name.
### moduleDescription
The module's description.
### moduleID
The ID of the part module. Since parts can have multiple harvesters, this field helps identify them.
### guiVisible
Toggles visibility of the GUI.
### textureModuleID
Harvesters can control WBIAnimatedTexture modules. This field tells the generator which WBIAnimatedTexture to control.
### animationThrottle
A throttle control to vary the animation speed of a controlled WBIAnimatedTexture
### startEffect
Harvesters can play a start effect when the generator is activated.
### stopEffect
Harvesters can play a stop effect when the generator is deactivated.
### runningEffect
Harvesters can play a running effect while the generator is running.
### waterfallEffectController
Name of the Waterfall effects control