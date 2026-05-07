using System.Threading.Tasks;

namespace RimMind.Contracts.Pipeline;

public interface IPipeline<TContext> where TContext : IPipelineContext
{
    Task ExecuteAsync(TContext context);
}
