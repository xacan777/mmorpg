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
        private const float JumpHeight = 1.6f;
        private const float TurnSpeed = 140f;

        private float _verticalVelocity;
        private bool _hasMovePoint;
        private Vector3 _movePoint;
        private bool _autoRunForward;
        private Vector3 _planarVelocity;
        private Animator _animator;
        private bool _animatorWarningShown;

        private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
        private static readonly int AnimGroundedHash = Animator.StringToHash("Grounded");
        private static readonly int AnimAttackHash = Animator.StringToHash("Attack");

        public void Initialize(GameSession session)
        {
            _session = session;
            _controller = GetComponent<CharacterController>();
            _controller.height = 2f;
            _controller.radius = 0.45f;
            _controller.center = new Vector3(0f, 1f, 0f);

            _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.runtimeAnimatorController == null && !_animatorWarningShown)
            {
                _animatorWarningShown = true;
                _session.ShowLootMessage("Animator найден, но не назначен Controller (Idle/Run/Attack)");
            }
        }

        private void Update()
        {
            HandleRotationInput();
            HandleTargetingAndMoveClick();
            HandleMovement();
            HandleAutoAttack();
            HandleConsumables();
            UpdateAnimatorState();
        }

        private void HandleRotationInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            float turn = 0f;
            if (Keyboard.current.leftArrowKey.isPressed)
            {
                turn -= 1f;
            }
            if (Keyboard.current.rightArrowKey.isPressed)
            {
                turn += 1f;
            }

            if (!Mathf.Approximately(turn, 0f))
            {
                transform.Rotate(0f, turn * TurnSpeed * Time.deltaTime, 0f, Space.World);
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                _autoRunForward = !_autoRunForward;
                _session.ShowLootMessage(_autoRunForward ? "Автобег: ВКЛ" : "Автобег: ВЫКЛ");
            }
        }

        private void HandleMovement()
        {
            Vector3 desiredPlanar = GetManualMovementWorldDirection();

            if (desiredPlanar.sqrMagnitude > 0.001f)
            {
                _hasMovePoint = false;
                _autoRunForward = false;
            }
            else
            {
                desiredPlanar = GetAutoMoveDirection();
            }

            if (desiredPlanar.sqrMagnitude > 1f)
            {
                desiredPlanar.Normalize();
            }

            desiredPlanar *= Speed;

            if (_controller.isGrounded)
            {
                _planarVelocity = desiredPlanar;
                _verticalVelocity = -1f;
                if (IsJumpPressed())
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                }
            }
            else
            {
                if (desiredPlanar.sqrMagnitude > 0.01f)
                {
                    _planarVelocity = Vector3.Lerp(_planarVelocity, desiredPlanar, Time.deltaTime * 2.2f);
                }
                _verticalVelocity += Gravity * Time.deltaTime;
            }

            var move = _planarVelocity;
            move.y = _verticalVelocity;
            _controller.Move(move * Time.deltaTime);

            Vector3 face = new Vector3(_planarVelocity.x, 0f, _planarVelocity.z);
            if (face.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), Time.deltaTime * 12f);
            }
        }

        private Vector3 GetManualMovementWorldDirection()
        {
            if (Keyboard.current == null)
            {
                return Vector3.zero;
            }

            var cam = Camera.main;
            Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
            Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 dir = Vector3.zero;

            if (Keyboard.current.wKey.isPressed)
            {
                dir += camForward;
            }
            if (Keyboard.current.sKey.isPressed)
            {
                dir -= camForward;
            }
            if (Keyboard.current.aKey.isPressed)
            {
                dir -= camRight;
            }
            if (Keyboard.current.dKey.isPressed)
            {
                dir += camRight;
            }

            if (Keyboard.current.upArrowKey.isPressed)
            {
                dir += transform.forward;
            }
            if (Keyboard.current.downArrowKey.isPressed)
            {
                dir -= transform.forward;
            }

            return dir.normalized;
        }

        private Vector3 GetAutoMoveDirection()
        {
            if (_hasMovePoint)
            {
                var toPoint = _movePoint - transform.position;
                toPoint.y = 0f;
                if (toPoint.sqrMagnitude < 0.6f * 0.6f)
                {
                    _hasMovePoint = false;
                    return Vector3.zero;
                }

                return toPoint.normalized;
            }

            if (_autoRunForward)
            {
                return GetCameraForwardOnPlane();
            }

            if (CurrentTarget == null || !CurrentTarget.IsAlive)
            {
                return Vector3.zero;
            }

            var toEnemy = CurrentTarget.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude <= AttackRange * AttackRange)
            {
                return Vector3.zero;
            }

            return toEnemy.normalized;
        }


        private static Vector3 GetCameraForwardOnPlane()
        {
            var cam = Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private void HandleTargetingAndMoveClick()
        {
            if (!IsLeftClickPressed())
            {
                return;
            }

            var currentCamera = Camera.main;
            if (currentCamera == null)
            {
                return;
            }

            if (!Physics.Raycast(currentCamera.ScreenPointToRay(ReadPointerPosition()), out var hit, 500f))
            {
                CurrentTarget = null;
                _hasMovePoint = false;
                return;
            }

            var enemyByRay = hit.collider.GetComponentInParent<EnemyCharacter>();
            if (enemyByRay != null && enemyByRay.IsAlive)
            {
                CurrentTarget = enemyByRay;
                _movePoint = enemyByRay.transform.position;
                _hasMovePoint = true;
                _session.ShowLootMessage($"Цель: {CurrentTarget.name}");
                return;
            }

            CurrentTarget = null;
            _movePoint = hit.point;
            _movePoint.y = transform.position.y;
            _hasMovePoint = true;
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
            _animator?.SetTrigger(AnimAttackHash);
            CurrentTarget.TakeDamage(Random.Range(1f, 5.1f));
        }

        private void UpdateAnimatorState()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
                if (_animator == null)
                {
                    return;
                }
            }

            float speed01 = Mathf.Clamp01(new Vector3(_planarVelocity.x, 0f, _planarVelocity.z).magnitude / Speed);
            _animator.SetFloat(AnimSpeedHash, speed01, 0.1f, Time.deltaTime);
            _animator.SetBool(AnimGroundedHash, _controller.isGrounded);
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
                transform.position = _session.GetSpawnPosition();
                CurrentTarget = null;
                _hasMovePoint = false;
                _session.ShowLootMessage("Вы погибли и возродились в лагере");
            }
        }

        public void AddXp(int amount)
        {
            Xp += amount;
            int need = Level * 100;
            while (Xp >= need)
            {
                Xp -= need;
                Level++;
                MaxHealth += 20f;
                Health = MaxHealth;
                _session.ShowLootMessage($"Новый уровень: {Level} (+20 к макс. HP)");
                need = Level * 100;
            }
        }

        public void AddGold(int amount) => Gold += amount;

        public void AddPotion(int amount) => Potions += amount;

        public float XpProgress01()
        {
            return Mathf.Clamp01(Xp / (float)(Level * 100));
        }

        private static bool IsLeftClickPressed()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private static bool IsJumpPressed()
        {
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
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
