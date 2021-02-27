            
Describes a space anomaly. Similar to asteroids, space anomalies are listed as unknown objects until tracked and visited. Each type of anomaly is defined by a SPACE_ANOMALY config node.
        
## Fields

### name
Identifier for the space anomaly.
### partName
Name of the part to spawn
### anomalyType
Type of anomaly. Default is generic.
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
### expirationDate
Timestamp when the anomaly expires. If set to -1 then it never expires.
### spawnTargetNumber
Spawn chance in a roll between 1 and 1000
### maxInstances
Maximum number of objects of this type that may exist at any given time. Default is 10. Set to -1 for unlimited number.
### vesselId
ID of the vessel as found in the FlightGlobals.VesselsUnloaded.
### isKnown
Flag to indicate whether or not the gate should automatically be added to the network's known gates. If set to false (the default), then players must visit the gate in order for it to be added to the network. Applies to anomalyType = jumpGate.
### networkID
Only gates with matching network IDs can connect to each other. Leave blank if the gate connects to any network. If there are only two gates in the network then there is no need to select the other gate from the list. You can add additional networks by adding a semicolon character in between network IDs. Applies to anomalyType = jumpGate.
### rendezvousDistance
Overrides the jumpgate's rendezvous distance.
## Methods


### CopyFrom(Blueshift.WBISpaceAnomaly)
Copies the fields from another space anomaly.
> #### Parameters
> **copyFrom:** The WBISpaceAnomaly whose fields we're interested in.


### Load(Blueshift.WBISpaceAnomaly,ConfigNode)
Loads the ConfigNode data into the anomaly object.
> #### Parameters
> **anomaly:** A WBISpaceAnomaly to load the data into.

> **node:** A ConfigNode containing serialized data.


### Save(System.String)
Serializes the anomaly to a ConfigNode.
> #### Parameters
> **nodeName:** A string containing the name of the node.

> #### Return value
> A ConfigNode with the serialized data.

### CreateNewInstancesIfNeeded(System.Collections.Generic.List{Blueshift.WBISpaceAnomaly})
Checks to see if we should create a new instance.

