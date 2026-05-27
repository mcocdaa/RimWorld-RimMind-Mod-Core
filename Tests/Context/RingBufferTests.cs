using System.Linq;
using RimMind.Application.Features.Utility;
using Xunit;

namespace RimMind.Tests.Context
{
    public class RingBufferTests
    {
        [Fact]
        public void Add_IncrementsCount()
        {
            var buf = new RingBuffer<int>(5);
            Assert.Equal(0, buf.Count);

            buf.Add(1);
            Assert.Equal(1, buf.Count);

            buf.Add(2);
            Assert.Equal(2, buf.Count);
        }

        [Fact]
        public void AsEnumerable_ReturnsItemsInOrder()
        {
            var buf = new RingBuffer<int>(5);
            buf.Add(10);
            buf.Add(20);
            buf.Add(30);

            var items = buf.AsEnumerable().ToList();
            Assert.Equal(new[] { 10, 20, 30 }, items);
        }

        [Fact]
        public void OverflowWraps_OldestOverwritten()
        {
            var buf = new RingBuffer<int>(3);
            buf.Add(1);
            buf.Add(2);
            buf.Add(3);
            // Buffer is full: [1, 2, 3], _idx = 0
            buf.Add(4);
            // Buffer: [4, 2, 3], _idx = 1
            buf.Add(5);
            // Buffer: [4, 5, 3], _idx = 2

            Assert.Equal(3, buf.Count);
            var items = buf.AsEnumerable().ToList();
            Assert.Equal(new[] { 3, 4, 5 }, items);
        }

        [Fact]
        public void Count_CappedAtSize()
        {
            var buf = new RingBuffer<int>(2);
            buf.Add(1);
            buf.Add(2);
            buf.Add(3);
            buf.Add(4);
            buf.Add(5);

            Assert.Equal(2, buf.Count);
        }

        [Fact]
        public void AsEnumerable_EmptyBuffer_ReturnsEmpty()
        {
            var buf = new RingBuffer<int>(5);
            Assert.Empty(buf.AsEnumerable());
        }

        [Fact]
        public void SingleItemBuffer_WrapsCorrectly()
        {
            var buf = new RingBuffer<int>(1);
            buf.Add(42);
            Assert.Equal(1, buf.Count);
            Assert.Equal(new[] { 42 }, buf.AsEnumerable().ToArray());

            buf.Add(99);
            Assert.Equal(1, buf.Count);
            Assert.Equal(new[] { 99 }, buf.AsEnumerable().ToArray());
        }
    }
}
