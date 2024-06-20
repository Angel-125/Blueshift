using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueshiftMassEffect
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class BlueshiftHarmonyPatcher: MonoBehaviour
    {
        public void Start()
        {
//            var harmony = new Harmony("com.wildblue.BlueshiftMassEffect.BlueshiftHarmonyPatcher");
//            harmony.PatchAll();
        }

        #region Patches
        /*
        [HarmonyPatch(typeof(Vessel), nameof(Vessel.GetTotalMass))]
        public class VesselMassPatch
        {
            static void Postfix(Vessel __instance, ref float __result)
            {
                if (!HighLogic.LoadedSceneIsFlight)
                    return;

                WBIMassEffectCore[] massEffectCores = BlueshiftHarmonyPatcher.getActiveMassEffectCores(__instance);
                float effectiveMass = BlueshiftHarmonyPatcher.calculateEffectiveMass(__result, massEffectCores);
                if (effectiveMass <= 0)
                {
                    return;
                }

                __instance.totalMass = effectiveMass;
                __result = effectiveMass;
            }
        }
        */

        /*
        [HarmonyPatch(typeof(ShipConstruct), nameof(ShipConstruct.GetTotalMass))]
        public class ShipConstructMassPatch
        {
            static void Postfix(ShipConstruct __instance, ref float __result)
            {
                WBIMassEffectCore[] massEffectCores = WBIMassEffectUtilities.getActiveCores(__instance);
                float massReductionFactor = WBIMassEffectUtilities.calculateMassReduction(massEffectCores);

                float totalMass = __result * massReductionFactor;
                if (totalMass < PhysicsGlobals.PartRBMassMin)
                    totalMass = PhysicsGlobals.PartRBMassMin;

                __result = totalMass;
            }
        }


        /*
         * DOES NOT APPEAR TO BE GETTING CALLED IN EDITOR.
        [HarmonyPatch(typeof(ShipConstruct), nameof(ShipConstruct.GetShipMass))]
        public class ShipConstructMassPatch
        {
            static void Postfix(ShipConstruct __instance, ref float __result, out float dryMass, out float fuelMass)
            {
                dryMass = 0.05f;
                fuelMass = 0.05f;
                __result = 0.1f;
                return;

                int count1 = __instance.parts.Count;
                while (count1-- > 0)
                {
                    Part part = __instance.parts[count1];
                    AvailablePart partInfo = part.partInfo;
                    float num2 = partInfo.partPrefab.mass + part.GetModuleMass(partInfo.partPrefab.mass, ModifierStagingSituation.CURRENT);
                    float num3 = 0.0f;
                    int count2 = part.Resources.Count;
                    while (count2-- > 0)
                    {
                        PartResource resource = part.Resources[count2];
                        PartResourceDefinition info = resource.info;
                        num3 += info.density * (float)resource.amount;
                    }
                    dryMass += num2;
                    fuelMass += num3;
                }

                // Calculate mass reduction factor
                Debug.Log("[ShipConstructMassPatch] - Calculating mass reduction...");
                WBIMassEffectCore[] massEffectCores = BlueshiftHarmonyPatcher.getActiveMassEffectCores(__instance);
                float massReductionFactor = BlueshiftHarmonyPatcher.calculateMassReduction(massEffectCores);
                if (massReductionFactor <= 0)
                    return;
                Debug.Log("[ShipConstructMassPatch] - massReductionFactor: " + massReductionFactor);

                // Calculate dryMass
                dryMass *= massReductionFactor;

                // Calculate fuelMass
                fuelMass *= massReductionFactor;

                // Calculate total mass
                __result = dryMass + fuelMass;
                if (__result < PhysicsGlobals.PartRBMassMin)
                    __result = PhysicsGlobals.PartRBMassMin;

            }
        }
        */

        /*
        [HarmonyPatch(typeof(Part), nameof(Part.UpdateMass))]
        public class PartUpdateMassPatch
        {
            static void Postfix(Part __instance, ref PartModuleList ___modules)
            {
                updatePartMass(__instance);
            }
        }

        [HarmonyPatch(typeof(Part), nameof(Part.ModulesOnFixedUpdate))]
        public class PartModulesOnFixedUpdatePatch
        {
            static void Postfix(Part __instance)
            {
                updatePartMass(__instance);
            }
        }

        /*
         * ModularFlightIntegrator.RegisterUpdateMassStatsOverride
        [HarmonyPatch(typeof(Part), nameof(Part.GetResourceMass))]
        public class PartResourceMassPatch
        {
            static void Postfix(Part __instance, ref float __result)
            {
                Debug.Log("[PartResourceMassPatch] Postfix called.");
                if (HighLogic.LoadedSceneIsEditor)
                {
                    ShipConstruct ship = EditorLogic.fetch.ship;
                    if (ship == null)
                        return;

                    WBIMassEffectCore[] massEffectCores = WBIMassEffectUtilities.getActiveCores(ship);
                    float massReductionFactor = WBIMassEffectUtilities.calculateMassReduction(massEffectCores);

                    __result *= massReductionFactor;
                }
                else if (HighLogic.LoadedSceneIsFlight)
                {
                    WBIMassEffectVesselModule vesselModule = __instance.vessel.FindVesselModuleImplementing<WBIMassEffectVesselModule>();
                    if (vesselModule == null)
                        return;

                    __result *= vesselModule.massReductionFactor;
                }
            }

            static void Postfix(Part __instance, ref float __result, out double thermalMass)
            {
                Debug.Log("[PartResourceMassPatch] Postfix Thermal called.");
                thermalMass = 0.0;
                int count = __instance.Resources.Count;
                while (count-- > 0)
                {
                    PartResource resource = __instance.Resources[count];
                    float num2 = (float)resource.amount * resource.info.density;
                    thermalMass += (double)num2 * (double)resource.info.specificHeatCapacity;
                }

                float massReductionFactor = 1f;
                if (HighLogic.LoadedSceneIsEditor)
                {
                    ShipConstruct ship = EditorLogic.fetch.ship;
                    if (ship == null)
                        return;

                    WBIMassEffectCore[] massEffectCores = WBIMassEffectUtilities.getActiveCores(ship);
                    massReductionFactor = WBIMassEffectUtilities.calculateMassReduction(massEffectCores);
                }
                else if (HighLogic.LoadedSceneIsFlight)
                {
                    WBIMassEffectVesselModule vesselModule = __instance.vessel.FindVesselModuleImplementing<WBIMassEffectVesselModule>();
                    if (vesselModule == null)
                        return;
                    massReductionFactor = vesselModule.massReductionFactor;
                }

                __result *= massReductionFactor;
            }
        }
        */
        #endregion

        #region Helpers
        private static void updatePartMass(Part part)
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                ShipConstruct ship = EditorLogic.fetch.ship;
                if (ship == null)
                    return;

                WBIMassEffectCore[] massEffectCores = WBIMassEffectUtilities.getActiveCores(ship);
                float massReductionFactor = WBIMassEffectUtilities.calculateMassReduction(massEffectCores);

                float massModifier = (part.mass + part.GetResourceMass()) * 0.99f;
                part.mass -= massModifier;

                /*
                float partMass = part.mass * massReductionFactor;
                if (partMass < PhysicsGlobals.PartRBMassMin)
                    partMass = PhysicsGlobals.PartRBMassMin;

                part.mass = partMass;
                */
            }
            else if (HighLogic.LoadedSceneIsFlight)
            {
                WBIMassEffectVesselModule vesselModule = part.vessel.FindVesselModuleImplementing<WBIMassEffectVesselModule>();
                if (vesselModule == null)
                    return;

                float massModifier = (part.mass + part.GetResourceMass()) * 0.99f;
                part.mass -= massModifier;

                /*
                float partMass = part.mass * vesselModule.massReductionFactor;
                if (partMass < PhysicsGlobals.PartRBMassMin)
                    partMass = PhysicsGlobals.PartRBMassMin;

                part.mass = partMass;
                */
            }
        }
        #endregion
    }
}