using System.Reflection;

namespace Blueshift
{
    public class SDWrapper
    {
        public static Assembly spaceDustAssembly;

        public static void Init()
        {
            //Get the assembly
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name == "SpaceDust")
                {
                    spaceDustAssembly = loadedAssembly.assembly;
                    break;
                }

            }
            if (spaceDustAssembly == null)
                return;

            //Now init the classes
            SDModuleSpaceDustHarvester.InitClass(spaceDustAssembly);
        }

        public static bool IsInstalled()
        {
            WFWrapper.Init();
            return WFWrapper.waterfallAssembly != null;
        }
    }
}
