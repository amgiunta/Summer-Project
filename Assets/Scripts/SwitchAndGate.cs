using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchAndGate : MonoBehaviour
{
    public enum SwitchState { Active, Inactive };
    public SwitchState state;

    public bool switch1 { get; set; }
    public bool switch2 { get; set; }

    public UnityEngine.Events.UnityEvent onActivate;
    public UnityEngine.Events.UnityEvent onDeactivate;

    SwitchState previousState;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (switch1 && switch2) { state = SwitchState.Active; }
        else { state = SwitchState.Inactive; }

        if (previousState == SwitchState.Inactive && state == SwitchState.Active) { onActivate.Invoke(); }
        else if (previousState == SwitchState.Active && state == SwitchState.Inactive) { onDeactivate.Invoke(); }
    }

    private void LateUpdate()
    {
        previousState = state;
    }
}
