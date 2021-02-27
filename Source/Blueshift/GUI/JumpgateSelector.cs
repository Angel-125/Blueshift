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
    /// Callback to indicate that a jumpgate was selected
    /// </summary>
    /// <param name="destinationGate">A Vessel representing the destination.</param>
    public delegate void GateSelectedDelegate(Vessel destinationGate);

    /// <summary>
    /// A simple dialog to select a jumpgate destination from.
    /// </summary>
    public class JumpgateSelector : Dialog<JumpgateSelector>
    {
        /// <summary>
        /// List of jumpgates to select from.
        /// </summary>
        public List<Vessel> jumpgates;

        /// <summary>
        /// Title of the selection dialog.
        /// </summary>
        public string titleText = "Select Jumpgate";

        /// <summary>
        /// Jumpgate selection message.
        /// </summary>
        public string selectionMessage = "Select the jump gate destination";

        /// <summary>
        /// Jumpgate select button title.
        /// </summary>
        public string selectButtonTitle = "Set Destination";

        /// <summary>
        /// Gate selected delegate.
        /// </summary>
        public GateSelectedDelegate gateSelectedDelegate;

        private Vector2 _scrollPos;
        private Vessel selectedVessel;

        public JumpgateSelector() :
        base("Select Jumpgate", 380, 480)
        {
            Resizable = false;
            WindowTitle = titleText;
        }

        protected override void DrawWindowContents(int windowId)
        {
            if (jumpgates == null || jumpgates.Count == 0)
            {
                return;
            }

            GUILayout.Label("<color=white>" + selectionMessage + "</color>");
            if (selectedVessel != null)
                GUILayout.Label("<color=white>" + selectedVessel.vesselName + "</color>");
            else
                GUILayout.Label(" ");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            int count = jumpgates.Count;
            for (int index = 0; index < count; index++)
            {
                if (GUILayout.Button(jumpgates[index].vesselName))
                {
                    selectedVessel = jumpgates[index];
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button(selectButtonTitle) && gateSelectedDelegate != null)
            {
                gateSelectedDelegate(selectedVessel);
                SetVisible(false);
            }
        }
    }
}
