using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

/// <summary>
/// Gateway Information Controller - Provides information about available routes
/// </summary>
[ApiController]
[Route("api/gateway")]
public class GatewayInfoController : ControllerBase
{
    /// <summary>
    /// Get information about available API routes through the gateway
    /// </summary>
    [HttpGet("routes")]
    public IActionResult GetRoutes()
    {
        var routes = new
        {
            description = "API Gateway Routes - All requests are proxied to microservices",
            routes = new[]
            {
                new { path = "/api/auth/{**catch-all}", service = "Identity Service", description = "Authentication endpoints" },
                new { path = "/api/subjects/{**catch-all}", service = "Core Service", description = "Subject management" },
                new { path = "/api/exams/{**catch-all}", service = "Core Service", description = "Exam management" },
                new { path = "/api/grades/{**catch-all}", service = "Core Service", description = "Grade management" },
                new { path = "/api/sessions/{**catch-all}", service = "Core Service", description = "Exam session management" },
                new { path = "/api/reports/{**catch-all}", service = "Core Service", description = "Reporting endpoints" },
                new { path = "/api/submissions/{**catch-all}", service = "Storage Service", description = "Submission upload and processing" },
                new { path = "/api/files/{**catch-all}", service = "Storage Service", description = "File access endpoints" },
                new { path = "/api/nestedzip/{**catch-all}", service = "Storage Service", description = "Nested ZIP processing" },
                new { path = "/health", service = "Gateway", description = "Health check endpoint" }
            },
            note = "To test individual microservices, access their Swagger UI directly. Gateway Swagger shows gateway-level documentation only."
        };

        return Ok(routes);
    }
}

