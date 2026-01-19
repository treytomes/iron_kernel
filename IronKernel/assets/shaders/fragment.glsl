// assets\shaders\fragment.glsl

#version 330 core

#define PALETTE_SIZE 256.0
  
// Input from vertex shader  
in vec2 vTexCoord;  
  
// Output color  
out vec4 FragColor;  
  
// Textures  
uniform sampler2D uTexture;  // Contains palette indices (0-255 in R channel)  
uniform sampler2D uPalette;  // 1D palette texture with 256 colors  

void main()
{
    // Fetch the normalized index from the texture (should be in range [0,1])  
    float normalizedIndex = texture(uTexture, vTexCoord).r;  
      
    // Ensure the index is in valid range  
    normalizedIndex = clamp(normalizedIndex, 0.0, 1.0);  
      
    // Convert to palette index and calculate palette texture coordinate  
    // Add 0.5 to sample from the center of the texel  
    float index = normalizedIndex * (PALETTE_SIZE - 1.0);  
    vec2 paletteCoord = vec2((index + 0.5) / PALETTE_SIZE, 0.5);  
      
    // Fetch the color from the palette  
    FragColor = texture(uPalette, paletteCoord);  
      
    // Ensure alpha is fully opaque  
    FragColor.a = 1.0;  
}