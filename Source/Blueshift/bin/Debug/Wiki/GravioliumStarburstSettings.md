            
Visual settings for a reusable Graviolium radial impact spray.
        
## Fields

### enabled
Whether callers should display the starburst.
### particleTexturePath
Extensionless GameDatabase path of the particle texture.
### particleColor
Base electric-blue particle tint.
### emissionRate
Particles emitted per second.
### maxParticles
Maximum simultaneously living particles.
### minLifetime
Minimum particle lifetime, in seconds.
### maxLifetime
Maximum particle lifetime, in seconds.
### minSize
Minimum particle size, in meters.
### maxSize
Maximum particle size, in meters.
### minRadialSpeed
Minimum radial spray speed, in meters per second.
### maxRadialSpeed
Maximum radial spray speed, in meters per second.
### backscatterSpeed
Maximum speed back toward the source after impact.
### brightnessVariation
Fractional variation applied to particle brightness.
## Methods


### Load(ConfigNode)
Loads any values present in a GRAVIOLIUM_STARBURST_FX config node. Values omitted from the node retain the defaults declared by this class.

