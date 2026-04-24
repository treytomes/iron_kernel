// assets\shaders\fragment.glsl

#version 330 core

in vec2 vTexCoord;

out vec4 FragColor;

uniform usampler2D uTexture;  // 16-bit unsigned index per pixel
uniform sampler2D uPalette;   // palette: PaletteSize x 1 RGB texture
uniform int uPaletteSize;     // number of palette entries (ColorDepth^3)

void main()
{
    // Fetch the raw 16-bit index (comes back as uint in the R channel)
    uint index = texture(uTexture, vTexCoord).r;

    // Look up the palette entry — sample from center of the texel
    float paletteU = (float(index) + 0.5) / float(uPaletteSize);
    FragColor = texture(uPalette, vec2(paletteU, 0.5));
    FragColor.a = 1.0;
}
