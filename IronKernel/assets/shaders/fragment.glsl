// assets\shaders\fragment.glsl

#version 330 core

in vec2 vTexCoord;

out vec4 FragColor;

uniform usampler2D uTexture;  // R16UI index texture
uniform sampler2D uPalette;   // palette: PaletteSize x 1 RGB
uniform int uPaletteSize;     // ColorDepth^3

void main()
{
    ivec2 texSize = textureSize(uTexture, 0);
    ivec2 texel = ivec2(vTexCoord * vec2(texSize));
    uint index = texelFetch(uTexture, texel, 0).r;

    float paletteU = (float(index) + 0.5) / float(uPaletteSize);
    FragColor = vec4(texture(uPalette, vec2(paletteU, 0.5)).rgb, 1.0);
}
