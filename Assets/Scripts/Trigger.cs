using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour {

    public bool triggerOnEnter = false;
    public bool oneShot = false;

    public UnityEngine.Events.UnityEvent OnTrigger;

    protected int triggerCount;

    public void TripTrigger() {
        if (oneShot && triggerCount >= 1) { return; }
        OnTrigger.Invoke();
        triggerCount++;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player") && triggerOnEnter) { TripTrigger(); }
    }
}
