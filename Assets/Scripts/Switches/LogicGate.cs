using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicGate : Trigger
{
    public enum GateType { And, Or, Nand, Nor, Xor};

    public GateType gateType;
    public List<Trigger> inputs;

    public void Start() {
        
    }

    public void GateCheck() {
        if (inputs == null) {return;}

        Debug.Log("Checking gate");

        switch (gateType) {
            case GateType.Xor:
                if (Or() && !And()) {
                    if (state == TriggerState.Inactive)
                        Activate();
                }
                else {
                    if (state == TriggerState.Active)
                        Deactivate();
                }
                return;
            case GateType.Nor:
                if (!Or()) {
                    if (state == TriggerState.Inactive)
                        Activate();
                }
                else {
                    if (state == TriggerState.Active)
                        Deactivate();
                }

                return;
            case GateType.Or:
                if (Or()) {
                    if (state == TriggerState.Inactive)
                        Activate();
                }
                else {
                    if (state == TriggerState.Active)
                        Deactivate();
                }

                return;
            case GateType.Nand:
                if (!And()) {
                    if (state == TriggerState.Inactive)
                        Activate();
                }
                else {
                    if (state == TriggerState.Active)
                        Deactivate();
                }
                return;
            case GateType.And:
                if (And()) {
                    if (state == TriggerState.Inactive)
                        Activate();
                }
                else {
                    if (state == TriggerState.Active)
                        Deactivate();
                }
                return;
            default:
                break;
        }
    }

    private bool And() {
        foreach (Trigger input in inputs) {
            if (input.state == TriggerState.Inactive) {
                return false;
            }
        }
        return true;
    }

    private bool Or() {
        foreach (Trigger input in inputs) {
            if (input.state == TriggerState.Active) {
                return true;
            }
        }
        return false;
    }

    public override void Activate()
    {
        base.Activate();
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }
}
