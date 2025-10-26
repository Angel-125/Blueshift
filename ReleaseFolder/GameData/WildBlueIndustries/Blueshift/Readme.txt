Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift
	FireflyAPI

Changes
- Lifted restriction on adding Graviolium to fuel tanks in the editor when using OmniStorage.
- Added lighting effects to the jump gates.
- Added lightIntensity field to the WBIJumpGate part module. It controls how bright to make the jump gate lights. The default value is 5.0.
- Added new EFFECT entries to the jump gates to play sounds upon gate startup and when a vessel is teleported. If the default sounds in the mod aren't desirable, there's always these (that I can't distribute): https://archive.org/details/stargate-worlds-sound-effects
- SPACE_ANOMALY: The spawnMode = fixedOrbit now supports randomly generated fixedSMA and fixedEccentricity. 
  Set fixedSMA = -1, and Blueshift will generate a random orbit between planet Radius + atmosphericDepth (if any) and planet sphereOfInfluence.
  Set fixedEccentricity = -1, and Blueshift will generate a random eccentricity between 0 and 1.

Extras
This folder contains optional Module Manager patches to enhance Blueshift.

- SampleCustomGateSoundsPatch: This patch makes it easy to replace the default jumpgate sound effects with custom ones. You'll need to convert any sound clips to ogg format (Audacity, which is free, can do this).
- JumpgatesElectricChargeToll: This patch swaps out the Graviolium requirements of jump gates to instead require Electric Charge.

Bug Fixes

- Removed prototype S1 warp core.
- Fixed performance issues when a craft has multiple warp engines but only one is running.
- Fixed issue- FINALLY!- where kerbals who travel through jump gates to a destination gate in space are stuck in walking mode.

---LICENSE---

Sounds effects courtesy of Pond5 and may NOT be redistributed.

Art Assets, including .mu, .png, and .dds files are copyright 2021-2025 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2025 by Michael Billard (Angel-125)

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