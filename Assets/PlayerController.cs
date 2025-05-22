using System;

using Assets;

using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D Player;

    public Animator PlayerAnimatorIdle;
    public Animator PlayerAnimatorRunning;
    public Animator PlayerAnimatorJumpNormal;
    public Animator PlayerAnimatorJumpSpin;
    public Animator PlayerAnimatorMorph;

    public SpriteRenderer PlayerSpriteRendererRunning;
    public SpriteRenderer PlayerSpriteRendererIdle;
    public SpriteRenderer PlayerSpriteRendererJumpNormal;
    public SpriteRenderer PlayerSpriteRendererJumpSpin;
    public SpriteRenderer PlayerSpriteRendererMorph;

    public float RunAcceleration = 1f;
    public float RunDeceleration = 1f;
    public float RunAutoDeceleration = 1f;              // Happens when player lets go of input
    public float JumpAcceleration = 10f;
    public float JumpInputTime = 0.5f;

    public float MaxRunVelocity = 1f;
    public float MaxFallVelocity = 1f;
    public float MaxJumpVelocity = 1f;

    // These [ReadOnly] Attributes are diagnostics that will show up in the
    // Unity drawer for this controller. These can probably be turned into
    // properties with private setters. (small TODO)

    /// <summary>
    /// Reference for the RigidBody2D gravity setting
    /// </summary>
    [Diagnostic] public float Gravity = 10f;

    [Diagnostic] public bool CollisionGround;
    [Diagnostic] public bool CollisionCeiling;
    [Diagnostic] public bool CollisionLeft;
    [Diagnostic] public bool CollisionRight;
    [Diagnostic] public float JumpAccumulatorLast;
    [Diagnostic] public float JumpInputCaptureTime;

    [Diagnostic] public bool Up;
    [Diagnostic] public bool Down;
    [Diagnostic] public bool Left;
    [Diagnostic] public bool Right;
    [Diagnostic] public bool Jump;

    [Diagnostic] public PlayerState State;
    [Diagnostic] public PlayerState NextState;

    // Used for encapsulating sprite renderer / animator and updating for state transitions
    protected PlayerStateAnimator PlayerStateAnimator;

    // Parameters (set from public facing parameters) (set in constructor)
    protected float MaxRunStartVelocity;

    // Scaling:
    //
    // 0) Gravity = 1
    // 1) Everything else (all accelerations) relative to Gravity (from user end)
    // 2) Set this parameter to make those work (per frame)
    //      -> Multiply the result of each calculation before
    //         setting the primary rigid body memory
    //
    float accelerationFrameScale = 100f;

    PlayerInputState playerInputState;
    CollisionState playerCollisionState;

    // Player sprites are setup Right-Facing. If they're flipped, the kinematics
    // responds accordingly.
    bool playerFlippedX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerCollisionState = new CollisionState();
        playerInputState = new PlayerInputState();
        playerFlippedX = false;

        this.PlayerStateAnimator = new PlayerStateAnimator(PlayerState.Idle, this.PlayerAnimatorIdle, this.PlayerSpriteRendererIdle, "TimeScale");

        this.MaxRunStartVelocity = this.MaxRunVelocity / 10.0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Procedure
        //
        // 0) Get the current frame's velocity vector
        // 1) Capture input / collision state for the player's RigidBody2D
        // 2) Capture desired state based on collision
        //      -> Transition: Update Animators, Set Diagnostics, Return
        //      -> Else:       Continue
        //

        // 3) Process state update for the player's velocity vector
        // 4) Process gravity (apply terminal velocity)
        // 5) Determine next state (based on velocity change)
        //      -> New State:  Set new state animation
        //      -> Same State: Process same state animation update
        // 6) Update Player Velocity (ACTUAL VECTOR)
        // 7) Set Diagnostics
        //


        // Setup Player Velocity (use current "memory" if it is managed for this Vector2)
        //
        var playerVelocity = this.Player.linearVelocity;

        // Capture Input State
        this.playerInputState.Capture(this.Player);

        // Capture Collision State
        this.playerCollisionState.Capture(this.Player);

        // State Transition? There could be a new collision to transition the state
        var stateDesired = DetermineNextState(this.PlayerStateAnimator.State, playerVelocity);

        // -> Update State Animator, Set Diagnostics, and Return. Next frame will start to take inputs.
        if (stateDesired != this.PlayerStateAnimator.State)
        {
            // Set State Animator
            this.PlayerStateAnimator.Set(stateDesired, GetAnimator(stateDesired), GetRenderer(stateDesired));

            // Diagnostics
            SetDiagnostics(stateDesired);

            // Next Frame
            return;
        }

        // Update Velocity (ref playerVelocity)
        ProcessStateUpdate(ref playerVelocity, Time.deltaTime);
        ProcessGravity(ref playerVelocity);

        // Get Next State (based on NEW velocity)
        var nextState = DetermineNextState(this.PlayerStateAnimator.State, playerVelocity);

        // No State Change
        if (nextState == this.PlayerStateAnimator.State)
        {
            // Applies final velocity vector to the state animation
            ProcessStateAnimationUpdate(playerVelocity, Time.deltaTime);
        }

        // State Change
        else
        {
            // Set Next State Animators
            this.PlayerStateAnimator.Set(nextState, GetAnimator(stateDesired), GetRenderer(stateDesired));
        }

        // UPDATE PLAYER VELOCITY (RigidBody2D)
        //
        this.Player.linearVelocity = playerVelocity;

        // Diagnostics
        SetDiagnostics(nextState);
    }

    private PlayerState DetermineNextState(PlayerState currentState, Vector2 currentVelocity)
    {
        switch (currentState)
        {
            case PlayerState.Idle:
            {
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> MovementGround
                    if (this.playerInputState.MoveLeftInput.IsSet() ||
                        this.playerInputState.MoveRightInput.IsSet())
                        return PlayerState.MovementGroundStart;

                    // -> JumpStart
                    else if (this.playerInputState.JumpInput.IsSet() &&
                             this.playerInputState.JumpInput.IsFirst())
                        return PlayerState.JumpStart;

                    // -> MorphStart
                    else if (this.playerInputState.MoveDownInput.IsSet())
                        return PlayerState.MorphStart;

                    // -> Idle
                    else
                        return PlayerState.Idle;
                }
                else
                    throw new Exception("Idle state when no collision with ground layer detected");
            }
            case PlayerState.MovementGround:
            {
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> JumpStart
                    if (this.playerInputState.JumpInput.IsSet() &&
                        this.playerInputState.JumpInput.IsFirst())
                        return PlayerState.JumpStart;

                    // -> MorphStart
                    else if (this.playerInputState.MoveDownInput.IsSet())
                        return PlayerState.MorphStart;

                    // -> MovementGroundEnd
                    else if (Math.Abs(currentVelocity.x) <= this.MaxRunStartVelocity)
                        return PlayerState.MovementGroundEnd;

                    // -> MovementGround
                    else
                        return PlayerState.MovementGround;
                }
                else
                    throw new Exception("MovementGround state when no collision with ground layer detected");
            }
            case PlayerState.JumpingNormal:
            {
                // TODO: Need to measure proximity to the ground surface(s)

                if (!this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> JumpEnd (!! This should be set already !!) (where did the jump end?)
                    if (this.playerCollisionState.CollisionGround.IsSet())
                        return PlayerState.JumpEnd;

                    else
                        return PlayerState.JumpingNormal;
                }
                else
                    throw new Exception("JumpingNormal state when collision with ground layer detected");
            }
            case PlayerState.JumpingSpin:
            {
                // TODO: Need to measure proximity to the ground surface(s)

                // -> JumpingSpin
                if (!this.playerCollisionState.CollisionGround.IsSet())
                    return PlayerState.JumpingSpin;

                // -> JumpEnd
                else
                    return PlayerState.JumpEnd;
            }
            case PlayerState.Morphed:
            {
                // Morphed on the ground
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> MorphEnd
                    if (this.playerInputState.MoveUpInput.IsSet())
                        return PlayerState.MorphEnd;

                    else
                        return PlayerState.Morphed;
                }

                // Morphed in the air
                else
                {
                    // -> MorphEnd
                    if (this.playerInputState.MoveUpInput.IsSet())
                        return PlayerState.MorphEnd;

                    else
                        return PlayerState.Morphed;
                }
            }
            case PlayerState.MovementGroundStart:
            {
                // On the ground
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> MovementGround
                    if (Math.Abs(currentVelocity.x) >= this.MaxRunStartVelocity)
                        return PlayerState.MovementGround;

                    // -> Idle
                    else if (currentVelocity.x == 0)
                        return PlayerState.Idle;

                    // -> MovementGroundStart
                    else
                        return PlayerState.MovementGroundStart;
                }
                else
                    throw new Exception("MovemengGroundStart while no collision with ground detected");
            }
            case PlayerState.MovementGroundEnd:
            {
                // On the ground
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    // -> MovementGround
                    if (Math.Abs(currentVelocity.x) >= this.MaxRunStartVelocity)
                        return PlayerState.MovementGround;

                    // -> Idle
                    else if (currentVelocity.x == 0)
                        return PlayerState.Idle;

                    // -> MovementGroundEnd
                    else
                        return PlayerState.MovementGroundEnd;
                }
                else
                    throw new Exception("MovementGroundEnd while no collision with ground detected");
            }
            case PlayerState.MovementGroundLTR:
            {
                // TODO
                return PlayerState.MovementGround;
            }
            case PlayerState.MovementGroundRTL:
            {
                // TODO
                return PlayerState.MovementGround;
            }
            case PlayerState.JumpStart:
            {
                // TODO
                if (this.playerInputState.MoveRightInput.IsSet() ||
                    this.playerInputState.MoveLeftInput.IsSet())
                {
                    return PlayerState.JumpingSpin;
                }
                else
                    return PlayerState.JumpingNormal;
            }
            case PlayerState.JumpEnd:
            {
                // -> MovementGround
                if (this.playerCollisionState.CollisionGround.IsSet())
                {
                    return PlayerState.MovementGround;
                }
                else
                    return PlayerState.JumpEnd;
            }

            // Fixed Animation: State transition must complete
            case PlayerState.MorphStart:
            {
                // -> Morphed
                if (this.PlayerStateAnimator.IsFinished())
                    return PlayerState.Morphed;

                else
                    return PlayerState.MorphStart;
            }

            // Fixed Animation: State transition must complete
            case PlayerState.MorphEnd:
            {
                // Un-morph animation finished
                if (this.PlayerStateAnimator.IsFinished())
                {
                    // -> MovementGround
                    if (this.playerCollisionState.CollisionGround.IsSet())
                        return PlayerState.MovementGround;

                    // -> JumpingNormal
                    else
                        return PlayerState.JumpingNormal;
                }
                else
                    return PlayerState.MorphEnd;
            }
            default:
                throw new Exception("Unhandled state transition PlayerController.DetermineNextState");
        }
    }

    private void ProcessStateUpdate(ref Vector2 playerVelocity, float deltaTime)
    {
        switch (this.PlayerStateAnimator.State)
        {
            case PlayerState.Idle:
                ProcessIdle(ref playerVelocity, deltaTime); break;

            case PlayerState.JumpingNormal:
                ProcessJumpingNormal(ref playerVelocity, deltaTime); break;

            case PlayerState.JumpStart:
                ProcessJumpStart(ref playerVelocity, deltaTime); break;

            case PlayerState.JumpEnd:
                ProcessJumpEnd(ref playerVelocity, deltaTime); break;

            case PlayerState.JumpingSpin:
                ProcessJumpingSpin(ref playerVelocity, deltaTime); break;

            case PlayerState.Morphed:
                ProcessMorphed(ref playerVelocity, deltaTime); break;

            case PlayerState.MovementGround:
                ProcessMovementGround(ref playerVelocity, deltaTime); break;

            case PlayerState.MovementGroundStart:
                ProcessMovementGroundStart(ref playerVelocity, deltaTime); break;

            case PlayerState.MovementGroundEnd:
                ProcessMovementGroundEnd(ref playerVelocity, deltaTime); break;

            case PlayerState.MovementGroundLTR:
                ProcessMovementGroundLTR(ref playerVelocity, deltaTime); break;

            case PlayerState.MovementGroundRTL:
                ProcessMovementGroundRTL(ref playerVelocity, deltaTime); break;

            case PlayerState.MorphStart:
                ProcessMorphStart(ref playerVelocity, deltaTime); break;

            case PlayerState.MorphEnd:
                ProcessMorphEnd(ref playerVelocity, deltaTime); break;

            default:
                throw new Exception("Unhandled PlayerState:  PlayerController.ProcessStateUpdate(...)");
        }
    }

    private void ProcessIdle(ref Vector2 playerVelocity, float deltaTime)
    {
        // Procedure:  Set player velocity based on current frame's
        //             input parameters & delta time. How long did
        //             the action take place?
        //

        // Idle: May be that the velocity near zero is finicky
        //
        playerVelocity.x = 0;
    }
    private void ProcessJumpingNormal(ref Vector2 playerVelocity, float deltaTime)
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input (1st input already capture to start this state)
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        // Jumping (in the air)
        if (!this.playerCollisionState.CollisionGround.IsSet())
        {
            // TODO: Mid-air normal-jump LTR, RTL transitions

            // Right
            if (!this.playerFlippedX)
            {
                // Accelerate
                if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunAcceleration * accelerationFrameScale * deltaTime;

                // Decelerate
                else if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunAcceleration * accelerationFrameScale * deltaTime;
            }

            // Left
            else
            {
                // Accelerate
                if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunAcceleration * accelerationFrameScale * deltaTime;

                // Decelerate
                else if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunAcceleration * accelerationFrameScale * deltaTime;
            }

        }
        else
            throw new Exception("ProcessJumpingNormal:  Detected ground collision! Should've been handled already!");
    }
    private void ProcessJumpStart(ref Vector2 playerVelocity, float deltaTime)
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input (1st input already capture to start this state)
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        playerVelocity.y = this.MaxJumpVelocity;

        // TODO: Apply JumpStart "velocity accumulator". 

        // Start of Jump
        //if (this.playerInputState.JumpInput.IsSet() &&
        //    this.playerInputState.JumpInput.GetAccumulator() < this.JumpInputTime)
        // {
        //     // FIX JUMP ACCELERATION (Needs capture also...)
        //     playerVelocity.y = this.MaxJumpVelocity;
        //     //playerVelocity.y += (this.JumpAcceleration * Time.deltaTime * accelerationFrameScale);
        // }
    }
    private void ProcessJumpEnd(ref Vector2 playerVelocity, float deltaTime)
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input (1st input already capture to start this state)
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        //playerVelocity.y = this.MaxJumpVelocity;

        // TODO: Apply JumpStart "velocity accumulator". 

        // Start of Jump
        //if (this.playerInputState.JumpInput.IsSet() &&
        //    this.playerInputState.JumpInput.GetAccumulator() < this.JumpInputTime)
        // {
        //     // FIX JUMP ACCELERATION (Needs capture also...)
        //     playerVelocity.y = this.MaxJumpVelocity;
        //     //playerVelocity.y += (this.JumpAcceleration * Time.deltaTime * accelerationFrameScale);
        // }
    }
    private void ProcessJumpingSpin(ref Vector2 playerVelocity, float deltaTime)
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input (1st input already capture to start this state)
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        // Jumping (in the air)
        if (!this.playerCollisionState.CollisionGround.IsSet())
        {
            // Right
            if (!this.playerFlippedX)
            {
                // Accelerate
                if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunAcceleration * accelerationFrameScale * deltaTime;

                // Decelerate
                else if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunAcceleration * accelerationFrameScale * deltaTime;
            }

            // Left
            else
            {
                // Accelerate
                if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunAcceleration * accelerationFrameScale * deltaTime;

                // Decelerate
                else if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunAcceleration * accelerationFrameScale * deltaTime;
            }

        }
        else
            throw new Exception("ProcessJumpingNormal:  Detected ground collision! Should've been handled already!");
    }
    private void ProcessMorphed(ref Vector2 playerVelocity, float deltaTime)
    {
        // Ground
        if (this.playerCollisionState.CollisionGround.IsSet())
        {
            // For now, let the velocity be set by MovementGround
            ProcessMovementGround(ref playerVelocity, deltaTime);
        }

        // Air
        else
            return;
    }
    private void ProcessMorphStart(ref Vector2 playerVelocity, float deltaTime)
    {
        // Probably some physics to consider
        return;
    }
    private void ProcessMorphEnd(ref Vector2 playerVelocity, float deltaTime)
    {
        // Probably some physics to consider
        return;
    }
    private void ProcessMovementGroundStart(ref Vector2 playerVelocity, float deltaTime)
    {
        // For now, let the velocity be set by MovementGround
        ProcessMovementGround(ref playerVelocity, deltaTime);
    }
    private void ProcessMovementGroundEnd(ref Vector2 playerVelocity, float deltaTime)
    {
        // For now, let the velocity be set by MovementGround
        ProcessMovementGround(ref playerVelocity, deltaTime);
    }
    private void ProcessMovementGroundLTR(ref Vector2 playerVelocity, float deltaTime)
    {
        // For now, let the velocity be set by MovementGround
        ProcessMovementGround(ref playerVelocity, deltaTime);
    }
    private void ProcessMovementGroundRTL(ref Vector2 playerVelocity, float deltaTime)
    {
        // For now, let the velocity be set by MovementGround
        ProcessMovementGround(ref playerVelocity, deltaTime);
    }
    private void ProcessMovementGround(ref Vector2 playerVelocity, float deltaTime)
    {
        // MovementGround
        //
        // 0) Double check the collision capture (not needed, but keep these in there)
        // 1) Movement:
        //      - Acceleration / Deceleration / Auto-Decelration (player lets go of input)
        //      - Max Velocity (clamp)
        //      - Right-to-Left / Left-to-Right transitions (TODO)
        // 
        // For the RTL / LTR transition, be sure to clamp the value at zero when the
        // sign change is detected (for now).
        //


        if (!this.playerCollisionState.CollisionGround.IsSet())
            throw new Exception("ProcessMovementGround:  Detected no ground collision!");

        else
        {
            // Left
            if (playerVelocity.x < 0)
            {
                // Decelerate
                if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Accelerate
                else if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Decelerate (Auto)
                else
                    playerVelocity.x += this.RunAutoDeceleration * Time.deltaTime * accelerationFrameScale;
            }

            // Right
            else
            {
                // Decelerate
                if (this.playerInputState.MoveLeftInput.IsSet())
                    playerVelocity.x -= this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Accelerate
                else if (this.playerInputState.MoveRightInput.IsSet())
                    playerVelocity.x += this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Decelerate (Auto)
                else
                    playerVelocity.x -= this.RunAutoDeceleration * Time.deltaTime * accelerationFrameScale;
            }
        }

        // Max Velocity
        if (playerVelocity.x < -1 * this.MaxRunVelocity)
            playerVelocity.x = -1 * this.MaxRunVelocity;

        // Zero (RTL / LTR)
        if (this.playerFlippedX && playerVelocity.x > 0)
            playerVelocity.x = 0;

        else if (!this.playerFlippedX && playerVelocity.x < 0)
            playerVelocity.x = 0;
    }
    private void ProcessGravity(ref Vector2 playerVelocity)
    {
        // Terminal Velocity
        playerVelocity.y = Math.Max(playerVelocity.y, -1 * this.MaxFallVelocity);
    }

    #region Animation State Update
    private void ProcessStateAnimationUpdate(Vector2 playerVelocity, float deltaTime)
    {
        // Animations: There are two types of animations:  fixed time, and scaled. Any of
        //             the data about the player can be used to see where the animation should
        //             be for scaled animations. Collision detection, velocity, collision layer
        //             proximity, etc...
        //
        //             Some (any) animations may be updated as fixed-time animations. Example:
        //             PlayerState.Idle. There is no parameter to tell you "what time it is". So,
        //             you can call Update(deltaTime) which scales it accordingly, with periodic
        //             repeat.
        //          

        // Calculate a (cheap) jumping scale factor
        var jumpOffset = playerVelocity.y > 0f ? 0.5f - (playerVelocity.y / this.MaxJumpVelocity / 2f) :
                                                 0.5f + (playerVelocity.y / this.MaxFallVelocity / 2f);

        switch (this.PlayerStateAnimator.State)
        {
            // Scaled
            case PlayerState.Morphed:
            case PlayerState.MovementGround:
            case PlayerState.MovementGroundStart:
            case PlayerState.MovementGroundEnd:
            {
                this.PlayerStateAnimator.Update(Math.Abs(playerVelocity.x) / this.MaxRunVelocity);
            }
            break;
            case PlayerState.JumpingNormal:
            {
                this.PlayerStateAnimator.Update(jumpOffset);
            }
            break;
            case PlayerState.JumpStart:
            case PlayerState.JumpEnd:
            {
                // Proximity to ground! TODO
                this.PlayerStateAnimator.Update(deltaTime);
            }
            break;

            // Fixed Time (periodic)
            case PlayerState.Idle:
            case PlayerState.JumpingSpin:
            case PlayerState.MovementGroundLTR:
            case PlayerState.MovementGroundRTL:
            case PlayerState.MorphStart:
            case PlayerState.MorphEnd:
            {
                this.PlayerStateAnimator.Update(deltaTime);
            }
            break;

            default:
                throw new Exception("Unhandled PlayerState:  PlayerController.ProcessStateUpdate(...)");
        }
    }
    private void FlipSpritesX()
    {
        // FLIP-X
        this.playerFlippedX = !this.playerFlippedX;

        this.PlayerSpriteRendererIdle.flipX = this.playerFlippedX;
        this.PlayerSpriteRendererJumpNormal.flipX = this.playerFlippedX;
        this.PlayerSpriteRendererJumpSpin.flipX = this.playerFlippedX;
        this.PlayerSpriteRendererRunning.flipX = this.playerFlippedX;
        this.PlayerSpriteRendererMorph.flipX = this.playerFlippedX;
    }
    #endregion

    private Animator GetAnimator(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                return this.PlayerAnimatorIdle;

            case PlayerState.MovementGround:
                return this.PlayerAnimatorRunning;

            case PlayerState.JumpingNormal:
                return this.PlayerAnimatorJumpNormal;

            case PlayerState.JumpingSpin:
                return this.PlayerAnimatorJumpSpin;

            case PlayerState.Morphed:
                return this.PlayerAnimatorMorph;

            case PlayerState.MovementGroundStart:
                return this.PlayerAnimatorRunning;

            case PlayerState.MovementGroundEnd:
                return this.PlayerAnimatorRunning;

            case PlayerState.MovementGroundLTR:
                return this.PlayerAnimatorRunning;

            case PlayerState.MovementGroundRTL:
                return this.PlayerAnimatorRunning;

            case PlayerState.JumpStart:
                return this.PlayerAnimatorJumpNormal;

            case PlayerState.JumpEnd:
                return this.PlayerAnimatorJumpNormal;

            case PlayerState.MorphStart:
                return this.PlayerAnimatorIdle;

            case PlayerState.MorphEnd:
                return this.PlayerAnimatorIdle;
            default:
                throw new Exception("Unhandled PlayerState:  PlayerController.GetAnimator");
        }
    }
    private SpriteRenderer GetRenderer(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                return this.PlayerSpriteRendererIdle;

            case PlayerState.MovementGround:
                return this.PlayerSpriteRendererRunning;

            case PlayerState.JumpingNormal:
                return this.PlayerSpriteRendererJumpNormal;

            case PlayerState.JumpingSpin:
                return this.PlayerSpriteRendererJumpSpin;

            case PlayerState.Morphed:
                return this.PlayerSpriteRendererMorph;

            case PlayerState.MovementGroundStart:
            case PlayerState.MovementGroundEnd:
            case PlayerState.MovementGroundLTR:
            case PlayerState.MovementGroundRTL:
                return this.PlayerSpriteRendererRunning;

            case PlayerState.JumpStart:
            case PlayerState.JumpEnd:
                return this.PlayerSpriteRendererJumpNormal;

            case PlayerState.MorphStart:
            case PlayerState.MorphEnd:
                return this.PlayerSpriteRendererIdle;
            default:
                throw new Exception("Unhandled PlayerState:  PlayerController.GetAnimator");
        }
    }
    private void SetDiagnostics(PlayerState desiredState)
    {
        this.CollisionCeiling = this.playerCollisionState.CollisionCeiling.IsSet();
        this.CollisionGround = this.playerCollisionState.CollisionGround.IsSet();
        this.CollisionLeft = this.playerCollisionState.CollisionLeft.IsSet();
        this.CollisionRight = this.playerCollisionState.CollisionRight.IsSet();
        this.JumpAccumulatorLast = this.playerInputState.JumpInput.GetCaptureTime();
        this.JumpInputCaptureTime = this.playerInputState.JumpInput.GetAccumulator();

        this.Up = this.playerInputState.MoveUpInput.IsSet();
        this.Down = this.playerInputState.MoveDownInput.IsSet();
        this.Left = this.playerInputState.MoveLeftInput.IsSet();
        this.Right = this.playerInputState.MoveRightInput.IsSet();
        this.Jump = this.playerInputState.JumpInput.IsSet();

        this.State = this.PlayerStateAnimator.State;
        this.NextState = desiredState;
    }
}
