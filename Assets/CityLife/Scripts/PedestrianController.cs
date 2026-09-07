using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// Controls a single pedestrian agent.
    ///
    /// State machine: Walking → Arriving → Idle → Walking.
    /// Walks strictly along sidewalk waypoints (offset ±4.0m from road center).
    /// Features full kinematic procedural locomotion:
    /// - Alternating leg strides synchronized to movement velocity (no foot sliding)
    /// - Counter-swinging arms with natural outward resting angle
    /// - Pelvis vertical bob, lateral weight-shift sway, and forward lean
    /// - Realistic idle breathing and ambient head-turning / looking around
    /// - Seamless Animator parameter integration if a skinned humanoid is attached
    /// </summary>
    public class PedestrianController : MonoBehaviour
    {
        public const string PoolTag = "Pedestrian";

        private const float ArrivalThreshold = 0.35f;
        private const float TurnSpeed        = 360f; // deg/sec
        private const float IdleChance       = 0.20f;

        [Header("Procedural Gait Settings")]
        [SerializeField] private float strideLength     = 0.75f; // meters per full 2-step cycle
        [SerializeField] private float maxLegAngle      = 28.0f; // degrees pitch at stride peak
        [SerializeField] private float maxArmAngle      = 22.0f; // degrees pitch for arm counter-swing
        [SerializeField] private float armRestAngle     = 4.0f;  // outward flare angle to clear torso
        [SerializeField] private float bounceHeight     = 0.035f;// pelvis bounce height per step
        [SerializeField] private float bodyRollAngle    = 2.2f;  // lateral sway towards stance foot
        [SerializeField] private float forwardLeanAngle = 3.5f;  // forward tilt based on walking speed

        [Header("Limb Pivots")]
        [SerializeField] public Transform hipL;
        [SerializeField] public Transform hipR;
        [SerializeField] public Transform shoulderL;
        [SerializeField] public Transform shoulderR;
        [SerializeField] public Transform torso;
        [SerializeField] public Transform head;

        public enum State
        {
            Walking,
            Arriving,
            Idle
        }

        [Header("State")]
        [SerializeField] private State currentState = State.Walking;

        private CityWaypointGraph _graph;
        private System.Random     _rng;
        private float             _speed;

        private SidewalkWaypoint _currentNode;
        private SidewalkWaypoint _previousNode;
        private SidewalkWaypoint _nextNode;

        private bool  _active;
        private float _idleTimer;
        private float _baseY;

        // Procedural animation state
        private float   _gaitPhase;
        private float   _walkWeight;
        private float   _idlePhase;
        private Vector3 _initialTorsoPos = new Vector3(0f, 1.05f, 0f);
        private float   _lookTimer;
        private float   _currentHeadYaw;
        private float   _targetHeadYaw;

        // Animator support
        private Animator _animator;
        private static readonly int SpeedHash       = Animator.StringToHash("Speed");
        private static readonly int WalkingHash     = Animator.StringToHash("IsWalking");
        private static readonly int GroundedHash    = Animator.StringToHash("Grounded");
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");

        private void Awake()
        {
            EnsureLimbPivots();
            _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Self-healing setup: ensures proper hip and shoulder joint pivots exist
        /// so limbs rotate around hip sockets and shoulders rather than limb centers.
        /// </summary>
        public void EnsureLimbPivots()
        {
            if (torso == null)
            {
                var t = transform.Find("Torso");
                if (t != null)
                {
                    torso = t;
                    _initialTorsoPos = torso.localPosition;
                }
            }
            else
            {
                _initialTorsoPos = torso.localPosition;
            }

            if (head == null) head = transform.Find("Head");

            // Left Hip
            if (hipL == null)
            {
                hipL = transform.Find("Hip_L");
                if (hipL == null)
                {
                    var leg = transform.Find("Leg_L");
                    if (leg != null)
                    {
                        var hipObj = new GameObject("Hip_L");
                        hipObj.transform.SetParent(transform, false);
                        float topY = leg.localPosition.y + leg.localScale.y * 0.5f;
                        hipObj.transform.localPosition = new Vector3(leg.localPosition.x, topY, leg.localPosition.z);
                        leg.SetParent(hipObj.transform, true);
                        hipL = hipObj.transform;
                    }
                }
            }

            // Right Hip
            if (hipR == null)
            {
                hipR = transform.Find("Hip_R");
                if (hipR == null)
                {
                    var leg = transform.Find("Leg_R");
                    if (leg != null)
                    {
                        var hipObj = new GameObject("Hip_R");
                        hipObj.transform.SetParent(transform, false);
                        float topY = leg.localPosition.y + leg.localScale.y * 0.5f;
                        hipObj.transform.localPosition = new Vector3(leg.localPosition.x, topY, leg.localPosition.z);
                        leg.SetParent(hipObj.transform, true);
                        hipR = hipObj.transform;
                    }
                }
            }

            // Left Shoulder
            if (shoulderL == null)
            {
                shoulderL = transform.Find("Shoulder_L");
                if (shoulderL == null)
                {
                    var arm = transform.Find("Arm_L");
                    if (arm != null)
                    {
                        var shldrObj = new GameObject("Shoulder_L");
                        shldrObj.transform.SetParent(transform, false);
                        float topY = arm.localPosition.y + arm.localScale.y * 0.5f;
                        shldrObj.transform.localPosition = new Vector3(arm.localPosition.x, topY, arm.localPosition.z);
                        arm.SetParent(shldrObj.transform, true);
                        shoulderL = shldrObj.transform;
                    }
                }
            }

            // Right Shoulder
            if (shoulderR == null)
            {
                shoulderR = transform.Find("Shoulder_R");
                if (shoulderR == null)
                {
                    var arm = transform.Find("Arm_R");
                    if (arm != null)
                    {
                        var shldrObj = new GameObject("Shoulder_R");
                        shldrObj.transform.SetParent(transform, false);
                        float topY = arm.localPosition.y + arm.localScale.y * 0.5f;
                        shldrObj.transform.localPosition = new Vector3(arm.localPosition.x, topY, arm.localPosition.z);
                        arm.SetParent(shldrObj.transform, true);
                        shoulderR = shldrObj.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Called by CityLifeManager each time a pedestrian is taken from the pool.
        /// Places the pedestrian directly on the sidewalk waypoint.
        /// </summary>
        public void Activate(CityWaypointGraph graph, System.Random rng,
                             SidewalkWaypoint startNode, SidewalkWaypoint firstNext,
                             float speed)
        {
            EnsureLimbPivots();
            _animator = GetComponentInChildren<Animator>();

            _graph        = graph;
            _rng          = rng;
            _speed        = speed;
            _currentNode  = startNode;
            _previousNode = null;
            _nextNode     = firstNext;
            _idleTimer    = 0f;
            _idlePhase    = (float)rng.NextDouble() * Mathf.PI * 2f;
            _gaitPhase    = (float)rng.NextDouble() * Mathf.PI * 2f;
            _walkWeight   = 1f;
            _lookTimer    = 0f;
            _currentHeadYaw = 0f;
            _targetHeadYaw  = 0f;
            _active       = true;
            currentState  = State.Walking;

            // Spawn directly ON THE SIDEWALK
            Vector3 spawnPos = startNode.Position;
            spawnPos.y       = 0f;
            _baseY           = 0f;
            transform.position = spawnPos;

            if (_nextNode != null)
            {
                var dir = _nextNode.Position - spawnPos;
                dir.y   = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }

        private void Update()
        {
            if (!_active || _nextNode == null) return;

            Vector3 prePos = transform.position;

            switch (currentState)
            {
                case State.Idle:
                    UpdateIdle();
                    break;

                case State.Walking:
                    UpdateWalking();
                    break;

                case State.Arriving:
                    UpdateArriving();
                    break;
            }

            // Calculate actual movement delta in horizontal plane
            Vector3 posDelta = transform.position - prePos;
            posDelta.y = 0f;
            float moveDist = posDelta.magnitude;
            float currentSpeed = Time.deltaTime > 0.0001f ? moveDist / Time.deltaTime : 0f;

            // Animate limbs, torso, and head based on actual movement
            AnimateMovement(currentSpeed, moveDist);
        }

        private void UpdateIdle()
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                currentState = State.Walking;
            }
        }

        private void UpdateWalking()
        {
            Vector3 target = _nextNode.Position;
            target.y = _baseY;

            // Move towards target waypoint
            transform.position = Vector3.MoveTowards(transform.position, target, _speed * Time.deltaTime);

            // Steer smoothly towards target
            Vector3 moveDir = target - transform.position;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.001f)
            {
                var targetRot = Quaternion.LookRotation(moveDir.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
            }

            // Arrival check
            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                          new Vector3(target.x, 0, target.z));
            if (dist < ArrivalThreshold)
            {
                currentState = State.Arriving;
            }
        }

        private void UpdateArriving()
        {
            _previousNode = _currentNode;
            _currentNode  = _nextNode;

            // Decide whether to pause and idle
            if (_rng.NextDouble() < IdleChance)
            {
                currentState = State.Idle;
                _idleTimer   = (float)(_rng.NextDouble() * 3.0 + 1.2); // 1.2 to 4.2 seconds
            }
            else
            {
                currentState = State.Walking;
            }

            // Pick next sidewalk waypoint
            _nextNode = _graph.GetNextSidewalkWaypoint(_currentNode, _previousNode, _rng);

            if (_nextNode == null && _previousNode != null)
            {
                // Fallback: reverse
                _nextNode = _previousNode;
            }
        }

        private const float ProceduralAnimLODSqrDist = 35f * 35f; // 35 meters

        /// <summary>
        /// Computes and applies full procedural locomotion poses to limbs, torso, and head,
        /// and updates any attached Animator component.
        /// </summary>
        private void AnimateMovement(float currentSpeed, float moveDist)
        {
            bool isMoving = currentState == State.Walking && moveDist > 0.0001f;

            // Advance gait cycle strictly based on distance traveled (zero foot-sliding)
            if (isMoving && strideLength > 0.05f)
            {
                _gaitPhase += (moveDist / strideLength) * Mathf.PI * 2f;
            }

            // Smooth cross-fade between walking and idle (transition time ~0.2s)
            _walkWeight = Mathf.MoveTowards(_walkWeight, isMoving ? 1f : 0f, Time.deltaTime * 5f);

            // ── ANIMATOR INTEGRATION (If rigged humanoid attached) ──
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, isMoving ? currentSpeed : 0f);
                _animator.SetBool(WalkingHash, isMoving);
                _animator.SetBool(GroundedHash, true);
                _animator.SetFloat(MotionSpeedHash, 1f);
            }

            // LOD Check: Skip heavy bone trigonometry if far from camera
            Transform camTr = LookAtCamera.MainCameraTransform;
            if (camTr != null)
            {
                float sqrDist = (transform.position - camTr.position).sqrMagnitude;
                if (sqrDist > ProceduralAnimLODSqrDist)
                {
                    return; // Distant pedestrian: navigation continues, but skip fine bone math
                }
            }

            // ── 1. LEGS (Alternating pitch from hip sockets) ──
            float legPitch = Mathf.Sin(_gaitPhase) * maxLegAngle * _walkWeight;
            // Slight knee lift / ground clearance when swinging forward
            float liftL = Mathf.Max(0f, Mathf.Cos(_gaitPhase)) * 7f * _walkWeight;
            float liftR = Mathf.Max(0f, -Mathf.Cos(_gaitPhase)) * 7f * _walkWeight;

            if (hipL != null)
                hipL.localRotation = Quaternion.Euler(legPitch + liftL, 0f, 0f);

            if (hipR != null)
                hipR.localRotation = Quaternion.Euler(-legPitch + liftR, 0f, 0f);

            // ── 2. ARMS (Counter-swing in opposition to legs) ──
            float armPitch = -Mathf.Sin(_gaitPhase) * maxArmAngle * _walkWeight;
            float idleArmSway = Mathf.Sin(Time.time * 1.6f + _idlePhase) * 2.5f * (1f - _walkWeight);

            if (shoulderL != null)
                shoulderL.localRotation = Quaternion.Euler(armPitch + idleArmSway, 0f, -armRestAngle);

            if (shoulderR != null)
                shoulderR.localRotation = Quaternion.Euler(-armPitch + idleArmSway, 0f, armRestAngle);

            // ── 3. TORSO (Pelvis bounce, lateral sway & forward lean) ──
            // Double-frequency vertical bounce (one bounce per step)
            float bounce = Mathf.Abs(Mathf.Sin(_gaitPhase)) * bounceHeight * _walkWeight;
            // Idle breathing rise
            float idleBreathe = Mathf.Sin(Time.time * 2.0f + _idlePhase) * 0.006f * (1f - _walkWeight);
            // Lateral body sway roll towards stance foot
            float roll = Mathf.Cos(_gaitPhase) * bodyRollAngle * _walkWeight;
            // Forward lean proportional to speed
            float lean = Mathf.Clamp(currentSpeed / 1.5f, 0f, 1.5f) * forwardLeanAngle * _walkWeight;

            if (torso != null)
            {
                torso.localPosition = _initialTorsoPos + new Vector3(0f, bounce + idleBreathe, 0f);
                torso.localRotation = Quaternion.Euler(lean, 0f, roll);
            }

            // ── 4. HEAD (Looking around during Idle, centered during Walk) ──
            if (head != null)
            {
                if (currentState == State.Idle)
                {
                    _lookTimer -= Time.deltaTime;
                    if (_lookTimer <= 0f)
                    {
                        _lookTimer = (float)(_rng.NextDouble() * 2.5 + 1.2);
                        // Turn head -30° to +30° to observe environment
                        _targetHeadYaw = (float)(_rng.NextDouble() * 60.0 - 30.0);
                    }
                }
                else
                {
                    _targetHeadYaw = 0f;
                }

                _currentHeadYaw = Mathf.MoveTowardsAngle(_currentHeadYaw, _targetHeadYaw, 100f * Time.deltaTime);
                head.localRotation = Quaternion.Euler(0f, _currentHeadYaw, 0f);
            }
                _animator.SetBool(GroundedHash, true);
                _animator.SetFloat(MotionSpeedHash, currentSpeed > 0.01f ? currentSpeed / 1.2f : 1f);
            }
        }

        private static MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        /// <summary>Tint body renderer for visual variety without leaking materials.</summary>
        public void SetBodyColor(Color color)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r == null) continue;
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, color);
                _propBlock.SetColor(ColorId, color);
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
