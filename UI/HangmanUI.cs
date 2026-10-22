using System.Text;
using HangmanSpectre.Models;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace HangmanSpectre.UI;

public sealed class HangmanUI
{
    private const string Violet = "#af87ff";
    private const string VioletLight = "#c9a8ff";
    private const string VioletShadow = "#5f3fa8";
    private const string GallowsColor = "#875fd7";
    private const string GroundColor = "#3d2a66";
    private const string RopeColor = "#d7af87";
    private const string FigureColor = "#e8e4f0";
    private const string MutedColor = "#6c6680";
    private const string DimHeartColor = "#8a5068";
    private const string WinColor = "#5fd787";
    private const string LoseColor = "#ff5f87";

    private const string KeyUnused = "#c9c3d6 on #2a2236";
    private const string KeyHit = "#0d2b18 on #5fd787";
    private const string KeyMiss = "#3a0d18 on #ff5f87";

    private static readonly string[] KeyboardRows =
    {
        "QWERTZUIOP",
        "ASDFGHJKL",
        "YXCVBNM"
    };

    private static readonly GameDifficulty[] Difficulties =
    {
        GameDifficulty.Easy,
        GameDifficulty.Medium,
        GameDifficulty.Hard
    };

    // Sizes are read on every redraw, so resizing the window takes
    // effect on the next screen update.
    private static int ConsoleWidth => AnsiConsole.Profile.Width;
    private static int ConsoleHeight => AnsiConsole.Profile.Height;

    private const string Title = "HANGMAN";
    private const int FullHeaderMinHeight = 30;
    private const int GallowsPanelWidth = 25;
    private const int SidePanelMinWidth = 44;

    public void ShowHeader()
    {
        AnsiConsole.Clear();

        bool compact = ConsoleWidth < BlockText.Measure(Title) + 2
                       || ConsoleHeight < FullHeaderMinHeight;

        if (compact)
        {
            AnsiConsole.Write(
                new Rule($"[bold {Violet}]HANGMAN[/] [{VioletLight}]· Gemini Word Challenge[/]")
                    .RuleStyle(Style.Parse(Violet)));
        }
        else
        {
            AnsiConsole.Write(
                BlockText.Create(Title, Violet, VioletShadow));
            AnsiConsole.WriteLine();

            AnsiConsole.Write(
                new Rule($"[{VioletLight}]Gemini Word Challenge[/]")
                    .RuleStyle(Style.Parse(Violet)));
        }

        AnsiConsole.WriteLine();
    }

    public GameDifficulty SelectDifficulty()
    {
        int selected = 1;

        AnsiConsole.Live(BuildDifficultyCards(selected))
            .Start(ctx =>
            {
                // Live only draws on Refresh, so show the cards right away.
                ctx.Refresh();

                while (true)
                {
                    ConsoleKey key = Console.ReadKey(intercept: true).Key;

                    if (key == ConsoleKey.Enter)
                    {
                        return;
                    }

                    selected = key switch
                    {
                        ConsoleKey.LeftArrow or ConsoleKey.UpArrow =>
                            (selected + Difficulties.Length - 1) % Difficulties.Length,
                        ConsoleKey.RightArrow or ConsoleKey.DownArrow =>
                            (selected + 1) % Difficulties.Length,
                        ConsoleKey.D1 or ConsoleKey.NumPad1 => 0,
                        ConsoleKey.D2 or ConsoleKey.NumPad2 => 1,
                        ConsoleKey.D3 or ConsoleKey.NumPad3 => 2,
                        _ => selected
                    };

                    ctx.UpdateTarget(BuildDifficultyCards(selected));
                    ctx.Refresh();
                }
            });

        return Difficulties[selected];
    }

    private static IRenderable BuildDifficultyCards(int selected)
    {
        var cards = Difficulties.Select((difficulty, index) =>
        {
            bool isSelected = index == selected;
            int lives = GetMaxLives(difficulty);
            string heartColor = isSelected ? LoseColor : DimHeartColor;
            string words = difficulty switch
            {
                GameDifficulty.Easy => "short",
                GameDifficulty.Medium => "medium",
                _ => "long"
            };

            var body = new Markup(
                $"[{MutedColor}]Lives  [/][{heartColor}]{new string('♥', lives)}[/]\n" +
                $"[{MutedColor}]Words  [/][{FigureColor}]{words}[/]");

            return new Panel(body)
            {
                Header = new PanelHeader(isSelected
                    ? $"[bold {VioletLight}] {difficulty} [/]"
                    : $"[{FigureColor}] {difficulty} [/]"),
                Border = isSelected ? BoxBorder.Double : BoxBorder.Rounded,
                BorderStyle = Style.Parse(isSelected ? Violet : MutedColor),
                Padding = new Padding(1, 0),
                Width = 24
            };
        });

        return new Rows(
            Align.Center(new Columns(cards).Collapse()),
            new Text(""),
            new Markup(
                $"[{MutedColor}]← → choose · 1 2 3 jump · Enter start[/]")
                .Centered());
    }

    public async Task<string> ShowLoadingAsync(
        Func<Task<string>> operation)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(
            new Rule($"[{VioletLight}]Preparing round[/]")
                .RuleStyle(Style.Parse(Violet)));

        return await AnsiConsole.Progress()
            .AutoClear(false)
            .HideCompleted(false)
            .Columns(
                new SpinnerColumn(Spinner.Known.Dots)
                {
                    Style = Style.Parse(Violet),
                    CompletedText = $"[{WinColor}]✔[/]"
                },
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn
                {
                    Width = 36,
                    CompletedStyle = Style.Parse(Violet),
                    FinishedStyle = Style.Parse(Violet),
                    RemainingStyle = Style.Parse(MutedColor)
                },
                new PercentageColumn
                {
                    Style = Style.Parse(FigureColor),
                    CompletedStyle = Style.Parse(WinColor)
                })
            .StartAsync(async ctx =>
            {
                var connect = ctx.AddTask("Connecting to Gemini");
                var generate = ctx.AddTask("Generating word", autoStart: false);
                var prepare = ctx.AddTask("Preparing board", autoStart: false);

                await FillAsync(connect, steps: 6);

                generate.StartTask();
                Task<string> wordTask = operation();

                // Creep towards 90% while the request runs, then finish.
                while (!wordTask.IsCompleted)
                {
                    generate.Increment((90 - generate.Value) * 0.15);
                    await Task.WhenAny(wordTask, Task.Delay(60));
                }

                string word = await wordTask;
                generate.Value = generate.MaxValue;

                prepare.StartTask();
                await FillAsync(prepare, steps: 5);

                return word;
            });
    }

    private static async Task FillAsync(ProgressTask task, int steps)
    {
        task.StartTask();

        for (int i = 0; i < steps; i++)
        {
            task.Increment(task.MaxValue / steps);
            await Task.Delay(50);
        }

        task.Value = task.MaxValue;
    }

    public void ShowFallbackNotice(string message)
    {
        AnsiConsole.MarkupLine(
            $"[yellow]{Markup.Escape(message)}[/]");

        AnsiConsole.MarkupLine(
            $"[{MutedColor}]Using a local fallback word instead.[/]");
    }

    public void ShowGame(
        string word,
        HashSet<char> guessedLetters,
        HashSet<char> incorrectGuesses,
        int lives,
        int maxLives)
    {
        ShowHeader();

        int mistakes = maxLives - lives;

        var gallowsPanel = new Panel(
            new Rows(
                Align.Center(new Markup(DrawHangman(mistakes, maxLives))),
                new Markup(
                    $"[{MutedColor}]Mistakes [/][{FigureColor}]{mistakes}[/]" +
                    $"[{MutedColor}] / {maxLives}[/]")
                    .Centered()))
        {
            Header = new PanelHeader($"[bold {VioletLight}] Gallows [/]"),
            Border = BoxBorder.Heavy,
            BorderStyle = Style.Parse(GallowsColor),
            Padding = new Padding(1, 0)
        };

        var sidePanels = new Rows(
            BuildWordPanel(word, guessedLetters),
            BuildKeyboardPanel(word, guessedLetters),
            BuildStatusPanel(word, guessedLetters, incorrectGuesses, lives, maxLives));

        int sideWidth = ConsoleWidth - GallowsPanelWidth - 1;

        if (sideWidth < SidePanelMinWidth)
        {
            // Too narrow for side by side: stack everything.
            AnsiConsole.Write(Align.Center(gallowsPanel));
            AnsiConsole.WriteLine();
            AnsiConsole.Write(sidePanels);
            return;
        }

        // Fixed widths: Grid can't measure nested expanding panels reliably.
        var grid = new Grid()
            .AddColumn(new GridColumn().Width(GallowsPanelWidth).Padding(0, 0, 1, 0))
            .AddColumn(new GridColumn().Width(sideWidth).Padding(0, 0, 0, 0));

        grid.AddRow(gallowsPanel, sidePanels);

        AnsiConsole.Write(grid);
    }

    private static Panel BuildWordPanel(string word, HashSet<char> guessedLetters)
    {
        string letters = string.Join(
            " ",
            word.Select(c => guessedLetters.Contains(c)
                ? $"[bold white]{Markup.Escape(char.ToUpperInvariant(c).ToString())}[/]"
                : $"[{Violet}]_[/]"));

        return new Panel(new Markup(letters).Centered())
        {
            Header = new PanelHeader($"[bold {VioletLight}] Word [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(Violet),
            Padding = new Padding(2, 0),
            Expand = true
        };
    }

    private static Panel BuildKeyboardPanel(string word, HashSet<char> guessedLetters)
    {
        string rows = string.Join(
            "\n",
            KeyboardRows.Select(row => string.Join(
                " ",
                row.Select(key =>
                {
                    char letter = char.ToLowerInvariant(key);
                    string style = !guessedLetters.Contains(letter)
                        ? KeyUnused
                        : word.Contains(letter) ? KeyHit : KeyMiss;

                    return $"[{style}] {key} [/]";
                }))));

        return new Panel(new Markup(rows).Centered())
        {
            Header = new PanelHeader($"[bold {VioletLight}] Keyboard [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(Violet),
            Padding = new Padding(1, 0),
            Expand = true
        };
    }

    private static Panel BuildStatusPanel(
        string word,
        HashSet<char> guessedLetters,
        HashSet<char> incorrectGuesses,
        int lives,
        int maxLives)
    {
        var distinctLetters = word.Distinct().ToList();
        int hits = distinctLetters.Count(guessedLetters.Contains);
        int open = distinctLetters.Count - hits;

        string hearts =
            $"[{LoseColor}]{new string('♥', lives)}[/]" +
            $"[{MutedColor}]{new string('♡', maxLives - lives)}[/]";

        const int barWidth = 20;
        int filled = (int)Math.Round((double)lives / maxLives * barWidth);
        string lifeBar =
            $"[{Violet}]{new string('█', filled)}[/]" +
            $"[{MutedColor}]{new string('░', barWidth - filled)}[/]";

        var breakdown = new BreakdownChart()
            .AddItem("Hits", hits, Style.Parse(WinColor).Foreground)
            .AddItem("Misses", incorrectGuesses.Count, Style.Parse(LoseColor).Foreground)
            .AddItem("Open", open, Style.Parse(MutedColor).Foreground)
            .WithValueColor(Style.Parse(FigureColor).Foreground);

        return new Panel(
            new Rows(
                new Markup(
                    $"[{MutedColor}]Lives  [/]{hearts}  {lifeBar}  " +
                    $"[{FigureColor}]{lives}/{maxLives}[/]"),
                new Markup(
                    $"[{MutedColor}]Missed [/]" +
                    (incorrectGuesses.Count == 0
                        ? $"[{MutedColor}]none yet[/]"
                        : $"[{LoseColor}]{Markup.Escape(string.Join(" ", incorrectGuesses.Select(char.ToUpperInvariant)))}[/]")),
                breakdown))
        {
            Header = new PanelHeader($"[bold {VioletLight}] Status [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse(Violet),
            Padding = new Padding(1, 0),
            Expand = true
        };
    }

    public char AskForLetter(HashSet<char> guessedLetters)
    {
        string input = AnsiConsole.Prompt(
            new TextPrompt<string>(
                $"[bold {VioletLight}]Enter a letter[/] [{Violet}]›[/]")
                .Validate(value =>
                {
                    if (string.IsNullOrWhiteSpace(value) ||
                        value.Trim().Length != 1 ||
                        !char.IsLetter(value.Trim()[0]))
                    {
                        return ValidationResult.Error(
                            $"[{LoseColor}]Enter exactly one letter.[/]");
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
            $"[{WinColor}]Correct! The letter is in the word.[/]");
    }

    public void ShowIncorrectGuess()
    {
        AnsiConsole.MarkupLine(
            $"[{LoseColor}]Wrong! You lost one life.[/]");
    }

    public void ShowResult(RoundResult round, IReadOnlyList<RoundResult> history)
    {
        AnsiConsole.Clear();

        int shownRounds = Math.Min(history.Count, MaxHistoryRows);
        // Title 6 + word panel 3 + table + prompt, plus blank lines.
        int baseHeight = 6 + 3 + (shownRounds + 4) + 6;
        bool showGallows = !round.Won
                           && ConsoleHeight >= baseHeight + GallowsTemplate.Length + 1;

        string title = round.Won ? "YOU WIN!" : "GAME OVER";
        string titleColor = round.Won ? WinColor : LoseColor;

        if (ConsoleWidth >= BlockText.Measure(title) + 2)
        {
            AnsiConsole.Write(BlockText.Create(title, titleColor, VioletShadow));
            AnsiConsole.WriteLine();
        }
        else
        {
            AnsiConsole.Write(
                new Rule($"[bold {titleColor}]{title}[/]")
                    .RuleStyle(Style.Parse(Violet)));
        }

        if (showGallows)
        {
            AnsiConsole.WriteLine();

            AnsiConsole.Write(
                Align.Center(
                    new Markup(DrawHangman(round.MaxLives, round.MaxLives, dead: true))));
            AnsiConsole.WriteLine();
        }

        AnsiConsole.WriteLine();

        string outcome = round.Won
            ? $"solved with {round.LivesLeft} {(round.LivesLeft == 1 ? "life" : "lives")} left"
            : "out of lives";

        AnsiConsole.Write(
            new Panel(
                new Markup(
                    $"[{MutedColor}]The word was [/]" +
                    $"[bold {VioletLight}]{Markup.Escape(round.Word.ToUpperInvariant())}[/]" +
                    $"[{MutedColor}]  ·  {outcome}[/]")
                    .Centered())
            {
                Border = BoxBorder.Double,
                BorderStyle = Style.Parse(Violet),
                Padding = new Padding(2, 0),
                Expand = true
            });

        AnsiConsole.WriteLine();

        // Rough table width: fixed columns plus the longest word.
        int tableWidth = 44 + history.TakeLast(MaxHistoryRows).Max(r => r.Word.Length);
        int chartWidth = ConsoleWidth - tableWidth - 4;

        if (chartWidth >= 30)
        {
            var grid = new Grid()
                .AddColumn(new GridColumn().NoWrap().Padding(0, 0, 4, 0))
                .AddColumn(new GridColumn());

            grid.AddRow(
                BuildHistoryTable(history),
                BuildHistoryCharts(history, Math.Min(chartWidth, 60)));

            AnsiConsole.Write(grid);
        }
        else
        {
            AnsiConsole.Write(BuildHistoryTable(history));
            AnsiConsole.WriteLine();
            AnsiConsole.Write(
                BuildHistoryCharts(history, Math.Clamp(ConsoleWidth, 20, 60)));
        }
    }

    private const int MaxHistoryRows = 8;

    private static Table BuildHistoryTable(IReadOnlyList<RoundResult> history)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Style.Parse(Violet).Foreground)
            .AddColumn($"[bold {VioletLight}]Round[/]")
            .AddColumn($"[bold {VioletLight}]Level[/]")
            .AddColumn($"[bold {VioletLight}]Word[/]")
            .AddColumn(new TableColumn($"[bold {VioletLight}]Misses[/]").RightAligned())
            .AddColumn($"[bold {VioletLight}]Result[/]");

        int first = Math.Max(0, history.Count - MaxHistoryRows);

        for (int i = first; i < history.Count; i++)
        {
            RoundResult r = history[i];

            table.AddRow(
                $"[{FigureColor}]{i + 1}[/]",
                $"[{FigureColor}]{r.Difficulty}[/]",
                $"[bold white]{Markup.Escape(r.Word)}[/]",
                $"[{FigureColor}]{r.Misses}/{r.MaxLives}[/]",
                r.Won ? $"[{WinColor}]WIN[/]" : $"[{LoseColor}]LOSS[/]");
        }

        return table;
    }

    private static IRenderable BuildHistoryCharts(
        IReadOnlyList<RoundResult> history,
        int width)
    {
        var chart = new BarChart()
            .Width(width)
            .Label($"[bold {VioletLight}]Misses per round[/]")
            .LeftAlignLabel()
            .WithMaxValue(history.Max(r => r.MaxLives));

        int first = Math.Max(0, history.Count - MaxHistoryRows);

        for (int i = first; i < history.Count; i++)
        {
            RoundResult r = history[i];

            chart.AddItem(
                $"Round {i + 1}",
                r.Misses,
                Style.Parse(r.Won ? Violet : LoseColor).Foreground);
        }

        int wins = history.Count(r => r.Won);

        var winRate = new BreakdownChart()
            .Width(width)
            .AddItem("Wins", wins, Style.Parse(WinColor).Foreground)
            .AddItem("Losses", history.Count - wins, Style.Parse(LoseColor).Foreground)
            .WithValueColor(Style.Parse(FigureColor).Foreground);

        return new Rows(
            chart,
            new Text(""),
            new Markup(
                $"[bold {VioletLight}]Win rate[/] " +
                $"[{FigureColor}]{(int)Math.Round(100.0 * wins / history.Count)}%[/]"),
            winRate);
    }

    public bool AskPlayAgain()
    {
        AnsiConsole.WriteLine();

        return AnsiConsole.Prompt(
            new ConfirmationPrompt($"[{FigureColor}]Play another round?[/]")
            {
                DefaultValue = true
            }
                .ChoicesStyle(Style.Parse(Violet))
                .DefaultValueStyle(Style.Parse(MutedColor)));
    }

    public void ShowGoodbye()
    {
        AnsiConsole.MarkupLine(
            $"\n[{Violet}]Thanks for playing Hangman![/]");
    }

    private static int GetMaxLives(GameDifficulty difficulty)
    {
        return difficulty switch
        {
            GameDifficulty.Easy => 8,
            GameDifficulty.Medium => 6,
            GameDifficulty.Hard => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
    }

    // Gallows template. Placeholders: r = rope, h = head,
    // b = body, L/R = arms, l/k = legs.
    private static readonly string[] GallowsTemplate =
    {
        "  ╔═══════════╗",
        "  ║           r",
        "  ║         hhhhh",
        "  ║         hhhhh",
        "  ║         hhhhh",
        "  ║          LbR",
        "  ║         L b R",
        "  ║           b",
        "  ║          l k",
        "  ║         l   k",
        "══╩══════════════════",
        "▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀"
    };

    private static readonly string[] AliveHead = { "╭─┸─╮", "│o o│", "╰─┬─╯" };
    private static readonly string[] DeadHead = { "╭─┸─╮", "│x x│", "╰─┬─╯" };

    private static string DrawHangman(
        int mistakes,
        int maxLives,
        bool dead = false)
    {
        int stage = Math.Clamp(
            (int)Math.Round((double)mistakes / maxLives * 6), 0, 6);

        int width = GallowsTemplate.Max(row => row.Length);
        string figureColor = dead ? LoseColor : FigureColor;
        var markup = new StringBuilder();

        for (int row = 0; row < GallowsTemplate.Length; row++)
        {
            var line = new StringBuilder();
            int headIndex = 0;

            foreach (char c in GallowsTemplate[row])
            {
                line.Append(c switch
                {
                    'r' => '┃',
                    'h' => stage >= 1
                        ? (dead ? DeadHead : AliveHead)[row - 2][headIndex++]
                        : ' ',
                    'b' => stage >= 2 ? '┃' : ' ',
                    'L' => stage >= 3 ? '╱' : ' ',
                    'R' => stage >= 4 ? '╲' : ' ',
                    'l' => stage >= 5 ? '╱' : ' ',
                    'k' => stage >= 6 ? '╲' : ' ',
                    _ => c
                });
            }

            string rendered = line.ToString().PadRight(width);
            string template = GallowsTemplate[row].PadRight(width);

            BlockText.AppendColored(markup, rendered, i =>
                rendered[i] == ' ' ? null : template[i] switch
                {
                    'r' => RopeColor,
                    '▀' => GroundColor,
                    'h' or 'b' or 'L' or 'R' or 'l' or 'k' => figureColor,
                    _ => GallowsColor
                });

            if (row < GallowsTemplate.Length - 1)
            {
                markup.Append('\n');
            }
        }

        return markup.ToString();
    }
}
