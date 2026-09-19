Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift
	FireflyAPI

HarmonyKSP (GameData/000_Harmony) is required.

---CHANGES---

- Fixed parts with multiple WBIAnimatedTexture modules loading all animated texture sequences from the first module instead of their individual moduleID configurations.
- Rebalanced Graviolium belts and collectors around a 30-Kerbin-day peak collection target for a GR-375 tank and the S2 Bussard Collector, with matching collection rates whether or not SpaceDust is installed. Smaller collectors now scale consistently by intake area, while Stardust uses a single rescale-aware harvesting path when SpaceDust is installed.
- Same-network jump gates now provide interstellar CommNet links while vessel-to-gate connections retain normal short-range behavior.
- Added WBICommNetLinkOptions so specifically configured relays can bypass CommNet range, occlusion, or both.
- WBI warp engines, warp coils, resource generators, and gravity generators now honor the Infinite Propellant and Infinite Electricity debug options.
- Fixed FlexGrav generators applying 105% of vessel mass instead of the configured EC mass percentage.
- Fixed nearby warp-capable vessels sharing or resetting the active vessel's throttle.
- Fixed multi-coil warp capacity and displacement impulse resetting after launch, reload, or editor state changes.
- Fixed warp cores not registering multi-coils while constructing vessels in the VAB or SPH.
- Fixed exiting timewarp removing orbital velocity and EVA Repairs failures leaving vessels stuck on rails.
- Fixed duplicate Far Future Technologies Graviolium harvesters when Flying Saucers is installed.
- Localized all shipped Blueshift part titles and descriptions.
- Fixed Blueshift fuel tanks and endcaps failing to occlude attached parts from aerodynamic drag.
- WBIAnimatedTexture now checks for moduleID when looking for its part config node.

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
