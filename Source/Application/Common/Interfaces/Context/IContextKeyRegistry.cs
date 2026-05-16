using System.Collections.Generic;
using RimMind.Domain.ValueObjects;

namespace RimMind.Application.Common.Interfaces.Context
{
    public interface IContextKeyRegistry
    {
        List<KeyMeta> GetAll();
    }
}
