Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

New Parts

- S1 Bussard Collector: This part collects Graviolium in the depths of space. It also works with SpaceDust.
- S2 Bussard Collector: This part collects Graviolium in the depths of space. It also works with SpaceDust.
- S1 Plasma Vent: This part reduces Static Charge built up in the ship by expending Xenon Gas if you have Kerbal Flying Saucers installed. Otherwise it just looks cool.
- S2 Plasma Vent: This part reduces Static Charge built up in the ship by expending Xenon Gas if you have Kerbal Flying Saucers installed. Otherwise it just looks cool.
- Miniature Jumpgate: This miniaturized jumpgate has limited range and has a limit to the mass that it can transport, but it can be used on a celestial body as well as in space. Plus, the gate's vessel pays the graviolium toll, not the traveler.
- Mini Jumpgate Platform: This platform is designed for use on planetary surfaces and to support the Miniature Jumpgate

Module Manager Patches

- SpaceDust fix- Thanks Rakete and Grimmas!
- Added ExpensiveDrives patch to increase the cost of warp tech. This is optional and found in the Blueshift/Extras folder. Just rename it from .txt to .cfg to use it. Thanks Grimmas!
- Fixed tech tree node positions for Community Tech Tree and Unkerballed Start.
- Added B9PS support to the Far Future Technologies patch.

Part Modules

- WBIModuleAnimation: This animation module lets you loop animations, play the animation in forward/reverse, and adjust the emissive textures on desired model transforms. Its state can be synchronized with a warp engine on the vessel and/or with a converter on the part if desired.

- WBIWarpSpeedHarvester: This specialized resource harvester collects resources while the vessel is at warp. Resource distributions are defined by INTERSTELLAR_DISTRIBUTION, GLOBAL_INTERPLANETARY_DISTRIBUTION, and INTERPLANETARY_DISTRIBUTION config nodes. Check out WildBlueIndustries/Blueshift/Resources/ResourceDefinitions.cfg for examples.

- WBIResourceVent: This is a souped up version of Kerbal Flying Saucers' WBIResourceDischarger. It is affected by warp engines.

- WBIJumpgate: Implemented ability to jump a vessel to a gate on the ground. Disabled the portal trigger when in the editor.

- WBIWarpEngine: Updated max warp speed calculations. Added Planetary Speed Brake toggle button. When enabled, the ship's max speed is limited based on how far down the planet's gravity well it is. When disabled, the ship can travel at interplanetary speeds within interplanetary space. Use with caution.

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021 by Michael Billard (Angel-125)

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