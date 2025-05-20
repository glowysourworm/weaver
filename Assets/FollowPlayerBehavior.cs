using UnityEngine;

public class FollowPlayerBehavior : MonoBehaviour
{
    public Rigidbody2D Player;
    public Transform MainCameraOffset;
    public float SmoothSpeed = 0.8f;
    Vector3 cameraPosition;
    Vector3 desiredPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cameraPosition = new Vector3();
        desiredPosition = new Vector3();
    }

    // Update is called once per frame
    void Update()
    {
        cameraPosition.x = this.MainCameraOffset.position.x;
        cameraPosition.y = this.MainCameraOffset.position.y;
        cameraPosition.z = 0;

        desiredPosition.x = (this.Player.position.x - cameraPosition.x) * this.SmoothSpeed * Time.deltaTime;
        desiredPosition.y = (this.Player.position.y - cameraPosition.y) * this.SmoothSpeed * Time.deltaTime;
        desiredPosition.z = 0;

        this.MainCameraOffset.Translate(desiredPosition);
    }
}
