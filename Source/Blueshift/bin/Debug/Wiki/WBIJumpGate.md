            
The WBIJumpGate is a part module that transports vessels that enter the gate to another gate some distance away. It allows instantaneous, faster than light travel without the need for the traveling vessel to carry expensive FTL equipment. The craft merely needs to approach the gate, select the desired destination, and then travel through the gate. The jump gate need not require resources to travel through, but the gates in Blueshift all require a Graviolium toll to be paid before travel is allowed. For the large gates (Jump Gate Anomaly, Astria Porta), the graviolium toll must be paid by the vessel that wishes to traverse the gate. For the Miniature Jumpgate, the gate itself pays the toll. Below is a sample config file for the jump gate. MODULE { name = WBIJumpGate // Only gates with matching network IDs can connect to each other. Leave blank if the gate connects to any network. // If there are only two gates in the network then there is no need to select the other gate from the list. // You can add additional networks by adding a semicolon character in between network IDs. networkID = 4.8.15.16.23.42 // If the gate has a limited jump range, then only those gates that are in the network and within range can be selected. // The exception is a network of two gates; max range is ignored. // Set to -1 (the default) for unlimited jump range. // Units are in light-years (9460700000000000 meters) maxJumpRange = -1 // Maximum width and height of the vessel that the gate can support. jumpMaxDimensions = 24,24 // Name of the portal trigger transform. The trigger is a collider set to Is Trigger in Unity. portalTriggerTransform = portalTrigger // Scale curve to use during startup. This should follow the Waterfall effect (if any). // During the startup sequence the Z-axis will be scaled according to this curve. Any vessel or vessel parts caught // by the portal trigger during startup will get vaporized unless "Jumpgates: desctructive startup" in Game Difficulty is disabled. triggerStartupScaleCurve { key = 0 1 key = 0.25 1 key = 0.625 50 key = 1 1 } runningEffect = running // Name of the waterfall effect controller, if any. waterfallEffectController = gateEffectsController // In seconds, how quickly to throttle up the waterfall effect from 0 to 1. effectSpoolTime = 0.5 // In order to jump a vessel, gates can require that the vessel pay a toll of one or more resources. // If the vessel doesn't have sufficient resources then it cannot jump. Simply add one or more Resource nodes. // The cost is per metric ton of the vessel. RESOURCE { name = Graviolium rate = 5 FlowMode = STAGE_PRIORITY_FLOW } // Defines a resource that must be paid in order to reach the desired destination. // This node overrides the older RESOURCE node that defined the jump toll. RESOURCE_TOLL { // Name of the toll. This is mainly for ModuleManager purposes. name = planetarySOIToll // Price tier- one of: planetary, interplanetary, interstellar priceTier = planetary // Name of the resource resourceName = Graviolium // Amount of resource per metric tonne mass of the traveler amountPerTonne = 0.1 // Resource is paid by the traveler that is initiating the jump paidByTraveler = false } RESOURCE_TOLL { name = interplanetaryToll priceTier = interplanetary resourceName = Graviolium amountPerTonne = 1 paidByTraveler = false } RESOURCE_TOLL { name = interstellarToll priceTier = interstellar resourceName = Graviolium amountPerTonne = 5 paidByTraveler = false } }
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### textureModuleID
This field tells the module which WBIAnimatedTexture to control.
### startupAnimation
Animation to play before playing the portal effect.
### runningEffect
Effect to play while the gate is running.
### startupEffect
Effect to play when the gate starts.
### teleportEffect
Effect to play when a vessel teleports.
### waterfallEffectController
Name of the Waterfall effects controller that controls the warp effects (if any).
### effectSpoolTime
In seconds, how quickly to throttle up the waterfall effect from 0 to 1.
### effectsThrottle
A control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### networkID
Only gates with matching network IDs can connect to each other. Leave blank if the gate connects to any network. If there are only two gates in the network then there is no need to select the other gate from the list. You can add additional networks by adding a semicolon character in between network IDs.
### maxJumpRange
If the gate has a limited jump range, then only those gates that are in the network and within range can be selected. The exception is a network of two gates; max range is ignored. Set to -1 (the default) for unlimited jump range. Units are in light-years (9460700000000000 meters)
### jumpMaxMass
Since KSP's vessel measurements are so wacked when in flight, we'll use a maximum jump mass instead. Set to -1 (the default value) for unlimited mass.
### jumpMaxDimensions
Maximum dimensions, in meters, that can fit in the gate to be transported. Width, length, height. Set a dimension to 0 for unrestricted size for that dimension.
### interactionRange
Range at which players can interact with the gate's PAW. Default is 500 meters.
### portalTriggerTransform
Name of the portal trigger transform. The trigger is a collider set to Is Trigger in Unity.
### triggerStartupScaleCurve
Scale curve to use during startup. This should follow the Waterfall effect (if any). During the startup sequence the Z-axis will be scaled according to this curve. Any vessel or vessel parts caught by the portal trigger during startup will get vaporized unless "Jumpgates: desctructive startup" in Game Difficulty is disabled.
### rendezvousDistance
Specifies the rendezvous distance. Default is 50 meters away from the gate's vessel transform. Set to -1 (the default) to use the value from Blueshift settings.
### autoActivate
Flag to automatically activate the jumpgate. It requires two gates in the network.
### lightIntensity
How bright to make the lights.
### waterfallFXModule
Optional (but highly recommended) Waterfall effects module
### vesselID
The ID of the vessel when it was first created.
## Methods


### SetGateEnabled(System.Boolean)
Enables/disables the jumpgate.
> #### Parameters
> **isEnabled:** A flag that sets the gate enabled/disabled.


