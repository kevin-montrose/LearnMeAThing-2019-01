using LearnMeAThing.Utilities;
using System;
using Xunit;

namespace LearnMeAThing.Tests
{
    public class IntrusiveLinkedListTests
    {
        class _Element : IIntrusiveLinkedListElement
        {
            public int? PreviousIndex { get; set; }
            public int? NextIndex { get; set; }

            public string Value { get; private set; }

            public _Element(string val)
            {
                Value = val;
            }

            public override string ToString() => $"{nameof(Value)}: {Value}, {nameof(PreviousIndex)}={PreviousIndex}, {nameof(NextIndex)}={NextIndex}";
        }

        [Fact]
        public void Fill()
        {
            var l = new IntrusiveLinkedList<_Element>(5);
            l.Add(new _Element("a"), 0);
            l.Add(new _Element("b"), 1);
            l.Add(new _Element("c"), 2);
            l.Add(new _Element("d"), 3);
            l.Add(new _Element("e"), 4);

            Assert.Throws<ArgumentOutOfRangeException>(() => l.Add(new _Element("f"), 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => l.Add(new _Element("z"), -1));
            Assert.Throws<InvalidOperationException>(() => l.Add(new _Element("bb"), 1));
        }

        [Fact]
        public void Add()
        {
            var l = new IntrusiveLinkedList<_Element>(5);
            Assert.Null(l.Head);
            Assert.Null(l.Tail);

            var a = new _Element("a");
            l.Add(a, 0);
            Assert.Equal(0, l.Head);
            Assert.Equal(0, l.Tail);
            Assert.Null(a.PreviousIndex);
            Assert.Null(a.NextIndex);

            var b = new _Element("b");
            l.Add(b, 1);
            Assert.Equal(0, l.Head);
            Assert.Equal(1, l.Tail);
            Assert.Null(a.PreviousIndex);
            Assert.Equal(1, a.NextIndex);
            Assert.Equal(0, b.PreviousIndex);
            Assert.Null(b.NextIndex);

            var c = new _Element("c");
            l.Add(c, 2);
            Assert.Equal(0, l.Head);
            Assert.Equal(2, l.Tail);
            Assert.Null(a.PreviousIndex);
            Assert.Equal(1, a.NextIndex);
            Assert.Equal(0, b.PreviousIndex);
            Assert.Equal(2, b.NextIndex);
            Assert.Equal(1, c.PreviousIndex);
            Assert.Null(c.NextIndex);

            var d = new _Element("d");
            l.Add(d, 3);
            Assert.Equal(0, l.Head);
            Assert.Equal(3, l.Tail);
            Assert.Null(a.PreviousIndex);
            Assert.Equal(1, a.NextIndex);
            Assert.Equal(0, b.PreviousIndex);
            Assert.Equal(2, b.NextIndex);
            Assert.Equal(1, c.PreviousIndex);
            Assert.Equal(3, c.NextIndex);
            Assert.Equal(2, d.PreviousIndex);
            Assert.Null(d.NextIndex);

            var e = new _Element("e");
            l.Add(e, 4);
            Assert.Equal(0, l.Head);
            Assert.Equal(4, l.Tail);
            Assert.Null(a.PreviousIndex);
            Assert.Equal(1, a.NextIndex);
            Assert.Equal(0, b.PreviousIndex);
            Assert.Equal(2, b.NextIndex);
            Assert.Equal(1, c.PreviousIndex);
            Assert.Equal(3, c.NextIndex);
            Assert.Equal(2, d.PreviousIndex);
            Assert.Equal(4, d.NextIndex);
            Assert.Equal(3, e.PreviousIndex);
            Assert.Null(e.NextIndex);
        }

        [Fact]
        public void Remove()
        {
            var l = new IntrusiveLinkedList<_Element>(5);
            var a = new _Element("a");
            var b = new _Element("b");
            var c = new _Element("c");
            var d = new _Element("d");
            var e = new _Element("e");

            l.Add(a, 0);
            l.Add(b, 1);
            l.Add(c, 2);
            l.Add(d, 3);
            l.Add(e, 4);

            // remove the head
            {
                l.Remove(0);

                // removed elements are null'd
                Assert.Null(l.Elements[0]);
                Assert.Null(a.NextIndex);
                Assert.Null(a.PreviousIndex);

                // head & tail correct
                Assert.Equal(1, l.Head);
                Assert.Equal(4, l.Tail);

                // linked list in correct order
                Assert.Null(b.PreviousIndex);
                Assert.Equal(2, b.NextIndex);
                Assert.Equal(1, c.PreviousIndex);
                Assert.Equal(3, c.NextIndex);
                Assert.Equal(2, d.PreviousIndex);
                Assert.Equal(4, d.NextIndex);
                Assert.Equal(3, e.PreviousIndex);
                Assert.Null(e.NextIndex);
            }

            // remove the tail
            {
                l.Remove(4);

                // removed elements are null'd
                Assert.Null(l.Elements[0]);
                Assert.Null(a.NextIndex);
                Assert.Null(a.PreviousIndex);
                Assert.Null(l.Elements[4]);
                Assert.Null(e.NextIndex);
                Assert.Null(e.PreviousIndex);

                // head & tail correct
                Assert.Equal(1, l.Head);
                Assert.Equal(3, l.Tail);

                // linked list in correct order
                Assert.Null(b.PreviousIndex);
                Assert.Equal(2, b.NextIndex);
                Assert.Equal(1, c.PreviousIndex);
                Assert.Equal(3, c.NextIndex);
                Assert.Equal(2, d.PreviousIndex);
                Assert.Null(d.NextIndex);
            }

            // remove in middle
            {
                l.Remove(2);

                // removed elements are null'd
                Assert.Null(l.Elements[0]);
                Assert.Null(a.NextIndex);
                Assert.Null(a.PreviousIndex);
                Assert.Null(l.Elements[2]);
                Assert.Null(c.NextIndex);
                Assert.Null(c.PreviousIndex);
                Assert.Null(l.Elements[4]);
                Assert.Null(e.NextIndex);
                Assert.Null(e.PreviousIndex);

                // head & tail correct
                Assert.Equal(1, l.Head);
                Assert.Equal(3, l.Tail);

                // linked list in correct order
                Assert.Null(b.PreviousIndex);
                Assert.Equal(3, b.NextIndex);
                Assert.Equal(1, d.PreviousIndex);
                Assert.Null(d.NextIndex);
            }

            // remove rest
            {
                l.Remove(1);

                // removed elements are null'd
                Assert.Null(l.Elements[0]);
                Assert.Null(a.NextIndex);
                Assert.Null(a.PreviousIndex);
                Assert.Null(l.Elements[1]);
                Assert.Null(b.NextIndex);
                Assert.Null(b.PreviousIndex);
                Assert.Null(l.Elements[2]);
                Assert.Null(c.NextIndex);
                Assert.Null(c.PreviousIndex);
                Assert.Null(l.Elements[4]);
                Assert.Null(e.NextIndex);
                Assert.Null(e.PreviousIndex);

                // head & tail correct
                Assert.Equal(3, l.Head);
                Assert.Equal(3, l.Tail);

                // linked list in correct order
                Assert.Null(d.PreviousIndex);
                Assert.Null(d.NextIndex);

                l.Remove(3);

                // removed elements are null'd
                Assert.Null(l.Elements[0]);
                Assert.Null(a.NextIndex);
                Assert.Null(a.PreviousIndex);
                Assert.Null(l.Elements[1]);
                Assert.Null(b.NextIndex);
                Assert.Null(b.PreviousIndex);
                Assert.Null(l.Elements[2]);
                Assert.Null(c.NextIndex);
                Assert.Null(c.PreviousIndex);
                Assert.Null(l.Elements[3]);
                Assert.Null(d.NextIndex);
                Assert.Null(d.PreviousIndex);
                Assert.Null(l.Elements[4]);
                Assert.Null(e.NextIndex);
                Assert.Null(e.PreviousIndex);

                // head & tail correct
                Assert.Null(l.Head);
                Assert.Null(l.Tail);
            }
        }

        [Fact]
        public void Compact()
        {
            // empty
            {
                var l = new IntrusiveLinkedList<_Element>(5);

                l.Compact(Array.Empty<int>());
                Assert.Null(l.Head);
                Assert.Null(l.Tail);
            }

            // no change
            {
                var l = new IntrusiveLinkedList<_Element>(5);
                var a = new _Element("a");
                var b = new _Element("b");
                var c = new _Element("c");
                var d = new _Element("d");
                var e = new _Element("e");
                l.Add(a, 0);
                l.Add(b, 1);
                l.Add(c, 2);
                l.Add(d, 3);
                l.Add(e, 4);

                var newMapping = new[] { 0, 1, 2, 3, 4 };
                l.Compact(newMapping);
                Assert.Equal(0, l.Head);
                Assert.Equal(4, l.Tail);

                Assert.Equal("a", l.Elements[0].Value);
                Assert.Null(a.PreviousIndex);
                Assert.Equal(1, a.NextIndex);

                Assert.Equal("b", l.Elements[1].Value);
                Assert.Equal(0, b.PreviousIndex);
                Assert.Equal(2, b.NextIndex);

                Assert.Equal("c", l.Elements[2].Value);
                Assert.Equal(1, c.PreviousIndex);
                Assert.Equal(3, c.NextIndex);

                Assert.Equal("d", l.Elements[3].Value);
                Assert.Equal(2, d.PreviousIndex);
                Assert.Equal(4, d.NextIndex);

                Assert.Equal("e", l.Elements[4].Value);
                Assert.Equal(3, e.PreviousIndex);
                Assert.Null(e.NextIndex);
            }

            // reverse
            {
                var l = new IntrusiveLinkedList<_Element>(5);
                var a = new _Element("a");
                var b = new _Element("b");
                var c = new _Element("c");
                var d = new _Element("d");
                var e = new _Element("e");
                l.Add(a, 0);
                l.Add(b, 1);
                l.Add(c, 2);
                l.Add(d, 3);
                l.Add(e, 4);

                var newMapping = new[] { 4, 3, 2, 1, 0 };
                l.Compact(newMapping);
                Assert.Equal(4, l.Head);
                Assert.Equal(0, l.Tail);

                Assert.Equal("e", l.Elements[0].Value);
                Assert.Equal(1, e.PreviousIndex);
                Assert.Null(e.NextIndex);

                Assert.Equal("d", l.Elements[1].Value);
                Assert.Equal(2, d.PreviousIndex);
                Assert.Equal(0, d.NextIndex);

                Assert.Equal("c", l.Elements[2].Value);
                Assert.Equal(3, c.PreviousIndex);
                Assert.Equal(1, c.NextIndex);

                Assert.Equal("b", l.Elements[3].Value);
                Assert.Equal(4, b.PreviousIndex);
                Assert.Equal(2, b.NextIndex);

                Assert.Equal("a", l.Elements[4].Value);
                Assert.Null(a.PreviousIndex);
                Assert.Equal(3, a.NextIndex);
            }

            // gaps
            {
                var l = new IntrusiveLinkedList<_Element>(10);
                var a = new _Element("a");
                var c = new _Element("c");
                var e = new _Element("e");

                l.Add(a, 1);
                l.Add(c, 9);
                l.Add(e, 3);

                var newMapping = new[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 2 };
                l.Compact(newMapping);

                Assert.Equal(0, l.Head);
                Assert.Equal(1, l.Tail);

                Assert.Equal("a", l.Elements[0].Value);
                Assert.Null(a.PreviousIndex);
                Assert.Equal(2, a.NextIndex);

                Assert.Equal("e", l.Elements[1].Value);
                Assert.Equal(2, e.PreviousIndex);
                Assert.Null(e.NextIndex);

                Assert.Equal("c", l.Elements[2].Value);
                Assert.Equal(0, c.PreviousIndex);
                Assert.Equal(1, c.NextIndex);
            }
        }
    }
}