            
Renders a visual-only, animated Graviolium connection between two transforms. Power modules own activation and gameplay; this component only follows endpoints.
        
## Properties

### IsPlaying
Indicates whether the beam is currently visible and following its endpoints.
## Methods


### Play(UnityEngine.Transform,UnityEngine.Vector3,UnityEngine.Transform,UnityEngine.Vector3,Blueshift.GravioliumBeamSettings)
Starts the beam or replaces its endpoints and visual settings. Local offsets allow callers to attach near a hand and at a visible target center.

### SetEndpoints(UnityEngine.Transform,UnityEngine.Vector3,UnityEngine.Transform,UnityEngine.Vector3)
Updates the transforms and local attachment offsets without restarting animation.

### StopEffect
Hides the beam while retaining its allocated renderers, materials, and point arrays.

