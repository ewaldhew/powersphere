using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRenderer : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    void Start()
    {
        Shader.SetGlobalTexture("_WindTex", TextureCreator.PerlinCurl(256, 0));
    }

    void LateUpdate()
    {
        Shader.SetGlobalFloat("_WindFrequency", 0.02f);
        Shader.SetGlobalFloat("_WindShiftSpeed", 0.02f);

        Shader.SetGlobalVector("_PlayerPosition", gameState.player.transform.position);
        Shader.SetGlobalFloat("_GrassSquashRadius", gameState.player.radius * 1.2f);
        Shader.SetGlobalFloat("_GrassSquashStrength", 0.5f * Mathf.PI);
    }
}
