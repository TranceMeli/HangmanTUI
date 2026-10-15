using HangmanSpectre.Models;
using HangmanSpectre.Services;
using HangmanSpectre.UI;

namespace HangmanTwo.Game;

public sealed class HangmanGame
{
    private readonly HangmanUI _ui;
    private readonly GeminiWordService _wordService;

    public HangmanGame(HangmanUI ui, GeminiWordService wordService)
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _wordService = wordService ?? throw new ArgumentNullException(nameof(wordService));
    }

    public async Task RunAsync()
    {
        bool playAgain = true;

        while (playAgain)
        {
            _ui.ShowHeader();

            GameDifficulty difficulty = _ui.SelectDifficulty();

            string word;

            try
            {
                word = await _ui.ShowLoadingAsync(
                    () => _wordService.GetWordAsync(difficulty));
            }
            catch (Exception ex)
            {
                _ui.ShowFallbackNotice(ex.Message);
                word = _wordService.GetFallbackWord(difficulty);
            }

            await PlayRoundAsync(word, difficulty);

            playAgain = _ui.AskPlayAgain();
        }

        _ui.ShowGoodbye();
    }

    private async Task PlayRoundAsync(
        string word,
        GameDifficulty difficulty)
    {
        int maxLives = GetMaxLives(difficulty);
        int lives = maxLives;

        var guessedLetters = new HashSet<char>();
        var incorrectGuesses = new HashSet<char>();

        while (lives > 0 &&
               !word.All(guessedLetters.Contains))
        {
            _ui.ShowGame(
                word,
                guessedLetters,
                incorrectGuesses,
                lives,
                maxLives);

            char guess = _ui.AskForLetter(guessedLetters);

            guessedLetters.Add(guess);

            if (word.Contains(guess))
            {
                _ui.ShowCorrectGuess();
            }
            else
            {
                incorrectGuesses.Add(guess);
                lives--;

                _ui.ShowIncorrectGuess();
            }

            await Task.Delay(700);
        }

        bool won = word.All(guessedLetters.Contains);

        _ui.ShowResult(word, won, maxLives);
    }

    private static int GetMaxLives(GameDifficulty difficulty)
    {
        return difficulty switch
        {
            GameDifficulty.Easy => 8,
            GameDifficulty.Medium => 6,
            GameDifficulty.Hard => 4,
            _ => throw new ArgumentOutOfRangeException(
                nameof(difficulty))
        };
    }
}