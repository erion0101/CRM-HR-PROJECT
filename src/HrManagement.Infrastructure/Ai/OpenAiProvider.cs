using HrManagement.Application.Features.Ai;
using System.Text.Json;
using System.ClientModel.Primitives;
using OpenAI.Embeddings;
using OpenAI.Responses;

namespace HrManagement.Infrastructure.Ai;

#pragma warning disable OPENAI001
internal sealed class OpenAiProvider(OpenAiCredentialResolver credentials, IAiRuntimeSettings settings) : IAiProvider, IEmbeddingProvider
{
    private const string AnswerSchema = """
        {"type":"object","additionalProperties":false,"properties":{"message":{"type":"string"},"sourceIds":{"type":"array","items":{"type":"string"}}},"required":["message","sourceIds"]}
        """;

    public async Task<AiProviderResponse> CompleteAsync(AiProviderRequest request, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout ?? TimeSpan.FromSeconds(settings.Value.RequestTimeoutSeconds));
        var key = credentials.GetApiKey() ?? throw new InvalidOperationException("OpenAI is not configured.");
        var options = new CreateResponseOptions
        {
            Model = settings.Value.ChatModel,
            StoredOutputEnabled = false,
            MaxOutputTokenCount = request.MaxOutputTokens,
            ParallelToolCallsEnabled = false,
            ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ParseEffort(settings.Value.ReasoningEffort) },
            TextOptions = new ResponseTextOptions
            {
                TextFormat = ResponseTextFormat.CreateJsonSchemaFormat("hr_assistant_answer", BinaryData.FromString(AnswerSchema), null, true)
            }
        };
        options.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
        options.InputItems.Add(ResponseItem.CreateDeveloperMessageItem(request.SystemPrompt));
        foreach (var message in request.Messages)
        {
            options.InputItems.Add(message.Role == "assistant"
                ? ResponseItem.CreateAssistantMessageItem(message.Content)
                : ResponseItem.CreateUserMessageItem(message.Content));
        }
        if (request.ProviderTurns is { Count: > 0 })
        {
            foreach (var turn in request.ProviderTurns)
            {
                foreach (var item in turn.OpaqueOutputItems)
                {
                    var responseItem = ModelReaderWriter.Read<ResponseItem>(BinaryData.FromString(item), ModelReaderWriterOptions.Json)
                        ?? throw new InvalidOperationException("The provider continuation item could not be restored.");
                    options.InputItems.Add(responseItem);
                }
                foreach (var exchange in turn.ToolExchanges)
                    options.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(exchange.Call.CallId, exchange.OutputJson));
            }
        }
        else
        {
            foreach (var exchange in request.Exchanges)
            {
                options.InputItems.Add(ResponseItem.CreateFunctionCallItem(exchange.Call.CallId, exchange.Call.Name, BinaryData.FromString(exchange.Call.ArgumentsJson)));
                options.InputItems.Add(ResponseItem.CreateFunctionCallOutputItem(exchange.Call.CallId, exchange.OutputJson));
            }
        }
        foreach (var tool in request.Tools)
        {
            options.Tools.Add(ResponseTool.CreateFunctionTool(tool.Name, BinaryData.FromString(tool.JsonSchema), true, tool.Description));
        }

        var client = new ResponsesClient(key);
        var result = (await client.CreateResponseAsync(options, timeout.Token)).Value;
        var calls = result.OutputItems.OfType<FunctionCallResponseItem>()
            .Select(x => new AiToolCall(x.CallId, x.FunctionName, x.FunctionArguments.ToString())).ToArray();
        var (text, sourceIds) = ParseAnswer(result.GetOutputText(), calls.Length > 0);
        var continuation = result.OutputItems
            .Select(item => ModelReaderWriter.Write(item, ModelReaderWriterOptions.Json).ToString())
            .ToArray();
        return new AiProviderResponse(text, sourceIds, calls, result.Usage?.InputTokenCount ?? 0,
            result.Usage?.OutputTokenCount ?? 0, result.Model, continuation);
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken)
    {
        var response = await CompleteAsync(new AiProviderRequest(
            "Set message to OK and sourceIds to an empty array.", [new AiProviderMessage("user", "Connection test")], [], [], 64), cancellationToken);
        return !string.IsNullOrWhiteSpace(response.Text);
    }

    public async Task<IReadOnlyList<float[]>> CreateEmbeddingsAsync(IReadOnlyList<string> inputs, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.Value.RequestTimeoutSeconds));
        var key = credentials.GetApiKey() ?? throw new InvalidOperationException("OpenAI is not configured.");
        var client = new EmbeddingClient(settings.Value.EmbeddingModel, key);
        var result = new List<float[]>(inputs.Count);
        foreach (var input in inputs)
        {
            var embedding = await client.GenerateEmbeddingAsync(input, new EmbeddingGenerationOptions { Dimensions = settings.Value.EmbeddingDimensions }, timeout.Token);
            result.Add(embedding.Value.ToFloats().ToArray());
        }
        return result;
    }

    private static ResponseReasoningEffortLevel ParseEffort(string value) => value.ToLowerInvariant() switch
    {
        "none" => ResponseReasoningEffortLevel.None,
        "minimal" => ResponseReasoningEffortLevel.Minimal,
        "medium" => ResponseReasoningEffortLevel.Medium,
        "high" => ResponseReasoningEffortLevel.High,
        _ => ResponseReasoningEffortLevel.Low
    };

    private static (string Text, IReadOnlyList<string> SourceIds) ParseAnswer(string raw, bool hasToolCalls)
    {
        if (hasToolCalls || string.IsNullOrWhiteSpace(raw)) return (raw, []);
        using var document = JsonDocument.Parse(raw);
        var root = document.RootElement;
        var message = root.GetProperty("message").GetString() ?? string.Empty;
        var ids = root.GetProperty("sourceIds").EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToArray();
        return (message, ids);
    }
}
#pragma warning restore OPENAI001
