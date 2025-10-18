using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Dtos.Responses;

public class ValidationErrorResponse
{

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("details")]
    public List<ValidationError>? Details { get; set; }


    public static string BuildErrorResponse(string? message = null, params ValidationError[] errors)
    {
        var response = new ValidationErrorResponse
        {
            Message = message ?? "Unexpected error occurred.",
            Details = IsNullOrEmpty(errors) ? null : errors.ToList()
        };
        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
    private static bool IsNullOrEmpty<T>(T[] array) => array == null || array.Length == 0;
    
    public class ValidationError
    {
        [JsonPropertyName("field")]
        public string? Field { get; set; }

        [JsonPropertyName("issues")]
        public List<string>? Issues { get; set; }
    }
}