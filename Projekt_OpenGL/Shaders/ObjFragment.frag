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
uniform bool uUseAlbedo;
uniform bool uUseMetallic;
uniform bool uUseRoughness;
uniform bool uUseEmission;
uniform bool uUseSpecular;

uniform sampler2D uAOTexture;
uniform sampler2D uNormalTexture;
uniform sampler2D uAlbedoTexture;
uniform sampler2D uMetallicTexture;
uniform sampler2D uRoughnessTexture;
uniform sampler2D uEmissionTexture;
uniform sampler2D uSpecularTexture;

uniform vec3 uEmissiveColor;
uniform float uEmissiveStrength;

void main()
{
    vec2 uv = FragPos.xz * 0.01;

    vec3 objectColor = uUseAlbedo ? texture(uAlbedoTexture, uv).rgb : Color.rgb;

    float aoFactor = uUseAO ? texture(uAOTexture, uv).r : 1.0;

    vec3 normal = normalize(Normal);
    if (uUseNormal) {
        vec3 normalMap = texture(uNormalTexture, uv).rgb * 2.0 - 1.0;
        normal = normalize(normal + normalMap * 0.5);
    }
    float metallic = uUseMetallic ? texture(uMetallicTexture, uv).r : 0.0;

    float roughness = uUseRoughness ? texture(uRoughnessTexture, uv).r : 0.5;

    float specularMapFactor = uUseSpecular ? texture(uSpecularTexture, uv).r : 1.0;

    vec3 lightDir = normalize(uLightPos - FragPos);
    vec3 viewDir = normalize(uViewPos - FragPos);
    vec3 reflectDir = reflect(-lightDir, normal);

    // Ambient
    vec3 ambient = uAmbientStrength * uLightColor * aoFactor;

    // Diffuse
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = uDiffuseStrength * diff * uLightColor;

    // Specular
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess * (1.0 - roughness));
    vec3 specular = uSpecularStrength * spec * uLightColor * specularMapFactor;

    // Emissive
    vec3 emission = vec3(0.0);
    if (uUseEmission)
    {
        vec3 emissiveTex = texture(uEmissionTexture, uv).rgb;
        emission = emissiveTex * uEmissiveStrength;
    }
    else
    {
        emission = uEmissiveColor * uEmissiveStrength;
    }

    vec3 result = (ambient + diffuse + specular) * mix(vec3(1.0), objectColor, 1.0 - metallic) + emission;
    FragColor = vec4(result, Color.a);
}