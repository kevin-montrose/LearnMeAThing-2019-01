using System;

namespace LearnMeAThing.Utilities
{
    struct Buffer<T>
        where T: IEquatable<T>
    {
        private T[] Array;
        private int Used;

        public int Count => Used;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

                return Array[index];
            }
        }

        public bool IsFull => Used == Array.Length;

        public Buffer(int maximumPoints)
        {
            Used = 0;
            Array = new T[maximumPoints];
        }

        public void Add(T p)
        {
            if (IsFull) throw new InvalidOperationException("Insufficient space to store another item");

            Array[Used] = p;
            Used++;
        }

        public void Set(int index, T p)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));

            Array[index] = p;
        }

        public void CopyInto(T[] p, int take)
        {
            System.Array.Copy(p, 0, Array, Used, take);
            Used += take;
        }

        public void Sort()
        {
            System.Array.Sort(Array, 0, Used);
        }

        public void Clear()
        {
            Used = 0;
        }

        public void Reverse()
        {
            System.Array.Reverse(Array, 0, Used);
        }

        public T[] ToArray()
        {
            var ret = new T[Used];
            for(var i = 0; i < Used; i++)
            {
                ret[i] = Array[i];
            }

            return ret;
        }

        public bool Contains(T item)
        {
            var itemHash = item.GetHashCode();

            for(var i = 0; i < Used; i++)
            {
                var other = Array[i];
                var otherHash = other.GetHashCode();
                if (itemHash != otherHash) continue;

                if (item.Equals(other)) return true;
            }

            return false;
        }

        public override string ToString() => $"{nameof(Count)}={Count:N0}";
    }
}
