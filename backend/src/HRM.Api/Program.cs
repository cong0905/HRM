using System.Diagnostics;
using HRM.Next.Api.Errors;
using HRM.Next.Api.Health;
using HRM.Next.Api.Security;
using HRM.Next.Api.Serialization;
using HRM.Next.Api.Auth;
using HRM.Next.Api.Workers;
using HRM.Next.Infrastructure;
using HRM.Next.Infrastructure.Notifications;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

namespace HRM.Next.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; });
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddSingleton<EmailOutboxWorker>();
        builder.Services.AddHostedService(sp => sp.GetRequiredService<EmailOutboxWorker>());
        builder.Services.AddControllers().AddJsonOptions(options => ContractJson.Configure(options.JsonSerializerOptions));
        builder.Services.ConfigureHttpJsonOptions(options => ContractJson.Configure(options.SerializerOptions));
        builder.Services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Keys.ToDictionary(key => key, _ => new[] { "Giá trị không hợp lệ." });
                var problem = new ValidationProblemDetails(errors) { Status = 400, Title = "Dữ liệu không hợp lệ.", Type = "about:blank" };
                problem.Extensions["errorCode"] = "validation.failed";
                problem.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                var result = new BadRequestObjectResult(problem);
                result.ContentTypes.Add("application/problem+json");
                return result;
            };
        });
        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd("errorCode", $"http.{context.ProblemDetails.Status}");
        });
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddHrmIdentity(builder.Configuration);
        builder.Services.AddHrmAuthorization();
        builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("sql", tags: ["ready"]);
        builder.Services.AddOpenApi("v1", options =>
        {
            options.AddSchemaTransformer((schema, context, _) =>
            {
                var underlying = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type);
                var scalar = underlying ?? context.JsonTypeInfo.Type;
                if (scalar == typeof(decimal))
                {
                    schema.Type = JsonSchemaType.String | (underlying is null ? 0 : JsonSchemaType.Null);
                    schema.Format = "decimal";
                    schema.Pattern = ContractJson.DecimalPattern;
                }
                if (scalar == typeof(DateTimeOffset))
                {
                    schema.Type = JsonSchemaType.String | (underlying is null ? 0 : JsonSchemaType.Null);
                    schema.Format = "date-time";
                }
                if (context.JsonTypeInfo.Type == typeof(HRM.Next.Application.Personnel.UpdateEmployeeRequest))
                {
                    // The controller checks scope before its required-version validation. Describe the
                    // actual write contract without moving validation ahead of the resource guard.
                    schema.Required ??= new HashSet<string>();
                    schema.Required.Add("rowVersion");
                    schema.Properties!["rowVersion"] = new OpenApiSchema { Type = JsonSchemaType.String, Description = "Required Base64 SQL rowversion." };
                    foreach (var field in new[] { "phone", "address", "fullName", "dateOfBirth", "hireDate" })
                    {
                        if (schema.Properties[field] is OpenApiSchema property) property.Type = JsonSchemaType.String;
                    }
                }
                if (typeof(ProblemDetails).IsAssignableFrom(context.JsonTypeInfo.Type))
                {
                    schema.Properties ??= new Dictionary<string, IOpenApiSchema>();
                    schema.Properties["errorCode"] = new OpenApiSchema { Type = JsonSchemaType.String };
                    schema.Properties["traceId"] = new OpenApiSchema { Type = JsonSchemaType.String };
                    schema.Required ??= new HashSet<string>();
                    schema.Required.Add("errorCode");
                    schema.Required.Add("traceId");
                }
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "HRM Next Foundation API";
                document.Info.Version = "v1";
                document.Servers = [new OpenApiServer { Url = "/" }];
                document.Components ??= new OpenApiComponents();
                document.Components.Schemas ??= new Dictionary<string, IOpenApiSchema>();
                document.Components.Schemas["DecimalString"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "decimal", Pattern = ContractJson.DecimalPattern };
                document.Components.Schemas["LocalDate"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date" };
                document.Components.Schemas["UtcTimestamp"] = new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" };
                return Task.CompletedTask;
            });
        });

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages(async context =>
        {
            var http = context.HttpContext;
            await Results.Problem(ApiProblems.Create(http, http.Response.StatusCode, $"http.{http.Response.StatusCode}")).ExecuteAsync(http);
        });
        app.UseAuthentication();
        app.UseMiddleware<CookieCsrfMiddleware>();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.MapControllers();
        app.MapGet("/health/live", () => new HealthResponse("Healthy")).AllowAnonymous().WithName("Liveness");
        app.MapGet("/health/ready", async (HealthCheckService health, CancellationToken token) =>
        {
            var report = await health.CheckHealthAsync(check => check.Tags.Contains("ready"), token);
            return Results.Json(new HealthResponse(report.Status.ToString()), statusCode: report.Status == HealthStatus.Healthy ? 200 : 503);
        }).AllowAnonymous().WithName("Readiness").Produces<HealthResponse>(200).Produces<HealthResponse>(503);
        if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Api:ExposeOpenApi"))
            app.MapOpenApi().AllowAnonymous();
        // No automatic migration, database creation or development seed on startup.
        app.Run();
    }
}
