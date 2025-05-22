using UnityEngine;

namespace Assets
{
    public class PlayerStateAnimator
    {
        public PlayerState State { get; private set; }
        protected Animator Animator { get; private set; }
        protected SpriteRenderer Renderer { get; private set; }

        // Time that has elapsed for current state. State animation is assumed to be periodic. This is used
        // for updating fixed-time animations - since we're taking control of animator.
        protected float TimeElapsed { get; private set; }

        private string _timeScalePropertyName;


        /// <summary>
        /// Sets animator / renderer for current state. All animators are assumed to have a TimeScale property (which
        /// was added manually for the player animators). If it is fixed time, the update will handle scaling out the
        /// deltaTime to the right frame.
        /// </summary>
        public PlayerStateAnimator(PlayerState currentState,
                                   Animator currentAnimator,
                                   SpriteRenderer currentRenderer,
                                   string timeScalePropertyName)
        {
            this.State = currentState;
            this.Animator = currentAnimator;
            this.Renderer = currentRenderer;

            // Begin
            this.Animator.enabled = true;
            this.Renderer.enabled = true;
            this.TimeElapsed = 0;

            _timeScalePropertyName = timeScalePropertyName;
        }

        public void Set(PlayerState nextState,
                        Animator nextAnimator,
                        SpriteRenderer nextRenderer)
        {
            // Disable (current state)
            this.Animator.enabled = false;
            this.Renderer.enabled = false;

            this.State = nextState;
            this.Animator = nextAnimator;
            this.Renderer = nextRenderer;
            this.TimeElapsed = 0;

            // Enable (next state)
            this.Animator.enabled = true;
            this.Renderer.enabled = true;
        }

        /// <summary>
        /// Updates animators (using scaled mode), but calculates update based on playback time.
        /// </summary>
        public void Update(float deltaTime)
        {
            this.TimeElapsed += deltaTime;

            if (this.TimeElapsed > this.Animator.playbackTime)
                this.TimeElapsed = 0;

            this.Animator.SetFloat(_timeScalePropertyName, this.TimeElapsed / this.Animator.playbackTime);
        }

        /// <summary>
        /// Updates animators using the scale; and sets time accumulator to playback time (based on the scale)
        /// </summary>
        public void UpdateTo(float scale)
        {
            this.TimeElapsed = scale * this.Animator.playbackTime;

            this.Animator.SetFloat(_timeScalePropertyName, scale);
        }

        /// <summary>
        /// Returns true if the animation has played through (once).
        /// </summary>
        public bool IsFinished()
        {
            return this.TimeElapsed >= this.Animator.playbackTime;
        }
    }
}
