using UnityEngine;
using System.Collections;
using System.IO;

/// <summary>
/// Manages the gameplay sequence of game.
/// </summary>
public class GameplayManager : Singleton<GameplayManager>
{
    #region Fields

    /// <summary>
    /// Field of the state of gameplay status.
    /// </summary>
    [SerializeField]
    private GameplayState gameplayState;

    /// <summary>
    /// Property of the state of gameplay status.
    /// </summary>
    public GameplayState GameplayState
    {
        get { return this.gameplayState; }
        private set { this.gameplayState = value; }
    }

    #endregion

    #region Methods

    /// <summary>
    /// 
    /// </summary>
    private void Awake()
    {
        GameplayManager.Instance.SetGameplayState(GameplayState.OnGameplayStart);
    }

    /// <summary>
    /// Sets the specified Gameplay State to the gameplayState field.
    /// </summary>
    /// <param name="state"></param>
    public void SetGameplayState(GameplayState state)
    {
        GameplayManager.Instance.gameplayState = state;
    }

    /// <summary>
    /// Starts the gameplay sequence.
    /// </summary>
    private IEnumerator StartGameplaySequence()
    {
        ScoreManager.Instance.CurrentScore = 0;
        BoardManager.Instance.InitializeBoard(BoardManager.Instance.AvailableBoards[0]);
        SlotManager.Instance.ClearSlots();
        yield return new WaitForSeconds(.25f);
        SlotManager.Instance.SpawnNewBlockSet();
    }

    /// <summary>
    /// 
    /// </summary>
    private void StartGameplay()
    {
        StartCoroutine(GameplayManager.Instance.StartGameplaySequence());
    }

    /// <summary>
    /// Update is called every frame, if the MonoBehaviour is enabled.
    /// </summary>
    private void Update()
    {
        if (!GameManager.Instance.GameState.Equals(GameState.Gameplay)) return;
        if (this.gameplayState.Equals(GameplayState.None)) return;

        switch (GameplayManager.Instance.gameplayState)
        {
            case GameplayState.OnGameplayStart:
                if (SaveManager.Instance.IsTherePlayerData())
                {
                    SaveManager.Instance.LoadPlayerData();
                    UiManager.Instance.GameplayMenu.UpdateHighScore();
                }
                if (SaveManager.Instance.IsThereSave())
                {
                    ScoreManager.Instance.CurrentScore = 0;
                    BoardManager.Instance.InitializeBoard(BoardManager.Instance.AvailableBoards[0]);
                    SlotManager.Instance.ClearSlots();
                    SaveManager.Instance.LoadProgress();
                    UiManager.Instance.GameplayMenu.UpdateCurrentScore();
                    GameplayManager.Instance.SetGameplayState(GameplayState.None);
                    break;
                }

                GameplayManager.Instance.StartGameplay();
                GameplayManager.Instance.SetGameplayState(GameplayState.None);
                break;

            case GameplayState.OnBlockDrag:

                break;

            case GameplayState.OnBlockDrop:
                if (SlotManager.Instance.AreSlotsEmpty())
                {
                    SlotManager.Instance.SpawnNewBlockSet();
                }

                foreach (Row row in BoardManager.Instance.CurrentBoard.Rows)
                {
                    if (BoardManager.Instance.IsRowFilled(row))
                    {
                        TileManager.Instance.FlagTilesOfRow(row);
                        BoardManager.Instance.RowsWillBeCleared.Add(row);
                    }
                }

                foreach (Column column in BoardManager.Instance.CurrentBoard.Columns)
                {
                    if (BoardManager.Instance.IsColumnFilled(column))
                    {
                        TileManager.Instance.FlagTilesOfColumn(column);
                        BoardManager.Instance.ColumnsWillBeCleared.Add(column);
                    }
                }

                if (BoardManager.Instance.RowsWillBeCleared.Count > 0 ||
                    BoardManager.Instance.ColumnsWillBeCleared.Count > 0)
                {
                    GameplayManager.Instance.SetGameplayState(GameplayState.OnDelete);
                    return;
                }

                if (BlockManager.Instance.QueuedBlocks.Count > 0)
                {
                    foreach (Block block in BlockManager.Instance.QueuedBlocks)
                    {
                        if (BlockManager.Instance.IsBlockFitsToBoard(block))
                        {
                            GameplayManager.Instance.SetGameplayState(GameplayState.None);
                            return;
                        }
                    }
                }
                GameplayManager.Instance.SetGameplayState(GameplayState.GameOver);
                break;
            case GameplayState.OnDelete:
                if (BoardManager.Instance.RowsWillBeCleared.Count > 0)
                {
                    foreach (Row row in BoardManager.Instance.RowsWillBeCleared)
                    {
                        TileManager.Instance.ClearFlaggedTilesOfRow(row);
                        ScoreManager.Instance.IncreseScore(ScoreManager.Instance.LineDeleteScore);
                        ScoreManager.Instance.UpdateHighScore();
                        UiManager.Instance.GameplayMenu.UpdateCurrentScore();
                        UiManager.Instance.GameplayMenu.UpdateHighScore();
                    }
                    BoardManager.Instance.RowsWillBeCleared.Clear();
                }

                if (BoardManager.Instance.ColumnsWillBeCleared.Count > 0)
                {
                    foreach (Column column in BoardManager.Instance.ColumnsWillBeCleared)
                    {
                        TileManager.Instance.ClearFlaggedTilesOfColumn(column);
                        ScoreManager.Instance.IncreseScore(ScoreManager.Instance.LineDeleteScore);
                        ScoreManager.Instance.UpdateHighScore();
                        UiManager.Instance.GameplayMenu.UpdateCurrentScore();
                        UiManager.Instance.GameplayMenu.UpdateHighScore();
                    }
                    BoardManager.Instance.ColumnsWillBeCleared.Clear();
                }

                if (BlockManager.Instance.QueuedBlocks.Count > 0)
                {
                    foreach (Block block in BlockManager.Instance.QueuedBlocks)
                    {
                        if (BlockManager.Instance.IsBlockFitsToBoard(block))
                        {
                            GameplayManager.Instance.SetGameplayState(GameplayState.None);
                            return;
                        }
                    }
                }
                GameplayManager.Instance.SetGameplayState(GameplayState.GameOver);
                break;

            case GameplayState.GameOver:
                GameplayManager.Instance.SetGameplayState(GameplayState.GameOvered);
                UiManager.Instance.GameOverMenu.Open();
                UiManager.Instance.GameOverMenu.UpdateCurrentScore();
                UiManager.Instance.GameOverMenu.UpdateHighScore();
                if (File.Exists(Application.persistentDataPath + "/save.txt"))
                {
                    File.Delete(Application.persistentDataPath + "/save.txt");
                }
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void RestartGameplay()
    {
        BoardManager.Instance.ClearBoard(BoardManager.Instance.CurrentBoard);
        SlotManager.Instance.ClearSlots();
        ScoreManager.Instance.ResetCurrentScore();
        UiManager.Instance.GameplayMenu.UpdateCurrentScore();
        GameplayManager.Instance.SetGameplayState(GameplayState.OnGameplayStart);
    }

    /// <summary>
    /// 
    /// </summary>
    public void PauseGameplay()
    {
        GameplayManager.Instance.SetGameplayState(GameplayState.Pause);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ResumeGameplay()
    {
        GameplayManager.Instance.SetGameplayState(GameplayState.None);
    }

    #endregion
}