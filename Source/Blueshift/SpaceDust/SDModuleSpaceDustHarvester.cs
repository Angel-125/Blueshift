using System;
using System.Linq;
using System.Reflection;

namespace Blueshift
{
    public class SDModuleSpaceDustHarvester
    {
        static Type typeModuleSpaceDustHarvester;
        static FieldInfo fiEnabled;

        public PartModule partModule;

        public static void InitClass(Assembly assembly)
        {
            typeModuleSpaceDustHarvester = assembly.GetTypes().First(t => t.Name.Equals("ModuleSpaceDustHarvester"));

            fiEnabled = typeModuleSpaceDustHarvester.GetField("Enabled");
        }

        public static SDModuleSpaceDustHarvester GetHarvesterModule(Part part)
        {
            {
                int count = part.Modules.Count;
                PartModule module;

                for (int index = 0; index < count; index++)
                {
                    module = part.Modules[index];
                    if (module.moduleName == "ModuleSpaceDustHarvester")
                    {
                        SDModuleSpaceDustHarvester harvesterModule = new SDModuleSpaceDustHarvester(module);
                        return harvesterModule;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Instantiates a new SDModuleSpaceDustHarvester
        /// </summary>
        /// <param name="module">The PartModule representing the FX module.</param>
        public SDModuleSpaceDustHarvester(PartModule module)
        {
            if (SDWrapper.spaceDustAssembly == null)
                SDWrapper.Init();

            partModule = module;
        }

        public bool Enabled
        {
            get
            {
                return (bool)fiEnabled.GetValue(partModule);
            }
        }
    }
}
