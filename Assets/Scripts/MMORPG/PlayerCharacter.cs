using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniMMORPG
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerCharacter : MonoBehaviour
    {
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int Gold { get; private set; }
        public int Potions { get; private set; }

        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;
        public EnemyCharacter CurrentTarget { get; private set; }

        private CharacterController _controller;
        private GameSession _session;
        private float _attackCooldown;

        private const float Speed = 6.5f;
        private const float Gravity = -20f;
        private const float AttackRange = 2.35f;
        private const float AttackPeriod = 0.45f;

        private float _verticalVelocity;

        public void Initialize(GameSession session)
        {
            _session = session;
            _controller = GetComponent<CharacterController>();
            _controller.height = 2f;
            _controller.radius = 0.45f;
            _controller.center = new Vector3(0f, 1f, 0f);
        }

        private void Update()
        {
            HandleMovement();
            HandleTargeting();
            HandleAutoAttack();
            HandleConsumables();
        }

        private void HandleMovement()
        {
            Vector2 moveAxis = ReadMoveInput();
            var inputDirection = new Vector3(moveAxis.x, 0f, moveAxis.y);
            if (inputDirection.sqrMagnitude > 1f)
            {
                inputDirection.Normalize();
            }

            var currentCamera = Camera.main;
            Vector3 move;
            if (currentCamera != null)
            {
                var camForward = currentCamera.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                var camRight = currentCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                move = (camForward * inputDirection.z + camRight * inputDirection.x) * Speed;
            }
            else
            {
                move = inputDirection * Speed;
            }

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
            }
            else
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);

            if (inputDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(move.x, 0f, move.z)), Time.deltaTime * 12f);
            }
        }

        private void HandleTargeting()
        {
            if (!IsAttackPressed())
            {
                return;
            }

            var currentCamera = Camera.main;
            if (currentCamera != null && Physics.Raycast(currentCamera.ScreenPointToRay(ReadPointerPosition()), out var hit, 100f))
            {
                var enemyByRay = hit.collider.GetComponentInParent<EnemyCharacter>();
                if (enemyByRay != null)
                {
                    CurrentTarget = enemyByRay;
                    _session.ShowLootMessage($"Цель: {CurrentTarget.name}");
                    return;
                }
            }

            CurrentTarget = FindNearestAliveEnemy();
            if (CurrentTarget != null)
            {
                _session.ShowLootMessage($"Цель: {CurrentTarget.name}");
            }
        }

        private void HandleAutoAttack()
        {
            _attackCooldown -= Time.deltaTime;
            if (CurrentTarget == null)
            {
                return;
            }

            if (!CurrentTarget.IsAlive)
            {
                CurrentTarget = null;
                return;
            }

            float distance = Vector3.Distance(transform.position, CurrentTarget.transform.position);
            if (distance > AttackRange)
            {
                return;
            }

            var toEnemy = CurrentTarget.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toEnemy), Time.deltaTime * 12f);
            }

            if (_attackCooldown > 0f)
            {
                return;
            }

            _attackCooldown = AttackPeriod;
            CurrentTarget.TakeDamage(Random.Range(1f, 5.1f));
        }

        private EnemyCharacter FindNearestAliveEnemy()
        {
            var enemies = FindObjectsOfType<EnemyCharacter>();
            EnemyCharacter nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearest = enemy;
                    nearestDist = dist;
                }
            }

            return nearest;
        }

        private void HandleConsumables()
        {
            if (IsPotionPressed() && Potions > 0 && Health < MaxHealth)
            {
                Potions--;
                Health = Mathf.Min(MaxHealth, Health + 45f);
                _session.ShowLootMessage("Использовано зелье: +45 HP");
            }
        }

        public void ReceiveDamage(float damage)
        {
            Health -= damage;
            if (Health <= 0f)
            {
                Health = MaxHealth;
                transform.position = new Vector3(0f, 1f, 0f);
                CurrentTarget = null;
                _session.ShowLootMessage("Вы погибли и возродились в лагере");
            }
        }

        public void AddLevelFromKill()
        {
            Level++;
            MaxHealth += 20f;
            Health = MaxHealth;
            Xp = 0;
            _session.ShowLootMessage($"Новый уровень: {Level} (+20 к макс. HP)");
        }

        public void AddGold(int amount) => Gold += amount;

        public void AddPotion(int amount) => Potions += amount;

        public float XpProgress01() => 0f;

        private static Vector2 ReadMoveInput()
        {
            if (Keyboard.current == null)
            {
                return Vector2.zero;
            }

            float horizontal = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float vertical = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);

            horizontal += (Keyboard.current.rightArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.leftArrowKey.isPressed ? 1f : 0f);
            vertical += (Keyboard.current.upArrowKey.isPressed ? 1f : 0f) - (Keyboard.current.downArrowKey.isPressed ? 1f : 0f);

            return new Vector2(Mathf.Clamp(horizontal, -1f, 1f), Mathf.Clamp(vertical, -1f, 1f));
        }

        private static bool IsAttackPressed()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private static bool IsPotionPressed()
        {
            return Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;
        }

        private static Vector2 ReadPointerPosition()
        {
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }
    }
}
