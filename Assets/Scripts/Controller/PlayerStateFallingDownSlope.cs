using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateFallingDownSlope : PlayerState
{
    public PlayerStateFallingDownSlope() : base(PlayerStatesList.FALLINGDOWNSLOPE)
    {

    }

    public override void Begin()
    {
        _player.StartFalling();
    }

    public override void StateUpdate()
    {
        _player.FallDownSlope();
        _player.CheckForGround();
        _player.CheckForNoGround();
    }

    public override void Exit()
    {
        _player.ResetSlopeMovement();
    }

}
