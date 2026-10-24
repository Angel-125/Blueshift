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

- Mk2 Fusion Thruster: A compact, gravity-catalyzed fusion reactor superheats its reaction mass to generate thrust with high specific impulse while also powering a built-in, miniaturized FlexGrav system.

Changes

- Fixed the Warp Travel Planner throwing an exception when a required generator fuel has no tank aboard the craft. Missing or empty fuel now reports the resource name and zero burn time in the editor and in flight.
- Updated Far Future Technologies compatibility for the S1 warp core to use liquid deuterium, and for the S0 endcap to remove Fusion Pellets and offer the existing deuterium/Graviolium tank options when B9 Part Switch is available without Flying Saucers.
- Jump gates now honor semicolon-separated network addresses, combine destinations without duplicates, and preserve all network memberships when loading a save. Changing addresses replaces the gate's previous memberships and refreshes its CommNet links.
- Added Change Network Address to player-built jump gates' part menus in the editor and in flight. Apply saves the new address and refreshes destinations; Cancel keeps the current address. Address changes reset the current gate connection. Anomaly gates retain their fixed network addresses.
- The network address dialog locks controls while open and releases its lock on Apply, Cancel, dismissal, vessel switching, or part/module cleanup.
- Added an 8-second warning when changing an address in flight leaves the gate as the only member of its new network. For multiple addresses, the check counts distinct gates across all listed networks.
- Changing a gate's address to a network with exactly two gates automatically selects the other gate and runs the normal startup sequence, including startup effects and destination toll calculation. Two-gate connections ignore the jump range limit.

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
