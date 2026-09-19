            
Caches CommNet node ownership and resolves WBICommNetLinkOptions for loaded and unloaded vessels.
        
## Methods


### InvalidateCaches(System.Boolean)
Marks CommNet ownership and policy caches stale and optionally requests a stock network rebuild.
> #### Parameters
> **queueRebuild:** Whether to request a CommNet rebuild.


### TryGetPolicy(CommNet.CommNode,CommNet.CommNode,Blueshift.WBICommNetLinkPolicy@)
Resolves the effective specialized-link policy for a pair of CommNet nodes.

