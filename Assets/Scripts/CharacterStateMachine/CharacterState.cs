using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class CharacterState {
    public List<StateTransition> transitions;
    public string name;
    public CharacterStateNetwork network;

    public CharacterState(string name) {
        this.name = name;
        transitions = new List<StateTransition>();
    }

    public CharacterState(string name, CharacterStateNetwork network) {
        this.name = name;
        this.network = network;
        network.AddStateToNetwork(this);
        transitions = new List<StateTransition>();
    }

    public abstract void OnStateEnter();
    public abstract void OnStateExit();
    public abstract void Subject();
    public abstract void Update();
    public abstract void FixedUpdate();

    public void AddTransition(CharacterState to, UnityEngine.Events.UnityAction callback = null) {
        StateTransition transition = new StateTransition(this, to, callback);
        transitions.Add(transition);
    }

    public void Transition(string stateName) {
        foreach (StateTransition transition in transitions) {
            if (transition.to.name == stateName) {
                transition.Transition();
                return;
            }
        }

        Debug.LogError("Could not find transition to state '" + stateName + "' from state '" + name + "'");
    }
    public void Transition(CharacterState to) {
        foreach (StateTransition transition in transitions) {
            if (transition.to == to) {
                transition.Transition();
                return;
            }
        }

        Debug.LogError("Could not find transition to state '" + to.name + "' from state '" + name + "'");
    }
    public static implicit operator bool(CharacterState state) {
        if (state.network != null)
        {
            return (!object.ReferenceEquals(state, null) && state.network.activeState == state);
        }
        else { return false; }
    }
}

[System.Serializable]
public class StateTransition {
    public CharacterState from;
    public CharacterState to;
    public UnityEngine.Events.UnityAction continueWith;

    public StateTransition(CharacterState from, CharacterState to, UnityEngine.Events.UnityAction callback) {
        this.from = from;
        this.to = to;
        this.continueWith = callback;
    }

    public void Transition() {
        from.OnStateExit();
        to.OnStateEnter();

        to.network.activeState = to;

        if (continueWith != null)
        {
            continueWith();
        }
    }
}
