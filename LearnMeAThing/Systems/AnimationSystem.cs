using LearnMeAThing.Components;
using LearnMeAThing.Managers;

namespace LearnMeAThing.Systems
{
    sealed class AnimationSystem : ASystem<EntityManager.EntitiesWithStatefulComponentEnumerable<AnimationComponent>>
    {
        public override SystemType Type => SystemType.Animation;

        public override EntityManager.EntitiesWithStatefulComponentEnumerable<AnimationComponent> DesiredEntities(EntityManager manager)
        => manager.EntitiesWithAnimation();

        public override void Update(GameState state, EntityManager.EntitiesWithStatefulComponentEnumerable<AnimationComponent> requestedEntities)
        {
            var onlyPlayerOrShadows = state.ExitSystem.IsTransitioning;

            foreach(var anim in requestedEntities)
            {
                var e = anim.Entity;
                
                if (onlyPlayerOrShadows)
                {
                    var isPlayer = e.Id == state.Player_Feet.Id || e.Id == state.Player_Body.Id || e.Id == state.Player_Head.Id;

                    var flags = state.EntityManager.GetFlagComponentsForEntity(e);
                    var isShadow = flags.Success && flags.Value.HasFlag(FlagComponent.DropShadow);

                    if (!isPlayer && !isShadow) continue;
                }

                var c = anim.Component;
                c.Advance();
            }
        }
    }
}
