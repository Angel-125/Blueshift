Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift
	FireflyAPI

IMPORTANT NOTE:

Sandcastle now requires Harmony for KSP. Be sure to download Harmony for KSP before downloading the latest Blueshift.
You can find it here: https://github.com/KSPModdingLibs/HarmonyKSP
And on CKAN.

---CHANGES---

New Parts

- Graviolum Field Resonator: A wearable gravitic field generator that uses polarized graviolium to temporarily reduce, redirect, or counteract the apparent weight of nearby objects. Field strength and recovery time vary considerably with target mass. 
*Powers*
When the Resonator is enabled, the wearer gains the following powers. Use of each power requires a cooldown before the same or a different power can be used. The length of the cooldown depends upon the power used, the target(s) of the power, and environmental factors. Some power effects can be combined...
Heft: Multiplies the maximum mass that the kerbal can lift during EVA Construction.
Levitate: Levitates the kerbal at 0.2 m/s. Can't be used in microgravity.
Push: Pushes an object away from the kerbal with a 0.5 kN force. Click and hold the mouse button to push the object.
Pull: Pulls an object towards the kerbal with a 0.5 kN force. Click and hold the mouse button to pull the object.
Lift: Lifts the target object upwards at a rate of 0.1 m/s. Click once to lift the desired object. Lasts 10 seconds, or until the power is disabled. Can't be used in microgravity.
Shove: Shoves the target object away from the kerbal with a force of 50 kN. Click the target object once to shove it away.
Launch: Launches the target object upward with a force of 50 kN. Click the target object once to launch it into the sky. Can't be used in microgravity.
Suspension Field: All objects within a 7 meter radius slowly rise upward at a rate of 0.1 m/s. The effect lasts 10 seconds, or until the power is disabled. Can't be used in microgravity.

*Powers By Experience Level*
As the kerbal gains experience stars, they gain more powers:
0-Stars: Heft, Push
1-Star: Pull
2-Stars: Levitate, Lift
3-Stars: Shove
4-Stars: Launch
5-Stars: Suspension Field

Given that this device is pure fantasy, you can disable it by renaming the NoFieldResonator.txt file in the Extras folder.

Changes

- Rebalanced Graviolium belts and collectors around a 30-Kerbin-day peak collection target for a GR-375 tank and the S2 Bussard Collector, with matching collection rates whether or not SpaceDust is installed. Smaller collectors now scale consistently by intake area, while Stardust uses a single rescale-aware harvesting path when SpaceDust is installed.
- Added WBICommNetLinkOptions so specifically configured relays can bypass CommNet range, occlusion, or both.
- Same-network jump gates now provide interstellar CommNet links while vessel-to-gate connections retain normal short-range behavior.
- Reworked the QE-1 Quantum Entanglement Transceiver. It has a combinable local relay range equal to a stock RA-2 Relay Antenna, but can link up with other Quantum Entanglement Transceivers to provide unlimited communications range. It's also blacklisted from printing by Sandcastle's printers, but you'll find a way to make it work in the Extras folder.
- Warp engines, warp coils, resource generators, and gravity generators now honor the Infinite Propellant and Infinite Electricity debug options.
- Localized all shipped Blueshift part titles and descriptions.
- Fixed FlexGrav generators applying 105% of vessel mass instead of the configured EC mass percentage.
- Fixed nearby warp-capable vessels sharing or resetting the active vessel's throttle.
- Fixed multi-coil warp capacity and displacement impulse resetting after launch, reload, or editor state changes.
- Fixed warp cores not registering multi-coils while constructing vessels in the VAB or SPH.
- Fixed exiting timewarp removing orbital velocity and EVA Repairs failures leaving vessels stuck on rails.
- Fixed duplicate Far Future Technologies Graviolium harvesters when Flying Saucers is installed.
- Fixed Blueshift fuel tanks and endcaps failing to occlude attached parts from aerodynamic drag.
- Fixed parts with multiple WBIAnimatedTexture modules loading all animated texture sequences from the first module instead of their individual moduleID configurations.

---END CHANGES---

---LICENSE---

Sounds effects courtesy of Pond5 and may NOT be redistributed.

Art Assets, including .mu, .png, and .dds files are copyright 2021-2026 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a fictitious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2026 by Michael Billard (Angel-125)

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
