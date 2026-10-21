using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using FirstHand.Core;

namespace FirstHand.Presentation
{
    /// <summary>
    /// Press-and-hold button for the breathing check (FR-15). Shows a filling ring and the seconds held,
    /// and publishes how long it was held when released. The rule (5 to 10 s) lives in the step, not here.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public string buttonId = "check_breathing";
        public Image fill;
        public TextMeshProUGUI counter;
        public float ringSeconds = 10f;
        public Color goodColor = new Color32(0x2B, 0xB6, 0x73, 0xFF);
        public Color earlyColor = new Color32(0x3A, 0x9B, 0xDC, 0xFF);
        public Color lateColor = new Color32(0xE6, 0x39, 0x46, 0xFF);
        public float goodMin = 5f, goodMax = 10f;

        private bool holding;
        private float held;

        public void OnPointerDown(PointerEventData e)
        {
            holding = true;
            held = 0;
            EventBus.Publish(new HoldStartedEvent { ButtonId = buttonId });
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) { if (holding) Release(); }

        private void OnDisable() { holding = false; held = 0; Draw(); }

        private void Update()
        {
            if (!holding) return;
            held += Time.deltaTime;
            Draw();
        }

        private void Release()
        {
            if (!holding) return;
            holding = false;
            EventBus.Publish(new HoldEndedEvent { ButtonId = buttonId, Seconds = held });
            held = 0;
            Draw();
        }

        private void Draw()
        {
            if (fill != null)
            {
                fill.fillAmount = Mathf.Clamp01(held / ringSeconds);
                fill.color = held < goodMin ? earlyColor : held <= goodMax ? goodColor : lateColor;
            }
            if (counter != null) counter.text = holding ? held.ToString("0") + "s" : "";
        }
    }
}
