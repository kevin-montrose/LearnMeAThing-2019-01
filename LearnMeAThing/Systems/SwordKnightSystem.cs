using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Systems
{
    sealed class SwordKnightSystem : ASystem<EntityManager.EntitiesWithStatefulComponentEnumerable<SwordKnightStateComponent>>
    {
        public const int DIAGONAL_SPEED = SetPlayerVelocitySystem.DIAGONAL_SPEED * 2 / 3;
        public const int MAX_SPEED = SetPlayerVelocitySystem.MAX_SPEED * 2 / 3;
        
        private const int TRANSITION_AFTER = 100;

        private const int MAXIMUM_DISTANCE_FROM_ORIGIN = 64 * 8;

        public override SystemType Type => SystemType.SwordKnight;

        // todo: replace this with something internal and reproduceable
        private static readonly Random Random_HACK = new Random();

        private static int Random(int max)
        => Random_HACK.Next(max);

        public override EntityManager.EntitiesWithStatefulComponentEnumerable<SwordKnightStateComponent> DesiredEntities(EntityManager manager)
        => manager.EntitiesWithSwordKnightState();

        public override void Update(GameState state, EntityManager.EntitiesWithStatefulComponentEnumerable<SwordKnightStateComponent> requestedEntities)
        {
            var manager = state.EntityManager;

            foreach (var e in requestedEntities)
            {
                var knight = e.Component;
                var feet = e.Entity;

                var associated = manager.GetAssociatedEntityFor(feet);
                if (associated == null)
                {
                    // glitch: ???
                    continue;
                }
                if (associated.EntitiesCount != 5)
                {
                    // glitch: ???
                    continue;
                }

                var body = associated.FirstEntity;
                var head = associated.SecondEntity.Value;
                var shield = associated.ThirdEntity.Value;
                var sword = associated.FourthEntity.Value;
                var cone = associated.FifthEntity.Value;

                UpdateImpl(state, knight, feet, body, head, shield, sword, cone);
            }
        }

        private static void UpdateImpl(GameState state, SwordKnightStateComponent knight, Entity feet, Entity body, Entity head, Entity shield, Entity sword, Entity cone)
        {
            AdvanceState(state, knight, feet);
            SetAnimations(state, knight, feet, body, head, shield, sword);
            StickKnightTogether(state, knight, feet, body, head, shield, sword, cone);
        }

        private static void AdvanceState(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            var manager = state.EntityManager;

            // todo: knight should be able to see things!

            knight.Steps++;
            
            var needsChange =
                // go through a loop, but only give up the chase if  the knight has gotten too far away
                (!knight.IsChasing && knight.Steps == TRANSITION_AFTER) ||
                (knight.WalkingDirection.HasValue && !CanWalkInDirection(state, knight, feet, knight.WalkingDirection.Value)) ||
                (knight.ChasingDirection.HasValue && !CanChase(state, knight, feet));

            if (needsChange)
            {
                knight.Steps = 0;
                ChangeState(state, knight, feet);
            }
            else
            {
                if(knight.IsDieing)
                {
                    AdvanceDieingState(state, feet);
                }
                if (knight.SearchingDirection.HasValue)
                {
                    AdvanceSearchState(state, knight);
                }
                else if (knight.WalkingDirection.HasValue)
                {
                    AdvanceWalkingState(state, knight, feet);
                }
                else if(knight.IsChasing)
                {
                    AdvanceChasingState(state, knight, feet);
                }
                else
                {
                    ClearVelocityIfNotAccelerating(state, feet);
                }
            }

            UpdateVisionCone(state, knight, feet);
        }

        private static void AdvanceDieingState(GameState state, Entity feet)
        {
            var manager = state.EntityManager;
            var feetAnim = manager.GetAnimationFor(feet);
            if (feetAnim.Name == AnimationNames.EnemyDeath)
            {
                // nothing to do, just player the thing
                return;
            }

            var feetPos = manager.GetPositionFor(feet);
            if (feetPos == null)
            {
                // glitch: ???
                return;
            }

            // free everything
            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc != null)
            {
                manager.ReleaseEntity(assoc.FirstEntity);
                if (assoc.SecondEntity != null) manager.ReleaseEntity(assoc.SecondEntity.Value);
                if (assoc.ThirdEntity != null) manager.ReleaseEntity(assoc.ThirdEntity.Value);
                if (assoc.FourthEntity != null) manager.ReleaseEntity(assoc.FourthEntity.Value);
                if (assoc.FifthEntity != null) manager.ReleaseEntity(assoc.FifthEntity.Value);
            }

            var feetVel = manager.GetVelocityFor(feet);
            if (feetVel != null) manager.RemoveComponent(feet, feetVel);

            var feetCol = manager.GetCollisionFor(feet);
            if (feetCol != null) manager.RemoveComponent(feet, feetCol);

            manager.RemoveComponent(feet, FlagComponent.TakesDamage);

            // todo: actually measure and position appropriately
            var newX = feetPos.X - 128 / 2 + 80 / 2;
            var newY = feetPos.Y - 128 / 2 + 12;

            feetPos.X_SubPixel = newX * PositionComponent.SUBPIXELS_PER_PIXEL;
            feetPos.Y_SubPixel = newY * PositionComponent.SUBPIXELS_PER_PIXEL;

            feetAnim.SwitchTo(AnimationNames.EnemyDeath);
        }

        private static void AdvanceChasingState(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            //  - chasing
            //    -> searching

            var manager = state.EntityManager;

            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc == null || assoc.EntitiesCount != 5)
            {
                // glitch: ???
                return;
            }

            var cone = assoc.FifthEntity.Value;
            var coneVel = manager.GetVelocityFor(cone);
            if (coneVel != null && (coneVel.X_SubPixels != 0 || coneVel.Y_SubPixels != 0))
            {
                // stop moving the cone
                coneVel.X_SubPixels = 0;
                coneVel.Y_SubPixels = 0;
            }

            var playerPos = manager.GetPositionFor(state.Player_Feet);
            var knightPos = manager.GetPositionFor(feet);
            var knightVel = manager.GetVelocityFor(feet);

            // figure out where the player is, and run towards them
            int deltaX, deltaY;

            if (playerPos == null || knightPos == null)
            {
                // glitch: ???
                return;
            }
            else
            {
                deltaX = playerPos.X - knightPos.X;
                deltaY = playerPos.Y - knightPos.Y;
            }

            // run in their direction
            var chasingVel = new Vector(deltaX, deltaY);
            chasingVel = chasingVel.Normalize();
            chasingVel = new Vector(chasingVel.DeltaX * MAX_SPEED, chasingVel.DeltaY * MAX_SPEED);
            
            knightVel.X_SubPixels = (int)chasingVel.DeltaX;
            knightVel.Y_SubPixels = (int)chasingVel.DeltaY;

            // point the knight's body at them
            SwordKnightChasing chasingDir;
            if (deltaX == 0 && deltaY == 0)
            {
                chasingDir = SwordKnightChasing.South;
            }
            else
            {
                if (Math.Abs(deltaX) > Math.Abs(deltaY))
                {
                    if (deltaX < 0)
                    {
                        chasingDir = SwordKnightChasing.West;
                    }
                    else
                    {
                        chasingDir = SwordKnightChasing.East;
                    }
                }
                else
                {
                    if (deltaY < 0)
                    {
                        chasingDir = SwordKnightChasing.North;
                    }
                    else
                    {
                        chasingDir = SwordKnightChasing.South;
                    }
                }
            }

            knight.SetChasingDirection(chasingDir);
        }

        private static void UpdateVisionCone(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            if (knight.IsChasing)
            {
                return;
            }

            var manager = state.EntityManager;
            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc == null || assoc.FifthEntity == null)
            {
                // glitch: ???
                return;
            }

            var vision = assoc.FifthEntity.Value;

            var cl = manager.GetCollisionFor(vision);
            if (cl == null)
            {
                // glitch: ???
                return;
            }

            if (knight.SearchingDirection.HasValue)
            {
                switch (knight.SearchingDirection.Value)
                {
                    case SwordKnightSearching.East:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Right;
                        break;
                    case SwordKnightSearching.West:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Left;
                        break;
                    case SwordKnightSearching.North:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Top;
                        break;
                    case SwordKnightSearching.South:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Bottom;
                        break;
                }
            }
            else if (knight.WalkingDirection.HasValue)
            {
                switch (knight.WalkingDirection.Value)
                {
                    case SwordKnightWalking.East:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Right;
                        break;
                    case SwordKnightWalking.West:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Left;
                        break;
                    case SwordKnightWalking.North:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Top;
                        break;
                    case SwordKnightWalking.South:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Bottom;
                        break;
                }
            }
            else
            {
                switch (knight.FacingDirection.Value)
                {
                    case SwordKnightFacing.East:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Right;
                        break;
                    case SwordKnightFacing.West:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Left;
                        break;
                    case SwordKnightFacing.North:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Top;
                        break;
                    case SwordKnightFacing.South:
                        cl.HitMap = AssetNames.SwordKnight_Vision_Bottom;
                        break;
                }
            }
        }

        private static void ClearVelocityIfNotAccelerating(GameState state, Entity feet)
        {
            var manager = state.EntityManager;

            var accel = manager.GetAccelerationFor(feet);
            if (accel == null)
            {
                var vel = manager.GetVelocityFor(feet);
                if (vel != null)
                {
                    vel.X_SubPixels = 0;
                    vel.Y_SubPixels = 0;
                }

                var assoc = manager.GetAssociatedEntityFor(feet);
                if (assoc != null && assoc.FourthEntity != null)
                {
                    var sword = assoc.FourthEntity.Value;

                    var swordVel = manager.GetVelocityFor(sword);
                    if (swordVel != null)
                    {
                        swordVel.X_SubPixels = 0;
                        swordVel.Y_SubPixels = 0;
                    }
                }
            }
        }

        private static void AdvanceWalkingState(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            var manager = state.EntityManager;

            var vel = manager.GetVelocityFor(feet);
            if(vel == null)
            {
                // glitch: ???
                return;
            }

            int deltaX_SubPixels, deltaY_SubPixels;

            switch (knight.WalkingDirection.Value)
            {
                case SwordKnightWalking.East:
                    deltaX_SubPixels = SetPlayerVelocitySystem.MAX_SPEED;
                    deltaY_SubPixels = 0;
                    break;
                case SwordKnightWalking.West:
                    deltaX_SubPixels = -SetPlayerVelocitySystem.MAX_SPEED;
                    deltaY_SubPixels = 0;
                    break;
                case SwordKnightWalking.North:
                    deltaX_SubPixels = 0;
                    deltaY_SubPixels = -SetPlayerVelocitySystem.MAX_SPEED;
                    break;
                case SwordKnightWalking.South:
                    deltaX_SubPixels = 0;
                    deltaY_SubPixels = SetPlayerVelocitySystem.MAX_SPEED;
                    break;
                default: return;
            }

            vel.X_SubPixels = deltaX_SubPixels;
            vel.Y_SubPixels = deltaY_SubPixels;
        }

        private static void AdvanceSearchState(GameState state, SwordKnightStateComponent knight)
        {
            switch (knight.FacingDirection.Value)
            {
                case SwordKnightFacing.East:
                    {
                        if (knight.Steps < TRANSITION_AFTER / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.East, SwordKnightSearching.North);
                        }
                        else if (knight.Steps < TRANSITION_AFTER * 2 / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.East, SwordKnightSearching.East);
                        }
                        else
                        {
                            knight.SetSearching(SwordKnightFacing.East, SwordKnightSearching.South);
                        }
                    }
                    break;
                case SwordKnightFacing.West:
                    {
                        if (knight.Steps < TRANSITION_AFTER / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.West, SwordKnightSearching.South);
                        }
                        else if (knight.Steps < TRANSITION_AFTER * 2 / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.West, SwordKnightSearching.West);
                        }
                        else
                        {
                            knight.SetSearching(SwordKnightFacing.West, SwordKnightSearching.North);
                        }
                    }
                    break;
                case SwordKnightFacing.North:
                    {
                        if (knight.Steps < TRANSITION_AFTER / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.North, SwordKnightSearching.West);
                        }
                        else if (knight.Steps < TRANSITION_AFTER * 2 / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.North, SwordKnightSearching.North);
                        }
                        else
                        {
                            knight.SetSearching(SwordKnightFacing.North, SwordKnightSearching.East);
                        }
                    }
                    break;
                case SwordKnightFacing.South:
                    {
                        if (knight.Steps < TRANSITION_AFTER / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.South, SwordKnightSearching.East);
                        }
                        else if (knight.Steps < TRANSITION_AFTER * 2 / 3)
                        {
                            knight.SetSearching(SwordKnightFacing.South, SwordKnightSearching.South);
                        }
                        else
                        {
                            knight.SetSearching(SwordKnightFacing.South, SwordKnightSearching.West);
                        }
                    }
                    break;
                default: return;
            }
        }

        private static void ChangeState(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            ClearVelocityIfNotAccelerating(state, feet);

            // transitions
            //  - dieing
            //    -> release all entities
            //  - chasing
            //    -> searching
            //  - standing
            //    -> searching
            //    -> walking
            //    -> random quarter turn, standing
            //  - walking
            //    -> stop, searching
            //    -> stop, standing
            //  - searching
            //    -> random quarter turn, walking
            //    -> stop, standing

            if(knight.IsDieing)
            {
                KillKnight(state, feet);
            }
            else if (knight.SearchingDirection.HasValue)
            {
                ChangeStateFromSearching(state, knight, feet);
            }
            else if (knight.WalkingDirection.HasValue)
            {
                ChangeStateFromWalking(state, knight);
            }
            else if (knight.IsChasing)
            {
                ChangeStateFromChasing(state, knight, feet);
            }
            else
            {
                ChangeStateFromStanding(state, knight, feet);
            }
        }

        private static void KillKnight(GameState state, Entity feet)
        {
            var manager = state.EntityManager;
            
            manager.ReleaseEntity(feet);
        }

        private static void ChangeStateFromChasing(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            //  - chasing
            //    -> searching

            var manager = state.EntityManager;

            var assoc = manager.GetAssociatedEntityFor(feet);
            if(assoc == null || assoc.EntitiesCount != 5)
            {
                // glitch: ???
                return;
            }

            var cone = assoc.FifthEntity.Value;
            var coneVel = manager.GetVelocityFor(cone);
            if(coneVel != null)
            {
                // make the cone move again, so it'll participate in collision
                coneVel.X_SubPixels = 1;
                coneVel.Y_SubPixels = 1;
            }

            var playerPos = manager.GetPositionFor(state.Player_Feet);
            var knightPos = manager.GetPositionFor(feet);

            // figure out where the player is, and focus on them 
            //   (then start searching again)
            SwordKnightSearching searchingDir;
            SwordKnightFacing facingDir;

            if (playerPos == null || knightPos == null)
            {
                searchingDir = SwordKnightSearching.North;
                facingDir = SwordKnightFacing.North;
            }
            else
            {
                var deltaX = knightPos.X - playerPos.X;
                var deltaY = knightPos.Y - playerPos.Y;

                if(deltaX == 0 && deltaY == 0)
                {
                    searchingDir = SwordKnightSearching.South;
                    facingDir = SwordKnightFacing.South;
                }
                else
                {
                    if(Math.Abs(deltaX) > Math.Abs(deltaY))
                    {
                        if(deltaX > 0)
                        {
                            searchingDir = SwordKnightSearching.West;
                            facingDir = SwordKnightFacing.West;
                        }
                        else
                        {
                            searchingDir = SwordKnightSearching.East;
                            facingDir = SwordKnightFacing.East;
                        }
                    }
                    else
                    {
                        if(deltaY > 0)
                        {
                            searchingDir = SwordKnightSearching.North;
                            facingDir = SwordKnightFacing.North;
                        }
                        else
                        {
                            searchingDir = SwordKnightSearching.South;
                            facingDir = SwordKnightFacing.South;
                        }
                    }
                }
            }
            
            knight.SetSearching(facingDir, searchingDir);
        }

        private static void ChangeStateFromStanding(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            //  - standing
            //    -> searching
            //    -> walking
            //    -> random quarter turn, standing

            switch (Random(3))
            {
                case 0:
                    // -> searching
                    {
                        switch (knight.FacingDirection.Value)
                        {
                            case SwordKnightFacing.East:
                                knight.SetSearching(SwordKnightFacing.East, SwordKnightSearching.East);
                                break;
                            case SwordKnightFacing.West:
                                knight.SetSearching(SwordKnightFacing.West, SwordKnightSearching.West);
                                break;
                            case SwordKnightFacing.South:
                                knight.SetSearching(SwordKnightFacing.South, SwordKnightSearching.South);
                                break;
                            case SwordKnightFacing.North:
                                knight.SetSearching(SwordKnightFacing.North, SwordKnightSearching.North);
                                break;
                            default: return;
                        }
                    }
                    break;
                case 1:
                    // -> walking
                    {
                        switch (knight.FacingDirection.Value)
                        {
                            case SwordKnightFacing.East:
                                if(!CanWalkInDirection(state, knight, feet, SwordKnightWalking.East))
                                {
                                    ChangeStateFromStanding(state, knight, feet);
                                    return;
                                }
                                knight.SetWalking(SwordKnightWalking.East);
                                SetSwordVelocity(state, feet, SwordKnightWalking.East);
                                break;
                            case SwordKnightFacing.West:
                                if (!CanWalkInDirection(state, knight, feet, SwordKnightWalking.West))
                                {
                                    ChangeStateFromStanding(state, knight, feet);
                                    return;
                                }
                                knight.SetWalking(SwordKnightWalking.West);
                                SetSwordVelocity(state, feet, SwordKnightWalking.West);
                                break;
                            case SwordKnightFacing.South:
                                if (!CanWalkInDirection(state, knight, feet, SwordKnightWalking.South))
                                {
                                    ChangeStateFromStanding(state, knight, feet);
                                    return;
                                }
                                knight.SetWalking(SwordKnightWalking.South);
                                SetSwordVelocity(state, feet, SwordKnightWalking.South);
                                break;
                            case SwordKnightFacing.North:
                                if (!CanWalkInDirection(state, knight, feet, SwordKnightWalking.North))
                                {
                                    ChangeStateFromStanding(state, knight, feet);
                                    return;
                                }
                                knight.SetWalking(SwordKnightWalking.North);
                                SetSwordVelocity(state, feet, SwordKnightWalking.North);
                                break;
                            default: return;
                        }
                    }
                    break;
                case 2:
                    // -> random quarter turn, standing
                    {
                        switch (knight.FacingDirection.Value)
                        {
                            case SwordKnightFacing.East:
                                switch (Random(2))
                                {
                                    case 0:
                                        knight.SetFacing(SwordKnightFacing.North);
                                        break;
                                    case 1:
                                        knight.SetFacing(SwordKnightFacing.South);
                                        break;
                                    default: throw new Exception("Umm");
                                }
                                break;
                            case SwordKnightFacing.West:
                                switch (Random(2))
                                {
                                    case 0:
                                        knight.SetFacing(SwordKnightFacing.North);
                                        break;
                                    case 1:
                                        knight.SetFacing(SwordKnightFacing.South);
                                        break;
                                    default: throw new Exception("Umm");
                                }
                                break;
                            case SwordKnightFacing.North:
                                switch (Random(2))
                                {
                                    case 0:
                                        knight.SetFacing(SwordKnightFacing.East);
                                        break;
                                    case 1:
                                        knight.SetFacing(SwordKnightFacing.West);
                                        break;
                                    default: throw new Exception("Umm");
                                }
                                break;
                            case SwordKnightFacing.South:
                                switch (Random(2))
                                {
                                    case 0:
                                        knight.SetFacing(SwordKnightFacing.East);
                                        break;
                                    case 1:
                                        knight.SetFacing(SwordKnightFacing.West);
                                        break;
                                    default: throw new Exception("Umm");
                                }
                                break;
                            default: return;
                        }
                    }
                    break;
            }
        }

        private static void ChangeStateFromWalking(GameState state, SwordKnightStateComponent knight)
        {
            //  - walking
            //    -> stop, searching
            //    -> stop, standing

            // -> stop, standing
            if (Random(2) == 0)
            {
                switch (knight.WalkingDirection.Value)
                {
                    case SwordKnightWalking.East:
                        knight.SetFacing(SwordKnightFacing.East);
                        break;
                    case SwordKnightWalking.West:
                        knight.SetFacing(SwordKnightFacing.West);
                        break;
                    case SwordKnightWalking.South:
                        knight.SetFacing(SwordKnightFacing.South);
                        break;
                    case SwordKnightWalking.North:
                        knight.SetFacing(SwordKnightFacing.North);
                        break;
                    default: return;
                }

                return;
            }

            // -> stop, searching
            switch (knight.WalkingDirection.Value)
            {
                case SwordKnightWalking.East:
                    knight.SetSearching(SwordKnightFacing.East, SwordKnightSearching.East);
                    break;
                case SwordKnightWalking.West:
                    knight.SetSearching(SwordKnightFacing.West, SwordKnightSearching.West);
                    break;
                case SwordKnightWalking.South:
                    knight.SetSearching(SwordKnightFacing.South, SwordKnightSearching.South);
                    break;
                case SwordKnightWalking.North:
                    knight.SetSearching(SwordKnightFacing.North, SwordKnightSearching.North);
                    break;
                default: return;
            }
        }

        private static void ChangeStateFromSearching(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            //  - searching
            //    -> stop, standing
            //    -> random quarter turn, walking

            // -> standing
            if (Random(2) == 0)
            {
                // searching == direction the head is
                switch (knight.SearchingDirection.Value)
                {
                    case SwordKnightSearching.East:
                        knight.SetFacing(SwordKnightFacing.East);
                        break;
                    case SwordKnightSearching.West:
                        knight.SetFacing(SwordKnightFacing.West);
                        break;
                    case SwordKnightSearching.South:
                        knight.SetFacing(SwordKnightFacing.South);
                        break;
                    case SwordKnightSearching.North:
                        knight.SetFacing(SwordKnightFacing.North);
                        break;
                    default: return;
                }

                return;
            }

            // -> walking

            SwordKnightWalking dir;

            // facing == direction the body is
            switch (knight.FacingDirection.Value)
            {
                case SwordKnightFacing.East:
                    switch (Random(3))
                    {
                        case 0: dir = SwordKnightWalking.North; break;
                        case 1: dir = SwordKnightWalking.East; break;
                        case 2: dir = SwordKnightWalking.South; break;
                        default: throw new Exception("Hmmm");
                    }
                    break;
                case SwordKnightFacing.West:
                    switch (Random(3))
                    {
                        case 0: dir = SwordKnightWalking.North; break;
                        case 1: dir = SwordKnightWalking.West; break;
                        case 2: dir = SwordKnightWalking.South; break;
                        default: throw new Exception("Hmmm");
                    }
                    break;
                case SwordKnightFacing.South:
                    switch (Random(3))
                    {
                        case 0: dir = SwordKnightWalking.East; break;
                        case 1: dir = SwordKnightWalking.South; break;
                        case 2: dir = SwordKnightWalking.West; break;
                        default: throw new Exception("Hmmm");
                    }
                    break;
                case SwordKnightFacing.North:
                    switch (Random(3))
                    {
                        case 0: dir = SwordKnightWalking.East; break;
                        case 1: dir = SwordKnightWalking.North; break;
                        case 2: dir = SwordKnightWalking.West; break;
                        default: throw new Exception("Hmmm");
                    }
                    break;
                default: return;
            }

            if (!CanWalkInDirection(state, knight, feet, dir))
            {
                ChangeStateFromSearching(state, knight, feet);
                return;
            }

            knight.SetWalking(dir);
            SetSwordVelocity(state, feet, dir);
        }

        private static void SetSwordVelocity(GameState state, Entity feet, SwordKnightWalking dir)
        {
            var manager = state.EntityManager;

            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc == null || assoc.FourthEntity == null)
            {
                // glitch: ???
                return;
            }

            var sword = assoc.FourthEntity.Value;
            var swordVel = manager.GetVelocityFor(sword);
            if(swordVel == null)
            {
                // glitch: ???
                return;
            }

            switch (dir)
            {
                case SwordKnightWalking.East:
                    swordVel.X_SubPixels = 1;
                    swordVel.Y_SubPixels = 0;
                    break;
                case SwordKnightWalking.West:
                    swordVel.X_SubPixels = -1;
                    swordVel.Y_SubPixels = 0;
                    break;
                case SwordKnightWalking.South:
                    swordVel.X_SubPixels = 0;
                    swordVel.Y_SubPixels = 1;
                    break;
                case SwordKnightWalking.North:
                    swordVel.X_SubPixels = 0;
                    swordVel.Y_SubPixels = -1;
                    break;
            }
        }

        private static bool CanWalkInDirection(GameState state, SwordKnightStateComponent knight, Entity feet, SwordKnightWalking dir)
        {
            var manager = state.EntityManager;
            var measurer = state.AssetMeasurer;

            var pos = manager.GetPositionFor(feet);
            if (pos == null)
            {
                // glitch: ???
                return false;
            }

            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc == null)
            {
                // glitch: ???
                return false;
            }

            var body = assoc.FirstEntity;
            var head = assoc.SecondEntity.Value;

            var feetAnim = manager.GetAnimationFor(feet);
            var bodyAnim = manager.GetAnimationFor(body);
            var headAnim = manager.GetAnimationFor(head);

            if (feetAnim == null || bodyAnim == null || headAnim == null)
            {
                // glitch: ???
                return false;
            }

            var feetDims = measurer.Measure(feetAnim.GetCurrentFrame(state.AnimationManager));
            var bodyDims = measurer.Measure(bodyAnim.GetCurrentFrame(state.AnimationManager));
            var headDims = measurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));

            var width = Math.Max(feetDims.Width, Math.Max(bodyDims.Width, headDims.Width));
            var height = feetDims.Height + bodyDims.Height + headDims.Height;

            var left = pos.X;
            var right = left + width;
            var top = pos.Y - bodyDims.Height - headDims.Height;
            var bottom = top + headDims.Height + bodyDims.Height + feetDims.Height;

            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);

            // make sure the knight is not going to walk off the screen
            if (left <= 0 && dir == SwordKnightWalking.West) return false;
            if (right >= roomDims.Width && dir == SwordKnightWalking.East) return false;
            if (top <= 0 && dir == SwordKnightWalking.North) return false;
            if (bottom >= roomDims.Height && dir == SwordKnightWalking.South) return false;

            var minLeft = knight.InitialPosition.X - MAXIMUM_DISTANCE_FROM_ORIGIN;
            var maxRight = knight.InitialPosition.X + width + MAXIMUM_DISTANCE_FROM_ORIGIN;
            var minTop = knight.InitialPosition.Y + feetDims.Height - height - MAXIMUM_DISTANCE_FROM_ORIGIN;
            var maxBottom = knight.InitialPosition.Y + feetDims.Height + MAXIMUM_DISTANCE_FROM_ORIGIN;

            if (left <= minLeft && dir == SwordKnightWalking.West) return false;
            if (right >= maxRight && dir == SwordKnightWalking.East) return false;
            if (top <= minTop && dir == SwordKnightWalking.North) return false;
            if (bottom >= maxBottom && dir == SwordKnightWalking.South) return false;

            return true;
        }

        // like can walk check, but accounts for the size of the cone
        private static bool CanChase(GameState state, SwordKnightStateComponent knight, Entity feet)
        {
            var manager = state.EntityManager;
            var measurer = state.AssetMeasurer;

            var pos = manager.GetPositionFor(feet);
            if (pos == null)
            {
                // glitch: ???
                return false;
            }

            var assoc = manager.GetAssociatedEntityFor(feet);
            if (assoc == null)
            {
                // glitch: ???
                return false;
            }

            var body = assoc.FirstEntity;
            var head = assoc.SecondEntity.Value;
            var cone = assoc.FifthEntity.Value;

            var feetAnim = manager.GetAnimationFor(feet);
            var bodyAnim = manager.GetAnimationFor(body);
            var headAnim = manager.GetAnimationFor(head);
            var coneAnim = manager.GetAnimationFor(cone);

            if (feetAnim == null || bodyAnim == null || headAnim == null || coneAnim == null)
            {
                // glitch: ???
                return false;
            }

            var feetDims = measurer.Measure(feetAnim.GetCurrentFrame(state.AnimationManager));
            var bodyDims = measurer.Measure(bodyAnim.GetCurrentFrame(state.AnimationManager));
            var headDims = measurer.Measure(headAnim.GetCurrentFrame(state.AnimationManager));
            var coneDims = measurer.Measure(coneAnim.GetCurrentFrame(state.AnimationManager));

            var width = Math.Max(feetDims.Width, Math.Max(bodyDims.Width, headDims.Width));
            var height = feetDims.Height + bodyDims.Height + headDims.Height;

            var left = pos.X;
            var right = left + width;
            var top = pos.Y - bodyDims.Height - headDims.Height;
            var bottom = top + headDims.Height + bodyDims.Height + feetDims.Height;

            var roomDims = state.RoomManager.Measure(state.CurrentRoom.Name);

            // make sure the knight is not going to walk off the screen
            if (left <= 0) return false;
            if (right >= roomDims.Width) return false;
            if (top <= 0) return false;
            if (bottom >= roomDims.Height) return false;

            var minLeft = knight.InitialPosition.X - MAXIMUM_DISTANCE_FROM_ORIGIN - coneDims.Width * 2 / 3;
            var maxRight = knight.InitialPosition.X + width + MAXIMUM_DISTANCE_FROM_ORIGIN + coneDims.Width * 2 / 3;
            var minTop = knight.InitialPosition.Y + feetDims.Height - height - MAXIMUM_DISTANCE_FROM_ORIGIN - coneDims.Height * 2 / 3;
            var maxBottom = knight.InitialPosition.Y + feetDims.Height + MAXIMUM_DISTANCE_FROM_ORIGIN + coneDims.Height * 2 / 3;

            if (left <= minLeft) return false;
            if (right >= maxRight) return false;
            if (top <= minTop) return false;
            if (bottom >= maxBottom) return false;

            return true;
        }

        private static void SetAnimations(GameState state, SwordKnightStateComponent knight, Entity feet, Entity body, Entity head, Entity shield, Entity sword)
        {
            if (knight.IsDieing)
            {
                // todo:
                return;
            }

            var manager = state.EntityManager;
            var animManager = state.AnimationManager;

            AnimationNames feetAnim, bodyAnim, headAnim, shieldAnim, swordAnim;

            feetAnim = bodyAnim = headAnim = shieldAnim = swordAnim = AnimationNames.NONE;


            if (knight.SearchingDirection.HasValue)
            {
                // point the body and whatnot
                switch (knight.FacingDirection.Value)
                {
                    case SwordKnightFacing.East:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingRight;
                        bodyAnim = AnimationNames.SwordKnight_Body_Right;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Right;
                        swordAnim = AnimationNames.SwordKnight_Sword_Right;
                        break;

                    case SwordKnightFacing.West:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingLeft;
                        bodyAnim = AnimationNames.SwordKnight_Body_Left;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Left;
                        swordAnim = AnimationNames.SwordKnight_Sword_Left;
                        break;

                    case SwordKnightFacing.South:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingBottom;
                        bodyAnim = AnimationNames.SwordKnight_Body_Bottom;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Bottom;
                        swordAnim = AnimationNames.SwordKnight_Sword_Bottom;
                        break;

                    case SwordKnightFacing.North:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingTop;
                        bodyAnim = AnimationNames.SwordKnight_Body_Top;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Top;
                        swordAnim = AnimationNames.SwordKnight_Sword_Top;
                        break;

                    default: return;
                }


                // point the head
                switch (knight.SearchingDirection.Value)
                {
                    case SwordKnightSearching.East:
                        headAnim = AnimationNames.SwordKnight_Head_Right;
                        break;

                    case SwordKnightSearching.West:
                        headAnim = AnimationNames.SwordKnight_Head_Left;
                        break;

                    case SwordKnightSearching.South:
                        headAnim = AnimationNames.SwordKnight_Head_Bottom;
                        break;

                    case SwordKnightSearching.North:
                        headAnim = AnimationNames.SwordKnight_Head_Top;
                        break;

                    default: return;
                }
            }
            else if (knight.WalkingDirection.HasValue)
            {
                switch (knight.WalkingDirection.Value)
                {
                    case SwordKnightWalking.East:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingRight;
                        bodyAnim = AnimationNames.SwordKnight_Body_Right;
                        headAnim = AnimationNames.SwordKnight_Head_Right;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Right;
                        swordAnim = AnimationNames.SwordKnight_Sword_Right;
                        break;

                    case SwordKnightWalking.West:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingLeft;
                        bodyAnim = AnimationNames.SwordKnight_Body_Left;
                        headAnim = AnimationNames.SwordKnight_Head_Left;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Left;
                        swordAnim = AnimationNames.SwordKnight_Sword_Left;
                        break;

                    case SwordKnightWalking.North:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingTop;
                        bodyAnim = AnimationNames.SwordKnight_Body_Top;
                        headAnim = AnimationNames.SwordKnight_Head_Top;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Top;
                        swordAnim = AnimationNames.SwordKnight_Sword_Top;
                        break;

                    case SwordKnightWalking.South:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingBottom;
                        bodyAnim = AnimationNames.SwordKnight_Body_Bottom;
                        headAnim = AnimationNames.SwordKnight_Head_Bottom;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Bottom;
                        swordAnim = AnimationNames.SwordKnight_Sword_Bottom;
                        break;
                }
            }
            else if (knight.IsChasing)
            {
                switch (knight.ChasingDirection.Value)
                {
                    case SwordKnightChasing.East:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingRight;
                        bodyAnim = AnimationNames.SwordKnight_Body_Right;
                        headAnim = AnimationNames.SwordKnight_Head_Right;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Right;
                        swordAnim = AnimationNames.SwordKnight_Sword_Right;
                        break;

                    case SwordKnightChasing.West:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingLeft;
                        bodyAnim = AnimationNames.SwordKnight_Body_Left;
                        headAnim = AnimationNames.SwordKnight_Head_Left;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Left;
                        swordAnim = AnimationNames.SwordKnight_Sword_Left;
                        break;

                    case SwordKnightChasing.South:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingBottom;
                        bodyAnim = AnimationNames.SwordKnight_Body_Bottom;
                        headAnim = AnimationNames.SwordKnight_Head_Bottom;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Bottom;
                        swordAnim = AnimationNames.SwordKnight_Sword_Bottom;
                        break;

                    case SwordKnightChasing.North:
                        feetAnim = AnimationNames.SwordKnight_Feet_WalkingTop;
                        bodyAnim = AnimationNames.SwordKnight_Body_Top;
                        headAnim = AnimationNames.SwordKnight_Head_Top;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Top;
                        swordAnim = AnimationNames.SwordKnight_Sword_Top;
                        break;

                    default: return;
                }
            }
            else
            {
                switch (knight.FacingDirection.Value)
                {
                    case SwordKnightFacing.East:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingRight;
                        bodyAnim = AnimationNames.SwordKnight_Body_Right;
                        headAnim = AnimationNames.SwordKnight_Head_Right;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Right;
                        swordAnim = AnimationNames.SwordKnight_Sword_Right;
                        break;

                    case SwordKnightFacing.West:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingLeft;
                        bodyAnim = AnimationNames.SwordKnight_Body_Left;
                        headAnim = AnimationNames.SwordKnight_Head_Left;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Left;
                        swordAnim = AnimationNames.SwordKnight_Sword_Left;
                        break;

                    case SwordKnightFacing.South:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingBottom;
                        bodyAnim = AnimationNames.SwordKnight_Body_Bottom;
                        headAnim = AnimationNames.SwordKnight_Head_Bottom;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Bottom;
                        swordAnim = AnimationNames.SwordKnight_Sword_Bottom;
                        break;

                    case SwordKnightFacing.North:
                        feetAnim = AnimationNames.SwordKnight_Feet_StandingTop;
                        bodyAnim = AnimationNames.SwordKnight_Body_Top;
                        headAnim = AnimationNames.SwordKnight_Head_Top;
                        shieldAnim = AnimationNames.SwordKnight_Shield_Top;
                        swordAnim = AnimationNames.SwordKnight_Sword_Top;
                        break;

                    default: return;
                }
            }

            var fAc = manager.GetAnimationFor(feet);
            var bAc = manager.GetAnimationFor(body);
            var hAc = manager.GetAnimationFor(head);
            var shAc = manager.GetAnimationFor(shield);
            var swAc = manager.GetAnimationFor(sword);

            if (fAc == null || bAc == null || hAc == null || shAc == null || swAc == null)
            {
                // glitch: ???
                return;
            }

            if (fAc.Name != feetAnim) fAc.SwitchTo(feetAnim);
            if (bAc.Name != bodyAnim) bAc.SwitchTo(bodyAnim);
            if (hAc.Name != headAnim) hAc.SwitchTo(headAnim);
            if (shAc.Name != shieldAnim) shAc.SwitchTo(shieldAnim);
            if (swAc.Name != swordAnim) swAc.SwitchTo(swordAnim);

            var fCc = manager.GetCollisionFor(feet);
            var bCc = manager.GetCollisionFor(body);
            var hCc = manager.GetCollisionFor(head);
            var shCc = manager.GetCollisionFor(shield);
            var swCc = manager.GetCollisionFor(sword);

            if (fCc == null || bCc == null || hCc == null || shCc == null || swCc == null)
            {
                // glitch: ???
                return;
            }

            fCc.HitMap = fAc.GetCurrentFrame(animManager);
            bCc.HitMap = bAc.GetCurrentFrame(animManager);
            hCc.HitMap = hAc.GetCurrentFrame(animManager);
            shCc.HitMap = shAc.GetCurrentFrame(animManager);
            swCc.HitMap = swAc.GetCurrentFrame(animManager);
        }

        private static void StickKnightTogether(GameState state, SwordKnightStateComponent knight, Entity feet, Entity body, Entity head, Entity shield, Entity sword, Entity cone)
        {
            if (knight.IsDieing)
            {
                // nothing to stick together
                return;
            }

            var manager = state.EntityManager;
            var feetPos = manager.GetPositionFor(feet);
            var bodyPos = manager.GetPositionFor(body);
            var headPos = manager.GetPositionFor(head);
            var shieldPos = manager.GetPositionFor(shield);
            var swordPos = manager.GetPositionFor(sword);
            var conePos = manager.GetPositionFor(cone);

            if (feetPos == null || bodyPos == null || headPos == null || shieldPos == null || swordPos == null || conePos == null)
            {
                // glitch: ???
                return;
            }

            var bodyFrame = UpdateBodyPosition(state, body, feetPos, bodyPos);
            var headFrame = UpdateHeadPosition(state, head, bodyPos, bodyFrame, headPos);
            UpdateShieldPosition(state, shield, bodyPos, shieldPos);
            UpdateSwordPosition(state, sword, bodyPos, swordPos);
            if (headFrame != null)
            {
                UpdateConePosition(state, cone, headPos, headFrame.Value, conePos);
            }
        }

        private static void UpdateConePosition(GameState state, Entity cone, PositionComponent headPos, AssetNames headFrame, PositionComponent conePos)
        {
            var manager = state.EntityManager;
            var anim = manager.GetAnimationFor(cone);
            if(anim == null)
            {
                // glitch: ???
                return;
            }

            var headDims = state.AssetMeasurer.Measure(headFrame);
            var coneDims = state.AssetMeasurer.Measure(anim.GetCurrentFrame(state.AnimationManager));

            var shiftX = headDims.Width / 2 - coneDims.Width / 2;
            var shiftY = headDims.Height / 2 - coneDims.Height / 2;

            var conePosX = headPos.X + shiftX;
            var conePosY = headPos.Y + shiftY;

            conePos.X_SubPixel = conePosX * PositionComponent.SUBPIXELS_PER_PIXEL;
            conePos.Y_SubPixel = conePosY * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        private static AssetNames? UpdateBodyPosition(GameState state, Entity body, PositionComponent feetPos, PositionComponent bodyPos)
        {
            var manager = state.EntityManager;
            var anim = manager.GetAnimationFor(body);
            if (anim == null)
            {
                // glitch: ???
                return null;
            }

            var curFrame = anim.GetCurrentFrame(state.AnimationManager);
            var dims = state.AssetMeasurer.Measure(curFrame);

            var x = feetPos.X;
            var y = feetPos.Y - dims.Height;

            switch (curFrame)
            {
                case AssetNames.SwordKnight_Body_Right:
                    x += 0;
                    y += 14;
                    break;
                case AssetNames.SwordKnight_Body_Left:
                    x += 14;
                    y += 14;
                    break;
                case AssetNames.SwordKnight_Body_Bottom:
                    x += 0;
                    y += 14;
                    break;
                case AssetNames.SwordKnight_Body_Top:
                    x += 0;
                    y += 14;
                    break;
            }

            bodyPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
            bodyPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;

            return curFrame;
        }

        private static AssetNames? UpdateHeadPosition(GameState state, Entity head, PositionComponent bodyPos, AssetNames? bodyFrame, PositionComponent headPos)
        {
            var manager = state.EntityManager;
            var anim = manager.GetAnimationFor(head);
            if (anim == null)
            {
                // glitch: ???
                return null;
            }

            var curFrame = anim.GetCurrentFrame(state.AnimationManager);
            var dims = state.AssetMeasurer.Measure(curFrame);

            var x = bodyPos.X;
            var y = bodyPos.Y - dims.Height;

            if (bodyFrame == AssetNames.SwordKnight_Body_Left || bodyFrame == AssetNames.SwordKnight_Body_Right)
            {
                switch (curFrame)
                {
                    case AssetNames.SwordKnight_Head_Left:
                    case AssetNames.SwordKnight_Head_Right:
                    case AssetNames.SwordKnight_Head_Top:
                    case AssetNames.SwordKnight_Head_Bottom:
                        x += 0;
                        y += 14;
                        break;
                }
            }

            if (bodyFrame == AssetNames.SwordKnight_Body_Bottom || bodyFrame == AssetNames.SwordKnight_Body_Top)
            {
                switch (curFrame)
                {
                    case AssetNames.SwordKnight_Head_Left:
                    case AssetNames.SwordKnight_Head_Right:
                    case AssetNames.SwordKnight_Head_Top:
                    case AssetNames.SwordKnight_Head_Bottom:
                        x += 16;
                        y += 34;
                        break;
                }
            }

            headPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
            headPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;

            return curFrame;
        }

        private static void UpdateSwordPosition(GameState state, Entity sword, PositionComponent bodyPos, PositionComponent swordPos)
        {
            var manager = state.EntityManager;
            var anim = manager.GetAnimationFor(sword);
            if (anim == null)
            {
                // glitch: ???
                return;
            }

            var curFrame = anim.GetCurrentFrame(state.AnimationManager);
            var dims = state.AssetMeasurer.Measure(curFrame);

            var x = bodyPos.X;
            var y = bodyPos.Y - dims.Height;

            switch (curFrame)
            {
                case AssetNames.SwordKnight_Sword_Right:
                    x += 58;
                    y += 28;
                    break;
                case AssetNames.SwordKnight_Sword_Left:
                    x -= 56;
                    y += 28;
                    break;
                case AssetNames.SwordKnight_Sword_Bottom:
                    x += 4;
                    y += 96;
                    break;
                case AssetNames.SwordKnight_Sword_Top:
                    x += 72;
                    y += 16;
                    break;
            }

            swordPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
            swordPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;
        }

        private static void UpdateShieldPosition(GameState state, Entity shield, PositionComponent bodyPos, PositionComponent shieldPos)
        {
            var manager = state.EntityManager;
            var anim = manager.GetAnimationFor(shield);
            if (anim == null)
            {
                // glitch: ???
                return;
            }

            var curFrame = anim.GetCurrentFrame(state.AnimationManager);
            var dims = state.AssetMeasurer.Measure(curFrame);

            var x = bodyPos.X;
            var y = bodyPos.Y - dims.Height;

            switch (curFrame)
            {
                case AssetNames.SwordKnight_Shield_Right:
                    x += 54;
                    y += 14;
                    break;
                case AssetNames.SwordKnight_Shield_Left:
                    x -= 2;
                    y += 14;
                    break;
                case AssetNames.SwordKnight_Shield_Bottom:
                    x += 60;
                    y += 60;
                    break;
                case AssetNames.SwordKnight_Shield_Top:
                    x -= 4;
                    y += 40;
                    break;
            }

            shieldPos.X_SubPixel = x * PositionComponent.SUBPIXELS_PER_PIXEL;
            shieldPos.Y_SubPixel = y * PositionComponent.SUBPIXELS_PER_PIXEL;
        }
    }
}