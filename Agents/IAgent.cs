using System.Threading.Tasks;
using SIFTGuardian.Models;

namespace SIFTGuardian.Agents;

public interface IAgent
{
    string Name { get; }
    Task<AgentResponse> ProcessAsync(InvestigationCase caseData, object? context = null);
}
