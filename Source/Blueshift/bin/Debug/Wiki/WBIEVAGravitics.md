            
Provides the EVA abilities supplied by a KerbalGear Graviolium Field Resonator. The module is created dynamically while a carried wearable requests it.
        
## Fields

### targetingRange
Maximum distance, in meters, at which the resonator can acquire a target.
### graviticsEnabled
Indicates whether the player has enabled the resonator.
### graviticsStatus
Current gravitic power state shown in the EVA action window.
### forcePercent
Scales the target speed of Lift, Suspension Field, and Levitate. At zero, those powers cancel vertical velocity and local gravity.
### powerSoundPath
Looping sound played while a mouse-activated power is active.
### powerSoundVolume
Power sound volume before KSP's Ship volume is applied.
### powerSoundPitch
Playback pitch for the power sound.
### debugMode
Writes state-change and interaction diagnostics to KSP.log.
### pupilGlowIntensity
Brightness multiplier for the electric-blue pupil glow.
### pupilGlowFalloff
Rim falloff used to spread the glow across the pupil surface.
### powerHeft
Increases this kerbal's stock EVA Construction lift limit.
### heftMultiplier
Multiplier applied to the calculated EVA Construction limit.
### heftCooldown
Recovery time after Heft is disabled, in seconds.
### heftCooldownEndUT
Universal time at which Heft may be enabled again.
### powerPush
Enables or disables the Push power.
### pushForce
Continuous Push force in kilonewtons (KSP force units).
### pushBaseCooldown
Base recovery time after Push, in seconds.
### pushCooldownPerSecond
Additional recovery time per second of continuous Push use.
### pushCooldownEndUT
Universal time at which Push becomes available again.
### powerPull
Enables or disables the Pull power.
### pullForce
Continuous Pull force in kilonewtons (KSP force units).
### pullBaseCooldown
Base recovery time after Pull, in seconds.
### pullCooldownPerSecond
Additional recovery time per second of continuous Pull use.
### pullCooldownEndUT
Universal time at which Pull becomes available again.
### powerLevitate
Gently levitates the active EVA kerbal while enabled.
### levitateTargetSpeed
Target upward velocity, in meters per second.
### levitateCooldownEndUT
Universal time at which Levitate becomes available again.
### powerLift
Enables or disables the Lift power.
### liftMaxMass
Maximum vessel mass eligible for Lift, in tonnes.
### liftDuration
Maximum active Lift duration, in seconds.
### liftTargetSpeed
Target upward velocity, in meters per second.
### liftVelocityResponseTime
Time used to gently correct toward the target speed.
### liftMaxCorrectionAcceleration
Maximum speed-correction acceleration, in meters per second squared.
### liftBaseCooldown
Minimum recovery time after Lift, in seconds.
### liftCooldownPerSecond
Cooldown multiplier per active Lift second.
### liftCooldownMassExponent
Exponent applied to target mass in the Lift cooldown curve.
### liftCooldownReferenceGravity
Reference gravity used to normalize Lift cooldown calculations.
### liftCooldownEndUT
Universal time at which Lift becomes available again.
### powerShove
Enables or disables the Shove power.
### shoveImpulse
Shove impulse in kilonewton-seconds (KSP impulse units).
### shoveForceDuration
Short interval over which the Shove impulse is delivered, in seconds.
### shoveFXDuration
Length of time that Shove's beam and starburst remain visible, in seconds.
### shoveBaseCooldown
Base recovery time after Shove, in seconds.
### shoveCooldownPerSecond
Additional recovery time per second of Shove use.
### shoveCooldownEndUT
Universal time at which Shove becomes available again.
### powerLaunch
Enables or disables the Launch power.
### launchImpulse
Launch impulse in kilonewton-seconds (KSP impulse units).
### launchForceDuration
Short interval over which the Launch impulse is delivered, in seconds.
### launchFXDuration
Length of time that Launch's beam and starburst remain visible, in seconds.
### launchBaseCooldown
Base recovery time after Launch, in seconds.
### launchCooldownPerSecond
Additional recovery time per second of Launch use.
### launchCooldownEndUT
Universal time at which Launch becomes available again.
### powerSuspensionField
Enables a timed field that gently lifts all eligible nearby vessels.
### suspensionFieldRadius
Maximum acquisition radius, in meters.
### suspensionFieldMaxMass
Maximum mass of each eligible vessel, in tonnes.
### suspensionFieldDuration
Maximum active field duration, in seconds.
### suspensionFieldTargetSpeed
Target upward velocity, in meters per second.
### suspensionFieldVelocityResponseTime
Time used to gently correct each vessel toward the target speed.
### suspensionFieldMaxCorrectionAcceleration
Maximum speed-correction acceleration, in meters per second squared.
### suspensionFieldBaseCooldown
Minimum recovery time after using the field, in seconds.
### suspensionFieldCooldownPerSecond
Cooldown multiplier for accumulated mass/gravity work.
### suspensionFieldCooldownMassExponent
Exponent applied independently to every affected vessel's mass.
### suspensionFieldCooldownReferenceGravity
Reference gravity used to normalize cooldown calculations.
### suspensionFieldCooldownEndUT
Universal time at which Suspension Field becomes available again.
## Methods


### OnLoad(ConfigNode)
Loads the reusable Graviolium visual settings nested in this module's config.

### OnStart(PartModule.StartState)
Initializes the dynamically created EVA module.

### OnActive
Makes the resonator available when KerbalGear activates this module.

### OnInactive
Stops all interaction when the resonator wearable is removed.

### Update
Updates target acquisition, highlights, and PAW status at frame cadence.

### FixedUpdate
Applies the selected gravitic power at physics cadence.

### OnDisable
Prevents a separately hosted beam from remaining visible if Unity disables this module.

### enforcePowerProgression(System.Boolean)
Removes power selections that the current EVA kerbal has not earned yet. This also protects against locked selections restored from a save or set by another module without relying solely on PAW visibility.

### calculateGravityCompensatedAcceleration(System.Single,System.Single)
Calculates the upward acceleration needed to counter local gravity plus a bounded velocity correction. Future timed levitation powers can reuse this.

### applyVesselAcceleration(Vessel,UnityEngine.Vector3,System.Single)
Applies a uniform acceleration to every physical part of a vessel.

### LateUpdate
Keeps the custom colors authoritative over stock mouse-over highlighting.

### OnDestroy
Restores transient visuals and unsubscribes if KSP destroys the dynamic module.

### GetModuleDisplayName
Returns the localized module title displayed in the EVA action window.

### GetInfo
Returns the localized resonator description for part information panels.

### ToggleResonator
Enables or disables the resonator's targeting mode.

### isValidPushTarget(Part)
Confirms that a target is still a loaded, unpacked, single-part vessel. This is called at the point of use so docking and construction changes are honored.

### calculatePushCooldown(System.Double,System.Single,System.Double)
Calculates Push recovery time. Target mass and local gravity are accepted here so a tuned nonlinear curve can be added without changing input or physics code.

### calculatePullCooldown(System.Double,System.Single,System.Double)
Calculates Pull recovery time. Target mass and local gravity are accepted here so a tuned nonlinear curve can be added without changing input or physics code.

### calculateShoveCooldown(System.Double,System.Single,System.Double)
Calculates Shove recovery time. Target mass and local gravity are accepted here so a tuned nonlinear curve can be added without changing input or physics code.

### calculateLaunchCooldown(System.Double,System.Single,System.Double)
Calculates Launch recovery time. Target mass and local gravity are accepted here so a tuned nonlinear curve can be added without changing input or physics code.

### calculateLiftCooldown(System.Double,System.Single,System.Double)
Calculates Lift recovery time from actual use, nonlinear target mass, and gravity.

### disableAuxiliaryPowersForPrimaryActivation
Disables powers that may remain armed alongside Push, Pull, Shove, or Launch but must yield when a primary power actually activates on a valid target. Future auxiliary powers can share this activation boundary by joining here.

