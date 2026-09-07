using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// Controls a single vehicle agent.
    ///
    /// Drives strictly along directed lane waypoints (offset ±1.5m, right-hand traffic).
    /// Features:
    /// - Smooth acceleration and realistic deceleration/braking physics
    /// - SphereCast obstacle & pedestrian detection with polite yielding
    /// - Dynamic front-wheel steering angle (turns wheels into corners)
    /// - Active brake lights emission (brightens on deceleration/stop)
    /// - Procedural wheel spin matching velocity
    /// </summary>
    public class CarController : MonoBehaviour
    {
        public const string PoolTag = "Car";

        private const float ArrivalThreshold    = 0.7f;
        private const float ObstacleCheckDist   = 3.5f;
        private const float StopDistance        = 1.8f;
        private const float Acceleration        = 8.0f;   // m/s^2
        private const float BrakingRate         = 16.0f;  // m/s^2
        private const float TurnSpeed           = 260f;   // deg/sec
        private const float MaxSteerAngle       = 30f;    // degrees
        private const float SteerReturnRate     = 140f;   // deg/sec
        private const float WheelSpinMultiplier = 50f;

        // Anti-deadlock thresholds
        private const float DeadlockCrawlThreshold = 2.5f; // Seconds stopped before forcing clearance
        private const float DespawnFailsafeThreshold = 8.0f; // Seconds wedged before despawning

        [Header("Visuals")]
        public Renderer bodyRenderer;
        public Renderer[] taillights;

        [Header("Wheels")]
        public Transform wheelFL;
        public Transform wheelFR;
        public Transform wheelRL;
        public Transform wheelRR;

        private CityWaypointGraph _graph;
        private System.Random     _rng;
        private float             _baseSpeed;
        private float             _targetSpeed;
        private float             _currentSpeed;
        private string            _poolTag = PoolTag;

        private LaneWaypoint _currentNode;
        private LaneWaypoint _previousNode;
        private LaneWaypoint _nextNode;

        private bool _active;
        private float _wheelSpinAngle;
        private float _currentSteerAngle;
        private bool _isBraking;
        private float _stoppedTimer;

        public float CurrentSpeed => _currentSpeed;
        public float StoppedTimer => _stoppedTimer;

        private static MaterialPropertyBlock _taillightBlock;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly Color BrakeLightCruise = new Color(0.65f, 0.05f, 0.05f) * 0.9f;
        private static readonly Color BrakeLightActive = new Color(1.00f, 0.08f, 0.08f) * 3.8f;

        private void Awake()
        {
            // Auto-discover taillight renderers if not explicitly assigned in inspector
            if (taillights == null || taillights.Length == 0)
            {
                var list = new System.Collections.Generic.List<Renderer>();
                var t1 = transform.Find("Taillight_L");
                if (t1 != null) list.Add(t1.GetComponent<Renderer>());
                var t2 = transform.Find("Taillight_R");
                if (t2 != null) list.Add(t2.GetComponent<Renderer>());
                taillights = list.ToArray();
            }
        }

        /// <summary>
        /// Called by CityLifeManager each time a car is taken from the pool.
        /// Places the car directly in the driving lane.
        /// </summary>
        public void Activate(CityWaypointGraph graph, System.Random rng,
                             LaneWaypoint startNode, LaneWaypoint firstNext,
                             float speed, string poolTag = PoolTag)
        {
            _graph        = graph;
            _rng          = rng;
            _poolTag      = string.IsNullOrEmpty(poolTag) ? PoolTag : poolTag;
            _baseSpeed    = speed;
            _targetSpeed  = speed;
            _currentSpeed = speed * 0.5f; // start with slight initial rollout
            _currentNode  = startNode;
            _previousNode = null;
            _nextNode     = firstNext;
            _active       = true;
            _currentSteerAngle = 0f;
            _isBraking    = false;
            _stoppedTimer = 0f;

            // Spawn directly in the driving lane
            Vector3 spawnPos = startNode.Position;
            spawnPos.y       = 0f;
            transform.position = spawnPos;

            if (_nextNode != null)
            {
                var dir = _nextNode.Position - spawnPos;
                dir.y   = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

            SetBrakeLights(false);
        }

        private void Update()
        {
            if (!_active || _nextNode == null) return;

            // Track stopped / stuck duration
            if (_currentSpeed < 0.2f && _targetSpeed <= 0.1f)
            {
                _stoppedTimer += Time.deltaTime;
                if (_stoppedTimer >= DespawnFailsafeThreshold)
                {
                    // Despawn failsafe: if permanently wedged for > 8s, recycle to pool
                    ReturnToPool();
                    return;
                }
            }
            else if (_currentSpeed > 0.3f)
            {
                _stoppedTimer = Mathf.Max(0f, _stoppedTimer - Time.deltaTime * 2f);
            }

            AdjustSpeedForObstacles();
            UpdateSmoothVelocity();
            MoveTowardTarget();
            SteerTowardTarget();
            UpdateWheelVisuals();

            Vector3 target = _nextNode.Position;
            target.y = 0f;
            float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), target);

            if (dist < ArrivalThreshold)
            {
                OnNodeArrived();
            }
        }

        private static readonly RaycastHit[] _sphereCastBuffer = new RaycastHit[12];
        private const float WheelLODSqrDist = 45f * 45f; // 45 meters

        /// <summary>
        /// SphereCast ahead to detect lead vehicles, crossing pedestrians, or players and compute target speed.
        /// Includes cross-traffic direction filtering and anti-deadlock clearance.
        /// </summary>
        private void AdjustSpeedForObstacles()
        {
            // Deadlock resolution: if stopped > 2.5s, force a crawl to clear the gridlock
            bool isResolvingDeadlock = _stoppedTimer >= DeadlockCrawlThreshold;

            Vector3 origin = transform.position + Vector3.up * 0.75f + transform.forward * 1.5f;
            float checkDist = isResolvingDeadlock ? (ObstacleCheckDist * 0.5f) : ObstacleCheckDist;

            int hitCount = Physics.SphereCastNonAlloc(origin, 0.35f, transform.forward, _sphereCastBuffer, checkDist);
            float closestDist = float.MaxValue;
            bool foundObstacle = false;

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _sphereCastBuffer[i];
                if (hit.transform == transform || hit.transform.IsChildOf(transform) ||
                    hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }
                if (hit.collider.isTrigger) continue;

                // Ignore road meshes, ground, terrain, or sidewalk tiles
                string hitName = hit.collider.gameObject.name;
                if (hitName.StartsWith("Road") || hitName.StartsWith("Env_Road") || 
                    hitName.StartsWith("Ground") || hitName.StartsWith("Terrain") ||
                    hitName.StartsWith("Sidewalk"))
                {
                    continue;
                }

                // Cross-traffic & Perpendicular Car Filtering
                var otherCar = hit.collider.GetComponentInParent<CarController>();
                if (otherCar != null && otherCar != this)
                {
                    float dot = Vector3.Dot(transform.forward, otherCar.transform.forward);

                    // If perpendicular / crossing traffic (dot around 0)
                    if (Mathf.Abs(dot) < 0.45f)
                    {
                        // If the other car is stopped and we are resolving deadlock (or we waited longer / tiebreaker), don't halt
                        if (otherCar.CurrentSpeed < 0.2f)
                        {
                            if (isResolvingDeadlock || _stoppedTimer >= otherCar.StoppedTimer)
                            {
                                // Only stop if within point-blank physical collision distance (< 0.8m)
                                if (hit.distance > 0.8f) continue;
                            }
                        }
                    }
                    // If oncoming opposite traffic (dot < -0.6f), ignore if offset sideways
                    else if (dot < -0.6f)
                    {
                        Vector3 toOther = otherCar.transform.position - transform.position;
                        float lateralOffset = Mathf.Abs(Vector3.Dot(transform.right, toOther));
                        if (lateralOffset > 1.2f)
                        {
                            continue; // In opposite lane, safe to pass
                        }
                    }
                }

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    foundObstacle = true;
                }
            }

            if (foundObstacle)
            {
                if (isResolvingDeadlock)
                {
                    // Crawl forward cautiously to clear intersection
                    _targetSpeed = _baseSpeed * 0.35f;
                    return;
                }

                if (closestDist <= StopDistance)
                {
                    _targetSpeed = 0f;
                }
                else
                {
                    float factor = Mathf.Clamp01((closestDist - StopDistance) / (ObstacleCheckDist - StopDistance));
                    _targetSpeed = _baseSpeed * factor;
                }
                return;
            }

            _targetSpeed = isResolvingDeadlock ? (_baseSpeed * 0.6f) : _baseSpeed;
        }

        /// <summary>
        /// Smoothly accelerates or brakes toward target velocity without jarring speed snaps.
        /// </summary>
        private void UpdateSmoothVelocity()
        {
            bool wantsBrake = (_targetSpeed < _currentSpeed - 0.5f) || (_targetSpeed <= 0.1f && _currentSpeed > 0.1f);
            float rate = wantsBrake ? BrakingRate : Acceleration;

            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, rate * Time.deltaTime);

            // Update brake lights emission
            bool brakingNow = wantsBrake || _currentSpeed < _baseSpeed * 0.4f;
            if (brakingNow != _isBraking)
            {
                _isBraking = brakingNow;
                SetBrakeLights(_isBraking);
            }
        }

        private void MoveTowardTarget()
        {
            if (_currentSpeed <= 0.001f) return;

            Vector3 target = _nextNode.Position;
            target.y = 0f;
            transform.position = Vector3.MoveTowards(transform.position, target, _currentSpeed * Time.deltaTime);
        }

        private void SteerTowardTarget()
        {
            Vector3 dir = _nextNode.Position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            // Vehicle heading steering
            var targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, TurnSpeed * Time.deltaTime);

            // Calculate front wheel turn angle relative to car heading
            float signedAngle = Vector3.SignedAngle(transform.forward, dir.normalized, Vector3.up);
            float desiredSteer = Mathf.Clamp(signedAngle, -MaxSteerAngle, MaxSteerAngle);
            _currentSteerAngle = Mathf.MoveTowards(_currentSteerAngle, desiredSteer, SteerReturnRate * Time.deltaTime);
        }

        /// <summary>
        /// Spins all wheels according to forward travel and yaws front wheels according to steer angle.
        /// </summary>
        private void UpdateWheelVisuals()
        {
            Transform camTr = LookAtCamera.MainCameraTransform;
            if (camTr != null)
            {
                float sqrDist = (transform.position - camTr.position).sqrMagnitude;
                if (sqrDist > WheelLODSqrDist) return; // Skip wheel mesh rotations when far from camera
            }

            _wheelSpinAngle = (_wheelSpinAngle + _currentSpeed * Time.deltaTime * WheelSpinMultiplier) % 360f;

            // Front wheels turn with steering angle
            if (wheelFL != null) wheelFL.localRotation = Quaternion.Euler(_wheelSpinAngle, _currentSteerAngle, 0f);
            if (wheelFR != null) wheelFR.localRotation = Quaternion.Euler(_wheelSpinAngle, _currentSteerAngle, 0f);

            // Rear wheels remain straight
            if (wheelRL != null) wheelRL.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
            if (wheelRR != null) wheelRR.localRotation = Quaternion.Euler(_wheelSpinAngle, 0f, 0f);
        }

        private void SetBrakeLights(bool active)
        {
            if (taillights == null || taillights.Length == 0) return;
            if (_taillightBlock == null) _taillightBlock = new MaterialPropertyBlock();

            Color c = active ? BrakeLightActive : BrakeLightCruise;
            foreach (var r in taillights)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_taillightBlock);
                _taillightBlock.SetColor(EmissionColorId, c);
                r.SetPropertyBlock(_taillightBlock);
            }
        }

        private void OnNodeArrived()
        {
            _previousNode = _currentNode;
            _currentNode  = _nextNode;

            // Dead-end reached → disappear and return to pool
            if (_currentNode.IsDeadEnd)
            {
                ReturnToPool();
                return;
            }

            _nextNode = _graph.GetNextLaneWaypoint(_currentNode, _previousNode, _rng);

            if (_nextNode == null)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            _active = false;
            CityObjectPool.Instance?.Return(string.IsNullOrEmpty(_poolTag) ? PoolTag : _poolTag, gameObject);
            CityLifeManager.Instance?.OnCarReturnedToPool();
        }

        private static MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId     = Shader.PropertyToID("_Color");

        /// <summary>Tint the car body renderer without leaking materials.</summary>
        public void SetBodyColor(Color color)
        {
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            var r = bodyRenderer != null ? bodyRenderer : GetComponent<Renderer>();
            if (r == null) r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, color);
                _propBlock.SetColor(ColorId, color);
                r.SetPropertyBlock(_propBlock);
            }
        }
    }
}
