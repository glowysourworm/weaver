using System;

using UnityEditor;

using UnityEngine;

[Flags]
public enum PlayerState
{
    Idle = 0,               // Assumed to be mutually exclusive to Running / Jumping
    Running = 1,            // Can combine Running / Jumping (for motion)
    JumpStart = 2,
    Jumping = 4             // Animations are handled during these states based on motion
}

public struct InputDetector
{
    bool input;
    float inputOnTime;
    float accumulator;

    /// <summary>
    /// Sets current time; and returns true if the current time represents a new
    /// input capture for the detector.
    /// </summary>
    public bool Set(bool nextInput)
    {
        // End Capture
        if (!nextInput)
        {
            // Never Started
            if (!input)
            {
                // Nothing to do
            }

            // End
            else
            {
                inputOnTime = accumulator;
                accumulator = 0;

                input = nextInput;
            }
        }

        // Start / Continue Capture
        else
        {
            // Start
            if (!input)
            {
                input = nextInput;
                return true;            // Return "NEW CAPTURE"
            }

            // Continue
            else
                accumulator += Time.deltaTime;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the input is currently set
    /// </summary>
    public bool IsSet()
    {
        return input;
    }

    /// <summary>
    /// Gets current capture result time (which holds its value after detector is unset)
    /// </summary>
    public float GetCaptureTime()
    {
        return inputOnTime;
    }

    /// <summary>
    /// Gets accumulator for current capture
    /// </summary>
    /// <returns></returns>
    public float GetAccumulator()
    {
        return accumulator;
    }
}

public class CollisionState
{
    // MULTIPLE COLLISION DETECTORS CAUSING RIGID BODY TO FREEZE
    //
    // (Until we get acquainted with Unity2D, lets get rid of the extra "ALL" tilemap)
    //

    //public const string COLLISION_ALL = "CollisionAll";
    public const string COLLISION_LEFT = "CollisionLeft";
    public const string COLLISION_RIGHT = "CollisionRight";
    public const string COLLISION_CEILING = "CollisionCeiling";
    public const string COLLISION_GROUND = "CollisionGround";

    //public InputDetector CollisionAll;
    public InputDetector CollisionLeft;
    public InputDetector CollisionRight;
    public InputDetector CollisionCeiling;
    public InputDetector CollisionGround;

    public CollisionState()
    {
        //this.CollisionAll = new InputDetector();
        this.CollisionLeft = new InputDetector();
        this.CollisionRight = new InputDetector();
        this.CollisionCeiling = new InputDetector();
        this.CollisionGround = new InputDetector();
    }

    public void Capture(Rigidbody2D player)
    {
        //this.CollisionAll.Set(player.IsTouchingLayers(LayerMask.GetMask(COLLISION_ALL)));
        this.CollisionLeft.Set(player.IsTouchingLayers(LayerMask.GetMask(COLLISION_LEFT)));
        this.CollisionRight.Set(player.IsTouchingLayers(LayerMask.GetMask(COLLISION_RIGHT)));
        this.CollisionGround.Set(player.IsTouchingLayers(LayerMask.GetMask(COLLISION_GROUND)));
        this.CollisionCeiling.Set(player.IsTouchingLayers(LayerMask.GetMask(COLLISION_CEILING)));
    }
}

public class PlayerInputState
{
    public InputDetector JumpInput;
    public InputDetector MoveLeftInput;
    public InputDetector MoveRightInput;
    public InputDetector MoveUpInput;
    public InputDetector MoveDownInput;

    public PlayerInputState()
    {
        this.JumpInput = new InputDetector();
        this.MoveLeftInput = new InputDetector();
        this.MoveRightInput = new InputDetector();
        this.MoveUpInput = new InputDetector();
        this.MoveDownInput = new InputDetector();
    }

    public void Capture(Rigidbody2D player)
    {
        // Jump
        this.JumpInput.Set(Input.GetKeyDown(KeyCode.F) || Input.GetKey(KeyCode.F));

        // Up
        this.MoveUpInput.Set(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKey(KeyCode.UpArrow));

        // Down
        this.MoveDownInput.Set(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKey(KeyCode.DownArrow));

        // Left
        this.MoveLeftInput.Set(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKey(KeyCode.LeftArrow));

        // Right
        this.MoveRightInput.Set(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKey(KeyCode.RightArrow));
    }
}

public class ReadOnlyAttribute : PropertyAttribute
{

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        GUI.enabled = false;

        string valueStr = "";

        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                valueStr = property.intValue.ToString();
                break;
            case SerializedPropertyType.Boolean:
                valueStr = property.boolValue.ToString();
                break;
            case SerializedPropertyType.Float:
                valueStr = property.floatValue.ToString("0.00000");
                break;
            case SerializedPropertyType.String:
                valueStr = property.stringValue;
                break;
            case SerializedPropertyType.Enum:

                // OUR SPECIAL FLAGS CASE
                if (property.name == "State")
                {
                    if (property.enumValueFlag == 0)
                        valueStr = "Idle";

                    else
                    {
                        if (((PlayerState)property.enumValueFlag & PlayerState.Running) != 0)
                            valueStr += "Running";

                        if (((PlayerState)property.enumValueFlag & PlayerState.JumpStart) != 0)
                            valueStr += string.IsNullOrEmpty(valueStr) ? "JumpStart" : " | JumpStart";

                        if (((PlayerState)property.enumValueFlag & PlayerState.Jumping) != 0)
                            valueStr += string.IsNullOrEmpty(valueStr) ? "Jumping" : " | Jumping";
                    }
                }
                else
                {
                    // Caught one error for this one; but not every time... (hmm.)
                    if (property.enumValueIndex >= 0 &&
                        property.enumValueIndex < property.enumDisplayNames.Length)
                        valueStr = property.enumDisplayNames[property.enumValueIndex];
                }


                break;
            default:
                valueStr = "(not supported)";
                break;
        }

        EditorGUI.LabelField(position, label.text, valueStr);

        GUI.enabled = true;
    }
}

public class PlayerController : MonoBehaviour
{
    public Rigidbody2D Player;
    public PlayerState MovementState = PlayerState.Idle;

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
    [ReadOnly] public float Gravity = 10f;

    [ReadOnly] public bool CollisionGround;
    [ReadOnly] public bool CollisionCeiling;
    [ReadOnly] public bool CollisionLeft;
    [ReadOnly] public bool CollisionRight;
    [ReadOnly] public float JumpAccumulatorLast;
    [ReadOnly] public float JumpInputCaptureTime;

    [ReadOnly] public bool Up;
    [ReadOnly] public bool Down;
    [ReadOnly] public bool Left;
    [ReadOnly] public bool Right;
    [ReadOnly] public bool Jump;

    [ReadOnly] public PlayerState State;

    Vector2 playerVelocity;
    Vector2 playerAcceleration;

    // Scaling:
    //
    // 0) Gravity = 1
    // 1) Everything else (all accelerations) relative to Gravity (from user end)
    // 2) Set this parameter to make those work (per frame)
    //      -> Multiply the result of each calculation before
    //         setting the primary rigid body memory
    //
    float accelerationFrameScale = 100f;

    PlayerState playerState;
    PlayerInputState playerInputState;
    CollisionState playerCollisionState;

    bool playerFlippedX;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerState = PlayerState.Idle;
        playerVelocity = new Vector2();
        playerAcceleration = new Vector2();
        playerCollisionState = new CollisionState();
        playerInputState = new PlayerInputState();
        playerFlippedX = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // Setup Player Velocity (use current "memory" if it is managed for this Vector2)
        //
        playerVelocity = this.Player.linearVelocity;

        // Capture Input State
        this.playerInputState.Capture(this.Player);

        // Capture Collision State
        this.playerCollisionState.Capture(this.Player);

        // Process Player State Update:  Updates the playerVelocity / playerAcceleration / playerState variables
        ProcessPlayerStateUpdate();

        // Update Player Animators
        SetPlayerStateAnimation();

        // Update some of our diagnostics
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

        this.State = this.playerState;

        // Finally, Set Player Rigid Body Update (up to owners of the struct to say how memory is managed)
        //
        this.Player.linearVelocity = playerVelocity;
    }

    private void ProcessPlayerStateUpdate()
    {
        // Design:  The states of motion should all follow a very clear / concise state machine
        //          implementation. This will help to avoid fidgeting with issues that arise from
        //          complexities of the rendering and 2D physics.
        //
        //          There will be Process[State] methods in the following switch statement. Each
        //          Process[State] method should only detail its own state manipulation - apart
        //          from any other coupled state. For example:  ProcessRunning sets ~Running
        //          to player state; but does not affect the animators (other than left-right flip
        //          local transform)
        //
        //          The motion for some of the player's animation states has a 2-frame "Start" 
        //          sequence. So, the Start (state) has been separated to a separate logic sequence
        //          which will accompany user inputs. The states should also separate X from Y inputs
        //          to decouple any of the complexities from state changing.
        //
        // Procedure:
        //
        // 0) Capture Collision / Input data for the frame (should have already been completed)
        // 1) Process Current State
        // 2) Set Current State Animation (key frames)
        //

        // PLAYER STATE (Can safely process X-Y dimensions separately)
        switch (this.playerState)
        {
            case PlayerState.Running | PlayerState.Jumping:
            {
                ProcessRunning();
                ProcessJumping();
            }
            break;

            case PlayerState.Running | PlayerState.JumpStart:
            {
                ProcessRunning();
                ProcessJumpStart();
            }
            break;

            case PlayerState.Running:
            {
                ProcessRunning();

                // -> JumpStart (~Jumping, Jump Input Set, On Ground)
                if ((this.playerState & PlayerState.Jumping) == 0 &&
                    this.playerInputState.JumpInput.IsSet() &&
                    this.playerCollisionState.CollisionGround.IsSet())
                    this.playerState |= PlayerState.JumpStart;
            }
            break;
            case PlayerState.JumpStart:
            {
                ProcessJumpStart();

                // -> Left / Right
                if (this.playerInputState.MoveLeftInput.IsSet() ||
                    this.playerInputState.MoveRightInput.IsSet())
                    this.playerState |= PlayerState.Running;
            }
            break;
            case PlayerState.Jumping:
            {
                ProcessJumping();

                // -> Left / Right
                if (this.playerInputState.MoveLeftInput.IsSet() ||
                    this.playerInputState.MoveRightInput.IsSet())
                    this.playerState |= PlayerState.Running;
            }
            break;

            case PlayerState.Idle:
            {
                // -> JumpStart (~Jumping, Jump Input Set, On Ground)
                if ((this.playerState & PlayerState.Jumping) == 0 &&
                    this.playerInputState.JumpInput.IsSet() &&
                    this.playerCollisionState.CollisionGround.IsSet())
                    this.playerState |= PlayerState.JumpStart;

                // -> Left / Right
                if (this.playerInputState.MoveLeftInput.IsSet() ||
                    this.playerInputState.MoveRightInput.IsSet())
                    this.playerState |= PlayerState.Running;

                else
                {
                    // Nothing to do
                }
            }
            break;


            default:
                break;
        }
    }

    private void SetPlayerStateAnimation()
    {
        // Calculate jump offset (if jumping, this will be used)
        var jumpOffset = this.playerVelocity.y > 0f ? 0.5f - (this.playerVelocity.y / this.MaxJumpVelocity / 2f) :
                                                      0.5f + (this.playerVelocity.y / this.MaxFallVelocity / 2f);

        // NOTE:  The "enabled" setting is the checkbox next to the component in the
        //        UI. This will be enabled / disabled for sprite renderers

        // Enable Proper Sprite Renderers
        switch (this.playerState)
        {
            // Jumping Takes Precedence
            case PlayerState.Running | PlayerState.Jumping:
            case PlayerState.Running | PlayerState.JumpStart:
            case PlayerState.JumpStart:
            case PlayerState.Jumping:
            {
                // Be careful to set this once (not every frame)
                if (!this.PlayerSpriteRendererJumpNormal.enabled)
                {
                    this.PlayerSpriteRendererJumpNormal.enabled = true;
                    this.PlayerSpriteRendererJumpSpin.enabled = false;
                    this.PlayerSpriteRendererIdle.enabled = false;
                    this.PlayerSpriteRendererRunning.enabled = false;
                    this.PlayerSpriteRendererMorph.enabled = false;
                }
            }
            break;

            // Running
            case PlayerState.Running:

                // Be careful to set this once (not every frame)
                if (!this.PlayerSpriteRendererRunning.enabled)
                {
                    this.PlayerSpriteRendererJumpNormal.enabled = false;
                    this.PlayerSpriteRendererJumpSpin.enabled = false;
                    this.PlayerSpriteRendererIdle.enabled = false;
                    this.PlayerSpriteRendererRunning.enabled = true;
                    this.PlayerSpriteRendererMorph.enabled = false;
                }
                break;

            // Idle
            case PlayerState.Idle:
            default:

                // Be careful to set this once (not every frame)
                if (!this.PlayerSpriteRendererIdle.enabled)
                {
                    this.PlayerSpriteRendererJumpNormal.enabled = false;
                    this.PlayerSpriteRendererJumpSpin.enabled = false;
                    this.PlayerSpriteRendererIdle.enabled = true;
                    this.PlayerSpriteRendererRunning.enabled = false;
                    this.PlayerSpriteRendererMorph.enabled = false;
                }
                break;
        }

        // Change State
        switch (this.playerState)
        {
            case PlayerState.Running | PlayerState.Jumping:

                // Set Frame (using time scale)
                this.PlayerAnimatorJumpNormal.SetFloat("TimeScale", jumpOffset);
                this.PlayerAnimatorJumpSpin.SetFloat("TimeScale", jumpOffset);

                break;
            case PlayerState.Running | PlayerState.JumpStart:

                // Set Frame (using time scale)
                this.PlayerAnimatorJumpNormal.SetFloat("TimeScale", jumpOffset);
                this.PlayerAnimatorJumpSpin.SetFloat("TimeScale", jumpOffset);

                break;
            case PlayerState.Running:

                // Set Frame (using time scale)
                this.PlayerAnimatorRunning.SetFloat("TimeScale", Math.Abs(this.Player.linearVelocityX) / this.MaxRunVelocity);
                break;
            case PlayerState.JumpStart:

                // Set Frame (using time scale)
                this.PlayerAnimatorJumpNormal.SetFloat("TimeScale", jumpOffset);
                this.PlayerAnimatorJumpSpin.SetFloat("TimeScale", jumpOffset);

                break;
            case PlayerState.Jumping:

                // Set Frame (using time scale)
                this.PlayerAnimatorJumpNormal.SetFloat("TimeScale", jumpOffset);
                this.PlayerAnimatorJumpSpin.SetFloat("TimeScale", jumpOffset);
                break;
            case PlayerState.Idle:
                break;
            default:
                break;
        }

    }

    // Movement in Y-Direction Only
    private void ProcessJumpStart()
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        // Start of Jump
        if (this.playerInputState.JumpInput.IsSet() &&
            this.playerInputState.JumpInput.GetAccumulator() < this.JumpInputTime)
        {
            // FIX JUMP ACCELERATION (Needs capture also...)
            playerVelocity.y = this.MaxJumpVelocity;
            //playerVelocity.y += (this.JumpAcceleration * Time.deltaTime * accelerationFrameScale);
        }

        // -> Jumping (in the air)
        else
        {
            // ~JumpStart (remove start state)
            this.playerState &= ~PlayerState.JumpStart;

            // -> Jumping
            this.playerState |= PlayerState.Jumping;
        }
    }
    private void ProcessJumping()
    {
        // Jump Envelope:
        //
        // 0) On Ground:       Capture jump input
        // 1) Start of Jump:   Capture jump input time (for brief configured delta)
        // 2) Jumping:         Take the actual jump velocity is calculated at this point, then left to gravity.
        //

        // Jumping (in the air)
        if (!this.playerCollisionState.CollisionGround.IsSet())
        {
            // Nothing to do
        }

        // -> ~Jumping (on the ground)
        else
            this.playerState &= ~PlayerState.Jumping;
    }

    // Movement in X-Direction Only
    private void ProcessRunning()
    {
        // Running Envelope:
        //
        // 0) On Ground:       Capture move inputs (flip animation accordingly)
        // 1) Start Running:   Accelerate until max velocity / Decelerate until idle
        // 2) Running:         Max Velocity... / Decelerate until idle
        //

        // -> Left
        if (this.playerInputState.MoveLeftInput.IsSet())
        {
            // Flip Animation X
            if (!this.playerFlippedX)
                FlipSpritesX();

            // Moving Right:  Decelerate
            if (playerVelocity.x > 0)
                playerVelocity.x -= this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

            // Moving Left / Idle: Accelerate
            else
                playerVelocity.x -= this.RunAcceleration * Time.deltaTime * accelerationFrameScale;

            // Max Velocity (Clamp)
            if (Math.Abs(playerVelocity.x) > this.MaxRunVelocity)
                playerVelocity.x = Math.Clamp(playerVelocity.x, -1 * this.MaxRunVelocity, this.MaxRunVelocity);
        }

        // -> Right
        else if (this.playerInputState.MoveRightInput.IsSet())
        {
            // Un-Flip Animation X
            if (this.playerFlippedX)
                FlipSpritesX();

            // Moving Left:  Decelerate
            if (playerVelocity.x < 0)
                playerVelocity.x += this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

            // Moving Right / Idle: Accelerate
            else
                playerVelocity.x += this.RunAcceleration * Time.deltaTime * accelerationFrameScale;

            // Max Velocity (Clamp)
            if (playerVelocity.x > this.MaxRunVelocity)
                playerVelocity.x = Math.Clamp(playerVelocity.x, -1 * this.MaxRunVelocity, this.MaxRunVelocity);
        }

        else
        {
            // Decelerate Until Idle (moving left)
            if (playerVelocity.x < 0)
            {
                playerVelocity.x += this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Clamp at zero
                playerVelocity.x = Math.Min(playerVelocity.x, 0);
            }

            else if (playerVelocity.x > 0)
            {
                playerVelocity.x -= this.RunDeceleration * Time.deltaTime * accelerationFrameScale;

                // Clamp at zero
                playerVelocity.x = Math.Max(playerVelocity.x, 0);
            }

            // -> Idle
            else
                this.playerState &= ~PlayerState.Running;               // Remove Running State
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

        // TRYING NOT TO FIDGET WITH SPRITES

        // There is a convenience setting called "FlipX"; but we need to learn how to tweak 
        // these "local" transforms. They should all be relative to Player
        //
        //var localScaleTop = this.PlayerSpriteRendererTop.transform.localScale;
        //var localPositionTop = this.PlayerSpriteRendererTop.transform.localPosition;
        //var localScaleBottom = this.PlayerSpriteRendererBottom.transform.localScale;

        // TOP:  Flip Horizontally; and add necessary offset for sprite size mismatch
        //localScaleTop.x *= -1;

        // Grid / Sprite Sizes / Relative Scales
        // -------------------------------------
        //
        // Grid Cell Size:             16px
        // Difference in Top / Bottom: 36 - 25 = 11px
        // Bottom Scale:               2.25
        // Top Scale:                  1.5625
        //
        // Local Cell Size = 25px
        // 
        // So, the local offset is 5px = 1 / 5 (th) of a cell = 0.2;
        //
        //localPositionTop.x += this.playerFlippedX ? -0.2f : 0.2f;

        //this.PlayerSpriteRendererTop.transform.localScale = localScaleTop;
        //this.PlayerSpriteRendererTop.transform.localPosition = localPositionTop;

        // BOTTOM: Flip Horizontally
        //localScaleBottom.x *= -1;

        //this.PlayerSpriteRendererBottom.transform.localScale = localScaleBottom;

        // IDLE: Flip Horizontally
        //this.PlayerSpriteRendererIdle.flipX = this.playerFlippedX;
    }
}
