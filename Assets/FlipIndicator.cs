using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipIndicator : MonoBehaviour
{
    PlayerControlerV2 player;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<PlayerControlerV2>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.flipPivot;
        transform.rotation = Quaternion.identity;
    }
}
