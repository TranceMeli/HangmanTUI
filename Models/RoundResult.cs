namespace HangmanSpectre.Models;

public sealed record RoundResult(
    GameDifficulty Difficulty,
    string Word,
    int Misses,
    int MaxLives,
    bool Won)
{
    public int LivesLeft => MaxLives - Misses;
}
