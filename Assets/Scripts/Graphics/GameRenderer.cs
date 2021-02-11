using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static VectorUtil;

public class GameRenderer : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

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
        windSampler._WindFrequency = gameState.windScale;
        windSampler._WindShiftSpeed = gameState.windShiftSpeed;

        Vector3 playerPosition = gameState.player.transform.position;
        Shader.SetGlobalVector("_PlayerPosition", playerPosition);
        Shader.SetGlobalFloat("_GrassSquashRadius", gameState.player.radius * 1.2f);
        Shader.SetGlobalFloat("_GrassSquashStrength", 0.5f * Mathf.PI);
        Shader.SetGlobalVector("_GrassLOD", new Vector4(15f, 15f, 5f, 10f));

        var colorSphere = gameState.GetColorSphere();
        Vector4 colorSpherePositionAndRadius = Vector4(colorSphere.position, colorSphere.radius);
        Shader.SetGlobalVector("_ColorSpherePositionAndRadius", Vector4(colorSphere.position, colorSphere.radius));

        var windSphere = gameState.GetWindSphere();
        Vector4 windSpherePositionAndRadius = Vector4(windSphere.position, windSphere.radius);
        Shader.SetGlobalVector("_WindSpherePositionAndRadius", windSpherePositionAndRadius);
        windSampler._WindSpherePositionAndRadius = windSpherePositionAndRadius;

        var greenSphere = gameState.GetGreenSphere();
        Vector4 greenSpherePositionAndRadius = Vector4(greenSphere.position, greenSphere.radius);
        Shader.SetGlobalVector("_GreenSpherePositionAndRadius", greenSpherePositionAndRadius);
    }
}
