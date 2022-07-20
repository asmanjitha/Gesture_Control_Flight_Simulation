using System;
using CoachAiEngine;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class BallPositionUpdater : MonoBehaviour {

    [SerializeField] private GameObject ball;
    private Transform _transform;

    private void Awake() {
        ball ??= GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _transform = ball.transform;
    }

    public void UpdateBallPosition(PublicEvent @event) {
        Debug.Log($"Got event: {@event.Type}");
        if (!(@event.Properties["location"] is JArray location)) return;

        var x = location[0].Value<float>();
        var y = location[1].Value<float>();
        var z = location[2].Value<float>();
        _transform.position = new Vector3(x, y, -z);
    }
}
