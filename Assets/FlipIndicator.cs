using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipIndicator : MonoBehaviour
{
    PlayerControllerV2 player;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<PlayerControllerV2>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.flipPivot;
        transform.rotation = Quaternion.identity;
    }
}
