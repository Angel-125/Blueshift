            
Counters the pull of gravity up to a maximum amount of gravitic acceleration.
        
## Fields

### engageEffectName
EFFECTS group played when the generator starts. Falls back to startEffect.
### disengageEffectName
EFFECTS group played when the generator stops. Falls back to stopEffect.
### flameoutEffectName
EFFECTS group played when the generator loses required resources.
### powerEffectName
EFFECTS group driven by generator output.
### runningEffectName
EFFECTS group played continuously while the generator is operating. Falls back to runningEffect.
### fwdThrustEffect
EFFECTS group driven by the forward component of gravitic acceleration.
### revThrustEffect
EFFECTS group driven by the reverse component of gravitic acceleration.
### vtolThrustEffect
EFFECTS group driven by the vertical component of gravitic acceleration.
### verticalUpEffectName
Optional EFFECTS group driven by positive Translate Y input.
### verticalDownEffectName
Optional EFFECTS group driven by negative Translate Y input.
## Methods


### updateContinuousEffects(System.Single,System.Single)
Drives all continuous stock EFFECTS groups from the generator's current state.

### stopContinuousEffects
Turns off effects that can be looped or continuously emitted.

