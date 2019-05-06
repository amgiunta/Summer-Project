using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderTrigger : Trigger {

    void OnCollisionStay2D(Collision2D other) {
        if (other.gameObject.CompareTag("Player")) { TripTrigger(); }
    }
}
