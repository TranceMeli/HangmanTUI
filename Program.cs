
using HangmanTwo.Game;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HangmanSpectre.UI;
using HangmanSpectre.Services;

namespace HangmanTwo;

internal static class Program
{
    private static async Task Main()
    {
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, builder) =>
            {
                builder.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Register services and UI
                services.AddSingleton<HangmanUI>();
                services.AddHttpClient();
                services.AddSingleton<GeminiWordService>();
                services.AddSingleton<HangmanGame>();
            })
            .Build();

        using var scope = host.Services.CreateScope();
        var game = scope.ServiceProvider.GetRequiredService<HangmanGame>();
        await game.RunAsync();
    }
}
