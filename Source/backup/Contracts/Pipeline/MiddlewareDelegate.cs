using System.Threading.Tasks;

namespace RimMind.Contracts.Pipeline;

public delegate Task MiddlewareDelegate<TContext>(TContext context) where TContext : IPipelineContext;
