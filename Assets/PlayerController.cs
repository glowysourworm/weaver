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
    public const string COLLISION_ALL = "CollisionAll";
    public const string COLLISION_LEFT = "CollisionLeft";
    public const string COLLISION_RIGHT = "CollisionRight";
    public const string COLLISION_CEILING = "CollisionCeiling";
    public const string COLLISION_GROUND = "CollisionGround";

    public Rigidbody2D Player;
    public PlayerState MovementState = PlayerState.Idle;
    public Animator PlayerAnimatorTop;
    public Animator PlayerAnimatorBottom;
    public SpriteRenderer PlayerSpriteRendererTop;
    public SpriteRenderer PlayerSpriteRendererBottom;

    public float RunAcceleration = 0.1f;
    public float RunVelocity = 0.1f;
    public float JumpVelocity = 1.0f;
    public float RunDeceleration = 1.0f;
    public float Gravity = 50f;

    Vector2 playerVelocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerVelocity = new Vector2();
    }

    // Update is called once per frame
    void Update()
    {
        playerVelocity = this.Player.linearVelocity;

        bool collisionAny = this.Player.IsTouchingLayers(LayerMask.GetMask(COLLISION_ALL));
        bool collisionLeft = this.Player.IsTouchingLayers(LayerMask.GetMask(COLLISION_LEFT));
        bool collisionRight = this.Player.IsTouchingLayers(LayerMask.GetMask(COLLISION_RIGHT));
        bool collisionGround = this.Player.IsTouchingLayers(LayerMask.GetMask(COLLISION_GROUND));
        bool collisionCeiling = this.Player.IsTouchingLayers(LayerMask.GetMask(COLLISION_CEILING));

        // Jump Only
        if (Input.GetKeyDown(KeyCode.UpArrow) && collisionGround)
        {
            playerVelocity.y = 100 * this.JumpVelocity * Time.deltaTime;
            //playerVelocity.y = this.JumpVelocity;
        }

        // Left / Right Acceleration
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            playerVelocity.x += this.RunAcceleration * Time.deltaTime;
            //playerVelocity.x = this.RunVelocity;

            this.PlayerSpriteRendererTop.flipX = false;
            this.PlayerSpriteRendererBottom.flipX = false;
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.LeftArrow))
        {
            playerVelocity.x -= this.RunAcceleration * Time.deltaTime;
            //playerVelocity.x = -1 * this.RunVelocity;

            this.PlayerSpriteRendererTop.flipX = true;
            this.PlayerSpriteRendererBottom.flipX = true;
        }

        // Gravity
        if (!collisionGround)
            playerVelocity.y -= this.Gravity * Time.deltaTime;
        

        this.Player.linearVelocity = playerVelocity;

        // Idle Animation
        //if (playerVelocity.sqrMagnitude <= 0.5f)
        //{
        //    this.PlayerAnimator.Play("player-idle");            
        //}

        //// Run Animation
        //else
        //{
        //    this.PlayerAnimator.Play("player-run");
        //}

        // Calculate position change
        //playerPosition.x += playerVelocity.x * Time.deltaTime;
        //playerPosition.y += playerVelocity.y * Time.deltaTime;

        //this.Player.position = playerPosition;
    }
}
