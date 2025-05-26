#version 330 core
out vec4 FragColor;

uniform vec3 uLightColor;
uniform vec3 uLightPos;
uniform vec3 uViewPos;
uniform float uShininess;
uniform float uAmbientStrength;
uniform float uDiffuseStrength;
uniform float uSpecularStrength;

uniform sampler2D uTexture;
uniform sampler2D uAOTexture;
uniform sampler2D uMetallicTexture;
uniform sampler2D uNormalTexture;
uniform sampler2D uRoughnessTexture;
uniform sampler2D uOpacityTexture;
uniform int uUseTexture;
uniform int uUseAO;
uniform int uUseMetallic;
uniform int uUseNormal;
uniform int uUseRoughness;
uniform int uUseOpacity;

in vec4 outCol;
in vec3 outNormal;
in vec3 outWorldPosition;
in vec2 outTexCoord;

vec3 getNormalFromMap()
{
    vec3 tangentNormal = texture(uNormalTexture, outTexCoord).xyz * 2.0 - 1.0;
    
    vec3 Q1 = dFdx(outWorldPosition);
    vec3 Q2 = dFdy(outWorldPosition);
    vec2 st1 = dFdx(outTexCoord);
    vec2 st2 = dFdy(outTexCoord);
    
    vec3 N = normalize(outNormal);
    vec3 T = normalize(Q1 * st2.t - Q2 * st1.t);
    vec3 B = -normalize(cross(N, T));
    mat3 TBN = mat3(T, B, N);
    
    return normalize(TBN * tangentNormal);
}

void main()
{
    float opacity = 1.0;
    if (uUseOpacity == 1) {
        opacity = texture(uOpacityTexture, outTexCoord).r;
        if (opacity < 0.1) {
            discard;
        }
    }
    
    vec3 ao = vec3(1.0);
    if (uUseAO == 1) {
        ao = texture(uAOTexture, outTexCoord).rgb;
    }
    
    float metallic = 0.0;
    if (uUseMetallic == 1) {
        metallic = texture(uMetallicTexture, outTexCoord).r;
    }
    
    float roughness = 0.5; 
if (uUseRoughness == 1) {
    roughness = texture(uRoughnessTexture, outTexCoord).r;
    if (roughness < 0.1) roughness = 0.1;
    if (roughness > 0.9) roughness = 0.9;
}

    vec4 baseColor;
    if (uUseTexture == 1) {
        baseColor = texture(uTexture, outTexCoord);
    } else {
        baseColor = outCol;
    }
    
    vec3 norm;
    if (uUseNormal == 1) {
        norm = getNormalFromMap();
    } else {
        norm = normalize(outNormal);
    }
    
    vec3 ambient = uAmbientStrength * uLightColor * ao;
    
    vec3 lightDir = normalize(uLightPos - outWorldPosition);
    float diff = max(dot(norm, lightDir), 0.0);
    vec3 diffuse = uDiffuseStrength * diff * uLightColor * (1.0 - metallic * 0.5);
    
    vec3 viewDir = normalize(uViewPos - outWorldPosition);
    vec3 reflectDir = reflect(-lightDir, norm);
   
    float adjustedShininess = uShininess * (1.0 + metallic * 0.5) / (roughness + 0.1);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), adjustedShininess);

    float specularStrength = uSpecularStrength * (1.0 + metallic) * (1.0 - roughness * 0.5);    
    vec3 specular = specularStrength * spec * uLightColor;
    
    vec3 result = (ambient + diffuse * mix(1.0, ao.r, 0.3) + specular) * baseColor.rgb;
    
    FragColor = vec4(result, baseColor.a * opacity);
}