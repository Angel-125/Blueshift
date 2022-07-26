            
This is a helper module for jumpgates that are built insted of ones that are single-piece.
        
## Fields

### debugMode
Debug flag.
### supportSegmentPartName
Name of the part that forms part of the ring. This part will be decoupled and deleted from the vessel when assembling the jumpgate. When that happens, one of the segmentMesh items will be enabled. Once all the segmentMesh entries are enabled, the ring becomes fully operational.
### assembledCoM
When fully assembed, where to place the center of mass
### enabledMeshCount
Current count of enabled mesh segments.
### portalTriggerName
Name of the portal trigger for the jumpgate.
## Methods


### AddSegment
Adds new segment to the jumpgate if one can be found. The located segment will be destroyed.

### CompleteAssembly
Debug method to complete gate assembly.

