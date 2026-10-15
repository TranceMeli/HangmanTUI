using HangmanSpectre.Models;
using Spectre.Console;

namespace HangmanSpectre.UI;

public sealed class HangmanUI
{
    public void ShowHeader()
    {
        AnsiConsole.Clear();

        AnsiConsole.Write(
            new FigletText("HANGMAN")
                .Centered()
                .Color(Color.MediumPurple1));

        AnsiConsole.Write(
            new Rule("[mediumpurple1]Gemini Word Challenge[/]")
                .RuleStyle(Style.Parse("mediumpurple1")));

        AnsiConsole.WriteLine();
    }

    public GameDifficulty SelectDifficulty()
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<GameDifficulty>()
                .Title("[bold mediumpurple1]Choose difficulty[/]")
                .HighlightStyle(
                    new Style(Color.MediumPurple1))
                .AddChoices(
                    GameDifficulty.Easy,
                    GameDifficulty.Medium,
                    GameDifficulty.Hard)
                .UseConverter(difficulty => difficulty switch
                {
                    GameDifficulty.Easy => "Easy   - 8 mistakes",
                    GameDifficulty.Medium => "Medium - 6 mistakes",
                    GameDifficulty.Hard => "Hard   - 4 mistakes",
                    _ => difficulty.ToString()
                }));
    }

    public void ShowGeneratingWord()
    {
        AnsiConsole.MarkupLine(
            "\n[mediumpurple1]Generating a word with Gemini...[/]");
    }

    public async Task<string> ShowLoadingAsync(
        Func<Task<string>> operation)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("mediumpurple1"))
            .StartAsync(
                "Asking Gemini for a word...",
                async _ => await operation());
    }

    public void ShowFallbackNotice(string message)
    {
        AnsiConsole.MarkupLine(
            $"[yellow]{Markup.Escape(message)}[/]");

        AnsiConsole.MarkupLine(
            "[grey]Using a local fallback word instead.[/]");
    }

    public void ShowGame(
        string word,
        HashSet<char> guessedLetters,
        HashSet<char> incorrectGuesses,
        int lives,
        int maxLives)
    {
        AnsiConsole.Clear();
        ShowHeader();

        string maskedWord = string.Join(
            " ",
            word.Select(c =>
                guessedLetters.Contains(c)
                    ? c.ToString()
                    : "_"));

        var wordPanel = new Panel(
            new Align(
                new Markup(
                    $"[bold white]{Markup.Escape(maskedWord)}[/]"),
                HorizontalAlignment.Center))
        {
            Header = new PanelHeader("[mediumpurple1]Word[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.MediumPurple1),
            Padding = new Padding(2, 1)
        };

        AnsiConsole.Write(wordPanel);
        AnsiConsole.WriteLine();

        string drawing = DrawHangman(maxLives - lives, maxLives);

        var drawingPanel = new Panel(
            new Align(
                new Markup($"[mediumpurple1]{drawing}[/]"),
                HorizontalAlignment.Center))
        {
            Header = new PanelHeader("[bold]Hangman[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Purple),
            Padding = new Padding(1, 0)
        };

        AnsiConsole.Write(drawingPanel);
        AnsiConsole.WriteLine();

        var stats = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("Lives")
            .AddColumn("Incorrect guesses")
            .AddRow(
                $"[bold]{lives}/{maxLives}[/]",
                incorrectGuesses.Count == 0
                    ? "[grey]None[/]"
                    : Markup.Escape(
                        string.Join(", ", incorrectGuesses)));

        AnsiConsole.Write(stats);
        AnsiConsole.WriteLine();
    }

    public char AskForLetter(HashSet<char> guessedLetters)
    {
        string input = AnsiConsole.Prompt(
            new TextPrompt<string>(
                "[mediumpurple1]Enter one letter[/]")
                .Validate(value =>
                {
                    if (string.IsNullOrWhiteSpace(value) ||
                        value.Trim().Length != 1 ||
                        !char.IsLetter(value.Trim()[0]))
                    {
                        return ValidationResult.Error(
                            "[red]Enter exactly one letter.[/]");
                    }

                    char letter = char.ToLowerInvariant(
                        value.Trim()[0]);

                    if (guessedLetters.Contains(letter))
                    {
                        return ValidationResult.Error(
                            "[yellow]You already tried that letter.[/]");
                    }

                    return ValidationResult.Success();
                }));

        return char.ToLowerInvariant(input.Trim()[0]);
    }

    public void ShowCorrectGuess()
    {
        AnsiConsole.MarkupLine(
            "\n[green]Correct! The letter is in the word.[/]");

        AnsiConsole.WriteLine();
    }

    public void ShowIncorrectGuess()
    {
        AnsiConsole.MarkupLine(
            "\n[red]Wrong! You lost one life.[/]");

        AnsiConsole.WriteLine();
    }

    public void ShowResult(string word, bool won, int maxLives)
    {
        AnsiConsole.Clear();
        ShowHeader();

        if (won)
        {
            AnsiConsole.Write(
                new FigletText("YOU WIN!")
                    .Centered()
                    .Color(Color.Green));
        }
        else
        {
            AnsiConsole.Write(
                new FigletText("GAME OVER")
                    .Centered()
                    .Color(Color.Red));

            AnsiConsole.WriteLine();

            AnsiConsole.MarkupLine(
                $"[red]{DrawHangman(maxLives, maxLives)}[/]");
        }

        AnsiConsole.Write(
            new Panel(
                new Align(
                    new Markup(
                        $"The word was: [bold mediumpurple1]" +
                        $"{Markup.Escape(word)}[/]"),
                    HorizontalAlignment.Center))
            {
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.MediumPurple1),
                Padding = new Padding(2, 1)
            });

        AnsiConsole.MarkupLine(
            won
                ? "\n[green]Congratulations! You guessed the word.[/]"
                : "\n[red]Better luck next time![/]");
    }

    public bool AskPlayAgain()
    {
        return AnsiConsole.Confirm(
            "\nPlay another round?", true);
    }

    public void ShowGoodbye()
    {
        AnsiConsole.MarkupLine(
            "\n[mediumpurple1]Thanks for playing Hangman![/]");
    }

    private static string DrawHangman(
        int mistakes,
        int maxLives)
    {
        int stage = (int)Math.Round(
            (double)mistakes / maxLives * 6);

        string[][] stages =
        {
            new[]
            {
                "  +---+",
                "  |   |",
                "      |",
                "      |",
                "      |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                "      |",
                "      |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                "  |   |",
                "      |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                " /|   |",
                "      |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                " /|\\  |",
                "      |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                " /|\\  |",
                " /    |",
                "========="
            },
            new[]
            {
                "  +---+",
                "  |   |",
                "  O   |",
                " /|\\  |",
                " / \\  |",
                "========="
            }
        };

        return string.Join(
            "\n",
            stages[Math.Clamp(stage, 0, stages.Length - 1)]);
    }
}
