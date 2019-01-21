using LearnMeAThing.Components;
using System;

namespace LearnMeAThing.Utilities
{
    /// <summary>
    /// Implements a simple fixed size object pool for the
    ///    given component type.
    ///    
    /// Everything is pre-allocated, so once we're up and 
    ///   running there shouldn't be any allocations.
    /// </summary>
    sealed class ComponentObjectPool<TComponentType>
        where TComponentType: AStatefulComponent, new()
    {
        private int NextAvailableIndex;
        private readonly TComponentType[] Pool;

        /// <summary>
        /// How many components are "active" and "allocated".
        /// </summary>
        public int Allocated => NextAvailableIndex;

        /// <summary>
        /// Create an object pool with the given capacity.
        /// </summary>
        public ComponentObjectPool(int capcity)
        {
            Pool = new TComponentType[capcity];
            for(var i = 0; i < Pool.Length; i++)
            {
                Pool[i] = new TComponentType();
            }
            NextAvailableIndex = 0;
        }

        /// <summary>
        /// Gets a component from the pool, if one is available.
        /// 
        /// Assigns the given id to the component, allowing us to
        ///   distinguish between previous "iterations" of the same
        ///   component.
        /// </summary>
        public Result<TComponentType> Get(int giveId)
        {
            if (giveId <= 0) throw new ArgumentException($"Ids must be positive, found: {giveId:N0}", nameof(giveId));
            if (NextAvailableIndex == Pool.Length) return Result.FailFor<TComponentType>();

            var ret = Pool[NextAvailableIndex];
            NextAvailableIndex++;

            ret.AssignId(giveId);

            return Result.From(ret);
        }

        /// <summary>
        /// Returns the given component back to the pool.
        /// </summary>
        public void Return(TComponentType component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));
            if (NextAvailableIndex == 0) throw new InvalidOperationException("Can't return object to a pool that is full");

            component.ClearId();

            NextAvailableIndex--;
            Pool[NextAvailableIndex] = component;
        }

        public override string ToString() => $"ObjectPoolOf: {typeof(TComponentType).Name}, {nameof(Allocated)}:{Allocated:N0}, Capacity:{Pool.Length:N0}";
    }
}
