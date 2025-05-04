Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

HOW TO FIX MISSING PART MODULES ERROR
This update renames a number of Wild Blue Industries' part modules and may cause KSP to complain when you try to load your craft files.
To fix this issue, follow the steps here: https://github.com/Angel-125/WildBlueCore/wiki/How-To-Fix-Missing-Part-Modules-Warning

New Parts

- S3Mk3 Warp Ring: This large warp ring can be surface-attached to Size 3 and Mk3 parts and offers a few pylon options.
- SC-400 Static Charge Converter: An advanced alternative to Plasma Vents/Plasma Contactors, this part converts Static Charge into Electric Charge- at least until it breaks and has to be either repaired or replaced.

Available when Kerbal Flying Saucers isn't installed:
- Stardust Graviolium Collector: Collects dust-form graviolium from the orbits of planets.
- Plasma Contactor: Based on the real-world plasma contactor used on the ISS, this device discharges Static Charge. It does so automatically when in an atmosphere or on the ground, and it uses Xenon Gas when in space.
- Plasma Contactor (Large): Larger version of the above.
- Plasma Contactor Module: Even larger version of the above. It's a 2.5m station module to allow starships to discharge while docked to a station.

Changes

- Updated the Wiki: RTFM!
- Added new Warp Range app. It's like a delta-v calculator, but for warp ships. It's available in the VAB, SPH, and during flight. You'll find it in the list of app buttons.
- Warp Coils now display their Warp Capacity and Displacement Impulse in the VAB/SPH.
- Updated warp effects to reduce the "neon tube" look. Don't like the new look? Check out the Extras folder for the original config.
- If left undefined in the settings file, Blueshift will calculate the distance of a light-year based on the Sidereal Year of the home world (a.k.a. length of a year) and the speed of light.
- Tweaked the performance specs on various warp tech and jump tech parts.
- You can now install Astria Porta Auxilium segments on an Astria Porta without needing to switch focus to the Astria Porta beforehand.
- Static Charge is now ever present on warp tech parts. If your vessel's Static Charge reaches its maximum, then warp engines will flame out. Be sure to equip your ship with Plasma Contactors/Plasma Vents, or your engines will shut down.
- jumpMaxDimensions for jumpgates are back! Thanks Sarbian for showing me how to properly calculate vessel dimensions in flight. :) Dimension limits can be disabled in the Blueshift Settings (it's off by default).
- Jumpgates now charge based on vessel mass AND distance for interstellar distances. This is in preparation for increasing the Graviolium toll to initiate jumps to bring the cost in line with warp tech.
- Removed Supercharger from warp engines; it didn't offer much of a performance boost and it complicated resource calculations.
- Added Radial Mounts part variants to the S2 and S3 Warp Cores.

Bug Fixes

- Fixed issue where vessels could not warp during time warp with Warp Dragging enabled, and no other vessels were in physics range.
- Fixed issues with surface attachment nodes on some parts.
- Fixed issues with Multicoil parts not correctly reporting their warp capacity & displacement impulse in the VAB/SPH.
- Fixed issue with the Mk2 Multicoil part's warp plasma animations going the wrong way.
- Fixed issue with the warp engine bow shock not aligning with the vessel properly in some situations.
- Fixed issue where multi-coil parts weren't increasing their displacement impulse after applying a part variant.
- Fixed issues where the Alien Jumpgate and the KFS UFO anomaly are uncontrollable when claimed by the player.
- Fixed missing resource entries for Electro Plasma to several parts with fusion reactors.

---LICENSE---
Art Assets, including .mu, .png, and .dds files are copyright 2021-2025 by Michael Billard, All Rights Reserved.

Wild Blue Industries is trademarked by Michael Billard. All rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

Source code copyright 2021-2024 by Michael Billard (Angel-125)

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