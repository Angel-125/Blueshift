            
A collection of utility functions that Blueshift uses.
        
## Methods


### BalanceResources(System.Collections.Generic.Dictionary{System.String,System.Double},System.Double)
Balances resource amounts so that all resources run out at the same time. Given an array of consumption rates (units/second) and a total storage capacity (units), this function distributes the available storage across the resources so that each one depletes simultaneously. The idea: - For each resource, TimeToEmpty = Amount / ConsumptionRate - All TimeToEmpty values must be equal. - Solve for the common time (T): T = TotalCapacity / Sum(ConsumptionRates) - Then each resource amount is: Amount_i = ConsumptionRate_i * T Example Visualization: Total Capacity = 10000 units Consumption Rates: [ 0.002, 0.005, 0.000188, 0.02 ] Storage split across 4 resources: [ Resource 1 ] ▓▓▓▓▓▓▓▓░░░░░░░░░░░░ [ Resource 2 ] ▓▓▓▓▓▓▓▓▓▓▓▓░░░░░░░░ [ Resource 3 ] ▓▓░░░░░░░░░░░░░░░░░░ [ Resource 4 ] ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ Resources with higher consumption rates (Resource 4) get more storage. Resources with slower consumption (Resource 3) get less storage. All resources deplete at the same time.
> #### Parameters
> **consumptionRates:** Array of resource consumption rates (units/second)

> **totalCapacity:** Total storage capacity (units)

> #### Return value
> Array of resource amounts corresponding to each consumption rate
> #### Exceptions
> **System.ArgumentException:** Thrown if inputs are invalid (null, empty, or zero rates)


### ComputeBurnTime(ShipConstruct,System.String@)
Computes the burn time for the ship's gravimetric generators given the ship's current resource amounts and the rate of consumption of the resources required to run the generators. The idea: - For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters. - For each input resource, determine how much of the resource the ship has. - Divide resource amount by input ratio to get the burn time for that ratio. - Determine the lowest burn time from the list of burn times. - The lowest burn time is returned as the burn time for all the converters.
> #### Parameters
> **ship:** A ShipConstruct to compute the burn time for.

> **status:** A string containing the result of the computation.

> #### Return value
> a double containing the burn time in seconds.

### ComputeBurnTime(Vessel,System.String@)
Computes the burn time for the ship's gravimetric generators given the ship's current resource amounts and the rate of consumption of the resources required to run the generators. The idea: - For each gravimetric generator, get their resource inputs and their resource input rates to obtain all the input resources and rates needed to run all the converters. - For each input resource, determine how much of the resource the ship has. - Divide resource amount by input ratio to get the burn time for that ratio. - Determine the lowest burn time from the list of burn times. - The lowest burn time is returned as the burn time for all the converters.
> #### Parameters
> **ship:** A ShipConstruct to compute the burn time for.

> **status:** A string containing the result of the computation.

> #### Return value
> a double containing the burn time in seconds.

### CalculateRange(System.Double,System.Double,System.Double@)
Computes the range in light-years based on burn time, warp factor, and pre-defined light-years.
> #### Parameters
> **burnTime:** Number of seconds of operation time.

> **warpFactor:** Warp velocity in multiples of C.

> **distanceTraveledMeters:** Out parameter that returns the distance traveled in meters.

> #### Return value
> The distance traveled in light-years.

### FormatTime(System.Double)
Formats the time
> #### Parameters
> **timeSeconds:** Amount of seconds to format

> #### Return value
> A string containing the time

### GetBounds(Vessel)
Courtesy of MechJeb by Sarbian Licensed under GPLV3 This Vessel extension computes the Bounds of the supplied vessel. This works both in the editor and in flight. EX: Bounds vesselBounds = FlightGlobals.ActiveVessel.GetBounds();
> #### Parameters
> **vessel:** A Vessel object to compute the bounds for.

> #### Return value
> A Bounds object containing the vessel's bounds.

### GetBounds(Part)
Courtesy of MechJeb by Sarbian Licensed under GPLV3 This Part extension computes the Bounds of the supplied part. This works both in the editor and flight. EX: Bounds partBounds = FlightGlobals.ActiveVessel.rootPart.GetBounds();
> #### Parameters
> **part:** A Part object to compute the Bounds for.

> #### Return value
> A Bounds object containing the part's bounds.

