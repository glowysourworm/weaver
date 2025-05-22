using UnityEngine;

namespace Assets
{
    public class InputDetector
    {
        bool isFirstInput;      // Detects a single-frame first input before accumulation
        bool input;
        float inputOnTime;
        float accumulator;

        /// <summary>
        /// Sets current time; and returns true if the current time represents a new
        /// input capture for the detector.
        /// </summary>
        public void Set(bool nextInput)
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
                    isFirstInput = false;

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
                    isFirstInput = true;    // NEW CAPTURE
                }

                // Continue
                else
                {
                    accumulator += Time.deltaTime;
                    isFirstInput = false;
                }
            }
        }

        /// <summary>
        /// Returns true if the input is currently set
        /// </summary>
        public bool IsSet()
        {
            return input;
        }

        /// <summary>
        /// Returns true for one frame on first signal
        /// </summary>
        public bool IsFirst()
        {
            return isFirstInput;
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
}
