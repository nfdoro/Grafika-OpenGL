#version 330 core

layout(location = 0) in vec3 aPos;
layout(location = 1) in vec4 aColor;
layout(location = 2) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;
uniform mat3 uNormal;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform float uShininess;

out vec4 vColor;

void main()
{
    vec4 worldPos = uModel * vec4(aPos, 1.0);
    vec3 fragPos = vec3(worldPos);
    vec3 norm = normalize(uNormal * aNormal);
    vec3 lightDir = normalize(uLightPos - fragPos);
    vec3 viewDir = normalize(uViewPos - fragPos);
    vec3 reflectDir = reflect(-lightDir, norm);

    // Ambient
    float ambientStrength = 0.1;
    vec3 ambient = ambientStrength * uLightColor;

    // Diffuse
    float diffuseStrength = 0.3;
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = diffuseStrength * uLightColor * diff;

    // Specular
    float specularStrength = 0.6;
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = specularStrength * uLightColor * spec;

    vec3 result = (ambient + diffuse + specular) * aColor.rgb;
    vColor = vec4(result, aColor.a);

    gl_Position = uProjection * uView * worldPos;
}
