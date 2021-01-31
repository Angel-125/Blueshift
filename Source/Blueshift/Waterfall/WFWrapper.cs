using System.Reflection;

/*
Source code copyright 2021, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace Blueshift
{
    public class WFWrapper
    {
        public static Assembly waterfallAssembly;

        public static void Init()
        {
            //Get the assembly
            foreach (AssemblyLoader.LoadedAssembly loadedAssembly in AssemblyLoader.loadedAssemblies)
            {
                if (loadedAssembly.name == "Waterfall")
                {
                    waterfallAssembly = loadedAssembly.assembly;
                    break;
                }

            }
            if (waterfallAssembly == null)
                return;

            //Now init the classes
            WFModuleWaterfallFX.InitClass(waterfallAssembly);
        }

        public static bool IsInstalled()
        {
            WFWrapper.Init();
            return WFWrapper.waterfallAssembly != null;
        }
    }
}
