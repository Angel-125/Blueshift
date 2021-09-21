using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Blueshift.EVARepairs
{
    internal class EVARepairsWrapper
    {
        const string kAssemblyName = "EVARepairs";

        public static Assembly assembly;

        public static void Init()
        {
            // Get the assembly
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name == kAssemblyName)
                {
                    assembly = loadedAssembly.assembly;
                    break;
                }

            }
            if (assembly == null)
                return;

            // Now init the classes
            BSModuleEVARepairs.InitClass(assembly);
        }

        public static bool IsInstalled()
        {
            Init();
            return assembly != null;
        }
    }

    internal class BSModuleEVARepairs
    {
        const string kEVARepairsModuleName = "ModuleEVARepairs";
        const string kSetRateMultiplierName = "SetRateMultiplier";
        const string kUpdateMTBFName = "UpdateMTBF";

        static Type typeModuleEVARepairs;
        static MethodInfo miSetRateMultiplier;
        static MethodInfo miUpdateMTBF;

        public PartModule partModule;

        public static void InitClass(Assembly assembly)
        {
            typeModuleEVARepairs = assembly.GetTypes().First(t => t.Name.Equals(kEVARepairsModuleName));

            miSetRateMultiplier = typeModuleEVARepairs.GetMethod(kSetRateMultiplierName, new[] { typeof(double) });
            miUpdateMTBF = typeModuleEVARepairs.GetMethod(kUpdateMTBFName, new[] { typeof(double) });
        }

        public static BSModuleEVARepairs GetPartModule(Part part)
        {
            int count = part.Modules.Count;
            PartModule module;

            for (int index = 0; index < count; index++)
            {
                module = part.Modules[index];
                if (module.moduleName == kEVARepairsModuleName)
                {
                    BSModuleEVARepairs evaRepairs = new BSModuleEVARepairs(module);
                    return evaRepairs;
                }
            }

            return null;
        }

        public BSModuleEVARepairs(PartModule module)
        {
            if (EVARepairsWrapper.assembly == null)
                EVARepairsWrapper.Init();

            partModule = module;
        }

        public void SetRateMultiplier(double rateMultiplier)
        {
            if (miSetRateMultiplier == null)
                return;

            miSetRateMultiplier.Invoke(partModule, new object[] { rateMultiplier });
        }

        public void UpdateMTBF(double elapsedTime)
        {
            if (miUpdateMTBF == null)
                return;

            miUpdateMTBF.Invoke(partModule, new object[] { elapsedTime });
        }
    }
}
