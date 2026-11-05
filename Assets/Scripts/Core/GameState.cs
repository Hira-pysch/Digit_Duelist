namespace DigitDuelist
{
    /// <summary>
    /// Represents the primary states in the Digit Duelist state machine.
    /// Matches the single-scene UI panel flowchart routing.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        ModeSelection, // Addition, Subtraction, Multiplication, Division, MDAS
        LoadGame,
        Settings,
        Gameplay,
        Paused,
        GameOver,
        LevelComplete
    }

    /// <summary>
    /// Mathematical operation modes available in the game.
    /// </summary>
    public enum MathOperation
    {
        Addition,
        Subtraction,
        Multiplication,
        Division,
        MDAS // Mixed operations: randomly picks +, -, *, / each turn
    }
}
