using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Upm.Api.OpenApi;

/// <summary>
/// Chỉ giữ media type JSON trong tài liệu OpenAPI (bỏ text/plain, text/json, application/*+json)
/// để ng-openapi-gen sinh một hàm duy nhất cho mỗi endpoint thay vì các biến thể <c>$Plain</c>/<c>$Json</c>.
/// </summary>
internal sealed class JsonOnlyContentFilter : IOperationFilter
{
    private static readonly string[] RemovedMediaTypes = ["text/plain", "text/json", "application/*+json"];

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Responses is not null)
        {
            foreach (var response in operation.Responses.Values)
            {
                Strip(response.Content);
            }
        }

        Strip(operation.RequestBody?.Content);
    }

    private static void Strip(IDictionary<string, OpenApiMediaType>? content)
    {
        if (content is null)
        {
            return;
        }

        foreach (var mediaType in RemovedMediaTypes)
        {
            content.Remove(mediaType);
        }
    }
}
