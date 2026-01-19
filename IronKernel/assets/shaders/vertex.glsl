// assets\shaders\vertex.glsl
#version 330 core  
  
// Input vertex attributes  
layout(location = 0) in vec2 aPosition;  // Vertex position in normalized device coordinates  
layout(location = 1) in vec2 aTexCoord;  // Texture coordinates (0,0) to (1,1)  
  
// Output to fragment shader  
out vec2 vTexCoord;  
  
void main()  
{  
    // Pass position directly (no transformation needed for screen-aligned quad)  
    gl_Position = vec4(aPosition, 0.0, 1.0);  
      
    // Pass texture coordinates to fragment shader  
    vTexCoord = aTexCoord;  
}  