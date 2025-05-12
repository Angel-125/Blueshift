using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace Blueshift.MassTech
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    sealed class WBIDampeningFieldLoader : MonoBehaviour
    {
        class DampeningFieldLoader: PartLoader
        {
            public override bool IsReady()
            {
                return true;
            }

            public override void StartLoad()
            {
                int count = PartLoader.LoadedPartsList.Count;
                AvailablePart availablePart;

                // Create the blacklist.
                ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("DAMPENER_BLACKLISTED_PARTMODULES");
                ConfigNode node;
                string[] partNameValues;
                string moduleBlacklist = string.Empty;
                for (int index = 0; index < nodes.Length; index++)
                {
                    node = nodes[index];
                    if (node.HasValue("moduleName"))
                    {
                        partNameValues = node.GetValues("moduleName");
                        if (partNameValues.Length > 0)
                        {
                            for (int partNameIndex = 0; partNameIndex < partNameValues.Length; partNameIndex++)
                                moduleBlacklist += partNameValues[partNameIndex] + ";";
                        }
                    }
                }
                Debug.Log("[Blueshift] - Blacklisted modules: " + moduleBlacklist);

                int moduleCount;
                bool isBlacklisted = false;
                for (int index = 0; index < count; index++)
                {
                    // Get the available part
                    availablePart = PartLoader.LoadedPartsList[index];

                    // Skip the part if it already has an inertial dampening field or it has no engine module or it's a warp engine.
                    if (availablePart.partPrefab.HasModuleImplementing<WBIInertialDampeningField>() || availablePart.partPrefab.HasModuleImplementing<ModuleEngines>() == false || availablePart.partPrefab.HasModuleImplementing<WBIWarpEngine>())
                    {
                        Debug.Log("[Blueshift] - Skipping blacklisted part " + availablePart.name);
                        continue;
                    }

                    // Skip part if it has a module on the blacklist.
                    moduleCount = availablePart.partPrefab.Modules.Count;
                    isBlacklisted = false;
                    for (int moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
                    {
                        if (moduleBlacklist.Contains(availablePart.partPrefab.Modules[moduleIndex].moduleName))
                        {
                            Debug.Log("[Blueshift] - Skipping blacklisted part " + availablePart.name);
                            isBlacklisted = true;
                            break;
                        }
                    }
                    if (isBlacklisted)
                        continue;

                    availablePart.partPrefab.AddModule("WBIInertialDampeningField", true);
                }
            }

            public override string ProgressTitle()
            {
                return Localizer.Format("#LOC_BLUESHIFT_partLoaderTitle");
            }
        }

        public void Awake()
        {
            List<LoadingSystem> loaders = LoadingScreen.Instance.loaders;
            if (loaders != null)
            {
                int count = loaders.Count;
                for (int index = 0; index < count; index++)
                {
                    if (loaders[index] is PartLoader)
                    {
                        GameObject gameObject = new GameObject();
                        DampeningFieldLoader modulesLoader = gameObject.AddComponent<DampeningFieldLoader>();
                        loaders.Insert(index + 1, modulesLoader);
                        break;
                    }
                }
            }
        }
    }
}
