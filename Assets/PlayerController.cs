using System;

using Unity.VisualScripting;

using UnityEditor.Animations;

using UnityEngine;
using UnityEngine.EventSystems;

public enum PlayerState
{
    Idle,
    Runing,
    Jumping
}

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D Player;
    public SpriteRenderer PlayerSpriteRenderer;
    public PlayerState MovementState = PlayerState.Idle;
    public Animator PlayerAnimator;

    public float RunVelocity = 0.1f;
    public float JumpVelocity = 1.0f;
    public float RunDeceleration = 1.0f;
    public float Gravity = 50f;

    Vector2 playerPosition;
    Vector2 playerVelocity;
    float acceleration = 50f;

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
        if (Input.GetKeyDown(KeyCode.UpArrow) && Math.Abs(playerVelocity.y) < 0.1f)
        {
            //playerVelocity.y = 100 * acceleration * Time.deltaTime;
            playerVelocity.y = this.JumpVelocity;
        }

        // Left / Right Acceleration
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            //playerVelocity.x += acceleration * Time.deltaTime;
            playerVelocity.x = this.RunVelocity;

            this.PlayerSpriteRenderer.flipX = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.LeftArrow))
        {
            //playerVelocity.x -= acceleration * Time.deltaTime;
            playerVelocity.x = -1 * this.RunVelocity;

            this.PlayerSpriteRenderer.flipX = true;
        }

        // Left / Right Deceleration
        if (!Input.GetKey(KeyCode.LeftArrow) &&
            !Input.GetKey(KeyCode.RightArrow))
        {
            playerVelocity.x = 0;
        }

        // Gravity
        playerVelocity.y -= this.Gravity * Time.deltaTime;

        // Clamp Velocity
        if (Math.Abs(playerVelocity.y) < 0.1f)
            playerVelocity.y = 0;

        this.Player.linearVelocity = playerVelocity;

        // Idle Animation
        if (playerVelocity.sqrMagnitude <= 0.5f)
        {
            this.PlayerAnimator.Play("player-idle");            
        }

        // Run Animation
        else
        {
            this.PlayerAnimator.Play("player-run");
        }

        // Calculate position change
        //playerPosition.x += playerVelocity.x * Time.deltaTime;
        //playerPosition.y += playerVelocity.y * Time.deltaTime;

        //this.Player.position = playerPosition;
    }
}
