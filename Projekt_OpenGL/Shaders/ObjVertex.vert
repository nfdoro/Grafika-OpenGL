﻿#version 330 core

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec4 aCol;
layout (location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat3 uNormal;

out vec3 FragPos;
out vec4 Color;
out vec3 Normal;

void main()
{
    FragPos = vec3(uModel * vec4(aPos, 1.0));
    Color = aCol;
    Normal = uNormal * aNormal;
    
    gl_Position = uProjection * uView * vec4(FragPos, 1.0);
}