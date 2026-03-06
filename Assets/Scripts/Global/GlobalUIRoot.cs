using UnityEngine;

public class GlobalUIRoot : MonoBehaviour
{
    public static GlobalUIRoot Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject settingsTabPanel;
    [SerializeField] private GameObject galleryPanel;

    [Header("Controllers")]
    [SerializeField] private GalleryPanelController galleryPanelController;

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
        if (settingsTabPanel != null) settingsTabPanel.SetActive(false);
        if (galleryPanel != null) galleryPanel.SetActive(false);

        if (galleryPanelController == null && galleryPanel != null)
            galleryPanelController = galleryPanel.GetComponent<GalleryPanelController>();

        if (FindFirstObjectByType<ScreenshotSystem>() == null)
            gameObject.AddComponent<ScreenshotSystem>();
    }

    public void OnClickSettings()
    {
        if (settingsTabPanel != null) settingsTabPanel.SetActive(true);
        if (galleryPanel != null) galleryPanel.SetActive(false);
    }

    public void OnClickCloseSettings()
    {
        if (settingsTabPanel != null) settingsTabPanel.SetActive(false);
        if (galleryPanel != null) galleryPanel.SetActive(false);
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
}
