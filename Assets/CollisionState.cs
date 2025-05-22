using UnityEngine;

namespace Assets
{
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
}
