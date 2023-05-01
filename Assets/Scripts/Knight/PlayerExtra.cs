using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Player
{
    public struct FrameInput
    {
        public float X;
        public bool JumpDown;
        public bool JumpUp;
        public bool Attack;
    }

    public interface IPlayerController
    { 
        Vector3 Velocity { get; }
        FrameInput Input { get; }
        bool JumpThisFrame { get; }
        bool LandingThisFrame { get; }
        Vector3 RawMovement { get; }
        bool Grounded { get; }
    }

    //…‰œﬂºÏ≤‚∑∂Œß
    public struct RayRange
    {
        public RayRange(float x1, float y1, float x2, float y2, Vector2 dir)
        {
            Start = new Vector2(x1, y1);
            End = new Vector2(x2, y2);
            Dir = dir;
        }
        public readonly Vector2 Start, End, Dir;
    }




}


