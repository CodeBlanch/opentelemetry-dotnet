// <copyright file="TestWebHostHelper.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

#if !NET6_0_OR_GREATER
using Microsoft.AspNetCore;
#endif

namespace TestApp.AspNetCore;

public sealed class TestWebHostHelper : IDisposable
{
#if !NET6_0_OR_GREATER
    private readonly IWebHost webHost;
#else
    private readonly WebApplication webApplication;
#endif

    public TestWebHostHelper(
        bool clearLoggingPipeline = true,
        Action<IServiceCollection>? configureServices = null,
        (string Template, RequestDelegate Handler)[]? routes = null,
        RequestDelegate? exceptionHandler = null)
    {
#if !NET6_0_OR_GREATER
        this.webHost = WebHost.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                if (clearLoggingPipeline)
                {
                    services.AddLogging(logging => logging.ClearProviders());
                }

                if (routes != null)
                {
                    services.AddMvc();
                }

                configureServices?.Invoke(services);
            })
            .Configure(app =>
            {
                if (exceptionHandler != null)
                {
                    app.UseExceptionHandler(
                        handler => handler.Run(exceptionHandler));
                }

                if (routes != null)
                {
                    app.UseMvc(routeBuilder =>
                    {
                        foreach (var (template, handler) in routes)
                        {
                            routeBuilder.MapRoute(template, handler);
                        }
                    });
                }
            })
            .Build();
#else
        var builder = WebApplication.CreateBuilder();

        if (clearLoggingPipeline)
        {
            builder.Logging.ClearProviders();
        }

        configureServices?.Invoke(builder.Services);

        this.webApplication = builder.Build();

        if (exceptionHandler != null)
        {
            this.webApplication.UseExceptionHandler(
                handler => handler.Run(exceptionHandler));
        }

        if (routes != null)
        {
            foreach (var (template, handler) in routes)
            {
                this.webApplication.Map(template, handler);
            }
        }
#endif
    }

    public void Run()
    {
#if !NET6_0_OR_GREATER
        this.webHost.Run();
#else
        this.webApplication.Run();
#endif
    }

    public Task RunAsync()
    {
#if !NET6_0_OR_GREATER
        return this.webHost.RunAsync();
#else
        return this.webApplication.RunAsync();
#endif
    }

    public void Dispose()
    {
#if !NET6_0_OR_GREATER
        this.webHost.Dispose();
#else
        ((IDisposable)this.webApplication).Dispose();
#endif
    }
}
