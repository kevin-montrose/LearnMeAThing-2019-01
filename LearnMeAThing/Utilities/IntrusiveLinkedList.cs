using System;

namespace LearnMeAThing.Utilities
{
    interface IIntrusiveLinkedListElement
    {
        int? PreviousIndex { get; set; }
        int? NextIndex { get; set; }
    }

    /// <summary>
    /// Represents an intrusively linked list backed by an array.
    /// 
    /// Supports O(1) lookup by id, and O(n) traversal
    /// </summary>
    sealed class IntrusiveLinkedList<T>
        where T : class, IIntrusiveLinkedListElement
    {
        public T[] Elements;
        private T[] CompactScratch;
        internal int? Head;
        internal int? Tail;

        public IntrusiveLinkedList(int capacity)
        {
            Elements = new T[capacity];
            CompactScratch = new T[capacity];
            Head = Tail = null;
        }

        /// <summary>
        /// Brings the list into sync with the provided new mapping.
        /// 
        /// newMapping[oldIndex] = newIndex
        /// </summary>
        public void Compact(int[] newMapping)
        {
            // nothing to do
            if (Head == null) return;
            
            var oldHead = Head.Value;
            var oldTail = Tail.Value;

            var oldElements = Elements;
            var newElements = CompactScratch;

            var earliestOldIx = int.MaxValue;
            var latestOldIx = -1;

            var cur = Head;
            while (cur != null)
            {
                var oldCurIndex = cur.Value;

                if(oldCurIndex < earliestOldIx)
                {
                    earliestOldIx = oldCurIndex;
                }

                if(oldCurIndex > latestOldIx)
                {
                    latestOldIx = oldCurIndex;
                }

                var curElem = oldElements[oldCurIndex];
                var oldPrevIndex = curElem.PreviousIndex;
                var oldNextIndex = curElem.NextIndex;

                var newCurIndex = newMapping[oldCurIndex];
                var newPrevIndex = oldPrevIndex != null ? newMapping[oldPrevIndex.Value] : default(int?);
                var newNextIndex = oldNextIndex != null ? newMapping[oldNextIndex.Value] : default(int?);
                
                newElements[newCurIndex] = curElem;

                if (newPrevIndex != null)
                {
                    curElem.PreviousIndex = newPrevIndex;
                }

                if (newNextIndex != null)
                {
                    curElem.NextIndex = newNextIndex;
                }

                // advance to the next element
                cur = oldNextIndex;
            }

            // swap to new elements
            Elements = newElements;
            CompactScratch = oldElements;

            // clear the scratch space, so we can use it next time
            //    and don't have to worry about it referencing anything
            //    while we're not using it
            var oldInUseLen = latestOldIx - earliestOldIx + 1;
            Array.Clear(CompactScratch, earliestOldIx, oldInUseLen);


            // now update Head and Tail
            //   we don't do this in the above loop because we might
            //   accidentally collide (new Head or Tail could equal an index in
            //   use in the old list, so we'd spuriously swap it)
            Head = newMapping[Head.Value];
            Tail = newMapping[Tail.Value];
        }

        public void Add(T item, int atIndex)
        {
            if (atIndex < 0 || atIndex >= Elements.Length) throw new ArgumentOutOfRangeException(nameof(atIndex));

            var oldItem = Elements[atIndex];
            if (oldItem != null) throw new InvalidOperationException($"Element already present at {atIndex}: {oldItem}");

            Elements[atIndex] = item;

            if (Head == null)
            {
                Head = Tail = atIndex;
            }
            else
            {
                var oldTail = Elements[Tail.Value];
                oldTail.NextIndex = atIndex;
                item.PreviousIndex = Tail.Value;

                Tail = atIndex;
            }
        }
        
        public T Remove(int atIndex)
        {
            if (atIndex < 0 || atIndex >= Elements.Length) throw new ArgumentOutOfRangeException(nameof(atIndex));

            var elem = Elements[atIndex];
            if (elem == null) return null;

            var elemPrevPtr = elem.PreviousIndex;
            var elemNextPtr = elem.NextIndex;

            Elements[atIndex] = null;
            elem.PreviousIndex = elem.NextIndex = null;

            if (atIndex == Head)
            {
                // removing from the front of the list, update the head pointer
                Head = elemNextPtr;
            }

            if (atIndex == Tail)
            {
                // removing from the end of the list, update the tail pointer
                Tail = elemPrevPtr;
            }

            // fetch the nodes that were before and after elem
            var beforeElem = elemPrevPtr != null ? Elements[elemPrevPtr.Value] : null;
            var afterElem = elemNextPtr != null ? Elements[elemNextPtr.Value] : null;

            if (beforeElem != null)
            {
                // point the node before elem to the node after elem
                beforeElem.NextIndex = elemNextPtr;
            }

            if (afterElem != null)
            {
                // point the node after elem to the node before elem
                afterElem.PreviousIndex = elemPrevPtr;
            }

            return elem;
        }
    }
}