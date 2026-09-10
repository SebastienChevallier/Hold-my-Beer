using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldMyBeer.UI
{
    /// <summary>
    /// Builds the placeholder uGUI hierarchy from code.
    ///
    /// Why code and not prefabs: this project is authored without a running editor,
    /// and a screen built here has no GUID to lose in a merge. The screens talk to
    /// their view through this factory only, so replacing it with real prefabs later
    /// means changing this file and nothing else.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Background = new(0.09f, 0.10f, 0.13f, 1f);
        public static readonly Color Panel = new(0.15f, 0.17f, 0.22f, 0.95f);
        public static readonly Color Accent = new(0.95f, 0.68f, 0.20f, 1f);
        public static readonly Color TextColor = new(0.92f, 0.93f, 0.96f, 1f);

        private static Font _font;

        private static Font Font =>
            _font ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas CreateCanvas(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;

            go.GetComponent<Image>().color = Panel;

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 28, 28);
            layout.spacing = 14f;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;

            return rect;
        }

        /// <summary>
        /// A panel pinned to the right edge of the screen instead of the centre.
        ///
        /// Parent it to the canvas, not to another panel: dropping it inside a
        /// VerticalLayoutGroup makes it push its siblings off-screen, which is exactly
        /// what a secondary panel must never do to the screen it belongs to.
        /// </summary>
        public static RectTransform CreateSidePanel(Transform parent, string name, Vector2 size,
                                                    float margin = 48f)
        {
            var rect = CreatePanel(parent, name, size);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-margin, 0f);
            return rect;
        }

        /// <summary>
        /// A vertical scrolling area. Returns the content transform: add children to it
        /// exactly as to a panel, and it grows and scrolls on its own.
        ///
        /// The ContentSizeFitter is what makes the scrollbar meaningful - without it the
        /// content keeps the viewport height and rows simply overflow, visible but
        /// unreachable.
        /// </summary>
        public static RectTransform CreateScrollList(Transform parent, float height)
        {
            var rootGo = new GameObject("ScrollList", typeof(RectTransform), typeof(ScrollRect),
                typeof(LayoutElement));
            rootGo.transform.SetParent(parent, false);

            var layoutElement = rootGo.GetComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 0f;

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(rootGo.transform, false);
            var viewport = (RectTransform)viewportGo.transform;
            StretchToParent(viewport);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);

            var content = (RectTransform)contentGo.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 8f;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childAlignment = TextAnchor.UpperCenter;

            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = rootGo.GetComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            return content;
        }

        public static Text CreateLabel(Transform parent, string content, int fontSize = 26,
                                       TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.color = TextColor;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = content;

            go.GetComponent<LayoutElement>().minHeight = fontSize + 10f;
            return text;
        }

        public static Button CreateButton(Transform parent, string label, System.Action onClick)
        {
            var go = new GameObject($"Button_{label}", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color = Accent;
            go.GetComponent<LayoutElement>().minHeight = 56f;

            var caption = CreateLabel(go.transform, label, 26, TextAnchor.MiddleCenter);
            caption.color = new Color(0.08f, 0.08f, 0.10f, 1f);
            StretchToParent((RectTransform)caption.transform);

            var button = go.GetComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }

        public static InputField CreateInputField(Transform parent, string placeholder, string initialValue = "")
        {
            var go = new GameObject($"Input_{placeholder}", typeof(RectTransform), typeof(Image),
                typeof(InputField), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);
            go.GetComponent<LayoutElement>().minHeight = 48f;

            var text = CreateLabel(go.transform, string.Empty, 24);
            text.supportRichText = false;
            InsetToParent((RectTransform)text.transform);

            var hint = CreateLabel(go.transform, placeholder, 24);
            hint.color = new Color(1f, 1f, 1f, 0.35f);
            hint.fontStyle = FontStyle.Italic;
            InsetToParent((RectTransform)hint.transform);

            var field = go.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = hint;
            field.text = initialValue;
            return field;
        }

        public static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void InsetToParent(RectTransform rect)
        {
            StretchToParent(rect);
            rect.offsetMin = new Vector2(12f, 6f);
            rect.offsetMax = new Vector2(-12f, -6f);
        }
    }
}
