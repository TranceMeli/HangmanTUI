# Hangman Spectre

[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-Modern-8A2BE2?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp)
[![Spectre.Console](https://img.shields.io/badge/Spectre.Console-0.57.2-4b9be6?style=for-the-badge)](https://spectreconsole.net/)

## Preview

<img width="960" height="479" alt="image" src="https://github.com/user-attachments/assets/faba306f-402d-4e76-bcd4-c70a2e88857b" />


Short description
-----------------
A console-based Hangman game using Spectre.Console for the UI. Word generation can be provided via a Gemini API; the API key is loaded from configuration.

Prerequisites
-------------
- .NET 10 SDK installed
- Optional: a valid Gemini API key (required if you want external word generation)

Configuration
-------------
Configuration is read with the following priority:

1. HangmanSpectre/appsettings.json (key: `Gemini:ApiKey`)
2. Environment variable `Gemini__ApiKey`
3. Local file `HangmanSpectre/gemini.key` (optional)

Example appsettings.json
```json
{
  "Gemini": {
	"ApiKey": "REPLACE_WITH_YOUR_API_KEY"
  }
}
```

Run
---
From the solution or project folder run:

dotnet run --project HangmanSpectre/HangmanSpectre.csproj

Architecture (brief)
--------------------
- Spectre.Console for the UI
- Generic Host (Microsoft.Extensions.Hosting) + DI
- GeminiWordService reads configuration via IConfiguration
- HangmanGame orchestrates game logic and UI

Troubleshooting
---------------
- If the program fails on startup due to a missing API key: check appsettings.json, create `gemini.key` or set the `Gemini__ApiKey` environment variable.
- Configuration files are copied to the output folder (CopyToOutputDirectory). If you run from a different folder, ensure files are available in the app output directory.
