Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

New Parts

- Mk2 Bussard Collector: This collector collects particles while at warp similar to the S1 and S2 Bussard Collectors.

- Mk2 Plasma Vent: This part vents plasma and eliminates Static Charge when Kerbal Flying Saucers is installed. Without it, it just looks cool.

WBIJumpGate

- Jumpgates will no longer automatically activate when there are only two gates in the network unless the autoActivate flag in WBIJumpGate is set.

- Added the ability to switch vesel focus back to the source jumpgate after making the jump. This makes it easier to bring multiple vessels through the gate.

WBIWarpEngine & Settings

- Added warpEngineerSkill to the global settings.cfg file, and changed the default value for WBIWarpEngine's warpEngineerSkill to an empty value. If WBIWarpEngine's warpEngineerSkill is defined, then it will override the global warpEngineerSkill value.

- Added warpSpeedBoostRank to the global settings.cfg file, and changed the default value for WBIWarpEngine's warpSpeedBoostRank to -1. If WBIWarpEngine's warpSpeedSkiwarpSpeedBoostRank is greater than 0, then it will override the global warpSpeedBoostRank value.

- Added warpSpeedSkillMultiplier to the global settings.cfg file, and changed the default value for WBIWarpEngine's warpSpeedSkillMultiplier to -1. If WBIWarpEngine's warpSpeedSkillMultiplier is greater than 0, then it will override the global warpSpeedSkillMultiplier value.

Fixes

- Fixed NREs generated during part loading.
- Fixed texture issues with mk2 warp tech parts.

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021-2022 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2022 by Michael Billard (Angel-125)

    This source code is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.