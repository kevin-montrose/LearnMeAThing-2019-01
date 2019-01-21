using LearnMeAThing.Assets;
using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Systems
{
    sealed class BushSystem: 
        ASystem<EntityManager.EntitiesWithStatefulComponentEnumerable<BushComponent>>
    {
        public override SystemType Type => SystemType.Bush;
        
        public void Cut(GameState state, Entity bush)
        {
            var b = state.EntityManager.GetBushFor(bush);

            b.NeedsCut = true;
        }

        public override EntityManager.EntitiesWithStatefulComponentEnumerable<BushComponent> DesiredEntities(EntityManager manager)
        => manager.EntitiesWithBush();

        public override void Update(GameState state, EntityManager.EntitiesWithStatefulComponentEnumerable<BushComponent> requestedEntities)
        {
            foreach(var e in requestedEntities)
            {
                var bushE = e.Entity;
                var bushState = e.Component;

                if (bushState.IsCut)
                {
                    UpdateCutBush(state, bushE);
                }

                // do the cutting after the "advance cut"-step
                //    so we don't skip a frame
                if (bushState.NeedsCut)
                {
                    bushState.NeedsCut = false;
                    CutBush(state, bushE);
                }
            }
        }

        private static void UpdateCutBush(GameState state, Entity bush)
        {
            var manager = state.EntityManager;
            var bushAnim = manager.GetAnimationFor(bush);
            if(bushAnim == null)
            {
                // glitch: ???
                return;
            }

            var activeAnimation = state.AnimationManager.Get(AnimationNames.Bush_Leaves);
            var doneAfter = activeAnimation.StepAfter * activeAnimation.Frames.Length;
            var done = bushAnim.TickCounter >= doneAfter;

            if (done)
            {
                manager.ReleaseEntity(bush);
            }
        }

        private static void CutBush(GameState state, Entity bush)
        {
            var manager = state.EntityManager;
            var bushPos = manager.GetPositionFor(bush);
            if (bushPos == null)
            {
                // glitch: ???
                return;
            }
            var bushAnim = manager.GetAnimationFor(bush);
            if (bushAnim == null)
            {
                // glitch: ???
                return;
            }
            var bushState = manager.GetBushFor(bush);
            if (bushState == null)
            {
                // glitch: ???
                return;
            }
            var bushCollision = manager.GetCollisionFor(bush);
            if(bushCollision == null)
            {
                // glitch: ???
                return;
            }
            var bushVelocity = manager.GetVelocityFor(bush);
            if(bushVelocity == null)
            {
                // glitch: ???
                return;
            }

            if(bushState.IsCut)
            {
                // glitch: ???
                return;
            }

            // cut it
            bushState.IsCut = true;

            // remove all the components that are now useless
            manager.RemoveComponent(bush, bushCollision);
            manager.RemoveComponent(bush, bushVelocity);
            manager.RemoveComponent(bush, FlagComponent.Level_Middle);
            manager.RemoveComponent(bush, FlagComponent.Level_Floor);            

            // update position so the leaves match where the bush once was
            var oldSize = state.AssetMeasurer.Measure(bushAnim.GetCurrentFrame(state.AnimationManager));
            bushAnim.SwitchTo(AnimationNames.Bush_Leaves);
            var newSize = state.AssetMeasurer.Measure(bushAnim.GetCurrentFrame(state.AnimationManager));

            var deltaX = (oldSize.Width - newSize.Width) / 2;
            var deltaY = oldSize.Height - newSize.Height;

            bushPos.X_SubPixel += (deltaX * PositionComponent.SUBPIXELS_PER_PIXEL);
            bushPos.Y_SubPixel += (deltaY * PositionComponent.SUBPIXELS_PER_PIXEL);
        }
    }
}
