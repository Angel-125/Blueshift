            
This part module enhances engine thrust and Isp. While the vessel's reported mass will remain unchanged, thrust, Isp, TWR, and delta-v values will be affected.
        
## Fields

### onDampenerUpdated
Signals that the inertial dampener was updated.
### onDampenerUpdatedEditor
Signals that the inertial dampener was updated in the editor.
### inertialDampeningFactor
How much internal dampening to produce
### ecMassPercentIncrease
Amount of increase in Electric Charge that it costs to run the generator. Computed as a percentage of vessel mass. So, if this value is 0.05 (the default), and the vessel is 100 tonnes, then the EC cost increases by 5. This is a value between 0 and 1.
### inertialDampeners
List of inertial dampeners on the vessel.

