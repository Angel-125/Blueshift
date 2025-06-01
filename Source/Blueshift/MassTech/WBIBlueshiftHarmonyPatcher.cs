using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace Blueshift
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class WBIBlueshiftHarmonyPatcher : MonoBehaviour
    {
        public void Start()
        {
            Harmony.DEBUG = true;
            var harmony = new Harmony("com.wildblue.Blueshift.BlueshiftHarmonyPatcher");
            harmony.PatchAll();
        }

        // When RequiredPropellantMass gets called, resultingThrust is updated.
        // Since we drop the mass flow rate and up the Isp, the thrust will remain the same.
        // But we want more thrust to simulate a reduction in inertial mass, so we'll up the thrust.
        public static AccessTools.FieldRef<ModuleEngines, float> resultingThrustRef = AccessTools.FieldRefAccess<ModuleEngines, float>("resultingThrust");

        // This appears to be used during flight.
        [HarmonyPatch(typeof(ModuleEngines))]
        [HarmonyPatch("RequiredPropellantMass")]
        public class ModuleEnginesRequiredPropellantMassPatch
        {
            static void Postfix(ModuleEngines __instance, ref double __result)
            {
                // Get the inertial dampening field.
                WBIInertialDampeningField dampeningField = __instance.part.FindModuleImplementing<WBIInertialDampeningField>();

                if (dampeningField != null && dampeningField.dampeningFactor > 0)
                    resultingThrustRef(__instance) *= dampeningField.dampeningFactor;
            }
        }

        // MaxThrustOutputVac appears to be used in the editor
        [HarmonyPatch(typeof(ModuleEngines))]
        [HarmonyPatch("MaxThrustOutputVac")]
        public class ModuleEnginesMaxThrustOutputVacPatch
        {
            static void Postfix(ModuleEngines __instance, ref float __result)
            {
                // Get the inertial dampening field.
                WBIInertialDampeningField dampeningField = __instance.part.FindModuleImplementing<WBIInertialDampeningField>();

                if (dampeningField != null && dampeningField.dampeningFactor > 0)
                    __result *= dampeningField.dampeningFactor;
            }
        }

        // MaxThrustOutputAtm appears to be used in the editor
        [HarmonyPatch(typeof(ModuleEngines))]
        [HarmonyPatch("MaxThrustOutputAtm")]
        public class ModuleEnginesMaxThrustOutputAtmPatch
        {
            static void Postfix(ModuleEngines __instance, ref float __result)
            {
                // Get the inertial dampening field.
                WBIInertialDampeningField dampeningField = __instance.part.FindModuleImplementing<WBIInertialDampeningField>();

                if (dampeningField != null && dampeningField.dampeningFactor > 0)
                    __result *= dampeningField.dampeningFactor;
            }
        }

        [HarmonyPatch(typeof(ModuleEngines))]
        [HarmonyPatch("GetEngineThrust")]
        public class ModuleEnginesGetEngineThrustPatch
        {
            static void Postfix(ModuleEngines __instance, ref float __result)
            {
                // Get the inertial dampening field.
                WBIInertialDampeningField dampeningField = __instance.part.FindModuleImplementing<WBIInertialDampeningField>();

                if (dampeningField != null && dampeningField.dampeningFactor > 0)
                    __result *= dampeningField.dampeningFactor;
            }
        }
    }

}