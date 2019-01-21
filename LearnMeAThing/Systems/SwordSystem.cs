using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    /// <summary>
    /// Handles the positioning, creation, and transition of a sword in response to a swing.
    /// </summary>
    sealed class SwordSystem: ASystem<EntityManager.EntitiesWithStatefulComponentEnumerable<SwordComponent>>
    {
        public override SystemType Type => SystemType.Sword;

        public override EntityManager.EntitiesWithStatefulComponentEnumerable<SwordComponent> DesiredEntities(EntityManager manager)
        => manager.EntitiesWithSword();

        private Buffer<Vector> SwordTinkedOff;

        public SwordSystem(int maximumPendingHits)
        {
            SwordTinkedOff = new Buffer<Vector>(maximumPendingHits);
        }

        public void CollidedWith(GameState state, Entity sword, Entity other, Vector pushDir)
        {
            var manager = state.EntityManager;
            // don't care about the sword for now, they're all owned the player
            //   and the responses are all the same

            var flagsRes = manager.GetFlagComponentsForEntity(other);
            if(!flagsRes.Success)
            {
                // glitch: ???
                return;
            }

            var flags = flagsRes.Value;

            // if they don't take damage, then we should tink off of them
            if (!flags.HasFlag(FlagComponent.TakesDamage))
            {
                // no need to track literally everything
                if (SwordTinkedOff.IsFull || SwordTinkedOff.Contains(pushDir)) return;

                SwordTinkedOff.Add(pushDir);
            }
        }

        public override void Update(GameState state, EntityManager.EntitiesWithStatefulComponentEnumerable<SwordComponent> requestedEntities)
        {
            foreach (var sword in requestedEntities)
            {
                var e = sword.Entity;
                var c = sword.Component;

                HandleSwordState(state, e, c);
            }

            HandleTinks(state, requestedEntities);

            // we do this after the fact, because it might create new entities
            //   and those won't be handled by the above
            var playerInput = state.EntityManager.GetInputsFor(state.Player_Feet);
            HandlePlayerInput(state, playerInput);
        }

        private void HandleTinks(GameState state, EntityManager.EntitiesWithStatefulComponentEnumerable<SwordComponent> swords)
        {
            const int PUSH_SUBPIXELS = 8 * PositionComponent.SUBPIXELS_PER_PIXEL;
            const int OVER_TICKS = 5;

            if (SwordTinkedOff.Count == 0) return;

            // hey, we did tink off of something!

            // determine the direction we should push the user
            var finalPush = Vector.Zero;
            for(var i = 0; i < SwordTinkedOff.Count; i++)
            {
                var v = SwordTinkedOff[i];
                finalPush += v;
            }
            SwordTinkedOff.Clear();
            finalPush = finalPush.Normalize();

            // stop all the swords from swinging
            foreach(var sword in swords)
            {
                StopSwordSwing(state, sword.Entity);
            }

            // accelerate the player away in `finalPush` direction
            var manager = state.EntityManager;
            var accel = manager.GetAccelerationFor(state.Player_Feet);
            if(accel == null)
            {
                var accelRes = manager.CreateAcceleration(0, 0, 0);
                if (!accelRes.Success)
                {
                    // glitch: ???
                    return;
                }
                accel = accelRes.Value;
                manager.AddComponent(state.Player_Feet, accel);
            }

            var transition = new Vector(finalPush.DeltaX * PUSH_SUBPIXELS, finalPush.DeltaY * PUSH_SUBPIXELS);
            accel.Push(transition, OVER_TICKS);
        }

        private static void HandleSwordState(GameState state, Entity sword, SwordComponent swordState)
        {
            // todo: may don't hard code this?
            var controlling = state.Player_Feet;

            var manager = state.EntityManager;
            var playerPos = manager.GetPositionFor(controlling);
            if (playerPos == null) return;

            var playerState = manager.GetPlayerStateFor(controlling);
            if (playerState == null) return;

            var swordPos = manager.GetPositionFor(sword);
            if (swordPos == null) return;

            var swordAnim = manager.GetAnimationFor(sword);
            if (swordAnim == null) return;

            var swordCollision = manager.GetCollisionFor(sword);
            if (swordCollision == null) return;

            var swordVelocity = manager.GetVelocityFor(sword);
            if (swordVelocity == null) return;

            var activeAnimation = state.AnimationManager.Get(swordAnim.Name);
            var doneAfter = activeAnimation.StepAfter * activeAnimation.Frames.Length;
            var done = swordAnim.TickCounter >= doneAfter;

            if (done)
            {
                StopSwordSwing(state, sword);
                return;
            }

            var curFrame = swordAnim.GetCurrentFrame(state.AnimationManager);
            swordCollision.HitMap = curFrame;

            StickToPlayer(state, playerPos, playerState, swordPos, swordAnim);
            SetVelocity(swordAnim.Name, swordVelocity);
        }

        private static void SetVelocity(AnimationNames swing, VelocityComponent swordVel)
        {
            // technically we don't want the velocity to ever actually _change_ the position
            //   but we need some motion so the collision system cares
            switch (swing)
            {
                case AnimationNames.Sword_Top:
                    swordVel.X_SubPixels = -1;
                    break;
                case AnimationNames.Sword_Left:
                    swordVel.Y_SubPixels = 1;
                    break;
                case AnimationNames.Sword_Right:
                    swordVel.Y_SubPixels = -1;
                    break;
                case AnimationNames.Sword_Bottom:
                    swordVel.X_SubPixels = 1;
                    break;
            }
        }

        private static void StickToPlayer(GameState state, PositionComponent playerPos, PlayerStateComponent playerState, PositionComponent swordPos, AnimationComponent swordAnim)
        {
            var torsoDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
            var facing = playerState.GetFacingDirection();
            if (facing == null) return;

            GetBasicSwordPosition(playerPos, facing.Value, torsoDims, out var swordX, out var swordY, out _, out _);

            if (playerState.SwingingDirection == PlayerSwinging.East)
            {
                // swing on the right from the top down
                switch (swordAnim.GetCurrentFrame(state.AnimationManager))
                {
                    
                    case AssetNames.Sword_RightTop4:
                        swordX -= 28;
                        swordY -= 10;
                        break;
                    case AssetNames.Sword_RightTop3:
                        swordX -= 28;
                        swordY -= 12;
                        break;
                    case AssetNames.Sword_RightTop2:
                        swordX -= 14;
                        swordY -= 8;
                        break;
                    case AssetNames.Sword_RightTop1:
                        swordX -= 14;
                        swordY += 6;
                        break;
                    case AssetNames.Sword_Right:
                        swordX -= 14;
                        swordY += 8;
                        break;
                    case AssetNames.Sword_BottomRight4:
                        swordX -= 14;
                        swordY += 12;
                        break;
                    case AssetNames.Sword_BottomRight3:
                        swordX -= 14;
                        swordY += 28;
                        break;
                    case AssetNames.Sword_BottomRight2:
                        swordX -= 30;
                        swordY += 34;
                        break;
                    case AssetNames.Sword_BottomRight1:
                        swordX -= 32;
                        swordY += 28;
                        break;
                }
            }

            if (playerState.SwingingDirection == PlayerSwinging.West)
            {
                // swinging on the left, from the top down
                switch (swordAnim.GetCurrentFrame(state.AnimationManager))
                {
                    
                    case AssetNames.Sword_TopLeft1:
                        swordX -= 32;
                        swordY -= 20;
                        break;
                    case AssetNames.Sword_TopLeft2:
                        swordX -= 36;
                        swordY -= 20;
                        break;
                    case AssetNames.Sword_TopLeft3:
                        swordX -= 50;
                        swordY -= 18;
                        break;
                    case AssetNames.Sword_TopLeft4:
                        swordX -= 50;
                        swordY -= 2;
                        break;
                    case AssetNames.Sword_Left:
                        swordX -= 48;
                        swordY -= 2;
                        break;
                    case AssetNames.Sword_LeftBottom1:
                        swordX -= 48;
                        swordY += 4;
                        break;
                    case AssetNames.Sword_LeftBottom2:
                        swordX -= 48;
                        swordY += 18;
                        break;
                    case AssetNames.Sword_LeftBottom3:
                        swordX -= 36;
                        swordY += 22;
                        break;
                    case AssetNames.Sword_LeftBottom4:
                        swordX -= 34;
                        swordY += 20;
                        break;
                }
            }

            if(playerState.SwingingDirection == PlayerSwinging.North)
            {
                switch (swordAnim.GetCurrentFrame(state.AnimationManager))
                {
                    case AssetNames.Sword_Top:
                        swordX -= 30;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_TopLeft1:
                        swordX -= 32;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_TopLeft2:
                        swordX -= 34;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_TopLeft3:
                        swordX -= 48;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_TopLeft4:
                        swordX -= 48;
                        swordY -= 64;
                        break;
                    case AssetNames.Sword_RightTop4:
                        swordX -= 28;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_RightTop3:
                        swordX -= 26;
                        swordY -= 78;
                        break;
                    case AssetNames.Sword_RightTop2:
                        swordX -= 12;
                        swordY -= 74;
                        break;
                    case AssetNames.Sword_RightTop1:
                        swordX -= 12;
                        swordY -= 64;
                        break;
                }
            }

            if (playerState.SwingingDirection == PlayerSwinging.South)
            {
                switch (swordAnim.GetCurrentFrame(state.AnimationManager))
                {
                    case AssetNames.Sword_Bottom:
                        swordX -= 12;
                        swordY -= 12;
                        break;
                    case AssetNames.Sword_LeftBottom4:
                        swordX -= 14;
                        swordY -= 12;
                        break;
                    case AssetNames.Sword_LeftBottom3:
                        swordX -= 16;
                        swordY -= 8;
                        break;
                    case AssetNames.Sword_LeftBottom2:
                        swordX -= 30;
                        swordY -= 10;
                        break;
                    case AssetNames.Sword_LeftBottom1:
                        swordX -= 30;
                        swordY -= 26;
                        break;
                    case AssetNames.Sword_BottomRight1:
                        swordX -= 10;
                        swordY -= 12;
                        break;
                    case AssetNames.Sword_BottomRight2:
                        swordX -= 6;
                        swordY -= 8;
                        break;
                    case AssetNames.Sword_BottomRight3:
                        swordX += 6;
                        swordY -= 8;
                        break;
                    case AssetNames.Sword_BottomRight4:
                        swordX += 6;
                        swordY -= 24;
                        break;
                }
            }

            swordPos.X_SubPixel = swordX * PositionComponent.SUBPIXELS_PER_PIXEL;
            swordPos.Y_SubPixel = swordY * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        private static void HandlePlayerInput(GameState state, InputsComponent playerInputs)
        {
            if(!playerInputs.PreviousSwingSword && playerInputs.SwingSword)
            {
                StopSwordSwing(state, null);
                var newSword = StartSwordSwing(state);
                if (newSword.Success)
                {
                    var swordState = state.EntityManager.GetSwordFor(newSword.Value);
                    if (swordState != null)
                    {
                        HandleSwordState(state, newSword.Value, swordState);
                    }
                }
            }
        }

        private static Result<Entity> StartSwordSwing(GameState state)
        {
            var manager = state.EntityManager;
            var player = state.Player_Feet;

            var playerPos = manager.GetPositionFor(player);
            if (playerPos == null)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }
            var playerState = manager.GetPlayerStateFor(player);
            if (playerState == null)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }

            var facingDirection = playerState.GetFacingDirection();
            if (facingDirection == null)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }

            var torsoDims = state.AssetMeasurer.Measure(AssetNames.Player_Body);
            GetBasicSwordPosition(playerPos, facingDirection.Value, torsoDims, out var swordX, out var swordY, out var swingDir, out var swordAnim);

            if (swordAnim == null)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }

            var swordRes = manager.NewEntity();
            if (!swordRes.Success)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }

            var res = ObjectCreator.CreateSword(state, swordX, swordY, swordAnim.Value);
            if (!res.Success)
            {
                // glitch: ???
                return Result.FailFor<Entity>();
            }

            playerState.Swing(swingDir);

            return res;
        }

        private static void GetBasicSwordPosition(
            PositionComponent playerPos, 
            PlayerFacing facingDirection, 
            (int Width, int Height) torsoDims,
            out int swordX, 
            out int swordY, 
            out PlayerSwinging swingDir,
            out AnimationNames? swordAnim
        )
        {
            switch (facingDirection)
            {
                case PlayerFacing.North:
                    swordX = playerPos.X + torsoDims.Width / 2;
                    swordY = playerPos.Y - torsoDims.Height;
                    swordAnim = AnimationNames.Sword_Top;
                    swingDir = PlayerSwinging.North;
                    break;
                case PlayerFacing.South:
                    swordX = playerPos.X + torsoDims.Width / 2;
                    swordY = playerPos.Y;
                    swordAnim = AnimationNames.Sword_Bottom;
                    swingDir = PlayerSwinging.South;
                    break;
                case PlayerFacing.East:
                    swordX = playerPos.X + torsoDims.Width;
                    swordY = playerPos.Y - 54;
                    swordAnim = AnimationNames.Sword_Right;
                    swingDir = PlayerSwinging.East;
                    break;
                case PlayerFacing.West:
                    swordX = playerPos.X;
                    swordY = playerPos.Y - 54;
                    swordAnim = AnimationNames.Sword_Left;
                    swingDir = PlayerSwinging.West;
                    break;

                default:
                    // glitch: ???
                    swordX = 0;
                    swordY = 0;
                    swingDir = PlayerSwinging.NONE;
                    swordAnim = AnimationNames.NONE;
                    return;
            }
        }

        private static void StopSwordSwing(GameState state, Entity? sword)
        {
            if(sword == null)
            {
                foreach(var e in state.EntityManager.EntitiesWithSword())
                {
                    sword = e.Entity;
                    break;
                }
            }
            
            if (sword == null) return;

            var manager = state.EntityManager;

            // kill the sword
            manager.ReleaseEntity(sword.Value);

            var player = state.Player_Feet;
            var playerState = manager.GetPlayerStateFor(player);
            if (playerState == null) return;

            playerState.StopSwing();
        }
    }
}
