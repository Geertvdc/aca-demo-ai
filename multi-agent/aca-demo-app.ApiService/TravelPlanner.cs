#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0110

using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;

public sealed class TravelPlanner
{
    private const string TripPlannerPrompt = @"
    <role:system>
    You are *Trip Planner*, an expert travel concierge.
    You can call three tools:

    • FlightAgent.run
    • HotelAgent.run
    • CostAgent.run

    Return a friendly Markdown itinerary ≤ total costs that you get from the tools in the target currency
    </role:system>

    <role:assistant>
    [Think about the request. Decide which tool to call first. Use the
    automatic function-calling format so the kernel routes it.]
    </role:assistant>";

    private readonly Kernel _kernel;
    private readonly KernelFunction _tripPlannerFn;

    public TravelPlanner(Kernel kernel, IEnumerable<AzureAIAgent> agents)
    {
        // build the TripPlanner prompt function
        _tripPlannerFn = KernelFunctionFactory.CreateFromPrompt(
            promptTemplate : TripPlannerPrompt,
            functionName   : "TripPlanner"); 

        // bundle prompt + tools into one plugin
        var functions = new List<KernelFunction> { _tripPlannerFn };
        functions.AddRange(
            agents.Select(a => AgentKernelFunctionFactory.CreateFromAgent(a)));

        var plugin = KernelPluginFactory.CreateFromFunctions("TravelAgents", functions);
        kernel.Plugins.Add(plugin);

        foreach (var f in kernel.Plugins.SelectMany(p => p.GetFunctionsMetadata()))
        Console.WriteLine($"{f.PluginName}.{f.Name}");

        _kernel = kernel;
    }

    public async Task<string> ExecuteAsync(ChatCompletionRequest req)
    {
        var args = new KernelArguments { ["history"] = JsonSerializer.Serialize(req.Messages) };
        var result = await _kernel.InvokeAsync(_tripPlannerFn, args);
        return result.GetValue<string>();
    }
}
