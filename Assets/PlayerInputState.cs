using UnityEngine;

namespace Assets
{
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
}
