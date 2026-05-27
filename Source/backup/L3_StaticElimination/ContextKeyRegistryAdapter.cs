using System.Collections.Generic;
using RimMind.Application.Common.Interfaces.Context;
using RimMind.Domain.ValueObjects;

namespace RimMind.Presentation.Context
{
    public class ContextKeyRegistryAdapter : IContextKeyRegistry
    {
        public List<KeyMeta> GetAll() => ContextKeyRegistry.GetAll();
    }
}
