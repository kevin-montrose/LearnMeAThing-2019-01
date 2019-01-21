using LearnMeAThing.Assets;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;
using System;
using System.Collections.Generic;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class ObjectCreatorTests
    {
        [Theory]
        [InlineData(RoomObjectTypes.Bush, 1, 1, new string[0])]
        [InlineData(RoomObjectTypes.FlowerTopLeft, 2, 2, new[] { "StartFrame=2" })]
        [InlineData(RoomObjectTypes.FlowerTopLeftBottomRight, 3, 3, new[] { "StartFrame=3" })]
        [InlineData(RoomObjectTypes.Tree, 4, 4, new string[0])]
        [InlineData(RoomObjectTypes.BushWall_TopBottom, 5, 5, new string[0])]
        [InlineData(RoomObjectTypes.Door, 6, 6, new[] { "TargetRoom=Kakariko", "TargetX=6", "TargetY=6" })]
        [InlineData(RoomObjectTypes.Stairs, 7, 7, new[] { "TargetRoom=Kakariko", "TargetX=7", "TargetY=7", "StairsDirection=Up" })]
        [InlineData(RoomObjectTypes.Pit, 8, 8, new[] { "TargetRoom=Kakariko", "TargetX=8", "TargetY=8" })]
        [InlineData(RoomObjectTypes.SwordKnight, 9, 9, new[] { "InitialFacingDirection=East" })]
        public void GracefulFailure_RoomObjects(RoomObjectTypes type, int x, int y, string[] properties)
        {
            var parsedProps = new List<RoomObjectProperty>();
            foreach (var prop in properties)
            {
                var ix = prop.IndexOf('=');
                var name = prop.Substring(0, ix);
                var val = prop.Substring(ix + 1);

                parsedProps.Add(new RoomObjectProperty(name, val));
            }

            var def = new RoomObject(type, x, y, parsedProps.ToArray());

            var needCalls = CallsNeededForSuccess();
            Assert.True(needCalls > 0);

            for(var i = 0; i < needCalls; i++)
            {
                var game = MakeGameState();
                var manager = new EntityManager(new _IIdIssuer(), 100);
                manager.FailAfterCalls = i;
                game.EntityManager = manager;

                // pre condition
                Assert.Equal(0, manager.NumLiveEntities);
                Assert.Equal(0, manager.NumLiveComponents);

                var createRes = ObjectCreator.Create(game, def, new Entity[100]);
                Assert.False(createRes.Success);

                // post condition
                Assert.Equal(0, manager.NumLiveEntities);
                Assert.Equal(0, manager.NumLiveComponents);
            }
            
            // normal invocation, just count how much we need to succeed
            int CallsNeededForSuccess()
            {
                var game = MakeGameState();

                var manager = new EntityManager(new _IIdIssuer(), 100);
                game.EntityManager = manager;
                game.AnimationManager = new _AnimationManager();

                var size = ObjectCreator.Create(game, def, new Entity[100]);
                Assert.True(size.Success);

                return manager.FallibleCallCount;
            }

            // make a bare minimum game state
            GameState MakeGameState()
            {
                var game = new GameState();

                // directly setting these so there's no player allocated
                game.AnimationManager = new _AnimationManager();
                game.AssetMeasurer = new _AssetMeasurer();

                return game;
            }
        }
        
        private void _GracefulFailure_Special(Func<GameState, Result<Entity>> createDel)
        {
            var needCalls = CallsNeededForSuccess();
            Assert.True(needCalls > 0);

            for (var i = 0; i < needCalls; i++)
            {
                var game = MakeGameState();
                var manager = new EntityManager(new _IIdIssuer(), 100);
                manager.FailAfterCalls = i;
                game.EntityManager = manager;

                // pre condition
                Assert.Equal(0, manager.NumLiveEntities);
                Assert.Equal(0, manager.NumLiveComponents);

                var createRes = createDel(game);
                Assert.False(createRes.Success);

                // post condition
                Assert.Equal(0, manager.NumLiveEntities);
                Assert.Equal(0, manager.NumLiveComponents);
            }

            // normal invocation, just count how much we need to succeed
            int CallsNeededForSuccess()
            {
                var game = MakeGameState();

                var manager = new EntityManager(new _IIdIssuer(), 100);
                game.EntityManager = manager;
                game.AnimationManager = new _AnimationManager();

                var size = createDel(game);
                Assert.True(size.Success);

                return manager.FallibleCallCount;
            }

            GameState MakeGameState()
            {
                var game = new GameState();

                // directly setting these so there's no player allocated
                game.AnimationManager = new _AnimationManager((AnimationNames.Sword_Bottom, new AnimationTemplate(AnimationNames.Sword_Bottom, new[] { AssetNames.Sword_Bottom }, 0)));
                game.AssetMeasurer = new _AssetMeasurer();

                return game;
            }
        }

        [Fact]
        public void GracefulFailure_Camera()
        => _GracefulFailure_Special(game => ObjectCreator.CreateCamera(game, 0, 0));

        [Fact]
        public void GracefulFailure_PlayerHead()
        => _GracefulFailure_Special(game => ObjectCreator.CreatePlayerHead(game, 0, 0));

        [Fact]
        public void GracefulFailure_PlayerBody()
        => _GracefulFailure_Special(game => ObjectCreator.CreatePlayerBody(game, 0, 0));

        [Fact]
        public void GracefulFailure_PlayerFeet()
        => _GracefulFailure_Special(game => ObjectCreator.CreatePlayerFeet(game, 0, 0));

        [Fact]
        public void GracefulFailure_Sword()
        => _GracefulFailure_Special(game => ObjectCreator.CreateSword(game, 0, 0, AnimationNames.Sword_Bottom));

        [Fact]
        public void GracefulFailure_DropShadow()
        => _GracefulFailure_Special(game => ObjectCreator.CreateDropShadow(game, 0, 0));
    }
}
