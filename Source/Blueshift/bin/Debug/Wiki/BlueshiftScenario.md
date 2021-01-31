            
This class helps starships determine when they're in interstellar space.
        
## Fields

### shared
Shared instance of the helper.
### homeSystemSOI
Sphere of influence radius of the home system.
### interstellarWarpSpeedMultiplier
When in intersteller space, vessels can go much faster. This multiplier tells us how much faster we can go. For comparison, Mass Effect Andromeda's Tempest can cruise at 4745 times light speed, or 13 light-years per day.
### homeSOIMultiplier
In game, the Sun has infinite Sphere of Influence, so we compute an artificial one based on the furthest planet from the Sun. To give a little wiggle room, we multiply the computed value by this multiplier.
## Methods


### OnAwake
Handles the awake event.

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

