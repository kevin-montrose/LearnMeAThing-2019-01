using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Handlers;
using System;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Helper class for holding all the "make a thing from these definitions"
    ///   logic that isn't stateful and doesn't belong in a manager.
    /// </summary>
     static partial class ObjectCreator
    {
        /// <summary>
        /// Maps a room object to entities.
        /// 
        /// Returns the number of entities allocated.
        /// 
        /// newEntityBuffer will hold the actual entities.
        /// </summary>
        public static Result<int> Create(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            if (newEntityBuffer == null) throw new ArgumentNullException(nameof(newEntityBuffer));

            switch (def.Type)
            {
                case RoomObjectTypes.FlowerTopLeft: return MakeFlowerTopLeft(state, def, newEntityBuffer);
                case RoomObjectTypes.FlowerTopLeftBottomRight: return MakeFlowerTopLeftBottomRight(state, def, newEntityBuffer);
                case RoomObjectTypes.Tree: return MakeTree(state, def, newEntityBuffer);
                case RoomObjectTypes.BushWall_TopBottom: return MakeBushWallTopBottom(state, def, newEntityBuffer);
                case RoomObjectTypes.Bush: return MakeBush(state, def, newEntityBuffer);
                case RoomObjectTypes.Door: return MakeDoor(state, def, newEntityBuffer);
                case RoomObjectTypes.Stairs: return MakeStairs(state, def, newEntityBuffer);
                case RoomObjectTypes.Pit: return MakePit(state, def, newEntityBuffer);
                case RoomObjectTypes.SwordKnight: return MakeSwordKnight(state, def, newEntityBuffer);
                default: throw new InvalidOperationException($"Unexpected RoomObject type: {def.Type}");
            }
        }

        

        public static Result<Entity> CreateDropShadow(GameState state, int x, int y)
        {
            var manager = state.EntityManager;

            var dropShadowRes = manager.NewEntity();
            if (!dropShadowRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var dropShadow = dropShadowRes.Value;

            var posRes = manager.CreatePosition(x, y);
            if(!posRes.Success)
            {
                manager.ReleaseEntity(dropShadow);
                return Result.FailFor<Entity>();
            }
            if(!manager.AddComponent(dropShadow, posRes.Value).Success)
            {
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(dropShadow);
                return Result.FailFor<Entity>();
            }

            var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.DropShadow, 0);
            if (!animRes.Success)
            {
                manager.ReleaseEntity(dropShadow);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(dropShadow, animRes.Value).Success)
            {
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(dropShadow);
                return Result.FailFor<Entity>();
            }

            if(!manager.AddComponent(dropShadow, FlagComponent.DropShadow).Success)
            {
                manager.ReleaseEntity(dropShadow);
                return Result.FailFor<Entity>();
            }

            return Result.From(dropShadow);
        }

        private static Result<int> MakePit(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;

            var pitRes = manager.NewEntity();
            if (!pitRes.Success)
            {
                return Result.FailFor<int>();
            }

            var pit = pitRes.Value;

            var x = def.X;
            var y = def.Y;

            var targetRoom = def.GetProperty("TargetRoom");
            if (!Enum.TryParse<RoomNames>(targetRoom, ignoreCase: true, out var targetRoomParsed)) throw new InvalidOperationException($"Unexpected target room: {targetRoom}");

            var targetX = def.GetProperty("TargetX");
            if (!int.TryParse(targetX, out var targetXParsed)) throw new InvalidOperationException($"Couldn't extract TargetX, found: {targetX}");
            var targetY = def.GetProperty("TargetY");
            if (!int.TryParse(targetY, out var targetYParsed)) throw new InvalidOperationException($"Couldn't extract TargetY, found: {targetY}");

            var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.Pit, 0);
            if (!animRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(pit, animRes.Value).Success)
            {
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            var collisionRes = manager.CreateCollision(AssetNames.Pit, 0, PitCollisionHandler.Collision, PitCollisionHandler.Push);
            if (!collisionRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(pit, collisionRes.Value).Success)
            {
                manager.ReleaseComponent(collisionRes.Value);
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            var posRes = manager.CreatePosition(x, y);
            if (!posRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(pit, posRes.Value).Success)
            {
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            var velRes = manager.CreateVelocity(0, 0);
            if (!velRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(pit, velRes.Value).Success)
            {
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            var pRes = manager.CreatePit(targetRoomParsed, targetXParsed, targetYParsed);
            if (!pRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(pit, pRes.Value).Success)
            {
                manager.ReleaseComponent(pRes.Value);
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            var floorRes = manager.AddComponent(pit, FlagComponent.Level_Floor);
            if (!floorRes.Success)
            {
                manager.ReleaseEntity(pit);
                return Result.FailFor<int>();
            }

            newEntityBuffer[0] = pit;

            return Result.From(1);
        }

        public static Result<Entity> CreateCamera(GameState state, int playerX, int playerY)
        {
            var manager = state.EntityManager;
            var assetMeasurer = state.AssetMeasurer;

            var cameraRes = manager.NewEntity();
            if (!cameraRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var playerSizeWidth =
                Math.Max(
                    assetMeasurer.Measure(AssetNames.Player_Feet).Width,
                    Math.Max(
                        assetMeasurer.Measure(AssetNames.Player_Body).Width,
                        assetMeasurer.Measure(AssetNames.Player_Head).Width
                    )
                );
            var playerSizeHeight =
                    assetMeasurer.Measure(AssetNames.Player_Feet).Height +
                    assetMeasurer.Measure(AssetNames.Player_Body).Height +
                    assetMeasurer.Measure(AssetNames.Player_Head).Height;
            var playerSize = (Width: playerSizeWidth, Height: playerSizeHeight);

            var camera = cameraRes.Value;

            var cameraCenterX = playerX + playerSize.Width / 2;
            var cameraCenterY = playerY + playerSize.Height / 2;
            var cameraX = cameraCenterX - GameState.WIDTH_HACK / 2;
            var cameraY = cameraCenterY - GameState.HEIGHT_HACK / 2;
            cameraX = Math.Max(0, cameraX);
            cameraY = Math.Max(0, cameraY);

            var posRes = manager.CreatePosition(cameraX, cameraY);
            if (!posRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(camera);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(camera, posRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(camera);
                return Result.FailFor<Entity>();
            }

            var cRes = manager.AddComponent(camera, FlagComponent.Camera);
            if (!cRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(camera);
                return Result.FailFor<Entity>();
            }

            return Result.From(camera);
        }

        public static Result<Entity> CreatePlayerHead(GameState state, int playerX, int playerY)
        {
            var manager = state.EntityManager;
            var assetMeasurer = state.AssetMeasurer;
            var animationManager = state.AnimationManager;

            var playerHeadRes = manager.NewEntity();
            if (!playerHeadRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var playerHead = playerHeadRes.Value;

            var bodySize = assetMeasurer.Measure(AssetNames.Player_Body);
            var bodyY = playerY - bodySize.Height;

            var headSize = assetMeasurer.Measure(AssetNames.Player_Head);
            var headY = bodyY - headSize.Height;

            var posRes = manager.CreatePosition(playerX, headY);
            if (!posRes.Success)
            {
                // free and bail
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerHead, posRes.Value).Success)
            {
                // free and bail
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }

            var animRes = manager.CreateAnimation(animationManager, AnimationNames.Player_Head, 0);
            if (!animRes.Success)
            {
                // free and bail
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerHead, animRes.Value).Success)
            {
                // free and bail
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }

            var colRes = manager.CreateCollision(AssetNames.Player_Head, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
            if (!colRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerHead, colRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(colRes.Value);
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }

            var velRes = manager.CreateVelocity(0, 0);
            if (!velRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerHead, velRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }

            var levelRes = manager.AddComponent(playerHead, FlagComponent.Level_Top);
            if (!levelRes.Success)
            {
                // free and bail
                manager.ReleaseEntity(playerHead);
                return Result.FailFor<Entity>();
            }

            return Result.From(playerHead);
        }

        public static Result<Entity> CreatePlayerBody(GameState state, int playerX, int playerY)
        {
            var manager = state.EntityManager;
            var assetMeasurer = state.AssetMeasurer;
            var animationManager = state.AnimationManager;

            var playerBodyRes = manager.NewEntity();
            if (!playerBodyRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var playerBody = playerBodyRes.Value;

            var bodySize = assetMeasurer.Measure(AssetNames.Player_Body);
            var bodyY = playerY - bodySize.Height;

            var posRes = manager.CreatePosition(playerX, bodyY);
            if (!posRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerBody, posRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }

            var animRes = manager.CreateAnimation(animationManager, AnimationNames.Player_Body, 0);
            if (!animRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerBody, animRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }

            var colRes = manager.CreateCollision(AssetNames.Player_Body, 0, DoNothingCollisionHandler.Collision, PlayerCollisionHandler.PlayerBodyPush);
            if (!colRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }
            if(!manager.AddComponent(playerBody, colRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(colRes.Value);
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }

            var velRes = manager.CreateVelocity(1, 1);
            if (!velRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerBody, velRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }

            var middleRes = manager.AddComponent(playerBody, FlagComponent.Level_Middle);
            if (!middleRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerBody);
                return Result.FailFor<Entity>();
            }

            return Result.From(playerBody);
        }

        public static Result<Entity> CreatePlayerFeet(GameState state, int playerX, int playerY)
        {
            var manager = state.EntityManager;
            var assetMeasurer = state.AssetMeasurer;
            var animationManager = state.AnimationManager;

            var playerFeetRes = manager.NewEntity();
            if (!playerFeetRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var playerFeet = playerFeetRes.Value;

            var feetSize = assetMeasurer.Measure(AssetNames.Player_Feet);

            var posRes = manager.CreatePosition(playerX, playerY);
            if (!posRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, posRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var inputRes = manager.CreateInputs();
            if (!inputRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, inputRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(inputRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var velRes = manager.CreateVelocity(0, 0);
            if (!velRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, velRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var animRes = manager.CreateAnimation(animationManager, AnimationNames.Player_Feet, 0);
            if (!animRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, animRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var colRes = manager.CreateCollision(AssetNames.Player_Feet, 0, PlayerCollisionHandler.Collision, PlayerCollisionHandler.Push);
            if (!colRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, colRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(colRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var playerRes = manager.CreatePlayerState(PlayerStanding.South);
            if (!playerRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(playerFeet, playerRes.Value).Success)
            {
                // cleanup and bail
                manager.ReleaseComponent(playerRes.Value);
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }


            var floorRes = manager.AddComponent(playerFeet, FlagComponent.Level_Floor);
            if (!floorRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            var pRes = manager.AddComponent(playerFeet, FlagComponent.Player);
            if (!pRes.Success)
            {
                // cleanup and bail
                manager.ReleaseEntity(playerFeet);
                return Result.FailFor<Entity>();
            }

            return Result.From(playerFeet);
        }

        private static Result<int> MakeStairs(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;

            var stairRes = manager.NewEntity();
            if (!stairRes.Success) return Result.FailFor<int>();

            var stair = stairRes.Value;

            var doorFrameRes = manager.NewEntity();
            if (!doorFrameRes.Success)
            {
                // cleanup what we've already allocated
                manager.ReleaseEntity(stair);
                return Result.FailFor<int>();
            }

            var doorFrame = doorFrameRes.Value;

            var x = def.X;
            var y = def.Y;

            var targetRoom = def.GetProperty("TargetRoom");
            if (!Enum.TryParse<RoomNames>(targetRoom, ignoreCase: true, out var targetRoomParsed)) throw new InvalidOperationException($"Unexpected target room: {targetRoom}");

            var targetX = def.GetProperty("TargetX");
            if (!int.TryParse(targetX, out var targetXParsed)) throw new InvalidOperationException($"Couldn't extract TargetX, found: {targetX}");
            var targetY = def.GetProperty("TargetY");
            if (!int.TryParse(targetY, out var targetYParsed)) throw new InvalidOperationException($"Couldn't extract TargetY, found: {targetY}");

            var stairsDirection = def.GetProperty("StairsDirection");
            if (!Enum.TryParse<StairDirections>(stairsDirection, ignoreCase: true, out var stairsDirectionParsed)) throw new InvalidOperationException($"Unexpected stair direction: {stairsDirection}");

            AnimationNames stairsAnim;
            switch (stairsDirectionParsed)
            {
                case StairDirections.Up: stairsAnim = AnimationNames.StairsUp; break;
                case StairDirections.Down: stairsAnim = AnimationNames.StairsDown; break;
                default: throw new InvalidOperationException($"Unexpected stair direction: {stairsDirectionParsed}");
            }

            // setup the stairs
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, stairsAnim, 0);
                if (!animRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(stair, animRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(stair, posRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.StairsUp, 0, StairCollisionHandler.Collision, StairCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(stair, colRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(stair, velRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var stairsRes = manager.CreateStairs(stairsDirectionParsed, targetRoomParsed, targetXParsed, targetYParsed);
                if (!stairsRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(stair, stairsRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(stairsRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(stair, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
            }

            // setup the door frame
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.StairsDoorFrame, 0);
                if (!animRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, animRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, posRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.StairsDoorFrame, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, colRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, velRes.Value).Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(doorFrame, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var ceilingRes = manager.AddComponent(doorFrame, FlagComponent.Level_Ceiling);
                if (!ceilingRes.Success)
                {
                    // free the entities, this will implicitly free anything that we
                    //   attached to them
                    manager.ReleaseEntity(stair);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
            }

            newEntityBuffer[0] = stair;
            newEntityBuffer[1] = doorFrame;

            return Result.From(2);
        }

        private static Result<int> MakeDoor(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;

            var doorRes = manager.NewEntity();
            if (!doorRes.Success) return Result.FailFor<int>();

            var door = doorRes.Value;

            var doorFrameRes = manager.NewEntity();
            if (!doorFrameRes.Success)
            {
                manager.ReleaseEntity(door);
                return Result.FailFor<int>();
            }

            var doorFrame = doorFrameRes.Value;

            var x = def.X;
            var y = def.Y;

            var targetRoom = def.GetProperty("TargetRoom");
            if (!Enum.TryParse<RoomNames>(targetRoom, ignoreCase: true, out var targetRoomParsed)) throw new InvalidOperationException($"Unexpected target room: {targetRoom}");

            var targetX = def.GetProperty("TargetX");
            if (!int.TryParse(targetX, out var targetXParsed)) throw new InvalidOperationException($"Couldn't extract TargetX, found: {targetX}");
            var targetY = def.GetProperty("TargetY");
            if (!int.TryParse(targetY, out var targetYParsed)) throw new InvalidOperationException($"Couldn't extract TargetY, found: {targetY}");

            // make door
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.Door, 0);
                if (!animRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(door, animRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(door, posRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.Door, 0, DoorCollisionHandler.Collision, DoorCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(door, colRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(door, velRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var dRes = manager.CreateDoor(targetRoomParsed, targetXParsed, targetYParsed);
                if (!dRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(door, dRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(dRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(door, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
            }

            // make door frame
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.DoorFrame, 0);
                if (!animRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, animRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, posRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.DoorFrame, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, colRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(doorFrame, velRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(doorFrame, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }

                var ceilingRes = manager.AddComponent(doorFrame, FlagComponent.Level_Ceiling);
                if (!ceilingRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(door);
                    manager.ReleaseEntity(doorFrame);
                    return Result.FailFor<int>();
                }
            }

            newEntityBuffer[0] = door;
            newEntityBuffer[1] = doorFrame;

            return Result.From(2);
        }

        // HACK
        private static int BushCount = 0;
        // END HACK
        private static Result<int> MakeBush(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            // HACK
            if (BushCount >= 20) return Result.FailFor<int>();
            BushCount++;
            // END HACK

            var manager = state.EntityManager;

            var bushRes = manager.NewEntity();
            if (!bushRes.Success) return Result.FailFor<int>();

            var bush = bushRes.Value;

            var x = def.X;
            var y = def.Y;

            var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.Bush, 0);
            if (!animRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(bush, animRes.Value).Success)
            {
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var posRes = manager.CreatePosition(x, y);
            if (!posRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(bush, posRes.Value).Success)
            {
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var colRes = manager.CreateCollision(AssetNames.Bush, 0, BushCollisionHandler.Collision, BushCollisionHandler.Push);
            if (!colRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(bush, colRes.Value).Success)
            {
                manager.ReleaseComponent(colRes.Value);
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var velRes = manager.CreateVelocity(0, 0);
            if (!velRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(bush, velRes.Value).Success)
            {
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var bRes = manager.CreateBush();
            if (!bRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(bush, bRes.Value).Success)
            {
                manager.ReleaseComponent(bRes.Value);
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var floorRes = manager.AddComponent(bush, FlagComponent.Level_Floor);
            if (!floorRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var midRes = manager.AddComponent(bush, FlagComponent.Level_Middle);
            if (!midRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            var dmgRes = manager.AddComponent(bush, FlagComponent.TakesDamage);
            if (!dmgRes.Success)
            {
                manager.ReleaseEntity(bush);
                return Result.FailFor<int>();
            }

            newEntityBuffer[0] = bush;

            return Result.From(1);
        }

        private static Result<int> MakeBushWallTopBottom(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;

            var baseRes = manager.NewEntity();
            if (!baseRes.Success)
            {
                return Result.FailFor<int>();
            }
            var topRes = manager.NewEntity();
            if (!topRes.Success)
            {
                manager.ReleaseEntity(baseRes.Value);
                return Result.FailFor<int>();
            }

            var wallBase = baseRes.Value;
            var wallTop = topRes.Value;

            var x = def.X;
            var y = def.Y;

            // setup the base of the wall
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.BushWall_Base_TopBottom, 0);
                if (!animRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallBase, animRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallBase, posRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.BushWall_Base_TopBottom, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallBase, colRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallBase, velRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(wallBase, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
            }

            // setup the top of the wall
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.BushWall_Top_TopBottom, 0);
                if (!animRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallTop, animRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(x, y);
                if (!posRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallTop, posRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.BushWall_Top_TopBottom, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallTop, colRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(wallTop, velRes.Value).Success)
                {
                    // free everything and bail
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }

                var middleRes = manager.AddComponent(wallTop, FlagComponent.Level_Middle);
                if (!middleRes.Success)
                {
                    // free everything and bail
                    manager.ReleaseEntity(wallBase);
                    manager.ReleaseEntity(wallTop);
                    return Result.FailFor<int>();
                }
            }

            newEntityBuffer[0] = wallBase;
            newEntityBuffer[1] = wallTop;

            return Result.From(2);
        }

        public static Result<Entity> CreateSword(GameState state, int swordX, int swordY, AnimationNames swordAnim)
        {
            var manager = state.EntityManager;
            var swordRes = manager.NewEntity();
            if (!swordRes.Success)
            {
                return Result.FailFor<Entity>();
            }

            var sword = swordRes.Value;

            var sRes = manager.CreateSword();
            if (!sRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(sword, sRes.Value).Success)
            {
                // free everything
                manager.ReleaseComponent(sRes.Value);
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            var posRes = manager.CreatePosition(swordX, swordY);
            if (!posRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(sword, posRes.Value).Success)
            {
                // free everything
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            var velRes = manager.CreateVelocity(0, 0);
            if (!velRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(sword, velRes.Value).Success)
            {
                // free everything
                manager.ReleaseComponent(velRes.Value);
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            var animRes = manager.CreateAnimation(state.AnimationManager, swordAnim, 0);
            if (!animRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(sword, animRes.Value).Success)
            {
                // free everything
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            var middleRes = manager.AddComponent(sword, FlagComponent.Level_Middle);
            if (!middleRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            var dmgRes = manager.AddComponent(sword, FlagComponent.DealsDamage);
            if (!dmgRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            var curAnim = manager.GetAnimationFor(sword);
            var curFrame = curAnim.GetCurrentFrame(state.AnimationManager);

            var colRes = manager.CreateCollision(curFrame, 0, SwordCollisionHandler.Collision, SwordCollisionHandler.Push);
            if (!colRes.Success)
            {
                // free everything
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }
            if (!manager.AddComponent(sword, colRes.Value).Success)
            {
                // free everything
                manager.ReleaseComponent(colRes.Value);
                manager.ReleaseEntity(sword);
                return Result.FailFor<Entity>();
            }

            return Result.From(sword);
        }

        private static Result<int> MakeTree(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;

            var trunkRes = manager.NewEntity();
            if (!trunkRes.Success)
            {
                return Result.FailFor<int>();
            }
            var canopyRes = manager.NewEntity();
            if (!canopyRes.Success)
            {
                manager.ReleaseEntity(trunkRes.Value);
                return Result.FailFor<int>();
            }

            var trunk = trunkRes.Value;
            var canopy = canopyRes.Value;

            var trunkX = def.X;
            var trunkY = def.Y + 12 * 16;

            // create trunk
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.TreeBottom, 0);
                if (!animRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(trunk, animRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(trunkX, trunkY);
                if (!posRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(trunk, posRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var colRes = manager.CreateCollision(AssetNames.TreeBottom, 0, DoNothingCollisionHandler.Collision, DoNothingCollisionHandler.Push);
                if (!colRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(trunk, colRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(colRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(trunk, velRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var floorRes = manager.AddComponent(trunk, FlagComponent.Level_Floor);
                if (!floorRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                newEntityBuffer[0] = trunk;
            }

            var canopyX = trunkX;
            var canopyY = trunkY - state.AssetMeasurer.Measure(AssetNames.TreeTop).Height + 40 + 12;

            // create canopy
            {
                var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.TreeTop, 0);
                if (!animRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(canopy, animRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(animRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var posRes = manager.CreatePosition(canopyX, canopyY);
                if (!posRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(canopy, posRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(posRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var velRes = manager.CreateVelocity(0, 0);
                if (!velRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }
                if (!manager.AddComponent(canopy, velRes.Value).Success)
                {
                    // free everything
                    manager.ReleaseComponent(velRes.Value);
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                var ceilingRes = manager.AddComponent(canopy, FlagComponent.Level_Ceiling);
                if (!ceilingRes.Success)
                {
                    // free everything
                    manager.ReleaseEntity(trunk);
                    manager.ReleaseEntity(canopy);
                    return Result.FailFor<int>();
                }

                newEntityBuffer[1] = canopy;
            }

            return Result.From(2);
        }

        private static Result<int> MakeFlowerTopLeftBottomRight(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;
            var startFrame = def.GetProperty("StartFrame");

            if (!int.TryParse(startFrame, out var startFrameParsed))
            {
                startFrameParsed = 0;
            }

            var toRes = state.EntityManager.NewEntity();
            if (!toRes.Success) return Result.FailFor<int>();

            var to = toRes.Value;
            newEntityBuffer[0] = to;

            var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.FlowerTopLeftBottomRight, startFrameParsed);
            if (!animRes.Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(to, animRes.Value).Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }

            var posRes = manager.CreatePosition(def.X, def.Y);
            if (!posRes.Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(to, posRes.Value).Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }

            return Result.From(1);
        }

        private static Result<int> MakeFlowerTopLeft(GameState state, RoomObject def, Entity[] newEntityBuffer)
        {
            var manager = state.EntityManager;
            var startFrame = def.GetProperty("StartFrame");

            if (!int.TryParse(startFrame, out var startFrameParsed))
            {
                startFrameParsed = 0;
            }

            var toRes = state.EntityManager.NewEntity();
            if (!toRes.Success) return Result.FailFor<int>();

            var to = toRes.Value;
            newEntityBuffer[0] = to;

            var animRes = manager.CreateAnimation(state.AnimationManager, AnimationNames.FlowerTopLeft, startFrameParsed);
            if (!animRes.Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(to, animRes.Value).Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseComponent(animRes.Value);
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }

            var posRes = manager.CreatePosition(def.X, def.Y);
            if (!posRes.Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }
            if (!manager.AddComponent(to, posRes.Value).Success)
            {
                // free the entity, which releases anything attached as well
                manager.ReleaseComponent(posRes.Value);
                manager.ReleaseEntity(to);
                return Result.FailFor<int>();
            }

            return Result.From(1);
        }
    }
}