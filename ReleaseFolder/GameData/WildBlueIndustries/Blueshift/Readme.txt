Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

Warp Tech

IMPORTANT NOTE: This update will affect the warp performance of existing craft. Unfortunately this is unavoidable. [REDACTED] apologizes for the inconvenience.

New Part

- S-3 Heavy Warp Sustainer: This part is a larger version of the S-3 Warp Sustainer that has greater Warp Capacity and a more powerful gravitic generator.

Changes

- Redesigned how warp speed is calculated in order to fix issues where adding additional generators and warp sustainers would decrease max speed. With this redesign:
  - Warp Coils determine how much mass can be displaced at warp. The more you have and/or the larger the coil, the more mass you can displace.
  - You can also add more generators to overcharge the warp coils and gain speed, but they will wear out faster if you have maintenance enabled.
  - As before, if your ship is more massive than the total warp displacement, then you'll move slower, but less massive ships will gain a speed boost.
  - Similarly, a ship that's underpowered won't move as fast as one that is overpowered.
  - The displacementImpulse field on the warp engine is no longer used and has been discarded.
  - The Supercharge toggle switch on the warp engine is no longer used and has been discarded. It is now automatically factored into the new equations.
  - Added new warpIgnitionThreshold to the warp engine. If the starship's total Gravity Waves produced per second divided by the total Gravity Waves consumed per second drops below this value, then the engine will flame out.

- In the editor, you'll see two new fields in the warp engine PAW: Displacement Multiplier, and Power Multiplier.
  - Displacement Multiplier is the ratio between the total mass displaced by the warp coils divided by the vessel's current mass. If it is less than one then the ship won't move as fast as a vessel with a ratio that is equal to or greater than 1.
  - Power Multiplier is the ratio between the total Gravity Waves generated per second divided by the total Gravity Waves consumed per second by the warp coils. If it is less than one then the ship won't move as fast as a vessel with a ratio that is equal to or greater than 1.
NOTE: If the Power Multiplier drops below the warp engine's warpIgnitionThreshold then the warp engine will flame out.

- To calculate the Effective Warp Capacity:
  - Calculate the Displacement Multiplier
  - Calculate the Power Multiplier
  - Take the sum of the vessel's total Warp Capacity, and multiply by the Dispacement Multiplier and the Power Multiplier.

- Updated the warp tech parts to reflect the redesigned warp speed calculations. In general:
  - Size 3 parts got a buff to their power generation to reflect their use in large starships.
  - The displacementImpulse ratings of the S1, S2, and Mk2 Warp Coils were adjusted to reflect that they have 1/10 the Warp Capacity and mass displacement of their corresponding Warp Ring.
  - The warp curves of all the engines were redrawn and given a speed boost, with the S3 engines getting the biggest boost.

- Warp engines now let you set the desired inclination when you auto-circularize your orbit. This setting doesn't apply when you rendezvous with another vessel.

- Fixed issue where the warp engine would flame out when transitioning from interplanetary to interstellar space, or when throttling up or down.
- Fixed issue where max warp speed wasn't being calculated when you loaded a ship into the editor.
- Fixed issue where a space anomaly would be deleted after its spawn timer expired and the player had visited it.

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