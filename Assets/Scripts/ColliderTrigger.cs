using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic component to set up collision triggers inside of unity.
/// </summary>
public class ColliderTrigger : Trigger {

    void OnCollisionStay2D(Collision2D other) {
        if (other.gameObject.CompareTag("Player")) { TripTrigger(); }
    }
}
