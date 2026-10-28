namespace PatientZero.Core
{
    /// <summary>
    /// The legal GameState transitions. GameFlow (A) must refuse anything this returns
    /// false for, and log it. Restart is a transition into Playing.
    ///
    ///   Boot        -> Calibration | Playing
    ///   Calibration -> Playing
    ///   Playing     -> Paused | Caught | Ending
    ///   Paused      -> Playing | Ending          (resume to whichever state was paused)
    ///   Caught      -> Playing                   (act checkpoint restore)
    ///   Ending      -> Paused | Complete
    ///   Complete    -> Playing                   (full restart)
    /// </summary>
    public static class GameStateRules
    {
        public static bool CanTransition(GameState from, GameState to)
        {
            if (from == to) return false;
            switch (from)
            {
                case GameState.Boot:
                    return to == GameState.Calibration || to == GameState.Playing;
                case GameState.Calibration:
                    return to == GameState.Playing;
                case GameState.Playing:
                    return to == GameState.Paused || to == GameState.Caught || to == GameState.Ending;
                case GameState.Paused:
                    return to == GameState.Playing || to == GameState.Ending;
                case GameState.Caught:
                    return to == GameState.Playing;
                case GameState.Ending:
                    return to == GameState.Paused || to == GameState.Complete;
                case GameState.Complete:
                    return to == GameState.Playing;
                default:
                    return false;
            }
        }

        /// <summary>States the pause input and headset focus loss may pause from.</summary>
        public static bool CanPauseFrom(GameState state)
        {
            return state == GameState.Playing || state == GameState.Ending;
        }

        /// <summary>States in which zombies may move and hear. Everywhere else they freeze.</summary>
        public static bool IsSimulationLive(GameState state)
        {
            return state == GameState.Playing || state == GameState.Ending;
        }
    }
}
