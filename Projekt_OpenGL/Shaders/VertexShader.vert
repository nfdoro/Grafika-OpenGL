#version 330 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec4 vCol;
layout (location = 2) in vec3 vNormal;
layout (location = 3) in vec2 vTexCoord;

uniform mat4 uModel;
uniform mat3 uNormal;
uniform mat4 uView;
uniform mat4 uProjection;

out vec4 outCol;
out vec3 outNormal;
out vec3 outWorldPosition;
out vec2 outTexCoord;
        
void main()
{
	outCol = vCol;
    outNormal = uNormal*vNormal;
    outWorldPosition = vec3(uModel * vec4(vPos, 1.0));
    outTexCoord = vTexCoord;
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
}