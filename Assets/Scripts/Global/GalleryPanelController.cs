using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GalleryPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform contentRoot;

    [Header("Layout")]
    [SerializeField] private Vector2 thumbnailSize = new Vector2(280f, 158f);
    [SerializeField] private int maxItems = 50;

    [Header("Grid")]
    [SerializeField] private int columns = 5;
    [SerializeField] private int maxRows = 4;

    [Header("Delete Button")]
    [SerializeField] private Vector2 deleteButtonSize = new Vector2(30f, 30f);
    [SerializeField] private Color deleteButtonColor = new Color(0.8f, 0.15f, 0.15f, 0.95f);

    [Header("Preview")]
    [SerializeField] private Vector2 previewSize = new Vector2(1000f, 560f);
    [SerializeField] private Vector2 previewCloseButtonSize = new Vector2(44f, 44f);

    private readonly List<Texture2D> loadedTextures = new List<Texture2D>();

    private GameObject previewOverlay;
    private RawImage previewRawImage;

    private void Awake()
    {
        EnsureContentRoot();
        EnsureLayout();
        EnsurePreviewOverlay();
    }

    private void OnEnable()
    {
        ScreenshotSystem.ScreenshotSaved += OnScreenshotSaved;
        Refresh();
    }

    private void OnDisable()
    {
        ScreenshotSystem.ScreenshotSaved -= OnScreenshotSaved;
    }

    public void Refresh()
    {
        if (contentRoot == null)
        {
            Debug.LogWarning("GalleryPanelController: contentRoot is missing.");
            return;
        }

        ClearItems();

        List<string> paths = ScreenshotSystem.GetScreenshotPathsNewestFirst();
        int visibleLimit = Mathf.Max(1, columns) * Mathf.Max(1, maxRows);
        int count = Mathf.Min(paths.Count, Mathf.Min(maxItems, visibleLimit));

        for (int i = 0; i < count; i++)
        {
            CreateThumbnail(paths[i]);
        }
    }

    private void OnScreenshotSaved(string _)
    {
        if (isActiveAndEnabled) Refresh();
    }

    private void EnsureContentRoot()
    {
        if (contentRoot != null) return;

        Transform found = transform.Find("GalleryContent");
        if (found != null)
        {
            contentRoot = found as RectTransform;
            return;
        }

        GameObject go = new GameObject("GalleryContent", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(24f, 24f);
        rt.offsetMax = new Vector2(-24f, -24f);
        contentRoot = rt;
    }

    private void EnsureLayout()
    {
        if (contentRoot == null) return;

        GridLayoutGroup grid = contentRoot.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = contentRoot.gameObject.AddComponent<GridLayoutGroup>();

        grid.cellSize = thumbnailSize;
        grid.spacing = new Vector2(12f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsurePreviewOverlay()
    {
        if (previewOverlay != null && previewRawImage != null) return;

        Transform found = transform.Find("PreviewOverlay");
        if (found != null)
        {
            previewOverlay = found.gameObject;
            previewRawImage = found.GetComponentInChildren<RawImage>(true);
            return;
        }

        previewOverlay = new GameObject("PreviewOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        previewOverlay.transform.SetParent(transform, false);

        RectTransform overlayRt = previewOverlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImage = previewOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.82f);

        GameObject previewObj = new GameObject("PreviewImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        previewObj.transform.SetParent(previewOverlay.transform, false);

        RectTransform previewRt = previewObj.GetComponent<RectTransform>();
        previewRt.anchorMin = new Vector2(0.5f, 0.5f);
        previewRt.anchorMax = new Vector2(0.5f, 0.5f);
        previewRt.pivot = new Vector2(0.5f, 0.5f);
        previewRt.sizeDelta = previewSize;
        previewRt.anchoredPosition = Vector2.zero;

        previewRawImage = previewObj.GetComponent<RawImage>();
        previewRawImage.color = Color.white;

        GameObject closeObj = new GameObject("Button-ClosePreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObj.transform.SetParent(previewOverlay.transform, false);

        RectTransform closeRt = closeObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-120f, -18f);
        closeRt.sizeDelta = previewCloseButtonSize;

        Image closeImage = closeObj.GetComponent<Image>();
        closeImage.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);

        Button closeButton = closeObj.GetComponent<Button>();
        closeButton.onClick.AddListener(ClosePreview);

        GameObject xMark = new GameObject("Text-X", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        xMark.transform.SetParent(closeObj.transform, false);

        RectTransform textRt = xMark.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = xMark.GetComponent<Text>();
        t.text = "X";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.black;
        t.fontSize = 26;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;

        previewOverlay.SetActive(false);
    }

    private void ClearItems()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child.name == "Button-Close") continue;
            Destroy(child.gameObject);
        }

        foreach (Texture2D tex in loadedTextures)
        {
            if (tex != null) Destroy(tex);
        }

        loadedTextures.Clear();

        ClosePreview();
    }

    private void CreateThumbnail(string imagePath)
    {
        if (!File.Exists(imagePath)) return;

        byte[] bytes = File.ReadAllBytes(imagePath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Destroy(tex);
            return;
        }

        loadedTextures.Add(tex);

        GameObject card = new GameObject($"Shot-{Path.GetFileNameWithoutExtension(imagePath)}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement), typeof(Button));
        card.transform.SetParent(contentRoot, false);

        Image bg = card.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);

        LayoutElement le = card.GetComponent<LayoutElement>();
        le.preferredWidth = thumbnailSize.x;
        le.preferredHeight = thumbnailSize.y;

        Button cardButton = card.GetComponent<Button>();
        cardButton.targetGraphic = bg;
        cardButton.onClick.AddListener(() => OpenPreview(tex));

        GameObject preview = new GameObject("Preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        preview.transform.SetParent(card.transform, false);

        RectTransform previewRt = preview.GetComponent<RectTransform>();
        previewRt.anchorMin = new Vector2(0f, 0f);
        previewRt.anchorMax = new Vector2(1f, 1f);
        previewRt.offsetMin = new Vector2(6f, 6f);
        previewRt.offsetMax = new Vector2(-6f, -6f);

        RawImage raw = preview.GetComponent<RawImage>();
        raw.texture = tex;
        raw.color = Color.white;
        raw.raycastTarget = false;

        CreateDeleteButton(card.transform, imagePath, tex, card);
    }

    private void CreateDeleteButton(Transform parent, string imagePath, Texture2D tex, GameObject card)
    {
        GameObject deleteButtonObj = new GameObject("Button-Delete", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        deleteButtonObj.transform.SetParent(parent, false);

        RectTransform buttonRt = deleteButtonObj.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(1f, 1f);
        buttonRt.anchorMax = new Vector2(1f, 1f);
        buttonRt.pivot = new Vector2(1f, 1f);
        buttonRt.anchoredPosition = new Vector2(-6f, -6f);
        buttonRt.sizeDelta = deleteButtonSize;

        Image buttonImage = deleteButtonObj.GetComponent<Image>();
        buttonImage.color = deleteButtonColor;

        Button button = deleteButtonObj.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => DeleteScreenshot(imagePath, tex, card));

        GameObject xMark = new GameObject("Text-X", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        xMark.transform.SetParent(deleteButtonObj.transform, false);

        RectTransform textRt = xMark.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = xMark.GetComponent<Text>();
        t.text = "X";
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.fontSize = 18;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
    }

    private void OpenPreview(Texture2D tex)
    {
        EnsurePreviewOverlay();
        if (previewOverlay == null || previewRawImage == null || tex == null) return;

        previewRawImage.texture = tex;
        previewOverlay.SetActive(true);
    }

    public void ClosePreview()
    {
        if (previewRawImage != null) previewRawImage.texture = null;
        if (previewOverlay != null) previewOverlay.SetActive(false);
    }

    private void DeleteScreenshot(string imagePath, Texture2D tex, GameObject card)
    {
        try
        {
            if (File.Exists(imagePath)) File.Delete(imagePath);
        }
        catch (IOException ex)
        {
            Debug.LogWarning($"Failed to delete screenshot: {imagePath}. {ex.Message}");
            return;
        }

        if (previewRawImage != null && previewRawImage.texture == tex)
            ClosePreview();

        if (tex != null)
        {
            loadedTextures.Remove(tex);
            Destroy(tex);
        }

        if (card != null) Destroy(card);

        if (isActiveAndEnabled) Refresh();
    }
}
