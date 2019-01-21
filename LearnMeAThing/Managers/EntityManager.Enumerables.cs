using LearnMeAThing.Components;
using LearnMeAThing.Entities;
using LearnMeAThing.Utilities;
using System;

namespace LearnMeAThing.Managers
{
    sealed partial class EntityManager
    {
        public struct EntitiesWithFlagComponentEnumerator: IDisposable
        {
            public Entity Current { get; private set; }

            private int Index;
            private int UsedComponents;
            private FlagComponents[] Flags;
            private FlagComponent Flag;

            public EntitiesWithFlagComponentEnumerator(
                FlagComponents[] flags, 
                FlagComponent flag,
                int usedComponents
            )
            {
                Current = default;
                Index = 1; // 0 is a magic number, never used so don't start there
                UsedComponents = usedComponents;
                Flags = flags;
                Flag = flag;
            }

            public bool MoveNext()
            {
                while (Index < UsedComponents)
                {
                    var cur = Index;
                    var e = Flags[cur];
                    Index++;

                    if (e.HasFlag(Flag))
                    {
                        Current = new Entity(cur);
                        return true;
                    }
                }

                return false;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Enumerable for all entities with a particular flag (or set of flags) set.
        /// </summary>
        public readonly struct EntitiesWithFlagComponentEnumerable
        {
            private readonly FlagComponents[] Flags;
            private readonly FlagComponent Flag;
            private readonly int UsedComponents;
            public EntitiesWithFlagComponentEnumerable(
                FlagComponents[] flags, 
                FlagComponent flag,
                int usedComponents
            )
            {
                Flags = flags;
                Flag = flag;
                UsedComponents = usedComponents;
            }

            public EntitiesWithFlagComponentEnumerator GetEnumerator() => new EntitiesWithFlagComponentEnumerator(Flags, Flag, UsedComponents);
        }

        public struct EntitiesWithStatefulComponentEnumerator<TComponentType> : IDisposable
            where TComponentType: AStatefulComponent

        {
            private (Entity Entity, TComponentType Component) _Current;
            public (Entity Entity, TComponentType Component) Current => _Current;

            private int? CurrentNode;
            private IntrusiveLinkedList<TComponentType> List;

            public EntitiesWithStatefulComponentEnumerator(
                IntrusiveLinkedList<TComponentType> list
            )
            {
                _Current = default;
                List = list;
                CurrentNode = List.Head;
            }

            public bool MoveNext()
            {
                if (CurrentNode == null) return false;

                var curIx = CurrentNode.Value;
                var elem = List.Elements[curIx];
                var nextIx = elem.NextIndex;

                _Current = (new Entity(curIx), elem);

                CurrentNode = nextIx;
                return true;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Enumerable for all entities with a stateful component.
        /// </summary>
        public readonly struct EntitiesWithStatefulComponentEnumerable<TComponentType>
            where TComponentType: AStatefulComponent
        {
            private readonly IntrusiveLinkedList<TComponentType> List;
            public EntitiesWithStatefulComponentEnumerable(
                IntrusiveLinkedList<TComponentType> list
            )
            {
                List = list;
            }

            public EntitiesWithStatefulComponentEnumerator<TComponentType> GetEnumerator() => new EntitiesWithStatefulComponentEnumerator<TComponentType>(List);
        }

        public struct AllEntitiesEnumerator: IDisposable
        {
            private Entity _Current;
            public Entity Current => _Current;

            private readonly Entity[] List;
            private readonly int Limit;
            private int Index;

            public AllEntitiesEnumerator(Entity[] list, int limit)
            {
                _Current = default;
                List = list;
                Limit = limit;
                Index = 1;  //  skip 0, because it's special
            }

            public bool MoveNext()
            {
                while(Index < Limit)
                {
                    var i = Index;
                    Index++;
                    var e = List[i];
                    if(e.Id != 0)
                    {
                        _Current = e;
                        return true;
                    }
                }

                return false;
            }

            public void Dispose() { }
        }

        /// <summary>
        /// Enumerable for all live entities in the system.
        /// </summary>
        public readonly struct AllEntitiesEnumerable
        {
            private readonly Entity[] List;
            private readonly int Limit;

            public AllEntitiesEnumerable(Entity[] list, int limit)
            {
                List = list;
                Limit = limit;
            }

            public AllEntitiesEnumerator GetEnumerator() => new AllEntitiesEnumerator(List, Limit);
        }
    }
}
