using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

[RequireComponent(typeof(SpriteShapeController))]
[RequireComponent(typeof(SpriteShapeRenderer))]
public class Connector : MonoBehaviour
{
    public Trigger source;
    public SpriteShape onProfile;
    public Color onColor;
    public SpriteShape offProfile;
    public Color offColor;


    private SpriteShapeController controller;
    new private SpriteShapeRenderer renderer;
    // Start is called before the first frame update
    void Awake()
    {
        controller = GetComponent<SpriteShapeController>();
        renderer = GetComponent<SpriteShapeRenderer>();

        if (source.state == Trigger.TriggerState.Active) {
            controller.spriteShape = onProfile;
            renderer.color = onColor;
        }
        else {
            controller.spriteShape = offProfile;
            renderer.color = offColor;
        }

        source.OnActivate.AddListener(() => {
            controller.spriteShape = onProfile;
            renderer.color = onColor;
        });
        source.OnDeactivate.AddListener(() => {
            controller.spriteShape = offProfile;
            renderer.color = offColor;
        });
    }
}
