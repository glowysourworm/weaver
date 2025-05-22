using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
    /// <summary>
    /// Each PlayerState corresponds to a separate animator
    /// </summary>
    public enum PlayerState
    {
        Idle = 0,
        MovementGround = 1,
        JumpingNormal = 2,
        JumpingSpin = 3,
        Morphed = 4,
        MovementGroundStart = 5,
        MovementGroundEnd = 6,
        MovementGroundLTR = 7,
        MovementGroundRTL = 8,
        JumpStart = 9,
        JumpEnd = 10,
        MorphStart = 11,
        MorphEnd = 12
    }
}
