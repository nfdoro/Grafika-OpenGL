#version 330 core

in vec3 FragPos;
in vec3 Normal;
in vec4 Color;

out vec4 FragColor;

uniform float uShininess;
uniform float uAmbientStrength;
uniform float uDiffuseStrength;
uniform float uSpecularStrength;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;

uniform bool uUseAO;
uniform bool uUseNormal;
uniform sampler2D uAOTexture;
uniform sampler2D uNormalTexture;

void main()
{
    // Base color from vertex color
    vec3 objectColor = Color.rgb;
    
    // Apply AO texture if available
    float aoFactor = 1.0;
    if (uUseAO) {
             vec2 aoTexCoord = vec2(FragPos.x * 0.01, FragPos.z * 0.01);
        aoFactor = texture(uAOTexture, aoTexCoord).r;
    }
    
    
    vec3 normal = normalize(Normal);
    if (uUseNormal) {
        vec2 normalTexCoord = vec2(FragPos.x * 0.01, FragPos.z * 0.01);
        vec3 normalMap = texture(uNormalTexture, normalTexCoord).rgb * 2.0 - 1.0;
        normal = normalize(normal + normalMap * 0.5);
    }
    
    // Ambient lighting
    vec3 ambient = uAmbientStrength * uLightColor * aoFactor;
    
    // Diffuse lighting
    vec3 lightDir = normalize(uLightPos - FragPos);
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = uDiffuseStrength * diff * uLightColor;
    
    // Specular lighting
    vec3 viewDir = normalize(uViewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
    vec3 specular = uSpecularStrength * spec * uLightColor;
    
    // Combine lighting
    vec3 result = (ambient + diffuse + specular) * objectColor;
    
    FragColor = vec4(result, Color.a);
}