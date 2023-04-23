            
This class helps starships determine when they're in interstellar space.
        
## Fields

### kLightYear
Light-year unit of measurement. Abbreviated "Ly."
### kGigaMeter
Gigameter unit of measurement. Abbreviate "Gm."
### kMegaMeter
Megameter unit of measurement. Abbreviated "Mm."
### messageDuration
How long to display a screen message.
### shared
Shared instance of the helper.
### debugMode
Flag to indicate that the mod is in debug mode.
### interstellarWarpSpeedMultiplier
When in intersteller space, vessels can go much faster. This multiplier tells us how much faster we can go. For comparison, Mass Effect Andromeda's Tempest can cruise at 4745 times light speed, or 13 light-years per day.
### warpEngineerSkill
Skill to use for improving warp engine performance.
### warpSpeedBoostRank
Minimum skill rank required to improve warp engine performance.
### warpSpeedSkillMultiplier
Skill multiplier to use when improving warp engine performance.
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
### maintenanceEnabled
Flag to indicate if parts require maintenance.
### minRendezvousDistancePlanetary
In meters, minimum distance in planetary space that's required to rendezvous with a vessel via auto-circularization.
### minRendezvousDistanceInterplanetary
In meters, minimum distance in interplanetary space that's required to rendezvous with a vessel via auto-circularization.
### rendezvousDistance
In meters, how close to the targed vessel should you end up at when you rendezvous with it during auto-circularization or a jump.
### interstellarResourceConsumptionModifier
This modifier reduces the resources required to power warp engines while in interstellar space. It is a percentage value between 0 and 99.999. The default value is 10. You can override this global setting by specifying this value in the WBIWarpEngine config.
### jumpGateSourceId
The source jumpgate that the traveler is traveling from. This is primarily used to set focus back to the source gate to jump something else.
### destinationGateId
The destination gate that the traveler is traviling to. This is primarily used to set focus back to the source gate to jump something else.
## Methods


### GetDestinationLocation(CelestialBody,CelestialBody)
Determines the spatial location of the destination celestial body relative to the source body.
> #### Parameters
> **sourceBody:** A CelestialBody representing the source. Typically this is the active vessel's mainBody.

> **destinationBody:** A CelestialBody containing the desired destination.

> #### Return value
> A WBISpacialLocations enum with the relative location of the destination.

### GetHighestRank(Vessel,System.String,ProtoCrewMember@)
Returns the highest ranking astronaut in the vessel that has the required skill.
> #### Parameters
> **vessel:** The vessel to check for the highest ranking kerbal.

> **skillName:** The name of the skill to look for. Examples include RepairSkill and ScienceSkill.

> **astronaut:** The astronaut that has the highest ranking skill.

> #### Return value
> The skill rank rating of the highest ranking astronaut (if any)

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

### GetParentStar(CelestialBody)
Find the parent star of the celestial body.
> #### Parameters
> **body:** The celestial body to check.

> #### Return value
> A CelestialBody that is the query parameter's star, or null.

