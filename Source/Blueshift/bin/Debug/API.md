# Blueshift


# WBIAnomalySpawnModes
            
Space anomalies can be set up in a variety of different orbits.
        
## Fields

### randomOrbit
Spawns in a random solar or planetary orbit.
### randomSolarOrbit
Spawns in a random solar orbit.
### randomPlanetOrbit
Spawns in a random planetary orbit.
### everyLastPlanet
Spawns in random orbit of every last planet in each star system. One per each planet. Ignores maxInstances.
### fixedOrbit
Spawns in a fixed orbit. One anomaly per orbit. Ignores maxInstances and orbitType.

# WBIAnomalyOrbitTypes
            
Describes the type of orbit to create when spawning a space anomaly.
        
## Fields

### elliptical
Your garden variety elliptical orbit.
### flyBy
Fly-by orbit
### random
Randomly select either elliptical or flyBy.

# WBISpatialLocations
            
Describes the vessel's current spatial location.
        
## Fields

### Unknown
Location unknown.
### Planetary
Planetary space: vessel's mainBody is a planet or a moon.
### Interplanetary
Interplanetary space: vessel's mainBody is a star.
### Interstellar
Interstellar space: the void between the stars...

# WBIModuleGeneratorFX
            
An enhanced version of the stock ModuleGenerator that supports playing effects.
        
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

# WBIPartModule
            
Just a simple base class to handle common functionality
        
## Methods


### getPartConfigNode
Retrieves the module's config node from the part config.
> #### Return value
> A ConfigNode for the part module.

### loadCurve(FloatCurve,System.String,ConfigNode)
Loads the desired FloatCurve from the desired config node.
> #### Parameters
> **curve:** The FloatCurve to load

> **curveNodeName:** The name of the curve to load

> **defaultCurve:** An optional default curve to use in case the curve's node doesn't exist in the part module's config.


# WBIAnimatedTexture
            
This class lets you animate textures by displaying a series of images in sequence. You can animate a material's diffuse and emissive texture. You include several textures that act as the individual animation frames, and the part module will show them in sequence. This is NOT as efficient as a texture strip but it's the best I can do for now, and it's easier to set up the UV maps on the meshes being animated.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### moduleID
The ID of the part module. Since parts can have multiple animated textures, this field helps identify them.
### textureTransformName
Name of the transform whose textures will be animated.
### animatedEmissiveTexture
The name of the animated texture, like "WarpPlasma." The actual textures should be numbered in sequence (WarpPlasma1, WarpPlasma2, etc).
### animatedDiffuseTexture
The name of the diffuse texture. It too can be animated.
### minFramesPerSecond
The minimum animation speed.
### maxFramesPerSecond
The maximum animation speed. Testing shows that with frame updates happening every 0.02 seconds, that corresponds to 50 frames per second.
### emissiveFadeTime
In seconds, how fast should the emissive fade when the animation isn't activated.
### isActivated
The activation switch. When not running, the animations won't be animated.
### animationThrottle
A throttle control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### fadesAtMinThrottle
A toggle that indicates whether or not to fade out the animations when the animationThrottle is set to zero.
## Methods


### blendTextures(System.Single,UnityEngine.Texture2D,UnityEngine.Texture2D,UnityEngine.Texture2D)
Blends two textures together and stores the result in an output texture. Curtesy of stupid_chris
> #### Parameters
> **blend:** Percentage to blend through (from 0 to 1)

> **from:** Beginning texture

> **to:** Finishing texture

> **output:** Texture to appear blended


### getTextures(System.String)
Retrieves all the textures for the specified name. If the name is "WarpPlasma" for instance, then the array of textures will have "WarpPlasma1" "WarpPlasma2" and so on. The method will keep looking for textures until it can no longer find a texture in the numbered sequence.
> #### Parameters
> **textureName:** The base name of the texture to search for. It should have the relative path such as WildBlueIndustries/Blueshift/Parts/Engine/WarpPlasma.

> #### Return value
> An array of string containing the numbered textures that comprise the animation.

# WBIWarpCoil
            
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
### animationThrottle
A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### displacementImpulse
Warp coils can efficiently move a certain amount of mass to light speed and beyond without penalties. Going over this limit incurs performance penalties, but staying under this value provides benefits. The displacement value is rated in metric tons.
### waterfallFXModule
Optional (but highly recommended) Waterfall effects module

# WBICircularizationStates
            
Circularization states for auto-circularization.
        
## Fields

### doNotCircularize
Don't circularize.
### needsCircularization
Orbit needs to be circularized.
### hasBeenCircularized
Orbit has been circularized
### canBeCircularized
Orbit can be circularized.

# WBIWarpEngine
            
The Warp Engine is designed to propel a vessel faster than light. It requires WarpCapacity That is produced by WBIWarpCoil part modules. ` MODULE { name = WBIWarpEngine ...Standard engine parameters here... moduleDescription = Enables fater than light travel. bowShockTransformName = bowShock minPlanetaryRadius = 3.0 displacementImpulse = 100 planetarySOISpeedCurve { key = 1 0.1 ... key = 0.1 0.005 } warpCurve { key = 1 0 key = 10 1 ... key = 1440 10 } waterfallEffectController = warpEffectController waterfallWarpEffectsCurve { key = 0 0 ... key = 1.5 1 } textureModuleID = WarpCore } `
        
## Fields

### moduleDescription
Short description of the module as displayed in the editor.
### minPlanetaryRadius
Minimum planetary radius needed to go to warp. This is used to calculate the user-friendly minimum warp altitude display.
### minWarpAltitudeDisplay
Minimum altitude at which the engine can go to warp. The engine will flame-out unless this altitude requirement is met.
### warpSpeed
The FTL velocity of the ship, measured in C, that is adjusted for throttle setting and thrust limiter.
### spatialLocation
Where we are in space.
### vesselCourse
The vessel's course- which is really just the selected target.
### targetDistance
Distance to the vessel's target
### planetarySOISpeedCurve
Limits top speed while in a planetary or munar SOI so we don't zoom past the celestial body. Out in interplanetary space we don't have a speed limit. The first number represents how close to the SOI edge the vessel is (1 = right at the edge, 0.1 = 10% of the distance to the SOI edge) The second number is the top speed multiplier.
### displacementImpulse
Warp engines can efficiently move a certain amount of mass to light speed and beyond without penalties. Going over this limit incurs performance penalties, but staying under this value provides benefits. The displacement value is rated in metric tons.
### warpCurve
In addition to any specified PROPELLANT resources, warp engines require warpCapacity. Only parts with a WBIWarpCoil part module can generate warpCapacity. The warp curve controls how much warpCapacity is neeeded to go light speed or faster. The first number represents the available warpCapacity, while the second number gives multiples of C. You can apply any kind of warp curve you want, but the baseline uses the Fibonacci sequence * 10. It may seem steep, but in KSP's small scale, 1C is plenty fast. This curve is modified by the engine's displacementImpulse and current vessel mass. effectiveWarpCapacity = warpCapacity * (displacementImpulse / vessel mass)
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### waterfallWarpEffectsCurve
Waterfall Warp Effects Curve. This is used to control the Waterfall warp field effects based on the vessel's current warp speed. The first number represents multiples of C, and the second number represents the level at which to drive the warp effects. The effects value ranges from 0 to 1, while there's no upper limit to multiples of C, so keep that in mind. The default curve is: key = 0 0 key = 1 0.5 key = 1.5 1
### textureModuleID
The name of the WBIAnimatedTexture to drive as part of the warp effects.
### photonicBoomEffectName
Optional effect to play when the vessel exceeds the speed of light.
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
### maxWarpSpeed
(Debug visible) Maximum possible warp speed.
### warpDistance
(Debug visible) Distance per physics update that the vessel will move.
### effectsThrottle
(Debug visible) Current throttle level for the warp effects.
### terrainHit
Hit test stuff to make sure we don't run into planets.
### layerMask
Layer mask used for the hit test
### warpEngines
List of active warp engines
### warpCoils
List of enabled warp coils
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
## Methods


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

### consumeCoilResources(Blueshift.WBIWarpCoil)
Consumes the warp coil's required resources.
> #### Parameters
> **warpCoil:** The WBIWarpCoil to check for required resources.

> #### Return value
> true if the coil successfully consumed its required resources, false if not.

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


# BlueshiftScenario
            
This class helps starships determine when they're in interstellar space.
        
## Fields

### kLightYear
Light-year unit of measurement. Abbreviated "Ly."
### kGigaMeter
Gigameter unit of measurement. Abbreviate "Gm."
### kMegaMeter
Megameter unit of measurement. Abbreviated "Mm."
### shared
Shared instance of the helper.
### interstellarWarpSpeedMultiplier
When in intersteller space, vessels can go much faster. This multiplier tells us how much faster we can go. For comparison, Mass Effect Andromeda's Tempest can cruise at 4745 times light speed, or 13 light-years per day.
### autoCircularize
Flag to indicate whether or not to auto-circularize the orbit.
### autoCircularizationDelay
In seconds, how long to wait between cutting the warp engine throttle and automatically circularizing the ship's orbit.
### circularizationResourceDef
It can cost resources to auto-circularize a ship after warp.
### circularizationCostPerTonne
How much circularizationResource does it cost per metric ton of ship to circularize its orbit.
### spawnSpaceAnomalies
Flag to indicate whether or not Space Anomalies are enabled.
## Methods


### GetSpatialLocation(Vessel)
Determines thevessel's spatial location.
> #### Parameters
> **vessel:** The Vessel to check.

> #### Return value
> A WBISpatialLocations withe spatial location.

### IsAStar(CelestialBody)
Determines whether or not the celestial body is a star.
> #### Parameters
> **body:** The body to test.

> #### Return value
> true if the body is a star, false if not.

### IsInInterstellarSpace(Vessel)
Determines whether or not the vessel is in interstellar space.
> #### Parameters
> **vessel:** 

> #### Return value
> 

### IsInSpace(Vessel)
Determines whether or not the vessel is in space.
> #### Parameters
> **vessel:** The Vessel to check.

> #### Return value
> true if the vessel is in space, false if not.

### GetEveryLastPlanet
Finds every last planet in every star system.
> #### Return value
> A List of CelestialBody

### GetStars
Finds all the stars in the game.
> #### Return value
> A Listcontaining all the stars in the game. Celestial bodies that are on the celestialBlacklist are ignored.

### GetPlanets
Returns a list of all the planets in the game.
> #### Return value
> A Listcontaining all the planets in the game. Celestial bodies that are on the celestialBlacklist are ignored.

### GetLastPlanet(CelestialBody)
Finds the last planet in the supplied star system.
> #### Parameters
> **star:** A Celestial Body that is the star to check.

> #### Return value
> A CelestialBody representing the last planet in the star system (if any)

### HasPlanets(CelestialBody)
Determines whether or not the celestial body has planets orbiting it.
> #### Parameters
> **celestialBody:** The CelestialBody to check for planets.

> #### Return value
> true if the celestialBody has orbiting planets, false if not.

### GetDistanceToTarget(Vessel,System.String@,System.String@)
Calculates the distance and units of measurement to the vessel's target (if any).
> #### Parameters
> **vessel:** The Vessel to check for targets.

> **units:** A string representing the units of measurement computed for the distance.

> **targetName:** A string representing the name of the vessel's target.

> #### Return value
> A double containing the distance. If there is no target then the distance is 0.

# WBIModuleHarvesterFX
            
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
Name of the Waterfall effects controller that controls the warp effects (if any).

# WBISpaceAnomaly
            
Describes a space anomaly. Similar to asteroids, space anomalies are listed as unknown objects until tracked and visited. Each type of anomaly is defined by a SPACE_ANOMALY config node.
        
## Fields

### name
Identifier for the space anomaly.
### partName
Name of the part to spawn
### sizeClass
Like asteroids, space anomalies have a size class that ranges from Size A (12 meters) to Size I (100+ meters). The default is A.
### spawnMode
How does an instance spawn
### orbitType
The type of orbit to create. Default is elliptical.
### maxDaysToClosestApproach
For flyBy orbits, the max number of days until the anomaly reaches the closest point in its orbit. Default is 30.
### flyByOrbitChance
For orbitType = random, on a roll of 1 to 100, what is the chance that the orbit will be flyBy. Default is 50.
### fixedBody
For fixedOrbit, the celestial body to spawn around.
### fixedSMA
For fixedOrbit, the Semi-Major axis of the orbit.
### fixedEccentricity
For fixedOrbit, the eccentrcity of the orbit.
### fixedInclination
Fixed inclination. Only used for fixedOrbit. If set to -1 then a random inclination will be used instead.
### minLifetime
For undiscovered objects, the minimum number of seconds that the anomaly can exist. Default is 86400 (1 day). Set to -1 to use maximum possible value. When set to -1, maxLifetime is ignored.
### maxLifetime
For undiscovered objects, the maximum number of seconds that the anomaly can exist. Default is 1728000 (20 days).
### spawnTargetNumber
Spawn chance in a roll between 1 and 1000
### maxInstances
Maximum number of objects of this type that may exist at any given time. Default is 10. Set to -1 for unlimited number.
### vesselId
ID of the vessel as found in the FlightGlobals.VesselsUnloaded.
## Methods


### CreateNewInstancesIfNeeded(System.Collections.Generic.List{Blueshift.WBISpaceAnomaly})
Checks to see if we should create a new instance.

# WBITechUnlock
            
This part module is designed to unlock random nodes in a tech tree. It can also drive Waterfall effects.
        
## Fields

### dieRoll
Maximum RNG value
### unlockTargetNumber
Target number to unlock a tech tree node
### unlockMessage
Tech unlock message
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### animationThrottle
A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### hasBeenVisited
Flag to indicate whether or not the part has been visited.
### waterfallFXModule
Optional (but highly recommended) Waterfall effects module