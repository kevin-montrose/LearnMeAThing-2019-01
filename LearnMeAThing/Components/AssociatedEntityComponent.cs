using LearnMeAThing.Entities;
using LearnMeAThing.Managers;
using System;

namespace LearnMeAThing.Components
{
    /// <summary>
    /// Links multiple entities together.
    /// 
    /// This component is automatically managed by the EntityManager,
    ///    so the entities are always "correct".
    /// </summary>
    sealed class AssociatedEntityComponent: AStatefulComponent, IHoldsEntity
    {
        public override ComponentType Type => ComponentType.AssociatedEntity;

        public Entity FirstEntity { get; set; }
        public Entity? SecondEntity { get; set; }
        public Entity? ThirdEntity { get; set; }
        public Entity? FourthEntity { get; set; }
        public Entity? FifthEntity { get; set; }

        public int EntitiesCount
        {
            get
            {
                if (FifthEntity.HasValue) return 5;
                if (FourthEntity.HasValue) return 4;
                if (ThirdEntity.HasValue) return 3;
                if (SecondEntity.HasValue) return 2;

                return 1;
            }
        }

        public Entity GetEntity(int ix)
        {
            switch (ix)
            {
                case 0: return FirstEntity;
                case 1: return SecondEntity.Value;
                case 2: return ThirdEntity.Value;
                case 3: return FourthEntity.Value;
                case 4: return FifthEntity.Value;
                default: throw new Exception($"Unexpected index: {ix}");
            }
        }

        public void SetEntity(int ix, Entity e)
        {
            switch (ix)
            {
                case 0: FirstEntity = e; break;
                case 1: SecondEntity = e; break;
                case 2: ThirdEntity = e; break;
                case 3: FourthEntity = e; break;
                case 4: FifthEntity = e; break;
                default: throw new Exception($"Unexpected index: {ix}");
            }
        }

        public void Initialize(Entity e1, Entity? e2, Entity? e3, Entity? e4, Entity? e5)
        {
            FirstEntity = e1;
            SecondEntity = e2;
            ThirdEntity = e3;
            FourthEntity = e4;
            FifthEntity = e5;

            if (e3.HasValue && !e2.HasValue) throw new InvalidOperationException("There can be no gaps in used entities");
            if (e4.HasValue && !e3.HasValue) throw new InvalidOperationException("There can be no gaps in used entities");
            if (e5.HasValue && !e4.HasValue) throw new InvalidOperationException("There can be no gaps in used entities");

            FirstEntity = e1;
            SecondEntity = e2;
            ThirdEntity = e3;
            FourthEntity = e4;
            FifthEntity = e5;
        }

        public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(FirstEntity)}: {FirstEntity}, {nameof(SecondEntity)}: {SecondEntity}, {nameof(ThirdEntity)}: {ThirdEntity}, {nameof(FourthEntity)}: {FourthEntity}, {nameof(FifthEntity)}: {FifthEntity}";
    }
}
