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
        private const float ObstacleCheckDist   = 5.5f;
        private const float StopDistance        = 2.8f;
        private const float Acceleration        = 8.0f;   // m/s^2
        private const float BrakingRate         = 16.0f;  // m/s^2
        private const float TurnSpeed           = 260f;   // deg/sec
        private const float MaxSteerAngle       = 30f;    // degrees
        private const float SteerReturnRate     = 140f;   // deg/sec
        private const float WheelSpinMultiplier = 50f;

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

        /// <summary>
        /// SphereCast ahead to detect lead vehicles, crossing pedestrians, or players and compute target speed.
        /// Ignores road meshes, curbs, ground, and terrain colliders.
        /// </summary>
        private void AdjustSpeedForObstacles()
        {
            Vector3 origin = transform.position + Vector3.up * 0.75f + transform.forward * 2.35f;
            float checkDist = ObstacleCheckDist;

            var hits = Physics.SphereCastAll(origin, 0.45f, transform.forward, checkDist);
            float closestDist = float.MaxValue;
            bool foundObstacle = false;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
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

                if (hit.distance < closestDist)
                {
                    closestDist = hit.distance;
                    foundObstacle = true;
                }
            }

            if (foundObstacle)
            {
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

            _targetSpeed = _baseSpeed;
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
