using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureDebug : MonoBehaviour
{
    [SerializeField]
    GameState gameState;

    [SerializeField]
    WindBuffer wind;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = gameState.player.transform.position + gameState.player.height * 0.49f * Vector3.down;
        transform.localScale = new Vector3(wind.Range * 2, wind.Range * 2, 1);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position += gameState.player.velocity * Time.deltaTime;
    }
}
