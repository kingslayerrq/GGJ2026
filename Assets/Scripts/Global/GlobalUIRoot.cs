using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GlobalUIRoot : MonoBehaviour
{
    public static GlobalUIRoot Instance { get; private set; }
    public static bool IsModalInputLocked { get; private set; }
    public static bool IsGamePaused { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject settingsTabPanel;
    [SerializeField] private GameObject galleryPanel;

    [Header("Controllers")]
    [SerializeField] private GalleryPanelController galleryPanelController;

    [Header("Modal")]
    [SerializeField] private GameObject modalBlocker;

    private readonly Dictionary<Selectable, bool> cachedInteractables = new Dictionary<Selectable, bool>();

    private float cachedTimeScale = 1f;
    private bool ownsPause;

    public bool IsSettingsOpen => settingsTabPanel != null && settingsTabPanel.activeInHierarchy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (galleryPanelController == null && galleryPanel != null)
            galleryPanelController = galleryPanel.GetComponent<GalleryPanelController>();

        if (FindFirstObjectByType<ScreenshotSystem>() == null)
            gameObject.AddComponent<ScreenshotSystem>();

        SetSettingsOpen(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ResumeGameIfOwned();
            Instance = null;
            IsModalInputLocked = false;
            IsGamePaused = false;
        }
    }

    public void OnClickSettings()
    {
        SetSettingsOpen(true);
    }

    public void OnClickCloseSettings()
    {
        SetSettingsOpen(false);
    }

    public void OnClickCloseGallery()
    {
        if (galleryPanel != null) galleryPanel.SetActive(false);
    }

    public void OnClickGallery()
    {
        if (galleryPanel == null) return;

        bool next = !galleryPanel.activeSelf;
        galleryPanel.SetActive(next);

        if (next && galleryPanelController != null)
            galleryPanelController.Refresh();
    }

    public void OnClickExit()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetSettingsOpen(bool open)
    {
        if (settingsTabPanel != null) settingsTabPanel.SetActive(open);
        if (!open && galleryPanel != null) galleryPanel.SetActive(false);

        ApplyInputLock(open);
    }

    private void ApplyInputLock(bool locked)
    {
        IsModalInputLocked = locked;

        EnsureModalBlocker();
        if (modalBlocker != null) modalBlocker.SetActive(locked);

        if (locked)
        {
            PauseGameIfNeeded();
            CacheAndDisableExternalSelectables();
        }
        else
        {
            ResumeGameIfOwned();
            RestoreSelectables();
        }
    }

    private void PauseGameIfNeeded()
    {
        if (ownsPause) return;

        cachedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        IsGamePaused = true;
        ownsPause = true;
    }

    private void ResumeGameIfOwned()
    {
        if (!ownsPause) return;

        Time.timeScale = cachedTimeScale;
        IsGamePaused = false;
        ownsPause = false;
    }

    private void CacheAndDisableExternalSelectables()
    {
        cachedInteractables.Clear();

        Selectable[] all = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Selectable selectable in all)
        {
            if (selectable == null) continue;
            if (IsAllowedWhileSettingsOpen(selectable.transform)) continue;

            cachedInteractables[selectable] = selectable.interactable;
            selectable.interactable = false;
        }
    }

    private void RestoreSelectables()
    {
        foreach (KeyValuePair<Selectable, bool> kvp in cachedInteractables)
        {
            if (kvp.Key == null) continue;
            kvp.Key.interactable = kvp.Value;
        }

        cachedInteractables.Clear();
    }

    private bool IsAllowedWhileSettingsOpen(Transform t)
    {
        if (t == null) return false;

        if (settingsTabPanel != null && t.IsChildOf(settingsTabPanel.transform)) return true;
        if (galleryPanel != null && t.IsChildOf(galleryPanel.transform)) return true;

        return false;
    }

    private void EnsureModalBlocker()
    {
        if (modalBlocker != null) return;

        if (settingsTabPanel == null) return;

        Transform parent = settingsTabPanel.transform.parent != null ? settingsTabPanel.transform.parent : transform;

        GameObject blocker = new GameObject("ModalBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        blocker.transform.SetParent(parent, false);

        RectTransform rt = blocker.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = blocker.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;

        blocker.transform.SetSiblingIndex(settingsTabPanel.transform.GetSiblingIndex());

        modalBlocker = blocker;
        modalBlocker.SetActive(false);
    }
}
