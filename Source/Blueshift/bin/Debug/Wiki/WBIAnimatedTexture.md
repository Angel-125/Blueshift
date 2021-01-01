            
This class lets you animate textures by displaying a series of images in sequence. You can animate a material's diffuse and emissive texture. You include several textures that act as the individual animation frames, and the part module will show them in sequence. This is NOT as efficient as a texture strip but it's the best I can do for now, and it's easier to set up the UV maps on the meshes being animated.
        
## Fields

### debugMode
A flag to enable/disable debug mode.
### moduleID
The ID of the part module. Since parts can have multiple animated textures, this field helps identify them.
### textureTransformName
Name of the transform whose textures will be animated.
### animatedEmissiveTexture
The name of the animated texture, like "WarpPlasma." The actual textures should be numbered in sequence (WarpPlasma1, WarpPlasma2, etc).
### animatedDiffuseTexture
The name of the diffuse texture. It too can be animated.
### minFramesPerSecond
The minimum animation speed.
### maxFramesPerSecond
The maximum animation speed. Testing shows that with frame updates happening every 0.02 seconds, that corresponds to 50 frames per second.
### emissiveFadeTime
In seconds, how fast should the emissive fade when the animation isn't activated.
### isActivated
The activation switch. When not running, the animations won't be animated.
### animationThrottle
A throttle control to vary the animation speed between minFramesPerSecond and maxFramesPerSecond
### fadesAtMinThrottle
A toggle that indicates whether or not to fade out the animations when the animationThrottle is set to zero.
## Methods


### blendTextures(System.Single,UnityEngine.Texture2D,UnityEngine.Texture2D,UnityEngine.Texture2D)
Blends two textures together and stores the result in an output texture. Curtesy of stupid_chris
> #### Parameters
> **blend:** Percentage to blend through (from 0 to 1)

> **from:** Beginning texture

> **to:** Finishing texture

> **output:** Texture to appear blended


### getTextures(System.String)
Retrieves all the textures for the specified name. If the name is "WarpPlasma" for instance, then the array of textures will have "WarpPlasma1" "WarpPlasma2" and so on. The method will keep looking for textures until it can no longer find a texture in the numbered sequence.
> #### Parameters
> **textureName:** The base name of the texture to search for. It should have the relative path such as WildBlueIndustries/Blueshift/Parts/Engine/WarpPlasma.

> #### Return value
> An array of string containing the numbered textures that comprise the animation.

