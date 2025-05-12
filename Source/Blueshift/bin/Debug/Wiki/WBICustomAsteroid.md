            
A customized version of ModuleAsteroid to allow for standard asteroid functionality while avoiding the procedural mesh generation. This is helpful for custom asteroid anomalies like Oumuamua.
        
## Fields

### sampleAcquired
Flag indicating that a sample of the asteroid has been acquired.
### scienceExperiment
The science experiment to run.
### flightCoMTracker
Tracker for the asteroid's center of mass.
## Methods


### OnStart(PartModule.StartState)
Overrides the start method to avoid generating a procedural asteroid.
> #### Parameters
> **state:** 


### RunExperiment
Replacement event for the asteroid's sample return experiment.

### TargetCoM
Replacement event for ModuleAsteroid's event to target the asteroid's center of mass.

