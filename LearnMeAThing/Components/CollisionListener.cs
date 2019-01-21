using LearnMeAThing.Assets;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;

namespace LearnMeAThing.Components
{
    delegate void CollisionHandler(GameState state, Entity self, Entity collidedWith, Point atPoint, ConvexPolygon ourPoly, ConvexPolygon theirPolygon);
    delegate void PushedAwayHandler(GameState state, Entity self, Entity other, Vector pushDirectionScreen);

    sealed class CollisionListener : AStatefulComponent
    {
        public override ComponentType Type => ComponentType.CollisionListener;
        
        public AssetNames HitMap { get; set; }
        
        // todo: remove this
        public int DesiredSpeed_HACK { get; private set; }
        
        public CollisionHandler OnCollision { get; private set; }

        public PushedAwayHandler OnPush { get; private set; }
        
        public void Initialize(
            AssetNames hitMap,
            int desiredSpeed,
            CollisionHandler onCollide,
            PushedAwayHandler onPush
        )
        {
            HitMap = hitMap;
            DesiredSpeed_HACK = desiredSpeed;
            OnCollision = onCollide;
            OnPush = onPush;
        }

        public override string ToString() => $"{Type}: {nameof(HitMap)}={HitMap}";
    }
}
