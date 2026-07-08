using System;
using System.Linq;
using RimMind.Application.Common.Interfaces.Extension;
using Xunit;

namespace RimMind.Tests.ArchTests.PhaseP6
{
    public class P6_BridgeModuleContractTests
    {
        [Fact]
        public void IBridgeModule_InheritsIExtension()
        {
            Assert.True(typeof(IExtension).IsAssignableFrom(typeof(IBridgeModule)));
        }

        [Fact]
        public void IBridgeModule_DeclaresIsRegisteredProperty()
        {
            var prop = typeof(IBridgeModule).GetProperty("IsRegistered");
            Assert.NotNull(prop);
            Assert.Equal(typeof(bool), prop!.PropertyType);
            Assert.True(prop!.GetIndexParameters().Length == 0, "IsRegistered must be parameterless.");
        }

        [Fact]
        public void IBridgeModule_DeclaresRegisterAndUnregisterMethods()
        {
            var register = typeof(IBridgeModule).GetMethod("Register");
            var unregister = typeof(IBridgeModule).GetMethod("Unregister");
            Assert.NotNull(register);
            Assert.NotNull(unregister);
            Assert.Empty(register!.GetParameters());
            Assert.Empty(unregister!.GetParameters());
            Assert.Equal(typeof(void), register!.ReturnType);
            Assert.Equal(typeof(void), unregister!.ReturnType);
        }
    }
}
