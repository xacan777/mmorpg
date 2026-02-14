using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniMMORPG
{
    public class CameraFollow : MonoBehaviour
    {
        private Transform _target;
        private float _distance = 11f;
        private float _yaw = 135f;
        private float _pitch = 32f;

        public void Initialize(Transform target)
        {
            _target = target;
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                _yaw += delta.x * 0.18f;
                _pitch -= delta.y * 0.12f;
                _pitch = Mathf.Clamp(_pitch, 12f, 70f);
            }

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var lookPoint = _target.position + Vector3.up * 1.4f;
            var desiredPos = lookPoint + rotation * new Vector3(0f, 0f, -_distance);

            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * 12f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position), Time.deltaTime * 14f);
        }

        private void SnapToTarget()
        {
            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            var lookPoint = _target.position + Vector3.up * 1.4f;
            transform.position = lookPoint + rotation * new Vector3(0f, 0f, -_distance);
            transform.LookAt(lookPoint);
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

            GUI.Box(new Rect(Screen.width - 430, 10, 420, 95), "Управление");
            GUI.Label(new Rect(Screen.width - 420, 40, 410, 20), "ЛКМ: цель/идти в точку, ПКМ+мышь: вращать камеру");
            GUI.Label(new Rect(Screen.width - 420, 60, 410, 20), "WASD: движение, Space: прыжок, 1: зелье");

            if (_messageTime > 0f)
            {
                GUI.Box(new Rect(Screen.width / 2f - 180f, Screen.height - 70f, 360f, 45f), _message);
            }
        }
    }
}
