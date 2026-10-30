// UNVERIFIED IN EDITOR / ON DEVICE
using PatientZero.Core;
using PatientZero.Environment.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace PatientZero.Environment.Mobile
{
    /// <summary>Retained screen-space touch affordances. Touch ownership belongs to the tested router, not mouse emulation.</summary>
    public sealed class MobileTouchHud : MonoBehaviour
    {
        private MobileTouchAdapter adapter;
        private Canvas canvas;
        private CanvasScaler scaler;
        private readonly RectTransform[] boxes = new RectTransform[11];
        private readonly Text[] labels = new Text[11];
        private RectTransform thumb;
        private Text orientation, help;
        private Font font;
        public void Bind(MobileTouchAdapter owner)
        {
            adapter = owner; font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Mobile touch HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 300;
            scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            for (int i = 1; i < boxes.Length; i++)
            {
                var panel = new GameObject(((MobileControl)i).ToString(), typeof(RectTransform), typeof(Image)); panel.transform.SetParent(root.transform, false);
                boxes[i] = panel.GetComponent<RectTransform>(); boxes[i].anchorMin = boxes[i].anchorMax = boxes[i].pivot = Vector2.zero;
                var image = panel.GetComponent<Image>(); image.color = new Color(.02f, .06f, .065f, i == (int)MobileControl.Look ? 0 : .6f); image.raycastTarget = true;
                labels[i] = Text(panel.transform, "Label"); labels[i].rectTransform.anchorMin = Vector2.zero; labels[i].rectTransform.anchorMax = Vector2.one;
                labels[i].rectTransform.offsetMin = labels[i].rectTransform.offsetMax = Vector2.zero;
            }
            var nub = new GameObject("Joystick thumb", typeof(RectTransform), typeof(Image)); nub.transform.SetParent(root.transform, false);
            thumb = nub.GetComponent<RectTransform>(); thumb.anchorMin = thumb.anchorMax = Vector2.zero; thumb.sizeDelta = Vector2.one * 28;
            nub.GetComponent<Image>().color = new Color(.75f, .9f, .85f, .7f); nub.GetComponent<Image>().raycastTarget = false;
            orientation = Text(root.transform, "Orientation"); orientation.text = "Rotate to landscape"; orientation.rectTransform.anchorMin = orientation.rectTransform.anchorMax = new Vector2(.5f, .5f);
            orientation.rectTransform.sizeDelta = new Vector2(500, 80);
            help = Text(root.transform, "Touch help"); help.text = "LEFT: MOVE   RIGHT: LOOK\nHOLD TO GRAB / DRAG TO MANIPULATE / RELEASE";
            help.rectTransform.anchorMin = help.rectTransform.anchorMax = new Vector2(.5f, .7f); help.rectTransform.sizeDelta = new Vector2(650, 100); Reflow();
        }
        private Text Text(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>(); text.font = font; text.fontSize = 20; text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.9f, .97f, .94f); text.raycastTarget = false; return text;
        }
        public void Reflow()
        {
            if (!adapter || adapter.Input == null) return;
            scaler.scaleFactor = Mathf.Clamp(Screen.height / 720f, .65f, 3); canvas.scaleFactor = scaler.scaleFactor;
            for (int i = 1; i < boxes.Length; i++)
            {
                var r = adapter.Input.Layout.At((MobileControl)i);
                boxes[i].anchoredPosition = new Vector2(r.X, r.Y) / canvas.scaleFactor;
                boxes[i].sizeDelta = new Vector2(r.Width, r.Height) / canvas.scaleFactor;
            }
        }
        private void Update()
        {
            if (!adapter || adapter.Input == null) return;
            var input = adapter.Input;
            for (int i = 1; i < boxes.Length; i++) boxes[i].gameObject.SetActive(input.Available((MobileControl)i));
            labels[(int)MobileControl.Move].text = input.TeleportOnly ? "PUSH UP TO STEP" : "MOVE"; labels[(int)MobileControl.Look].text = input.AimMode ? "DRAG TO AIM HELD OBJECT" : input.GripHeld ?
                input.Act == ActId.Awakening ? "SWIPE UP TO PULL THE PLANK" : input.Act == ActId.Exit ? "SWIPE DOWN AND HOLD" : "SWIPE SIDEWAYS TO TURN / AIM CABLE AT SOCKET" : "DRAG TO LOOK / TAP TO HOLD";
            labels[(int)MobileControl.Interact].text = input.GripHeld ? "RELEASE" : "HOLD";
            labels[(int)MobileControl.Aim].text = input.AimMode ? "PUZZLE MODE" : "AIM OBJECT";
            labels[(int)MobileControl.Throw].text = "THROW"; labels[(int)MobileControl.Flashlight].text = input.LightOn ? "LIGHT OFF" : "LIGHT ON";
            labels[(int)MobileControl.Pause].text = "PAUSE / RESUME"; labels[(int)MobileControl.Restart].text = "RESTART";
            labels[(int)MobileControl.Step].text = "STEP"; labels[(int)MobileControl.Start].text = "START / RESUME";
            orientation.enabled = !input.Layout.Landscape;
            help.enabled = input.State == GameState.Calibration && input.Layout.Landscape && !input.Transitioning;
            thumb.gameObject.SetActive(input.Available(MobileControl.Move));
            thumb.anchoredPosition = (input.Layout.At(MobileControl.Move).Centre + input.Movement * input.Layout.JoystickRadius) / canvas.scaleFactor;
        }
        private void OnDisable() { if (canvas) canvas.gameObject.SetActive(false); }
    }
}
