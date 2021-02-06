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
    }

    void LateUpdate()
    {
        Shader.SetGlobalFloat("_AnimTime", Time.timeSinceLevelLoad);

        Shader.SetGlobalFloat("_WindFrequency", gameState.windScale);
        Shader.SetGlobalFloat("_WindShiftSpeed", gameState.windShiftSpeed);
        windSampler._WindFrequency = gameState.windScale;
        windSampler._WindShiftSpeed = gameState.windShiftSpeed;

        ref var player = ref gameState.player;
        Vector3 playerPosition = player.transform.position;
        Shader.SetGlobalVector("_PlayerPosition", gameState.player.transform.position);
        Shader.SetGlobalFloat("_GrassSquashRadius", gameState.player.radius * 1.2f);
        Shader.SetGlobalFloat("_GrassSquashStrength", 0.5f * Mathf.PI);

        var colorSphere = gameState.colorSphere;
        Vector3 colorSpherePosition = colorSphere.isHeld ? playerPosition : colorSphere.position;
        Shader.SetGlobalVector("_ColorSpherePositionAndRadius", Vector4(colorSpherePosition, 10f));
    }
}
