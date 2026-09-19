using System;
using System.Collections.Generic;
using CommNet;
using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// Effective special handling for one CommNet connection evaluation.
    /// </summary>
    internal struct WBICommNetLinkPolicy
    {
        internal bool bypassRange;
        internal bool bypassOcclusion;

        internal bool HasOverrides
        {
            get { return bypassRange || bypassOcclusion; }
        }
    }

    /// <summary>
    /// Caches CommNet node ownership and resolves WBICommNetLinkOptions for loaded and unloaded vessels.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class WBICommNetLinkManager : MonoBehaviour
    {
        private const string kOptionsModuleName = "WBICommNetLinkOptions";

        private sealed class LinkOptionsData
        {
            internal bool bypassRange;
            internal bool bypassOcclusion;
            internal bool requireMatchingPeer = true;
            internal string linkID = string.Empty;
            internal bool useJumpGateNetwork;
        }

        private static readonly List<LinkOptionsData> emptyOptions = new List<LinkOptionsData>();
        private static WBICommNetLinkManager instance;

        private readonly Dictionary<CommNode, Vessel> vesselsByNode = new Dictionary<CommNode, Vessel>();
        private readonly Dictionary<Guid, List<LinkOptionsData>> optionsByVessel = new Dictionary<Guid, List<LinkOptionsData>>();
        private readonly Dictionary<Guid, string[]> jumpgateNetworksByVessel = new Dictionary<Guid, string[]>();
        private bool cacheDirty = true;

        /// <summary>
        /// Marks CommNet ownership and policy caches stale and optionally requests a stock network rebuild.
        /// </summary>
        /// <param name="queueRebuild">Whether to request a CommNet rebuild.</param>
        public static void InvalidateCaches(bool queueRebuild = true)
        {
            if (instance != null)
                instance.cacheDirty = true;

            if (queueRebuild && HighLogic.LoadedSceneIsFlight && CommNetNetwork.Instance != null)
                CommNetNetwork.Instance.QueueRebuild();
        }

        /// <summary>
        /// Resolves the effective specialized-link policy for a pair of CommNet nodes.
        /// </summary>
        internal static bool TryGetPolicy(CommNode firstNode, CommNode secondNode, out WBICommNetLinkPolicy policy)
        {
            policy = new WBICommNetLinkPolicy();
            if (instance == null || firstNode == null || secondNode == null)
                return false;

            return instance.tryGetPolicy(firstNode, secondNode, out policy);
        }

        private void Awake()
        {
            instance = this;
            registerEvents();
            cacheDirty = true;
        }

        private void Start()
        {
            InvalidateCaches();
        }

        private void OnDestroy()
        {
            unregisterEvents();
            if (instance == this)
                instance = null;
        }

        private bool tryGetPolicy(CommNode firstNode, CommNode secondNode, out WBICommNetLinkPolicy policy)
        {
            policy = new WBICommNetLinkPolicy();
            ensureCaches();

            Vessel firstVessel;
            Vessel secondVessel;
            vesselsByNode.TryGetValue(firstNode, out firstVessel);
            vesselsByNode.TryGetValue(secondNode, out secondVessel);

            List<LinkOptionsData> firstOptions = getOptions(firstVessel);
            List<LinkOptionsData> secondOptions = getOptions(secondVessel);
            if (firstOptions.Count == 0 && secondOptions.Count == 0)
                return false;

            applyUnilateralOptions(firstOptions, ref policy);
            applyUnilateralOptions(secondOptions, ref policy);
            applyMatchingOptions(firstOptions, secondOptions, ref policy);
            applyJumpGateOptions(firstVessel, secondVessel, firstOptions, secondOptions, ref policy);

            return policy.HasOverrides;
        }

        private void ensureCaches()
        {
            if (cacheDirty)
                refreshCaches();
        }

        private void refreshCaches()
        {
            vesselsByNode.Clear();
            optionsByVessel.Clear();
            jumpgateNetworksByVessel.Clear();
            cacheDirty = false;

            if (FlightGlobals.Vessels == null)
                return;

            int count = FlightGlobals.Vessels.Count;
            for (int index = 0; index < count; index++)
            {
                Vessel vessel = FlightGlobals.Vessels[index];
                if (vessel == null)
                    continue;

                if (vessel.Connection != null && vessel.Connection.Comm != null)
                    vesselsByNode[vessel.Connection.Comm] = vessel;

                List<LinkOptionsData> options = vessel.loaded
                    ? getLoadedOptions(vessel)
                    : getUnloadedOptions(vessel);
                if (options.Count > 0)
                    optionsByVessel[vessel.id] = options;

                if (BlueshiftScenario.shared != null)
                {
                    string[] jumpgateNetworks = BlueshiftScenario.shared.GetJumpgateNetworks(vessel.id);
                    if (jumpgateNetworks.Length > 0)
                        jumpgateNetworksByVessel[vessel.id] = jumpgateNetworks;
                }
            }
        }

        private List<LinkOptionsData> getOptions(Vessel vessel)
        {
            if (vessel == null)
                return emptyOptions;

            List<LinkOptionsData> options;
            if (optionsByVessel.TryGetValue(vessel.id, out options))
                return options;

            return emptyOptions;
        }

        private static List<LinkOptionsData> getLoadedOptions(Vessel vessel)
        {
            List<LinkOptionsData> options = new List<LinkOptionsData>();
            List<WBICommNetLinkOptions> modules = vessel.FindPartModulesImplementing<WBICommNetLinkOptions>();
            int count = modules.Count;
            for (int index = 0; index < count; index++)
            {
                WBICommNetLinkOptions module = modules[index];
                options.Add(new LinkOptionsData
                {
                    bypassRange = module.bypassRange,
                    bypassOcclusion = module.bypassOcclusion,
                    requireMatchingPeer = module.requireMatchingPeer,
                    linkID = normalizeLinkID(module.linkID),
                    useJumpGateNetwork = module.useJumpGateNetwork
                });
            }
            return options;
        }

        private static List<LinkOptionsData> getUnloadedOptions(Vessel vessel)
        {
            List<LinkOptionsData> options = new List<LinkOptionsData>();
            ProtoVessel protoVessel = vessel.protoVessel;
            if (protoVessel == null || protoVessel.protoPartSnapshots == null)
                return options;

            int partCount = protoVessel.protoPartSnapshots.Count;
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                ProtoPartSnapshot partSnapshot = protoVessel.protoPartSnapshots[partIndex];
                if (partSnapshot == null || partSnapshot.modules == null)
                    continue;

                bool foundSavedOptions = false;
                int moduleCount = partSnapshot.modules.Count;
                for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
                {
                    ProtoPartModuleSnapshot moduleSnapshot = partSnapshot.modules[moduleIndex];
                    if (moduleSnapshot == null || moduleSnapshot.moduleName != kOptionsModuleName)
                        continue;

                    foundSavedOptions = true;
                    ConfigNode values = moduleSnapshot.moduleValues;
                    options.Add(new LinkOptionsData
                    {
                        bypassRange = getBool(values, "bypassRange", false),
                        bypassOcclusion = getBool(values, "bypassOcclusion", false),
                        requireMatchingPeer = getBool(values, "requireMatchingPeer", true),
                        linkID = normalizeLinkID(getString(values, "linkID")),
                        useJumpGateNetwork = getBool(values, "useJumpGateNetwork", false)
                    });
                }

                // Existing unloaded vessels may predate the options module in their snapshots. Use the current
                // part prefab until KSP next loads and re-saves the vessel with the new persistent fields.
                if (!foundSavedOptions && partSnapshot.partInfo != null && partSnapshot.partInfo.partPrefab != null)
                {
                    List<WBICommNetLinkOptions> prefabModules =
                        partSnapshot.partInfo.partPrefab.FindModulesImplementing<WBICommNetLinkOptions>();
                    for (int moduleIndex = 0; moduleIndex < prefabModules.Count; moduleIndex++)
                    {
                        WBICommNetLinkOptions module = prefabModules[moduleIndex];
                        options.Add(new LinkOptionsData
                        {
                            bypassRange = module.bypassRange,
                            bypassOcclusion = module.bypassOcclusion,
                            requireMatchingPeer = module.requireMatchingPeer,
                            linkID = normalizeLinkID(module.linkID),
                            useJumpGateNetwork = module.useJumpGateNetwork
                        });
                    }
                }
            }

            return options;
        }

        private static void applyUnilateralOptions(List<LinkOptionsData> options, ref WBICommNetLinkPolicy policy)
        {
            int count = options.Count;
            for (int index = 0; index < count; index++)
            {
                LinkOptionsData option = options[index];
                if (option.useJumpGateNetwork || option.requireMatchingPeer)
                    continue;

                policy.bypassRange |= option.bypassRange;
                policy.bypassOcclusion |= option.bypassOcclusion;
            }
        }

        private static void applyMatchingOptions(List<LinkOptionsData> firstOptions, List<LinkOptionsData> secondOptions, ref WBICommNetLinkPolicy policy)
        {
            int firstCount = firstOptions.Count;
            int secondCount = secondOptions.Count;
            for (int firstIndex = 0; firstIndex < firstCount; firstIndex++)
            {
                LinkOptionsData first = firstOptions[firstIndex];
                if (first.useJumpGateNetwork || string.IsNullOrEmpty(first.linkID))
                    continue;

                for (int secondIndex = 0; secondIndex < secondCount; secondIndex++)
                {
                    LinkOptionsData second = secondOptions[secondIndex];
                    if (second.useJumpGateNetwork || string.IsNullOrEmpty(second.linkID) ||
                        !string.Equals(first.linkID, second.linkID, StringComparison.OrdinalIgnoreCase))
                        continue;

                    // A matched link receives only capabilities requested by both peers. This intersection prevents
                    // differently configured endpoints from accidentally broadening each other's policy.
                    policy.bypassRange |= first.bypassRange && second.bypassRange;
                    policy.bypassOcclusion |= first.bypassOcclusion && second.bypassOcclusion;
                }
            }
        }

        private void applyJumpGateOptions(Vessel firstVessel, Vessel secondVessel, List<LinkOptionsData> firstOptions,
            List<LinkOptionsData> secondOptions, ref WBICommNetLinkPolicy policy)
        {
            if (firstVessel == null || secondVessel == null || !shareJumpgateNetwork(firstVessel.id, secondVessel.id))
                return;

            int firstCount = firstOptions.Count;
            int secondCount = secondOptions.Count;
            for (int firstIndex = 0; firstIndex < firstCount; firstIndex++)
            {
                LinkOptionsData first = firstOptions[firstIndex];
                if (!first.useJumpGateNetwork)
                    continue;

                for (int secondIndex = 0; secondIndex < secondCount; secondIndex++)
                {
                    LinkOptionsData second = secondOptions[secondIndex];
                    if (!second.useJumpGateNetwork)
                        continue;

                    policy.bypassRange |= first.bypassRange && second.bypassRange;
                    policy.bypassOcclusion |= first.bypassOcclusion && second.bypassOcclusion;
                }
            }
        }

        private bool shareJumpgateNetwork(Guid firstVesselID, Guid secondVesselID)
        {
            string[] firstNetworks;
            string[] secondNetworks;
            if (!jumpgateNetworksByVessel.TryGetValue(firstVesselID, out firstNetworks) ||
                !jumpgateNetworksByVessel.TryGetValue(secondVesselID, out secondNetworks))
                return false;

            for (int firstIndex = 0; firstIndex < firstNetworks.Length; firstIndex++)
            {
                for (int secondIndex = 0; secondIndex < secondNetworks.Length; secondIndex++)
                {
                    if (firstNetworks[firstIndex] == secondNetworks[secondIndex])
                        return true;
                }
            }

            return false;
        }

        private static string normalizeLinkID(string linkID)
        {
            return string.IsNullOrEmpty(linkID) ? string.Empty : linkID.Trim();
        }

        private static bool getBool(ConfigNode node, string valueName, bool defaultValue)
        {
            if (node == null || !node.HasValue(valueName))
                return defaultValue;

            bool value;
            return bool.TryParse(node.GetValue(valueName), out value) ? value : defaultValue;
        }

        private static string getString(ConfigNode node, string valueName)
        {
            return node != null && node.HasValue(valueName) ? node.GetValue(valueName) : string.Empty;
        }

        private void registerEvents()
        {
            GameEvents.onVesselCreate.Add(onVesselChanged);
            GameEvents.onVesselDestroy.Add(onVesselChanged);
            GameEvents.onVesselWasModified.Add(onVesselChanged);
            GameEvents.onVesselLoaded.Add(onVesselChanged);
            GameEvents.onVesselUnloaded.Add(onVesselChanged);
            GameEvents.onPartCouple.Add(onPartCouple);
            GameEvents.onPartUndock.Add(onPartUndock);
            GameEvents.onVesselsUndocking.Add(onVesselsUndocking);
            GameEvents.onGameStatePostLoad.Add(onGameStatePostLoad);
            GameEvents.onLevelWasLoadedGUIReady.Add(onLevelWasLoaded);
        }

        private void unregisterEvents()
        {
            GameEvents.onVesselCreate.Remove(onVesselChanged);
            GameEvents.onVesselDestroy.Remove(onVesselChanged);
            GameEvents.onVesselWasModified.Remove(onVesselChanged);
            GameEvents.onVesselLoaded.Remove(onVesselChanged);
            GameEvents.onVesselUnloaded.Remove(onVesselChanged);
            GameEvents.onPartCouple.Remove(onPartCouple);
            GameEvents.onPartUndock.Remove(onPartUndock);
            GameEvents.onVesselsUndocking.Remove(onVesselsUndocking);
            GameEvents.onGameStatePostLoad.Remove(onGameStatePostLoad);
            GameEvents.onLevelWasLoadedGUIReady.Remove(onLevelWasLoaded);
        }

        private void onVesselChanged(Vessel vessel)
        {
            InvalidateCaches();
        }

        private void onPartCouple(GameEvents.FromToAction<Part, Part> action)
        {
            InvalidateCaches();
        }

        private void onPartUndock(Part part)
        {
            InvalidateCaches();
        }

        private void onVesselsUndocking(Vessel firstVessel, Vessel secondVessel)
        {
            InvalidateCaches();
        }

        private void onGameStatePostLoad(ConfigNode node)
        {
            InvalidateCaches();
        }

        private void onLevelWasLoaded(GameScenes scene)
        {
            InvalidateCaches();
        }
    }
}
