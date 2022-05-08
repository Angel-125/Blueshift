Blueshift: Kerbal FTL

---INSTALLATION---

Simply copy all the files into your GameData folder. When done, it should look like:

GameData
	WildBlueIndustries
		Blueshift

Changes

- Adding missing S3 Heavy Warp Sustainer to patch for Far Future Technologies.
- Fixed Jumpgate Completed message spam.
- Replaced jumpMaxDimensions with jumpMaxMass. Since KSP's vessel dimension in flight are widely off from in the editor, the new
- jumpMaxMass specifies the maximum mass that a jumpgate can transport. By default it is set to -1, which means there are no limits.
- Jumpgates can now specify a RESOURCE_TOLL config node in place of the older RESOURCE node to define the jump toll. Here's an example:

// Resource tolls override the older method of charging travelers.
RESOURCE_TOLL
{
	// Name of the toll. This is maily for ModuleManager purposes.
	name = planetarySOIToll

	// Price tier- one of: planetary, interplanetary, interstellar
	priceTier = planetary

	// Name of the resource
	resourceName = Graviolium

	// Amount of resource per metric tonne mass of the traveler
	amountPerTonne = 0.1

	// Resource is paid by the traveler that is initiating the jump, or by the jumpgate
	paidByTraveler = true
}

- The Astria Porta and the jumpgate space anomaly now specify planetary, interplanetary, and interstellar tiered price tolls.

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