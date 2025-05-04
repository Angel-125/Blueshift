using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace Blueshift
{
    public class WBIModuleResourceCapacitor: WBIPartModule, IModuleInfo
    {
        #region Constant
        private const float messageDuration = 5f;
        #endregion

        #region Fields
        [KSPField(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "#LOC_BLUESHIFT_capacitorStatus")]
        public string status;

        [KSPField(guiActive = true, guiActiveUnfocused = true, unfocusedRange = 10, guiName = "#LOC_BLUESHIFT_capacitorCycles", guiFormat = "n0", guiUnits = "%")]
        public float dischargeCyclesPercent;

        [KSPField]
        public int dischargeCycles = 100;

        [KSPField(isPersistant = true)]
        public int dischargeCyclesRemaining = -2;

        [KSPField(isPersistant = true)]
        public bool isOperational = true;
        #endregion

        #region Housekeeping
        string statusCharging;
        string statusBroken;
        string statusIdle;
        #endregion

        #region IModuleInfo
        public string GetModuleTitle()
        {
            return Localizer.Format("#LOC_BLUESHIFT_capacitorTitle");
        }

        public Callback<Rect> GetDrawModulePanelCallback()
        {
            return null;
        }

        public string GetPrimaryField()
        {
            return Localizer.Format("#LOC_BLUESHIFT_capacitorPrimaryField", new string[1] { dischargeCycles.ToString() });
        }

        public override string GetModuleDisplayName()
        {
            return GetModuleTitle();
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_capacitorDesc"));
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_capacitorRequires"));
            int count = resHandler.inputResources.Count;
            for (int index = 0; index < count; index++)
            {
                info.AppendLine("<color=white>- " + resHandler.inputResources[index].resourceDef.displayName + ": " + resHandler.inputResources[index].amount.ToString("n2") + "</color>");
            }
            info.AppendLine(" ");
            info.AppendLine(Localizer.Format("#LOC_BLUESHIFT_capacitorProduces"));
            count = resHandler.outputResources.Count;
            for (int index = 0; index < count; index++)
            {
                info.AppendLine("<color=white>- " + resHandler.outputResources[index].resourceDef.displayName + ": " + resHandler.outputResources[index].amount.ToString("n2") + "</color>");
            }

            return info.ToString();
        }
        #endregion

        #region Events
        /// <summary>
        /// Repairs the part.
        /// </summary>
        [KSPEvent(guiName = "#LOC_BLUESHIFT_repairPart", externalToEVAOnly = false, guiActiveUnfocused = true, unfocusedRange = 10)]
        public virtual void RepairPart()
        {
            if (canRepairPart())
            {
                consumeRepairKits(FlightGlobals.ActiveVessel);
                isOperational = true;
                dischargeCyclesRemaining = dischargeCycles;
                status = statusIdle;
            }
        }
        #endregion

        #region Overrides
        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (resHandler.inputResources.Count <= 0 || resHandler.outputResources.Count <= 0)
                return;
            if (isOperational == false || dischargeCyclesRemaining <= 0)
                return;

            // Find the input resource
            ModuleResource moduleResourceInput = resHandler.inputResources[0];
            if (part.Resources.Contains(moduleResourceInput.name) == false)
                return;
            PartResource partResource = part.Resources[moduleResourceInput.name];
            if (partResource.amount <= 0)
            {
                status = statusIdle;
            }

            // If our tank isn't full then we're done
            if (partResource.amount < moduleResourceInput.amount)
            {
                if (partResource.amount <= 0)
                    status = statusIdle;
                else
                    status = statusCharging;
                return;
            }

            // We have enough, now convert
            partResource.amount -= moduleResourceInput.amount;
            if (partResource.amount < 0)
                partResource.amount = 0;

            ModuleResource outputResource = resHandler.outputResources[0];
            part.RequestResource(outputResource.id, outputResource.amount, outputResource.flowMode);

            // Now update the discharge cycles
            dischargeCyclesRemaining -= 1;
            if (dischargeCyclesRemaining <= 0)
            {
                status = statusBroken;
                isOperational = false;
                Events["RepairPart"].guiName = Localizer.Format("#LOC_BLUESHIFT_repairPart", new string[1] { part.partInfo.title });
                Events["RepairPart"].active = true;
            }
            dischargeCyclesPercent = (float)dischargeCyclesRemaining / (float)dischargeCycles * 100f;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            cacheStrings();

            if (dischargeCyclesRemaining <= -2)
            {
                status = statusIdle;
                dischargeCyclesRemaining = dischargeCycles;
            }

            dischargeCyclesPercent = (float)dischargeCyclesRemaining / (float)dischargeCycles * 100f;

            Events["RepairPart"].guiName = Localizer.Format("#LOC_BLUESHIFT_repairPart", new string[1] { part.partInfo.title });
            Events["RepairPart"].active = !isOperational;
        }
        #endregion

        #region helpers
        void cacheStrings()
        {
            statusCharging = Localizer.Format("#LOC_BLUESHIFT_capacitorCharging");
            statusBroken = Localizer.Format("#LOC_BLUESHIFT_capacitorBroken");
            statusIdle = Localizer.Format("#LOC_BLUESHIFT_capacitorIdle");
        }

        bool canRepairPart(string maintenanceSkill = "RepairSkill", int minimumSkillLevel = 1, string repairKitName = "evaRepairKit", int repairKitsRequired = 1)
        {
            // Make sure that we have sufficient skill
            if (!hasSufficientSkill(FlightGlobals.ActiveVessel, maintenanceSkill, minimumSkillLevel))
            {
                string message = Localizer.Format("#LOC_BLUESHIFT_insufficientSkill", new string[1] { minimumSkillLevel.ToString() });
                ScreenMessages.PostScreenMessage(message, messageDuration, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            // Make sure that we have sufficient repair kits.
            if (!hasEnoughRepairKits(FlightGlobals.ActiveVessel, repairKitsRequired, repairKitName))
            {
                string message = Localizer.Format("#LOC_BLUESHIFT_insufficientKits", new string[1] { repairKitsRequired.ToString() });
                ScreenMessages.PostScreenMessage(message, messageDuration, ScreenMessageStyle.UPPER_CENTER);
                return false;
            }

            // A-OK
            return true;
        }

        void consumeRepairKits(Vessel vessel, string repairKitName = "evaRepairKit", int amount = 1)
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            ModuleInventoryPart inventory;
            int count = inventories.Count;
            int repairPartsFound = 0;
            int repairPartsRemaining = amount;

            // Hack to remove expended kits when the kerbal returns from EVA...
            ProtoCrewMember astronaut = null;
            if (vessel.isEVA)
            {
                astronaut = vessel.GetVesselCrew()[0];
            }

            for (int index = 0; index < count; index++)
            {
                inventory = inventories[index];

                if (inventory.ContainsPart(repairKitName))
                {
                    repairPartsFound += inventory.TotalAmountOfPartStored(repairKitName);

                    if (repairPartsFound >= repairPartsRemaining)
                    {
                        inventory.RemoveNPartsFromInventory(repairKitName, repairPartsRemaining, true);
                        break;
                    }
                    else
                    {
                        repairPartsRemaining -= repairPartsFound;
                        inventory.RemoveNPartsFromInventory(repairKitName, repairPartsFound, true);
                    }
                }
            }
        }

        private bool hasEnoughRepairKits(Vessel vessel, int repairKitsRequired, string repairKitName = "evaRepairKit")
        {
            List<ModuleInventoryPart> inventories = vessel.FindPartModulesImplementing<ModuleInventoryPart>();
            int count = inventories.Count;
            int repairPartsFound = 0;

            for (int index = 0; index < count; index++)
            {
                if (inventories[index].ContainsPart(repairKitName))
                {
                    repairPartsFound += inventories[index].TotalAmountOfPartStored(repairKitName);
                    if (repairPartsFound >= repairKitsRequired)
                        return true;
                }
            }

            return false;
        }

        private bool hasSufficientSkill(Vessel vessel, string maintenanceSkill, int minimumSkillLevel)
        {
            ProtoCrewMember astronaut;
            int highestSkill = 0;

            // Make sure that we have sufficient skill.
            if (vessel.isEVA)
                highestSkill = getHighestRank(vessel, maintenanceSkill, out astronaut);
            else
                highestSkill = getHighestRank(vessel, maintenanceSkill, out astronaut);

            if (highestSkill < minimumSkillLevel)
                return false;

            return true;
        }

        public int getHighestRank(Vessel vessel, string skillName, out ProtoCrewMember astronaut)
        {
            astronaut = null;
            if (string.IsNullOrEmpty(skillName))
                return 0;
            try
            {
                if (vessel.GetCrewCount() == 0)
                    return 0;
            }
            catch
            {
                return 0;
            }

            string[] skillsToCheck = skillName.Split(new char[] { ';' });
            string checkSkill;
            int highestRank = 0;
            int crewRank = 0;
            bool hasABadass = false;
            bool hasAVeteran = false;
            bool hasAHero = false;
            for (int skillIndex = 0; skillIndex < skillsToCheck.Length; skillIndex++)
            {
                checkSkill = skillsToCheck[skillIndex];

                //Find the highest racking kerbal with the desired skill (if any)
                ProtoCrewMember[] vesselCrew = vessel.GetVesselCrew().ToArray();
                for (int index = 0; index < vesselCrew.Length; index++)
                {
                    if (vesselCrew[index].HasEffect(checkSkill))
                    {
                        if (vesselCrew[index].isBadass)
                            hasABadass = true;
                        if (vesselCrew[index].veteran)
                            hasAVeteran = true;
                        if (vesselCrew[index].isHero)
                            hasAHero = true;
                        crewRank = vesselCrew[index].experienceTrait.CrewMemberExperienceLevel();
                        if (crewRank > highestRank)
                        {
                            highestRank = crewRank;
                            astronaut = vesselCrew[index];
                        }
                    }
                }
            }

            if (hasABadass)
                highestRank += 1;
            if (hasAVeteran)
                highestRank += 1;
            if (hasAHero)
                highestRank += 1;

            return highestRank;
        }
        #endregion
    }
}
