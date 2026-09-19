using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// Passive configuration for specialized CommNet links. Link IDs are compared without regard to case.
    /// </summary>
    public class WBICommNetLinkOptions : PartModule
    {
        /// <summary>
        /// Bypasses the stock CommNet range check for an eligible link.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool bypassRange = false;

        /// <summary>
        /// Bypasses the stock CommNet occlusion check for an eligible link.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool bypassOcclusion = false;

        /// <summary>
        /// Requires a compatible WBICommNetLinkOptions module on the other endpoint.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool requireMatchingPeer = true;

        /// <summary>
        /// Identifies compatible specialized relays. Matching is case-insensitive.
        /// </summary>
        [KSPField(isPersistant = true)]
        public string linkID = string.Empty;

        /// <summary>
        /// Uses Blueshift's jump-gate registry instead of linkID matching.
        /// </summary>
        [KSPField(isPersistant = true)]
        public bool useJumpGateNetwork = false;
    }
}
