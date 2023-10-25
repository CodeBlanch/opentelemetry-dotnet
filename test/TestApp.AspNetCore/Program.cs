// <copyright file="Program.cs" company="OpenTelemetry Authors">
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
using System.Net.Http;
using Microsoft.AspNetCore;
#endif

using TestApp.AspNetCore;

public class Program
{
#if !NET6_0_OR_GREATER
    public static void Main(string[] args)
    {
        CreateWebHostBuilder(args).Build().Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        => WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>();
#else
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services);

        var app = builder.Build();

        Configure(app);

        app.Run();
    }
#endif

    private static void ConfigureServices(IServiceCollection services)
    {
#if NET6_0_OR_GREATER
        services.AddControllers();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
#endif

        services.AddSwaggerGen();

        services.AddMvc();

        services.AddSingleton<HttpClient>();

        services.AddSingleton(
            new CallbackMiddleware.CallbackMiddlewareImpl());

        services.AddSingleton(
            new ActivityMiddleware.ActivityMiddlewareImpl());
    }

    private static void Configure(
#if !NET6_0_OR_GREATER
        IApplicationBuilder app)
#else
        WebApplication app)
#endif
    {
#if !NET6_0_OR_GREATER
        var environment = app.ApplicationServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
#else
        var environment = app.Environment;
#endif

        // Configure the HTTP request pipeline.
        if (environment.IsDevelopment())
        {
#if !NET6_0_OR_GREATER
            app.UseDeveloperExceptionPage();
#endif
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

#if NET6_0_OR_GREATER
        app.MapControllers();
#endif

        app.UseMiddleware<CallbackMiddleware>();

        app.UseMiddleware<ActivityMiddleware>();

#if !NET6_0_OR_GREATER
        app.UseMvc();
#endif
    }

#if !NET6_0_OR_GREATER
    private sealed class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            Program.ConfigureServices(services);
        }

        public void Configure(IApplicationBuilder app)
        {
            Program.Configure(app);
        }
    }
#endif
}
