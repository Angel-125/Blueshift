            
Visual settings for a reusable Graviolium connection beam.
        
## Fields

### plasmaColor
Color of the wide plasma layer.
### currentColor
Color of the narrow electric-current layer.
### plasmaWidth
Width, in meters, of the plasma layer.
### currentWidth
Width, in meters, of the electric-current layer.
### pointCount
Number of points used by each line renderer.
### waveAmplitude
Amplitude, in meters, of the primary traveling wave.
### waveCycles
Number of primary wave cycles along the beam.
### waveSpeed
Animation speed of the primary wave.
### secondaryWaveAmplitude
Amplitude, in meters, of the secondary traveling wave.
### secondaryWaveCycles
Number of secondary wave cycles along the beam.
### secondaryWaveSpeed
Animation speed of the secondary wave.
### motesEnabled
Whether plasma motes travel along the beam.
### moteColor
Base color of the plasma motes.
### moteTexturePath
Extensionless GameDatabase path of the texture applied to each mote.
### moteDensity
Approximate number of motes per meter of beam length.
### moteTravelSpeed
Base mote travel speed along the beam, in meters per second.
### maxMotes
Maximum number of pooled motes.
### moteMinSize
Minimum mote width, in meters.
### moteMaxSize
Maximum mote width, in meters.
### moteOrbitRadius
Maximum distance motes orbit from the animated beam centerline.
### moteOrbitCycles
Number of mote orbit cycles along the beam.
### moteOrbitSpeed
Animation speed of the mote orbit.
### moteSpeedVariation
Fractional variation applied to individual mote speed.
### moteBrightnessVariation
Fractional variation applied to individual mote brightness.
## Methods


### Load(ConfigNode)
Loads any values present in a GRAVIOLIUM_BEAM_FX config node. Values omitted from the node retain the defaults declared by this class.

