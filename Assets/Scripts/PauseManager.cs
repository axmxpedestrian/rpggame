using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [SerializeField] private GameObject pauseMenu;
    private bool isPaused = false;

    private Canvas pauseCanvas;
    private CanvasGroup pauseCanvasGroup;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 自动获取子对象中的Canvas
            pauseCanvas = GetComponentInChildren<Canvas>(true);
            if (pauseCanvas != null)
            {
                pauseCanvasGroup = pauseCanvas.GetComponent<CanvasGroup>();
                if (pauseCanvasGroup == null)
                {
                    pauseCanvasGroup = pauseCanvas.gameObject.AddComponent<CanvasGroup>();
                }

                // 初始状态
                pauseCanvas.enabled = false;
                pauseCanvasGroup.alpha = 0;
                pauseCanvasGroup.interactable = false;
                pauseCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                Debug.LogError("PauseCanvas not found in children!");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        bool newPauseState = !pauseMenu.activeSelf;
        pauseMenu.SetActive(newPauseState);
        Time.timeScale = newPauseState ? 0 : 1;
    }

    public void ResumeGame()
    {
        TogglePause();
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1;

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }


    private void OnEnable()
    {
        GameEvents.OnPauseToggle += TogglePause;
    }

    private void OnDisable()
    {
        GameEvents.OnPauseToggle -= TogglePause;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}