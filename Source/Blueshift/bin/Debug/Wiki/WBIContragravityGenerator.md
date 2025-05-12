            
Counters the pull of gravity up to a maximum amount of gravitic acceleration.
        
## Fields

### maxGForceCancellation
In meters per second-squared, the amount of acceleration due to gravity that can be negated. If this value meets or exceeds the local gravity, then only 95% of local gravity can be negated.
### effectiveGravity
Display value of the vessel's effective gravity, in units of g.
### ecMassPercentIncrease
Amount of increase in Electric Charge that it costs to run the generator. Computed as a percentage of vessel mass. So, if this value is 0.05 (the default), and the vessel is 100 tonnes, then the EC cost increases by 5. This is a value between 0 and 1.
### canApplyContragravity
Flag indicating that the generator should cancel the effects of gravity.
### gravityReductionFactor
How much to reduce the gravity by.
### vesselPartCount
Current vessel part count.
### disableConverterUponIvalidFlightState
Flag indicating if the converter should auto-disable itself when t