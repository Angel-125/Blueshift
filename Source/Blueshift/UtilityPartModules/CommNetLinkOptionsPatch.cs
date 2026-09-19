using CommNet;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Blueshift
{
    /// <summary>
    /// Installs Blueshift's narrowly scoped CommNet link-policy patch.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class CommNetLinkOptionsPatch : MonoBehaviour
    {
        private const string kHarmonyID = "com.wildblueindustries.blueshift.commnet-link-options";
        private static bool isPatched;

        private void Awake()
        {
            if (isPatched)
                return;

            new Harmony(kHarmonyID).PatchAll(typeof(CommNetLinkOptionsPatch).Assembly);
            isPatched = true;
        }

        [HarmonyPatch(typeof(CommNetNetwork), "ResetNetwork")]
        private static class ResetNetworkPatch
        {
            [HarmonyPrefix]
            private static void Prefix()
            {
                WBICommNetLinkManager.InvalidateCaches(false);
            }
        }

        [HarmonyPatch(typeof(CommNetwork), "SetNodeConnection")]
        private static class SetNodeConnectionPatch
        {
            private static readonly MethodInfo tryConnectMethod = AccessTools.Method(typeof(CommNetwork), "TryConnect");
            private static readonly MethodInfo testOcclusionMethod = AccessTools.Method(typeof(CommNetwork), "TestOcclusion");

            [HarmonyPrefix]
            private static bool Prefix(CommNetwork __instance, CommNode a, CommNode b, ref bool __result)
            {
                WBICommNetLinkPolicy policy;
                if (!WBICommNetLinkManager.TryGetPolicy(a, b, out policy))
                    return true;

                if (a == null || b == null || (a.isHome && b.isHome) || !hasAntennaPower(a) || !hasAntennaPower(b))
                    return true;

                Vector3d firstPosition = a.precisePosition;
                Vector3d secondPosition = b.precisePosition;
                double squaredDistance = (secondPosition - firstPosition).sqrMagnitude;
                double distance = calculateDistance(a, b, ref squaredDistance);

                bool aCanRelay;
                bool bCanRelay;
                bool bothRelay;
                getConnectionEligibility(a, b, squaredDistance, policy.bypassRange,
                    out aCanRelay, out bCanRelay, out bothRelay);

                if (!aCanRelay && !bCanRelay && !bothRelay)
                {
                    __result = tryConnect(__instance, a, b, distance, false, false, false);
                    return false;
                }

                if (!policy.bypassOcclusion && squaredDistance > 0 &&
                    !testOcclusion(__instance, firstPosition, a.occluder, secondPosition, b.occluder, distance))
                {
                    __result = tryConnect(__instance, a, b, distance, false, false, false);
                    return false;
                }

                double connectionDistance = policy.bypassRange ? 0.0000001 : distance;
                if (connectionDistance <= 0)
                    connectionDistance = 0.0000001;

                __result = tryConnect(__instance, a, b, connectionDistance, aCanRelay, bCanRelay, bothRelay);
                return false;
            }

            private static bool tryConnect(CommNetwork network, CommNode firstNode, CommNode secondNode, double distance,
                bool firstCanRelay, bool secondCanRelay, bool bothRelay)
            {
                return (bool)tryConnectMethod.Invoke(network,
                    new object[] { firstNode, secondNode, distance, firstCanRelay, secondCanRelay, bothRelay });
            }

            private static bool testOcclusion(CommNetwork network, Vector3d firstPosition, Occluder firstOccluder,
                Vector3d secondPosition, Occluder secondOccluder, double distance)
            {
                return (bool)testOcclusionMethod.Invoke(network,
                    new object[] { firstPosition, firstOccluder, secondPosition, secondOccluder, distance });
            }

            private static bool hasAntennaPower(CommNode node)
            {
                return node.antennaRelay.power > 0 || node.antennaTransmit.power > 0;
            }

            private static double calculateDistance(CommNode firstNode, CommNode secondNode, ref double squaredDistance)
            {
                double distanceOffset = firstNode.distanceOffset + secondNode.distanceOffset;
                if (distanceOffset != 0)
                {
                    double adjustedDistance = System.Math.Sqrt(squaredDistance) + distanceOffset;
                    if (adjustedDistance > 0)
                    {
                        squaredDistance = adjustedDistance * adjustedDistance;
                        return adjustedDistance;
                    }

                    squaredDistance = 0;
                    return 0;
                }

                return System.Math.Sqrt(squaredDistance);
            }

            private static void getConnectionEligibility(CommNode firstNode, CommNode secondNode, double squaredDistance,
                bool bypassRange, out bool firstCanRelay, out bool secondCanRelay, out bool bothRelay)
            {
                if (bypassRange)
                {
                    bothRelay = firstNode.antennaRelay.power > 0 && secondNode.antennaRelay.power > 0;
                    firstCanRelay = bothRelay ||
                        (firstNode.antennaRelay.power > 0 && secondNode.antennaTransmit.power > 0);
                    secondCanRelay = bothRelay ||
                        (firstNode.antennaTransmit.power > 0 && secondNode.antennaRelay.power > 0);
                    return;
                }

                IRangeModel rangeModel = CommNetScenario.RangeModel;
                if (rangeModel == null)
                {
                    firstCanRelay = false;
                    secondCanRelay = false;
                    bothRelay = false;
                    return;
                }

                bothRelay = rangeModel.InRange(firstNode.antennaRelay.power, secondNode.antennaRelay.power, squaredDistance);
                firstCanRelay = bothRelay;
                secondCanRelay = bothRelay;
                if (!bothRelay)
                {
                    firstCanRelay = rangeModel.InRange(firstNode.antennaRelay.power, secondNode.antennaTransmit.power, squaredDistance);
                    secondCanRelay = rangeModel.InRange(firstNode.antennaTransmit.power, secondNode.antennaRelay.power, squaredDistance);
                }
            }
        }
    }
}
