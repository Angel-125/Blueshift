            
Counters the pull of gravity up to a maximum amount of gravitic acceleration.
        
## Fields

### 
EFFECTS group played when the generator starts. Falls back to startEffect.
### 
EFFECTS group played when the generator stops. Falls back to stopEffect.
### 
EFFECTS group played when the generator loses required resources.
### 
EFFECTS group driven by generator output.
### 
EFFECTS group played continuously while the generator is operating. Falls back to runningEffect.
### 
EFFECTS group driven by the forward component of gravitic acceleration.
### 
EFFECTS group driven by the reverse component of gravitic acceleration.
### 
EFFECTS group driven by the vertical component of gravitic acceleration.
### 
Optional EFFECTS group driven by positive Translate Y input.
### 
Optional EFFECTS group driven by negative Translate Y input.
### maxGForceCancellation
In meters per second-squared, the amount of acceleration due to gravity that the device is rated for. If this value meets or exceeds the local gravity, then only 95% of local gravity can be negated.
### maxGravityNegatedPercent
A value between 0 and 100, this field represents the maximum percentage of local gravity that can be negated. If multiple generators are present, then this value is averaged between the active generators.
### flexGravOutput
Shows percentage of output of the FlexGrav generator; based on current throttle setting.
### gravityCoupling
Shows percentage of how well the generator can interact with local gravity.
### flexGravAcceleration
Shows amount of acceleration available.
### horizontalAcceleration
Display value of the vessel's horizontal acceleration, in units of m/s^2.
### verticalAcceleration
Display value of the vessel's vertical acceleration, in units of m/s^2.
### verticalLiftAngle
Redirects gravity forward (0) or upward (90)
### flexGravManualOutput
Sets output of the generator in manual mode.
### throttleControlEnabled
Flag to indicate whether or not to use the main throttle to control generator output.
### useForwardVector
Flag to indicate whether or not to forward or reverse acceleration
### ecMassPercentIncrease
Amount of increase in Electric Charge that it costs to run the generator. Computed as a percentage of vessel mass. So, if this value is 0.05 (the default), and the vessel is 100 tonnes, then the EC cost increases by 5. This is a value between 0 and 1.
### vesselPartCount
Current vessel part count.
### flexGravGenerators
List of contragravity generators on the vessel.
## Methods


### 
Drives all continuous stock EFFECTS groups from the generator's current state.

### 
Turns off effects that can be looped or continuously emitted.

### SetForwardAccelerationAction(KSPActionParam)
Sets acceleration fully forward.
> #### Parameters
> **param:** 


### SetReversedAccelerationAction(KSPActionParam)
Sets acceleration fully reverse.
> #### Parameters
> **param:** 


### SetVerticalAccelerationAction(KSPActionParam)
Sets acceleration fully vertical.
