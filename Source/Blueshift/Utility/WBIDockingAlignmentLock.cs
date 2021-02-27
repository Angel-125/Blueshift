using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

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
    /// <summary>
    /// A simple helper class to lock the docking alignment.
    /// </summary>
    public class WBIDockingAlignmentLock: WBIPartModule
    {
        /// <summary>
        /// Toggles docking alignment to locked/unlocked.
        /// </summary>
        [KSPField(guiActive = true, isPersistant = true, guiName = "Docking Alignment")]
        [UI_Toggle(enabledText = "Locked", disabledText = "Unlocked")]
        public bool lockAlignment = false;

        ModuleDockingNode dockingNode;
        float captureMinRollDot;
        float snapOffset;

        public override void OnUpdate()
        {
            base.OnUpdate();

            if (dockingNode != null)
            {
                dockingNode.snapRotation = lockAlignment;
                dockingNode.captureMinRollDot = lockAlignment ? 0.99996192f : captureMinRollDot;
                dockingNode.snapOffset = lockAlignment ? 30f : snapOffset;
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            dockingNode = part.FindModuleImplementing<ModuleDockingNode>();
            if (dockingNode != null)
            {
                captureMinRollDot = dockingNode.captureMinRollDot;
                snapOffset = dockingNode.snapOffset;
            }
        }

    }
}
