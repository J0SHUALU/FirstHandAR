using UnityEngine;
using FirstHand.Core;

namespace FirstHand.Entities
{
    public class PhoneProp : SimEntity
    {
        [SerializeField] private GameObject screenGlow;
        private bool ringing;

        public override void ResetEntity()
        {
            ringing = false;
            if (screenGlow != null) screenGlow.SetActive(false);
        }

        public void SetCallActive(bool active)
        {
            ringing = active;
            if (screenGlow != null) screenGlow.SetActive(active);
        }

        public override void OnTapped(Vector3 worldPoint)
            => EventBus.Publish(new UiActionEvent { Action = "open_phone" });

        private void Update()
        {
            if (ringing && screenGlow != null)
                screenGlow.transform.localScale = Vector3.one * (1f + Mathf.PingPong(Time.time * 2f, 0.15f));
        }
    }
}
