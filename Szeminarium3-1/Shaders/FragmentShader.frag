#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor1;
uniform vec3 uLightPos1;

uniform vec3 uLightColor2;
uniform vec3 uLightPos2;

uniform vec3 uViewPos;
uniform float uShininess;

in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;

void main()
{
    float ambientStrength = 0.1;
    float diffuseStrength = 0.3;
    float specularStrength = 0.6;

    vec3 norm = normalize(outNormal);
    vec3 viewDir = normalize(uViewPos - outWorldPosition);

    // --- Light 1 calculations ---
    vec3 lightDir1 = normalize(uLightPos1 - outWorldPosition);
    float diff1 = max(dot(norm, lightDir1), 0.0);
    vec3 reflectDir1 = reflect(-lightDir1, norm);
    float spec1 = pow(max(dot(viewDir, reflectDir1), 0.0), uShininess);

    vec3 ambient1 = ambientStrength * uLightColor1;
    vec3 diffuse1 = diff1 * uLightColor1 * diffuseStrength;
    vec3 specular1 = spec1 * uLightColor1 * specularStrength;

    // --- Light 2 calculations ---
    vec3 lightDir2 = normalize(uLightPos2 - outWorldPosition);
    float diff2 = max(dot(norm, lightDir2), 0.0);
    vec3 reflectDir2 = reflect(-lightDir2, norm);
    float spec2 = pow(max(dot(viewDir, reflectDir2), 0.0), uShininess);

    vec3 ambient2 = ambientStrength * uLightColor2;
    vec3 diffuse2 = diff2 * uLightColor2 * diffuseStrength;
    vec3 specular2 = spec2 * uLightColor2 * specularStrength;

    // Combine both lights
    vec3 result = (ambient1 + diffuse1 + specular1 + ambient2 + diffuse2 + specular2) * outCol.rgb;

    FragColor = vec4(result, outCol.w);
}
