Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

Resources

- The resource requirements to power warp tech parts will scale upwards when traveling in interstellar space, but retain their minimum power requirements when in interplanetary or planetary space. To accomplish this, WBIWarpEngine adds two new fields: 
	warpPowerGeneratorID: controls WBIModuleGeneratorFX when its moduleID matches this value.
	interstellarPowerMultiplier: Multiplies resource consumption and productions rates. Defaults to 10.

- Added conversion from Ore to Fusion Pellets to the stock Size 2 ISRU (available if you don't have Wild Blue Tools & Far Future Technologies installed, which have their own resource chains).

Warp Travel

- To gradually accelerate to interstellar speed, engines now have an interstellarAccelerationCurve, which is explained below:
Whenever you cross into interstellar space, or are already in interstellar space and throttled down, then this acceleration curve will be applied. 
The warp speed will be max warp speed * curve's speed modifier, and it is affected by how long ago you made a speed change.
The first number represents the number of seconds since crossing the boundary or throttling up, and the second number is the multiplier.
We don't apply this curve when going from interstellar to interplanetary space, so you'll slow down pretty quick.
You can override the engine's interstellarAccelerationCurve, but this the default curve:
interstellarAccelerationCurve
{
	key = 0 0.001
	key = 5 0.01
	key = 7 0.1
	key = 9 0.5
	key = 10 1
}
TIP: warp engines have a Thrust Limiter just like any other engine, and it will affect your top speed along with your throttle setting.

- Changed "Auto-circularize orbit after warp" game option to "Enable circularization helper." This change enables the "Auto-circularize orbit" button in the warp engine's Part Action Window that will auto-circularize orbit the starship's orbit- but it still costs Graviolium to do so.
- The "Auto-circularize orbit" button can be mapped to an action key.
- Removed the "autoCircularizationDelay" field from Settings.cfg.

Far Future Technologies
- Warp engines, warp cores, and gravitic generators will use Liquid Deuterium instead of Fusion Pellets when Far Future Technologies is installed.
- Stand-alone fusion reactors are removed when Far Future Technologies is installed.
- S2 and S3 FTL tanks are hidden when Far Future Technologies is installed. Use the FFT tanks instead. The dedicated Graviolium tanks and the Mk2, S1 Endcap, and S2 Endcap tanks are still available.

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