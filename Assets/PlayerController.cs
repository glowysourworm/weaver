using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D Player;

    Vector2 playerPosition;
    Vector2 playerVelocity;
    float acceleration = 50f;
    float gravity = 10f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.Player = GetComponent<Rigidbody2D>();

        playerVelocity = new Vector2();
    }

    // Update is called once per frame
    void Update()
    {
        playerPosition = this.Player.position;
        playerVelocity = this.Player.linearVelocity;

        // Jump Only
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            playerVelocity.y = 100 * acceleration * Time.deltaTime;
        }

        // Left / Right Acceleration
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            playerVelocity.x += acceleration * Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.LeftArrow))
        {
            playerVelocity.x -= acceleration * Time.deltaTime;
        }

        // Gravity
        playerVelocity.y -= gravity * Time.deltaTime;

        this.Player.linearVelocity = playerVelocity;

        // Calculate position change
        //playerPosition.x += playerVelocity.x * Time.deltaTime;
        //playerPosition.y += playerVelocity.y * Time.deltaTime;

        //this.Player.position = playerPosition;
    }
}
