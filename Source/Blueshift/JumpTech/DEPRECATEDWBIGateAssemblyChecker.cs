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
    /// A handy class for making sure that a jumpgate is fully assembled.
    /// </summary>
    public class WBIGateAssemblyChecker: WBIPartModule
    {
        /// <summary>
        /// Total number of segments to check.
        /// </summary>
        [KSPField]
        public int totalSegments = 10;

        /// <summary>
        /// Name of the node to check for other gate segments.
        /// </summary>
        [KSPField]
        public string primaryNodeName = string.Empty;

        /// <summary>
        /// Name of the node to check for other gate segments.
        /// </summary>
        [KSPField]
        public string secondaryNodeName = string.Empty;

        WBIJumpGate jumpgateController;
        int partCount = -1;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            jumpgateController = part.FindModuleImplementing<WBIJumpGate>();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (jumpgateController == null)
                return;
            if (partCount == vessel.parts.Count)
                return;

            partCount = vessel.parts.Count;

            // Make sure that segments are connected to each other.
            checkForCompleteAssembly();
        }

        private void checkForCompleteAssembly()
        {
            WBIGateAssemblyChecker[] checkers = vessel.FindPartModulesImplementing<WBIGateAssemblyChecker>().ToArray();

            // Check total required segments.
            if (checkers.Length < totalSegments)
            {
                disableJumpController();
                return;
            }

            // Make sure that segments are connected to each other.
            WBIGateAssemblyChecker checker;
            bool primaryNodeSegmentFound = false;
            bool secondaryNodeSegmentFound = false;
            int foundSegments = 0;
            for (int index = 0; index < checkers.Length; index++)
            {
                checker = checkers[index];

                AttachNode attachNode = checker.part.FindAttachNode(primaryNodeName);
                if (attachNode == null)
                {
                    disableJumpController();
                    return;
                }
                primaryNodeSegmentFound = findSegment(attachNode.attachedPart);

                attachNode = checker.part.FindAttachNode(secondaryNodeName);
                if (attachNode == null)
                {
                    disableJumpController();
                    return;
                }
                secondaryNodeSegmentFound = findSegment(attachNode.attachedPart);

                if (primaryNodeSegmentFound || secondaryNodeSegmentFound)
                    foundSegments += 1;
            }

            if (foundSegments >= totalSegments)
                enableJumpController();
            else
                disableJumpController();
        }

        private bool findSegment(Part attachedPart)
        {
            if (attachedPart == null)
                return false;

            // Skip docking ports
            WBIGateAssemblyChecker checker;
            ModuleDockingNode dockingNode = attachedPart.FindModuleImplementing<ModuleDockingNode>();
            if (dockingNode != null && dockingNode.otherNode != null)
            {
                AttachNode[] nodes = dockingNode.otherNode.part.attachNodes.ToArray();
                for (int index = 0; index < nodes.Length; index++)
                {
                    if (nodes[index].attachedPart == null)
                        continue;

                    dockingNode = nodes[index].attachedPart.FindModuleImplementing<ModuleDockingNode>();
                    if (dockingNode != null)
                        continue;

                    return findSegment(nodes[index].attachedPart);
                }
            }

            // Empty docking port? Then can't find segment.
            else if (dockingNode != null)
            {
                return false;
            }

            // No docking ports, so see if the attached part has a checker.
            checker = attachedPart.FindModuleImplementing<WBIGateAssemblyChecker>();

            return checker != null;
        }

        private void enableJumpController()
        {
            jumpgateController.SetGateEnabled(true);
        }

        private void disableJumpController()
        {
            jumpgateController.SetGateEnabled(false);
        }
    }
}
