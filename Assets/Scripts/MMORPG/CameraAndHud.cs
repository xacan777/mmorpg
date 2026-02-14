using UnityEngine;

namespace MiniMMORPG
{
    public class CameraFollow : MonoBehaviour
    {
        private Transform _target;
        private Vector3 _offset = new Vector3(0f, 8f, -8f);

        public void Initialize(Transform target)
        {
            _target = target;
            transform.position = target.position + _offset;
            transform.LookAt(target.position + Vector3.up * 1.2f);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, _target.position + _offset, Time.deltaTime * 6f);
            transform.LookAt(_target.position + Vector3.up * 1.2f);
        }
    }

    public class MMORPGHud : MonoBehaviour
    {
        private GameSession _session;
        private string _message = "Добро пожаловать в мини-MMORPG";
        private float _messageTime = 3f;

        public void Initialize(GameSession session)
        {
            _session = session;
        }

        public void ShowMessage(string text)
        {
            _message = text;
            _messageTime = 3f;
        }

        private void Update()
        {
            _messageTime -= Time.deltaTime;
        }

        private void OnGUI()
        {
            if (_session.Player == null)
            {
                return;
            }

            var player = _session.Player;
            GUI.Box(new Rect(10, 10, 300, 125), "Персонаж");
            GUI.Label(new Rect(20, 40, 280, 20), $"HP: {player.Health:0}/{player.MaxHealth:0}");
            GUI.HorizontalScrollbar(new Rect(20, 62, 220, 18), 0f, player.Health / player.MaxHealth, 0f, 1f);
            GUI.Label(new Rect(20, 82, 280, 20), $"LVL: {player.Level}  XP: {player.Xp}/{player.Level * 100}");
            GUI.HorizontalScrollbar(new Rect(20, 102, 220, 18), 0f, player.XpProgress01(), 0f, 1f);

            GUI.Box(new Rect(10, 145, 300, 80), "Инвентарь");
            GUI.Label(new Rect(20, 175, 280, 20), $"Золото: {player.Gold}   Зелья (клавиша 1): {player.Potions}");

            if (player.CurrentTarget != null && player.CurrentTarget.IsAlive)
            {
                var t = player.CurrentTarget;
                GUI.Box(new Rect(Screen.width / 2f - 170f, 10f, 340f, 55f), $"Цель: Монстр  HP: {t.Health:0}/{t.MaxHealth:0}");
                GUI.HorizontalScrollbar(new Rect(Screen.width / 2f - 155f, 38f, 310f, 16f), 0f, Mathf.Clamp01(t.Health / t.MaxHealth), 0f, 1f);
            }

            GUI.Box(new Rect(Screen.width - 380, 10, 370, 95), "Управление");
            GUI.Label(new Rect(Screen.width - 370, 40, 350, 20), "WASD - движение, ЛКМ - атака по монстру");
            GUI.Label(new Rect(Screen.width - 370, 60, 350, 20), "Подбирайте лут, убивайте монстров, прокачивайтесь");

            if (_messageTime > 0f)
            {
                GUI.Box(new Rect(Screen.width / 2f - 180f, Screen.height - 70f, 360f, 45f), _message);
            }
        }
    }
}
