using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

using static VectorUtil;

public class GameRenderer : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    [SerializeField]
    PostProcess postProcessRendererFeature;
    [SerializeField]
    bool postProcessingEnabled;

    [SerializeField]
    WindBuffer windBuffer;
    public WindSampler windSampler { get; private set; }

    void OnEnable()
    {
        Texture2D windTex = TextureCreator.PerlinCurl(256, 0);
        Shader.SetGlobalTexture("_WindTex", windTex);
        windSampler = new WindSampler();
        windSampler._WindTex = windTex;

        Texture2D noiseTex = TextureCreator.PerlinCloudsTiling(256, 0);
        Shader.SetGlobalTexture("_NoiseTex", noiseTex);
    }

    void LateUpdate()
    {
        Shader.SetGlobalFloat("_AnimTime", Time.timeSinceLevelLoad);

        Shader.SetGlobalFloat("_WindFrequency", gameState.windScale);
        Shader.SetGlobalFloat("_WindShiftSpeed", gameState.windShiftSpeed);
        Shader.SetGlobalFloat("_WindStrength", gameState.windStrength);
        windSampler._WindFrequency = gameState.windScale;
        windSampler._WindShiftSpeed = gameState.windShiftSpeed;
        windSampler._WindStrength = gameState.windStrength;

        Shader.SetGlobalTexture("_WindBuffer", windBuffer.WindPotential);
        Shader.SetGlobalVector("_WindBufferCenter", windBuffer.CenterPosition);
        Shader.SetGlobalFloat("_WindBufferRange", windBuffer.Range);
        Shader.SetGlobalFloat("_DynamicWindStrength", gameState.dynamicWindStrength);
        windSampler._WindBuffer = windBuffer.WindPotential;
        windSampler._WindBufferCenter = windBuffer.CenterPosition;
        windSampler._WindBufferRange = windBuffer.Range;
        windSampler._DynamicWindStrength = gameState.dynamicWindStrength;
        windSampler._DynamicWindRadius = gameState.dynamicWindRadius;

        Vector3 playerPosition = gameState.player.transform.position;
        Shader.SetGlobalVector("_PlayerPosition", playerPosition);
        Shader.SetGlobalFloat("_GrassSquashRadius", gameState.player.radius * 1.2f);
        Shader.SetGlobalFloat("_GrassSquashStrength", 0.5f * Mathf.PI);
        Shader.SetGlobalVector("_GrassLOD", new Vector4(15f, 15f, 5f, 10f));

        var colorSphere = gameState.GetColorSphere();
        Vector4 colorSpherePositionAndRadius = Vector4(colorSphere.position, colorSphere.radius);
        Shader.SetGlobalVector("_ColorSpherePositionAndRadius", colorSpherePositionAndRadius);

        var windSphere = gameState.GetWindSphere();
        Vector4 windSpherePositionAndRadius = Vector4(windSphere.position, windSphere.radius);
        Shader.SetGlobalVector("_WindSpherePositionAndRadius", windSpherePositionAndRadius);
        windSampler._WindSpherePositionAndRadius = windSpherePositionAndRadius;

        var greenSphere = gameState.GetGreenSphere();
        Vector4 greenSpherePositionAndRadius = Vector4(greenSphere.position, greenSphere.radius);
        Shader.SetGlobalVector("_GreenSpherePositionAndRadius", greenSpherePositionAndRadius);

        var waterSphere = gameState.GetWaterSphere();
        Vector4 waterSpherePositionAndRadius = Vector4(waterSphere.position, waterSphere.radius);
        Shader.SetGlobalVector("_WaterSpherePositionAndRadius", waterSpherePositionAndRadius);

        var wallGlowRadius = gameState.HeldSpheres.Length > 0 ? gameState.HeldSpheres[0].radius : gameState.passiveBoundaryGlowRadius;
        Shader.SetGlobalFloat("_WallGlowRadius", wallGlowRadius);

        postProcessRendererFeature.settings.postProcessing = postProcessingEnabled;
    }
}
