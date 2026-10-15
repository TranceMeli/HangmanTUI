using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HangmanSpectre.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Diagnostics;

namespace HangmanSpectre.Services;

public sealed class GeminiWordService
{
    private readonly string _apiKey;

    public GeminiWordService(IConfiguration configuration)
    {
        // Priority: configuration (appsettings / environment variables), then local file 'gemini.key'.
        var keyFromConfig = configuration["Gemini:ApiKey"];

        const string keyFile = "gemini.key";

        _apiKey = !string.IsNullOrWhiteSpace(keyFromConfig)
            ? keyFromConfig
            : Environment.GetEnvironmentVariable("Gemini__ApiKey");

        if (string.IsNullOrWhiteSpace(_apiKey) && File.Exists(keyFile))
        {
            _apiKey = File.ReadAllText(keyFile).Trim();
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key is missing. Place the key in 'HangmanSpectre/appsettings.json' (Gemini:ApiKey), in 'HangmanSpectre/gemini.key', or set the environment variable 'Gemini__ApiKey'.");
        }
    }
    public async Task<string> GetWordAsync(GameDifficulty difficulty)
    {
        await Task.Delay(300);
        return GetFallbackWord(difficulty);
    }
    public string GetFallbackWord(GameDifficulty difficulty)
    {
        var easy = new[] { "apple", "banana", "orange", "grape", "lemon" };
        var medium = new[] { "hangman", "computer", "network", "picture", "station" };
        var hard = new[] { "asynchronous", "polymorphism", "encapsulation", "inheritance", "serialization" };

        var rnd = new Random();

        return difficulty switch
        {
            GameDifficulty.Easy => easy[rnd.Next(easy.Length)],
            GameDifficulty.Medium => medium[rnd.Next(medium.Length)],
            GameDifficulty.Hard => hard[rnd.Next(hard.Length)],
            _ => easy[0]
        };
    }
}
