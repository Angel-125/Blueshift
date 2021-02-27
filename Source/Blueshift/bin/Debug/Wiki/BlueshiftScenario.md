            
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
### circularizationResourceDef
It can cost resources to auto-circularize a ship after warp.
### circularizationCostPerTonne
How much circularizationResource does it cost per metric ton of ship to circularize its orbit.
### spawnSpaceAnomalies
Flag to indicate whether or not Space Anomalies are enabled.
### spawnJumpgates
Flag to indicate whether or not Jumpgate anomalies are enabled.
### jumpgateStartupIsDestructive
The jumpgate startup sequence is destructive. Stay clear!
## Methods


### AddJumpgateToNetwork(Blueshift.WBISpaceAnomaly)
Adds the jumpgate anomaly to the network.
> #### Parameters
> **anomaly:** The WBISpaceAnomaly to add.


### AddJumpgateToNetwork(System.String,System.String)
Adds the jumpgate to the network.
> #### Parameters
> **vesselID:** A string containing the ID of the jumpgate vessel.

> **networkID:** A string containing the ID of the jumpgate network.


### GetDestinationGates(System.String,Vector3d,System.Double)
Returns the list of possible destination gates that are in range of the specified origin point.
> #### Parameters
> **networkID:** A string containing the network ID.

> **originPoint:** A Vector3d containing the origin point to check for gates in range.

> **maxJumpRange:** A double containing the maximum jump range, measured in light-years. Set to -1 to ignore max jump range.

> #### Return value
> A List of Vessel containing the vessels in the network that are in range, or null if no network or vessels in range could be found.

### GetAnomaly(System.String)
Returns the anomaly matching the desired vesselID.
> #### Parameters
> **vesselID:** A string containing the vessel ID.

> #### Return value
> A WBISpaceAnomaly if the anomaly can be found, or null if not.

### GetVessel(System.String)
Attempts to locate the destination vessel based on the ID supplied.
> #### Parameters
> **vesselID:** A string containing the vessel ID

> #### Return value
> A Vessel if one can be found, null if not.

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

