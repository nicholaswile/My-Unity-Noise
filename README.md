# My-Unity-Noise
My implementations of various pseudorandom noise generation algorithms in Unity. Adapted for the default render pipeline from Jasper Flick's URP demos on pseudorandom noise from <a href="https://catlikecoding.com/">Catlike Coding</a>. My implemenations may serve as a reference for learners to compare their own projects to, especially those who want to write HLSL shaders rather than utilize the Universal Render Pipeline with Shader Graph.
## Spacial Hashing
TODO: Add recordings. 

## xxHash
<img src="screenshots/Unity_HashRotator.gif">
<img src="screenshots/Unity_xxHash.gif">

Utilizes an <a href="https://github.com/Cyan4973/xxHash/blob/dev/doc/xxhash_spec.md">xxHash</a> by Yann Collet to create noise. 