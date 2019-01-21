using System;
using Xunit;
using LearnMeAThing.Managers;
using LearnMeAThing.Entities;
using System.Collections.Generic;
using System.Linq;
using LearnMeAThing.Components;
using LearnMeAThing.Assets;

namespace LearnMeAThing.Tests
{
    public class EntityManagerTests
    {
        [Fact]
        public void CompactingComponents()
        {
            var animManager = new _AnimationManager((AnimationNames.Bush, new AnimationTemplate(AnimationNames.Bush, new[] { AssetNames.Bush }, 0)));

            var manager = new EntityManager(new _IIdIssuer(), 1000);
            
            var ac1 = manager.CreateAcceleration(0, 0, 1).Value;
            var ac2 = manager.CreateAcceleration(0, 0, 1).Value;

            var an1 = manager.CreateAnimation(animManager, AnimationNames.Bush, 0).Value;
            var an2 = manager.CreateAnimation(animManager, AnimationNames.Bush, 0).Value;
            
            var b1 = manager.CreateBush().Value;
            var b2 = manager.CreateBush().Value;

            var c1 = manager.CreateCollision(AssetNames.Bush, 0, null, null).Value;
            var c2 = manager.CreateCollision(AssetNames.Bush, 0, null, null).Value;

            var i1 = manager.CreateInputs().Value;
            var i2 = manager.CreateInputs().Value;

            var ps1 = manager.CreatePlayerState(PlayerStanding.East).Value;
            var ps2 = manager.CreatePlayerState(PlayerStanding.East).Value;

            var p1 = manager.CreatePosition(0, 0).Value;
            var p2 = manager.CreatePosition(0, 0).Value;

            var s1 = manager.CreateSword().Value;
            var s2 = manager.CreateSword().Value;

            var v1 = manager.CreateVelocity(0, 0).Value;
            var v2 = manager.CreateVelocity(0, 0).Value;

            {
                var e1 = manager.NewEntity().Value;
                var e2 = manager.NewEntity().Value;
                var e3 = manager.NewEntity().Value;
                var e4 = manager.NewEntity().Value;
                var e5 = manager.NewEntity().Value;

                manager.AddComponent(e1, ac1);
                manager.AddComponent(e1, an1);
                manager.AddComponent(e1, b1);
                manager.AddComponent(e1, FlagComponent.Camera);

                manager.ReleaseEntity(e1);

                manager.AddComponent(e2, ac1);
                manager.AddComponent(e2, b1);
                manager.AddComponent(e2, v1);
                manager.AddComponent(e2, FlagComponent.Camera);

                manager.AddComponent(e3, p1);
                manager.AddComponent(e3, ps1);

                manager.AddComponent(e4, p2);
                manager.AddComponent(e4, c1);
                manager.AddComponent(e4, FlagComponent.DealsDamage);

                manager.AddComponent(e5, v2);
                manager.AddComponent(e5, ac2);
                manager.AddComponent(e5, s1);
                manager.AddComponent(e5, i2);
                manager.AddComponent(e5, FlagComponent.Player);

                manager.ReleaseEntity(e4);
                manager.RemoveComponent(e2, ac1);
            }
            manager.Compact(Array.Empty<IHoldsEntity>());

            // we expect for e1 & e4 to be gone
            //   so e2 becomes Id = 1
            //      e3 Id = 2
            //      e5 Id = 3
            //
            // Id = 1 should have b1, v1, and Camera
            // Id = 2 should have p1 & ps1
            // Id = 3 should have v2, ac2, s1, i2, and Player
            Assert.Equal(4, manager.NextAvailableEntity);

            var newE1 = new Entity(1);
            Assert.Equal(b1, manager.GetBushFor(newE1));
            Assert.Equal(v1, manager.GetVelocityFor(newE1));
            Assert.True(manager.GetFlagComponentsForEntity(newE1).Value.HasFlag(FlagComponent.Camera));

            var newE2 = new Entity(2);
            Assert.Equal(p1, manager.GetPositionFor(newE2));
            Assert.Equal(ps1, manager.GetPlayerStateFor(newE2));

            var newE3 = new Entity(3);
            Assert.Equal(v2, manager.GetVelocityFor(newE3));
            Assert.Equal(ac2, manager.GetAccelerationFor(newE3));
            Assert.Equal(s1, manager.GetSwordFor(newE3));
            Assert.Equal(i2, manager.GetInputsFor(newE3));
            Assert.True(manager.GetFlagComponentsForEntity(newE3).Value.HasFlag(FlagComponent.Player));

            { 
                var newAc = _GetComponents(manager.EntitiesWithAcceleration());
                Assert.Equal(1, newAc.Count);
                Assert.Equal(ac2, newAc[0].Item2);
                Assert.Equal(3, newAc[0].Item1.Id);
            }
            {
                var newAn = _GetComponents(manager.EntitiesWithAnimation());
                Assert.Equal(0, newAn.Count);
            }
            {
                var newB = _GetComponents(manager.EntitiesWithBush());
                Assert.Equal(1, newB.Count);
                Assert.Equal(b1, newB[0].Item2);
                Assert.Equal(1, newB[0].Item1.Id);
            }
            {
                var newC = _GetComponents(manager.EntitiesWithCollision());
                Assert.Equal(0, newC.Count);
            }
            {
                var newI = _GetComponents(manager.EntitiesWithInputs());
                Assert.Equal(1, newI.Count);
                Assert.Equal(i2, newI[0].Item2);
                Assert.Equal(3, newI[0].Item1.Id);
            }
            {
                var newPS = _GetComponents(manager.EntitiesWithPlayerState());
                Assert.Equal(1, newPS.Count);
                Assert.Equal(ps1, newPS[0].Item2);
                Assert.Equal(2, newPS[0].Item1.Id);
            }
            {
                var newP = _GetComponents(manager.EntitiesWithPosition());
                Assert.Equal(1, newP.Count);
                Assert.Equal(p1, newP[0].Item2);
                Assert.Equal(2, newP[0].Item1.Id);
            }
            {
                var newS = _GetComponents(manager.EntitiesWithSword());
                Assert.Equal(1, newS.Count);
                Assert.Equal(s1, newS[0].Item2);
                Assert.Equal(3, newS[0].Item1.Id);
            }
            {
                var newV = _GetComponents(manager.EntitiesWithVelocity());
                Assert.Equal(2, newV.Count);
                Assert.Equal(v1, newV[0].Item2);
                Assert.Equal(1, newV[0].Item1.Id);
                Assert.Equal(v2, newV[1].Item2);
                Assert.Equal(3, newV[1].Item1.Id);
            }
        }

        // grab all the components
        private static List<(Entity, T)> _GetComponents<T>(EntityManager.EntitiesWithStatefulComponentEnumerable<T> list)
            where T : AStatefulComponent
        {
            var ret = new List<(Entity, T)>();
            foreach (var item in list) ret.Add(item);

            return ret;
        }

        [Fact]
        public void CompactingAssociatedEntityComponents()
        {
            var manager = new EntityManager(new _IIdIssuer(), 1000);

            var e1 = manager.NewEntity().Value;
            var e2 = manager.NewEntity().Value;
            var e3 = manager.NewEntity().Value;
            var e4 = manager.NewEntity().Value;
            var e5 = manager.NewEntity().Value;

            var associated1 = manager.CreateAssociatedEntity(e2, e5).Value;
            manager.AddComponent(e1, associated1);

            var associated2 = manager.CreateAssociatedEntity(e1).Value;
            manager.AddComponent(e5, associated2);

            manager.ReleaseEntity(e3);
            manager.ReleaseEntity(e4);

            manager.Compact(Array.Empty<IHoldsEntity>());

            Assert.Equal(3, manager.NumLiveEntities);
            Assert.Equal(2, manager.NumLiveComponents);

            var a1 = manager.GetAssociatedEntityFor(new Entity(1));
            Assert.NotNull(a1);
            Assert.Equal(2, a1.FirstEntity.Id);
            Assert.NotNull(a1.SecondEntity);
            Assert.Equal(3, a1.SecondEntity.Value.Id);

            var a2 = manager.GetAssociatedEntityFor(new Entity(3));
            Assert.NotNull(a2);
            Assert.Equal(1, a2.FirstEntity.Id);
        }

        [Fact]
        public void Exhausts()
        {
            var manager = new EntityManager(new _IIdIssuer(), 10);
            var e1 = manager.NewEntity();
            Assert.True(e1.Success);
            Assert.Equal(1, e1.Value.Id);

            for (var i = 2; i <= 10; i++)
            {
                var eN = manager.NewEntity();
                Assert.True(eN.Success);
                Assert.Equal(i, eN.Value.Id);
            }

            var eExhausted = manager.NewEntity();
            Assert.False(eExhausted.Success);
        }

        class _ReuseHoldsEntities: IHoldsEntity
        {
            private List<Entity> List;

            public int EntitiesCount => List.Count;

            public _ReuseHoldsEntities(List<Entity> list)
            {
                List = list;
            }

            public Entity GetEntity(int ix)
            => List[ix];

            public void SetEntity(int ix, Entity e)
            => List[ix] = e;
        }

        [Fact]
        public void Reuse()
        {
            var manager = new EntityManager(new _IIdIssuer(), 10);

            var currentEntities = new List<Entity>();
            var reuse = new _ReuseHoldsEntities(currentEntities);

            for (var i = 0; i < 5; i++)
            {
                var e = manager.NewEntity();
                Assert.True(e.Success);
                currentEntities.Add(e.Value);

                var uniqueIds = currentEntities.Select(c => c.Id).Distinct().Count();
                Assert.Equal(currentEntities.Count, uniqueIds);
            }

            var r = new Random();

            for (var i = 0; i < 10_000; i++)
            {
                var toReleaseIx = r.Next(currentEntities.Count);

                var e = currentEntities[toReleaseIx];
                currentEntities.RemoveAt(toReleaseIx);
                manager.ReleaseEntity(e);

                if (manager.IsFull)
                {
                    manager.Compact(new[] { reuse });
                }

                var nE = manager.NewEntity();
                Assert.True(nE.Success);
                currentEntities.Add(nE.Value);

                var uniqueIds = currentEntities.Select(c => c.Id).Distinct().Count();
                Assert.Equal(currentEntities.Count, uniqueIds);
            }
        }

        [Fact]
        public void FlagComponents()
        {
            var manager = new EntityManager(new _IIdIssuer(), 10);

            var e1 = manager.NewEntity().Value;
            var e2 = manager.NewEntity().Value;

            {
                var r1 = manager.AddComponent(e1, (FlagComponent)2);
                Assert.True(r1.Success);
            }

            {
                var r2 = manager.AddComponent(e2, (FlagComponent)4);
                Assert.True(r2.Success);
            }

            {
                var r1C = manager.GetFlagComponentsForEntity(e1);
                Assert.True(r1C.Success);
                Assert.True(r1C.Value.HasFlag((FlagComponent)2));
                Assert.False(r1C.Value.HasFlag((FlagComponent)4));
            }

            {
                var r2C = manager.GetFlagComponentsForEntity(e2);
                Assert.True(r2C.Success);
                Assert.False(r2C.Value.HasFlag((FlagComponent)2));
                Assert.True(r2C.Value.HasFlag((FlagComponent)4));
            }

            {
                var r3c = manager.GetFlagComponentsForEntity(new Entity(3));
                Assert.False(r3c.Success);
            }

            {
                manager.RemoveComponent(e1, (FlagComponent)2);
                var r1C = manager.GetFlagComponentsForEntity(e1);
                Assert.True(r1C.Success);
                Assert.False(r1C.Value.HasFlag((FlagComponent)2));
                Assert.False(r1C.Value.HasFlag((FlagComponent)4));
            }

            {
                manager.RemoveComponent(e1, (FlagComponent)4);
                var r1C = manager.GetFlagComponentsForEntity(e1);
                Assert.True(r1C.Success);
                Assert.False(r1C.Value.HasFlag((FlagComponent)2));
                Assert.False(r1C.Value.HasFlag((FlagComponent)4));
            }

            {
                manager.RemoveComponent(e2, (FlagComponent)2);
                var r2C = manager.GetFlagComponentsForEntity(e2);
                Assert.True(r2C.Success);
                Assert.False(r2C.Value.HasFlag((FlagComponent)2));
                Assert.True(r2C.Value.HasFlag((FlagComponent)4));
            }

            {
                manager.RemoveComponent(e2, (FlagComponent)4);
                var r2C = manager.GetFlagComponentsForEntity(e2);
                Assert.True(r2C.Success);
                Assert.False(r2C.Value.HasFlag((FlagComponent)2));
                Assert.False(r2C.Value.HasFlag((FlagComponent)4));
            }
        }
        
        [Fact]
        public void StatefulComponents()
        {
            var manager = new EntityManager(new _IIdIssuer(), 10);

            var e1 = manager.NewEntity().Value;
            var e2 = manager.NewEntity().Value;

            var state1 = manager.CreatePosition( 12, 12).Value;
            var state2 = manager.CreateVelocity(14, 14).Value;

            {
                var r1 = manager.AddComponent(e1, state1);
                Assert.True(r1.Success);
            }

            {
                var r2 = manager.AddComponent(e2, state2);
                Assert.True(r2.Success);
            }

            {
                var on1 = new[] { manager.GetPositionFor(e1) };
                Assert.Equal(1, on1.Length);
                Assert.Equal(12, (int)on1[0].X);
                Assert.Equal(12, (int)on1[0].Y);
            }

            {
                var on2 = new[] { manager.GetVelocityFor(e2) };
                Assert.Equal(1, on2.Length);
                Assert.Equal(14, (int)on2[0].X_SubPixels);
                Assert.Equal(14, (int)on2[0].Y_SubPixels);
            }

            manager.RemoveComponent(e1, state2);
            
            {
                var on1 = new[] { manager.GetPositionFor(e1) };
                Assert.Equal(1, on1.Length);
                Assert.Equal(12, (int)on1[0].X);
                Assert.Equal(12, (int)on1[0].Y);
            }
        }
        
        [Fact]
        public void Enumerating()
        {
            var manager = new EntityManager(new _IIdIssuer(), 10);

            var e1 = manager.NewEntity().Value;
            var e2 = manager.NewEntity().Value;
            var e3 = manager.NewEntity().Value;
            var e4 = manager.NewEntity().Value;

            const FlagComponent FC1 = (FlagComponent)2;
            const FlagComponent FC2 = (FlagComponent)4;
            const FlagComponent FC3 = (FlagComponent)8;
            const FlagComponent FC4 = (FlagComponent)16;

            manager.AddComponent(e1, FC1);
            manager.AddComponent(e2, FC2);
            manager.AddComponent(e3, FC1);
            manager.AddComponent(e4, FC3);

            var c1 = manager.CreateVelocity(6, 6).Value;
            var c2 = manager.CreateVelocity(6, 6).Value;
            var c3 = manager.CreatePosition(11, 11).Value;
            var c4 = manager.CreateAcceleration(12, 12, 12).Value;

            manager.AddComponent(e1, c1);
            manager.AddComponent(e2, c2);
            manager.AddComponent(e2, c3);
            manager.AddComponent(e4, c4);

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWith(FC1))
                {
                    res.Add(item);
                }

                Assert.Equal(2, res.Count);
                Assert.True(res.Contains(e1));
                Assert.True(res.Contains(e3));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWith(FC2))
                {
                    res.Add(item);
                }

                Assert.Equal(1, res.Count);
                Assert.True(res.Contains(e2));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWith(FC3))
                {
                    res.Add(item);
                }

                Assert.Equal(1, res.Count);
                Assert.True(res.Contains(e4));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWith(FC4))
                {
                    res.Add(item);
                }

                Assert.Equal(0, res.Count);
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWithVelocity())
                {
                    res.Add(item.Entity);
                }

                Assert.Equal(2, res.Count);
                Assert.True(res.Contains(e1));
                Assert.True(res.Contains(e2));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWithPosition())
                {
                    res.Add(item.Entity);
                }

                Assert.Equal(1, res.Count);
                Assert.True(res.Contains(e2));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWithAcceleration())
                {
                    res.Add(item.Entity);
                }

                Assert.Equal(1, res.Count);
                Assert.True(res.Contains(e4));
            }

            {
                var res = new List<Entity>();
                foreach (var item in manager.EntitiesWithSword())
                {
                    res.Add(item.Entity);
                }

                Assert.Equal(0, res.Count);
            }
        }
    }
}
