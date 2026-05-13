using System;
using System.Threading.Tasks;
using RimMind.Domain.Events.Result;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    public class TraceContextTests
    {
        [Fact]
        public void BeginScope_Sets_Current()
        {
            using (TraceContext.BeginScope("scope-1"))
            {
                Assert.Equal("scope-1", TraceContext.Current);
            }
        }

        [Fact]
        public void BeginScope_Dispose_Restores_Previous()
        {
            Assert.Null(TraceContext.Current);
            using (TraceContext.BeginScope("outer"))
            {
                Assert.Equal("outer", TraceContext.Current);
                using (TraceContext.BeginScope("inner"))
                {
                    Assert.Equal("inner", TraceContext.Current);
                }
                Assert.Equal("outer", TraceContext.Current);
            }
            Assert.Null(TraceContext.Current);
        }

        [Fact]
        public void Nested_BeginScope_Push_Pop_Correctly()
        {
            Assert.Null(TraceContext.Current);
            using (TraceContext.BeginScope("a"))
            {
                Assert.Equal("a", TraceContext.Current);
                using (TraceContext.BeginScope("b"))
                {
                    Assert.Equal("b", TraceContext.Current);
                    using (TraceContext.BeginScope("c"))
                    {
                        Assert.Equal("c", TraceContext.Current);
                    }
                    Assert.Equal("b", TraceContext.Current);
                }
                Assert.Equal("a", TraceContext.Current);
            }
            Assert.Null(TraceContext.Current);
        }

        [Fact]
        public void Current_Is_Null_By_Default()
        {
            Assert.Null(TraceContext.Current);
        }

        [Fact]
        public async Task AsyncLocal_Isolation_Between_Tasks()
        {
            using (TraceContext.BeginScope("parent"))
            {
                var t1 = Task.Run(async () =>
                {
                    using (TraceContext.BeginScope("task-1"))
                    {
                        await Task.Yield();
                        return TraceContext.Current;
                    }
                });

                var t2 = Task.Run(async () =>
                {
                    using (TraceContext.BeginScope("task-2"))
                    {
                        await Task.Yield();
                        return TraceContext.Current;
                    }
                });

                var r1 = await t1;
                var r2 = await t2;

                Assert.Equal("task-1", r1);
                Assert.Equal("task-2", r2);
                Assert.Equal("parent", TraceContext.Current);
            }
        }

        [Fact]
        public async Task Child_Task_Inherits_Parent_TraceId()
        {
            using (TraceContext.BeginScope("inherited"))
            {
                var result = await Task.Run(() => TraceContext.Current);
                Assert.Equal("inherited", result);
            }
        }

        [Fact]
        public void Dispose_Is_Idempotent()
        {
            var scope = TraceContext.BeginScope("idem");
            scope.Dispose();
            scope.Dispose();
            Assert.Null(TraceContext.Current);
        }
    }
}
