using System;
using RimMind.Domain.ValueObjects;
using Xunit;

namespace RimMind.Presentation.Tests.Result
{
    public class ResultTests
    {
        [Fact]
        public void Ok_Creates_IsOk_True()
        {
            var result = Result<int, RimMindError>.Ok(42);
            Assert.True(result.IsOk);
        }

        [Fact]
        public void Ok_Creates_IsErr_False()
        {
            var result = Result<int, RimMindError>.Ok(42);
            Assert.False(result.IsErr);
        }

        [Fact]
        public void Err_Creates_IsErr_True()
        {
            var error = new RimMindError(RimMindErrorCode.InternalError, "fail");
            var result = Result<int, RimMindError>.Err(error);
            Assert.True(result.IsErr);
        }

        [Fact]
        public void Err_Creates_IsOk_False()
        {
            var error = new RimMindError(RimMindErrorCode.InternalError, "fail");
            var result = Result<int, RimMindError>.Err(error);
            Assert.False(result.IsOk);
        }

        [Fact]
        public void Value_On_Ok_Returns_Value()
        {
            var result = Result<string, RimMindError>.Ok("hello");
            Assert.Equal("hello", result.Value);
        }

        [Fact]
        public void Value_On_Err_Throws_InvalidOperationException()
        {
            var error = new RimMindError(RimMindErrorCode.InternalError, "fail");
            var result = Result<int, RimMindError>.Err(error);
            Assert.Throws<InvalidOperationException>(() => result.Value);
        }

        [Fact]
        public void Error_On_Err_Returns_Error()
        {
            var error = new RimMindError(RimMindErrorCode.InternalError, "fail");
            var result = Result<int, RimMindError>.Err(error);
            Assert.Same(error, result.Error);
        }

        [Fact]
        public void Error_On_Ok_Throws_InvalidOperationException()
        {
            var result = Result<int, RimMindError>.Ok(1);
            Assert.Throws<InvalidOperationException>(() => result.Error);
        }

        [Fact]
        public void Match_On_Ok_Executes_OnOk()
        {
            var result = Result<int, RimMindError>.Ok(10);
            var matched = result.Match(v => v * 2, e => -1);
            Assert.Equal(20, matched);
        }

        [Fact]
        public void Match_On_Err_Executes_OnErr()
        {
            var error = new RimMindError(RimMindErrorCode.Cancelled, "cancelled");
            var result = Result<int, RimMindError>.Err(error);
            var matched = result.Match(v => v * 2, e => e.Code == RimMindErrorCode.Cancelled ? -1 : -2);
            Assert.Equal(-1, matched);
        }

        [Fact]
        public void Map_On_Ok_Transforms_Value()
        {
            var result = Result<int, RimMindError>.Ok(5);
            var mapped = result.Map(v => v.ToString());
            Assert.True(mapped.IsOk);
            Assert.Equal("5", mapped.Value);
        }

        [Fact]
        public void Map_On_Err_Propagates_Error()
        {
            var error = new RimMindError(RimMindErrorCode.Timeout, "timed out");
            var result = Result<int, RimMindError>.Err(error);
            var mapped = result.Map(v => v.ToString());
            Assert.True(mapped.IsErr);
            Assert.Same(error, mapped.Error);
        }

        [Fact]
        public void TryGetValue_On_Ok_Returns_True_And_Out_Value()
        {
            var result = Result<int, RimMindError>.Ok(99);
            Assert.True(result.TryGetValue(out var value));
            Assert.Equal(99, value);
        }

        [Fact]
        public void TryGetValue_On_Err_Returns_False()
        {
            var error = new RimMindError(RimMindErrorCode.InternalError, "fail");
            var result = Result<int, RimMindError>.Err(error);
            Assert.False(result.TryGetValue(out _));
        }

        [Fact]
        public void TryGetError_On_Err_Returns_True_And_Out_Error()
        {
            var error = new RimMindError(RimMindErrorCode.NpcNotFound, "missing");
            var result = Result<int, RimMindError>.Err(error);
            Assert.True(result.TryGetError(out var err));
            Assert.Same(error, err);
        }

        [Fact]
        public void TryGetError_On_Ok_Returns_False()
        {
            var result = Result<int, RimMindError>.Ok(1);
            Assert.False(result.TryGetError(out _));
        }
    }
}
