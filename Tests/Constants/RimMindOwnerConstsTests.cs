using RimMind.Application.Common.Constants;
using Xunit;

namespace RimMind.Core.Tests.Constants
{
    /// <summary>
    /// RimMindOwnerConsts 常量值验证。
    /// 确保常量值与历史字面量 "RimMindCore" 一致，避免 UnregisterByOwner 查询失效。
    /// </summary>
    public class RimMindOwnerConstsTests
    {
        [Fact]
        public void CoreModId_Is_RimMindCore()
        {
            // 常量值必须与历史字面量一致，否则会破坏已序列化的存档数据
            // 和潜在的 UnregisterByOwner("RimMindCore") 查询。
            Assert.Equal("RimMindCore", RimMindOwnerConsts.CoreModId);
        }

        [Fact]
        public void CoreModId_Is_Const_String()
        {
            // 确保是 const（编译期常量），非 static readonly
            // const 在调用方内联，无运行时查找开销
            Assert.True(typeof(RimMindOwnerConsts)
                .GetField(nameof(RimMindOwnerConsts.CoreModId))?
                .IsLiteral == true);
        }
    }
}
