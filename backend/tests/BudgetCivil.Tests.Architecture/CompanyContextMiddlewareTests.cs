using BudgetCivil.Api.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BudgetCivil.Tests.Architecture;

public sealed class CompanyContextMiddlewareTests
{
    [Fact]
    public async Task Un_endpoint_anonimo_no_exige_contexto_de_compania()
    {
        var nextCalled = false;
        var middleware = new CompanyContextMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()),
            "catalogs"));

        await middleware.InvokeAsync(context, null!, null!);

        Assert.True(nextCalled);
    }
}
