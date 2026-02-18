# CONSIDERATIONS

Right now a color is represented by 3 channels with 6 levels in each channel.
This color concept can be abstracted into the framebuffer module, then the userland color can be 3 floating point channels that are clamped to the 0-5 range at render time.
This would allow the color depth in the framebuffer to change without requiring a rewrite of userland graphics.
