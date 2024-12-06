using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class MazeGenerator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;
    public GameObject tilePrefab;
    public GameObject playerPrefab;
    public GameObject finishPrefab;
    public GameObject orbPrefab;
    public Text timerText;
    public GameObject gameOverPanel; // Game Over UI Panel
    public Text gameOverMessage; // Game Over message text
    public Button restartButton;
    public GameObject pauseMenuPanel; // Reference to the Pause Menu panel
    public Button resumeButton; // Button to resume the game
    public Button exitButton; // Button to exit the game
    private bool isPaused = false; // Tracks if the game is paused


    public float gameTime = 60f;
    public int orbCount = 5;
    public float timeAddedByOrb = 5f;
    public float timePenalty = 5f;
    public float questionTimePenalty = 10f; // Penalty to game time for unanswered question

    public GameObject questionPanel;
    public Text questionText;
    public Button choiceButton1;
    public Button choiceButton2;
    public Text questionTimerText; // New UI for question timer
    public float questionTimeLimit = 10f; // Time limit for questions

    private int[,] maze;
    private float currentTime;
    private bool gameEnded = false;
    private bool isQuestionActive = false; // To track if a question is being answered
    private float questionCurrentTime; // Tracks remaining time for the current question
    private GameObject player; // Reference to the player object
    private GameObject finish; // Reference to the finish object
    private int collectedOrbs = 0; // Count of collected orbs

    private QuestionManager questionManager;
    private OrbHandler currentActiveOrb; // Track the orb associated with the active question

    void Start()
    {
        Time.timeScale = 1;
        GenerateMaze();
        DrawMaze();
        SpawnPlayer();
        SpawnOrbs();

        currentTime = gameTime;
        UpdateTimerUI();

        questionManager = new QuestionManager();
        questionManager.LoadQuestions(Path.Combine(Application.streamingAssetsPath, "questions.json"));

        if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        // Ensure pause menu is hidden at the start
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        // Check if the ESC key is pressed to toggle pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu(); // Call the pause toggle function
            return; // Skip the rest of the Update logic when toggling pause
        }

        // If the game is paused or ended, skip further updates
        if (gameEnded || isPaused) return;

        if (isQuestionActive)
        {
            // Question timer logic
            questionCurrentTime -= Time.deltaTime;
            UpdateQuestionTimerUI();

            if (questionCurrentTime <= 0)
            {
                questionCurrentTime = 0;
                QuestionTimeExpired();
            }
        }
        else
        {
            // Main game timer logic
            currentTime -= Time.deltaTime;
            UpdateTimerUI();

            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame(false); // End game with "time out" condition
            }
        }
    }

    void GenerateMaze()
    {
        maze = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                maze[x, y] = 1;
            }
        }

        int startX = Random.Range(1, width - 1);
        int startY = Random.Range(1, height - 1);
        Carve(startX, startY);
    }

    void Carve(int x, int y)
    {
        maze[x, y] = 0;

        int[] directions = { 0, 1, 2, 3 };
        ShuffleArray(directions);

        foreach (int dir in directions)
        {
            int dx = 0, dy = 0;
            if (dir == 0) dy = 1;
            else if (dir == 1) dx = 1;
            else if (dir == 2) dy = -1;
            else if (dir == 3) dx = -1;

            int newX = x + dx * 2;
            int newY = y + dy * 2;

            if (newX > 0 && newX < width - 1 && newY > 0 && newY < height - 1 && maze[newX, newY] == 1)
            {
                maze[x + dx, y + dy] = 0;
                Carve(newX, newY);
            }
        }
    }

    void DrawMaze()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (maze[x, y] == 1)
                {
                    Vector2 position = new Vector2(x, y);
                    GameObject wall = Instantiate(tilePrefab, position, Quaternion.identity, transform);

                    if (!wall.GetComponent<BoxCollider2D>())
                    {
                        wall.AddComponent<BoxCollider2D>();
                    }
                }
            }
        }
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0 : 1; // Pause or resume game time
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1; // Resume game time
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the current scene
    }

    public void ExitGame()
    {
        Application.Quit(); // Quit the application
        Debug.Log("Game is exiting...");

        #if UNITY_EDITOR
        // Stop play mode in the Unity Editor
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        // Quit the application when built
        Application.Quit();
    #endif
    }


    void SpawnPlayer()
    {
        Vector2 playerPosition = FindOpenPositionNearBottom();
        player = Instantiate(playerPrefab, playerPosition, Quaternion.identity); // Keep reference to the player
    }

    void SpawnFinish()
    {
        Vector2 finishPosition = FindOpenPositionNearTopRight();
        finish = Instantiate(finishPrefab, finishPosition, Quaternion.identity);
        finish.name = "Finish"; // Assign name to the finish object for recognition
    }

    void SpawnOrbs()
    {
        for (int i = 0; i < orbCount; i++)
        {
            Vector2 orbPosition = FindRandomOpenPosition();
            GameObject orb = Instantiate(orbPrefab, orbPosition, Quaternion.identity);
            orb.AddComponent<OrbHandler>().Setup(this, timeAddedByOrb, timePenalty);
        }
    }

    public Vector2 FindRandomOpenPosition()
    {
        while (true)
        {
            int x = Random.Range(1, width - 1);
            int y = Random.Range(1, height - 1);
            if (maze[x, y] == 0) return new Vector2(x, y);
        }
    }

    Vector2 FindOpenPositionNearBottom()
    {
        for (int y = 1; y < height / 2; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (maze[x, y] == 0) return new Vector2(x, y);
            }
        }
        return new Vector2(width / 2, 1);
    }

    Vector2 FindOpenPositionNearTopRight()
    {
        for (int x = width - 2; x > width / 2; x--)
        {
            for (int y = height - 2; y > height / 2; y--)
            {
                if (maze[x, y] == 0) return new Vector2(x, y);
            }
        }
        return new Vector2(width - 2, height - 2);
    }

    void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[rnd];
            array[rnd] = temp;
        }
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"Time Left: {Mathf.Ceil(currentTime)}";
        }
    }

    void UpdateQuestionTimerUI()
    {
        if (questionTimerText != null)
        {
            questionTimerText.text = $"Question Time: {Mathf.Ceil(questionCurrentTime)}";
        }
    }

    public void PlayerReachedFinish()
    {
        EndGame(true); // Trigger the "You won" scenario
    }

    public void AddTime(float timeToAdd)
    {
        currentTime += timeToAdd;
    }

    void EndGame(bool playerWon)
    {
        gameEnded = true;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverMessage != null)
            {
                gameOverMessage.text = playerWon
                    ? "Congratulations! You won!"
                    : "Game Over! You ran out of time!";
            }
        }
    }

    public void ShowQuestion(OrbHandler orb)
    {
        if (questionManager == null) return;

        isQuestionActive = true; // Pause the main timer
        currentActiveOrb = orb; // Set the current active orb
        TogglePlayerMovement(false); // Disable player movement

        Question question = questionManager.GetRandomQuestion();

        if (question == null)
        {
            Debug.LogError("No questions available!");
            TogglePlayerMovement(true); // Re-enable player movement
            isQuestionActive = false; // Resume game if no questions are available
            orb.Respawn(); // Respawn the orb
            return;
        }

        questionPanel.SetActive(true);
        questionText.text = question.text;

        // Set up choices
        choiceButton1.onClick.RemoveAllListeners();
        choiceButton2.onClick.RemoveAllListeners();

        choiceButton1.GetComponentInChildren<Text>().text = question.choices[0];
        choiceButton2.GetComponentInChildren<Text>().text = question.choices[1];

        choiceButton1.onClick.AddListener(() => HandleAnswer(question.choices[0], question));
        choiceButton2.onClick.AddListener(() => HandleAnswer(question.choices[1], question));

        questionCurrentTime = questionTimeLimit; // Reset question timer
        UpdateQuestionTimerUI();
    }

    private void HandleAnswer(string chosenAnswer, Question question)
    {
        questionPanel.SetActive(false);
        isQuestionActive = false; // Resume the main timer
        TogglePlayerMovement(true); // Enable player movement

        if (chosenAnswer == question.correctAnswer)
        {
            AddTime(currentActiveOrb.timeToAdd);
            Destroy(currentActiveOrb.gameObject);
            OrbCollected(); // Increment collected orbs
            questionManager.MarkQuestionUsed(question); // Only mark if answered correctly
        }
        else
        {
            AddTime(-currentActiveOrb.timePenalty);
            currentActiveOrb.Respawn(); // Respawn the orb if the answer is incorrect
            // Allow question reuse, do not mark it as used
        }
    }

    private void QuestionTimeExpired()
    {
        questionPanel.SetActive(false);
        isQuestionActive = false; // Resume the main timer
        TogglePlayerMovement(true); // Enable player movement

        AddTime(-questionTimePenalty); // Apply penalty for not answering

        // Respawn orb in a new random position
        currentActiveOrb.Respawn();
        // Allow question reuse, do not mark it as used
    }

    private void TogglePlayerMovement(bool canMove)
    {
        if (player != null)
        {
            var playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.enabled = canMove;
            }
        }
    }

    private void OrbCollected()
    {
        collectedOrbs++;
        if (collectedOrbs >= orbCount)
        {
            SpawnFinish(); // Spawn finish line when all orbs are collected
        }
    }
}



public class OrbHandler : MonoBehaviour
{
    private MazeGenerator mazeGenerator;
    public float timeToAdd;
    public float timePenalty;

    public void Setup(MazeGenerator generator, float timeAdded, float penalty)
    {
        mazeGenerator = generator;
        timeToAdd = timeAdded;
        timePenalty = penalty;
    }

    public void Respawn()
    {
        Vector2 newPosition = mazeGenerator.FindRandomOpenPosition();
        transform.position = newPosition;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            mazeGenerator.ShowQuestion(this);
        }
    }
}
