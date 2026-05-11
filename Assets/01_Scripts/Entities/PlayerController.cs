using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Drive (WheelCollider 스타일 파라미터)")]
    public float cruiseSpeed = 10f;
    public float maxForwardSpeed = 26f;
    public float acceleration = 18f;
    public float velocityBlend = 12f;
    public float minimumAutoForwardSpeed = 12f;
    public float minimumAutoAcceleration = 10f;

    [Header("Steer")]
    public float steeringInputDeadZone = 0.03f;
    public float maxSteerAngle = 45f;
    public float steerResponse = 140f;
    public float steerYawMultiplier = 1.9f;
    public float steerSpeedReference = 20f;
    public AnimationCurve steerBySpeed = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f));
    public bool limitHeadingToForward = true;
    [Range(1f, 89f)] public float maxHeadingOffsetFromStart = 80f;

    [Header("Drift / Grip")]
    public bool autoDriftBySteer = true;
    public float autoDriftSteerThreshold = 0.45f;
    public float driftLateralGrip = 2.6f;
    public float normalLateralGrip = 12f;
    public float driftLateralVelocity = 7f;
    public float driftForwardBoost = 2.4f;

    [Header("Ground Follow")]
    public float groundCheckDistance = 1.6f;
    public float groundProbeUpOffset = 0.25f;
    public float groundStickForce = 0.5f;
    public float verticalVelocityBlend = 8f;
    public LayerMask groundLayerMask = ~0;
    public bool suppressSeamBounce = true;
    [Min(0f)] public float groundedMaxRiseSpeed = 0.15f;
    [Min(0f)] public float groundedRiseAllowance = 0.08f;
    [Min(0.1f)] public float maxDepenetrationSpeed = 1.2f;
    [Range(0.05f, 1f)] public float groundNormalLerp = 0.35f;
    [Min(0f)] public float desiredGroundClearance = 0.08f;
    [Min(0f)] public float clearanceAdjustStrength = 14f;
    [Min(0f)] public float maxClearanceRiseSpeed = 0.35f;

    [Header("Pitch Align")]
    public bool alignPitchToRoad = true;
    public float pitchSampleDistance = 1.25f;
    public float pitchRayStartHeight = 1.0f;
    public float pitchRayDistance = 2.6f;
    public float maxPitchAngle = 25f;
    public float rotationLerpSpeed = 12f;
    public float neutralSnapAngle = 0.08f;

    [Header("Step Assist")]
    public bool enableStepAssist = false;
    public float stepHeight = 0.35f;
    public float stepCheckForwardDistance = 0.9f;
    public float stepCheckLowerHeight = 0.08f;
    public float stepAssistSpeed = 4f;
    public float minStepForwardSpeed = 3f;
    public LayerMask stepLayerMask = ~0;

    private Rigidbody _rb;
    private Collider _bodyCollider;
    private float _yawAngle;
    private float _steerAngle;
    private float _initialYawAngle;
    private Vector3 _smoothedGroundNormal = Vector3.up;
    private float _currentGroundClearance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _bodyCollider = GetComponent<Collider>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.maxDepenetrationVelocity = maxDepenetrationSpeed;

        if (_bodyCollider != null)
        {
            PhysicMaterial noBounceMaterial = _bodyCollider.material;
            if (noBounceMaterial == null)
            {
                noBounceMaterial = new PhysicMaterial("Player_NoBounce");
                _bodyCollider.material = noBounceMaterial;
            }

            noBounceMaterial.bounciness = 0f;
            noBounceMaterial.bounceCombine = PhysicMaterialCombine.Minimum;
        }

        _yawAngle = transform.eulerAngles.y;
        _initialYawAngle = _yawAngle;
        _smoothedGroundNormal = Vector3.up;
        _currentGroundClearance = desiredGroundClearance;

        // 경사 피치 정렬을 위해 Roll만 고정하고 Pitch는 허용
        RigidbodyConstraints constraints = _rb.constraints;
        constraints |= RigidbodyConstraints.FreezeRotationZ;
        constraints &= ~RigidbodyConstraints.FreezeRotationX;
        _rb.constraints = constraints;
    }

    private void Start()
    {
        ApplyInitialForwardSpeed();
    }

    private void ApplyInitialForwardSpeed()
    {
        float initialForwardSpeed = Mathf.Max(maxForwardSpeed, minimumAutoForwardSpeed);
        Quaternion yawRotation = Quaternion.Euler(0f, _yawAngle, 0f);
        Vector3 forwardDir = yawRotation * Vector3.forward;
        _rb.velocity = (forwardDir * initialForwardSpeed) + (Vector3.up * _rb.velocity.y);
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        float steerInput = GetSteerInput();
        bool driftInput = autoDriftBySteer && Mathf.Abs(steerInput) >= autoDriftSteerThreshold;

        bool hasGround = TryGetGroundNormal(out Vector3 groundNormal);
        ApplyInitialForwardSpeed();
        UpdateSteering(steerInput);
        ApplyVelocity(steerInput, driftInput, hasGround, groundNormal);
        ApplyRotation(steerInput, hasGround, groundNormal);
        ApplyStepAssist();
    }
    private float GetSteerInput()
    {
        float slideInput = InputManager.Instance != null ? InputManager.Instance.SlideDirection : 0f;

        float steerInput = slideInput;
        if (Mathf.Abs(steerInput) < steeringInputDeadZone) steerInput = 0f;
        return steerInput;
    }

    private void UpdateSteering(float steerInput)
    {
        float speed = Mathf.Abs(Vector3.Dot(_rb.velocity, transform.forward));
        float speed01 = Mathf.Clamp01(speed / Mathf.Max(steerSpeedReference, 0.01f));
        float speedFactor = Mathf.Clamp(steerBySpeed.Evaluate(speed01), 0.15f, 1f);

        float targetSteerAngle = steerInput * maxSteerAngle;
        _steerAngle = Mathf.MoveTowards(_steerAngle, targetSteerAngle, steerResponse * Time.fixedDeltaTime);

        float yawDelta = _steerAngle * speedFactor * steerYawMultiplier * Time.fixedDeltaTime;
        if (Mathf.Abs(steerInput) < steeringInputDeadZone && Mathf.Abs(yawDelta) < 0.0001f) yawDelta = 0f;
        _yawAngle += yawDelta;

        if (limitHeadingToForward)
        {
            float relativeYaw = Mathf.DeltaAngle(_initialYawAngle, _yawAngle);
            relativeYaw = Mathf.Clamp(relativeYaw, -maxHeadingOffsetFromStart, maxHeadingOffsetFromStart);
            _yawAngle = _initialYawAngle + relativeYaw;
        }
    }

    private void ApplyVelocity(float steerInput, bool driftInput, bool hasGround, Vector3 groundNormal)
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yawAngle, 0f);
        Vector3 forwardDir = yawRotation * Vector3.forward;
        Vector3 rightDir = yawRotation * Vector3.right;

        if (hasGround)
        {
            forwardDir = Vector3.ProjectOnPlane(forwardDir, groundNormal).normalized;
            rightDir = Vector3.ProjectOnPlane(rightDir, groundNormal).normalized;
        }

        Vector3 localVelocity = Quaternion.Inverse(yawRotation) * _rb.velocity;

        float targetForward = Mathf.Max(maxForwardSpeed, minimumAutoForwardSpeed);
        float autoAcceleration = Mathf.Max(acceleration, minimumAutoAcceleration);
        localVelocity.z = Mathf.MoveTowards(localVelocity.z, targetForward, autoAcceleration * Time.fixedDeltaTime);

        float lateralGrip = driftInput ? driftLateralGrip : normalLateralGrip;
        float targetLateral = driftInput ? steerInput * driftLateralVelocity : 0f;
        localVelocity.x = Mathf.Lerp(localVelocity.x, targetLateral, lateralGrip * Time.fixedDeltaTime);
        if (Mathf.Abs(localVelocity.x) < 0.01f) localVelocity.x = 0f;

        // 좌우 스크롤과 전진 가속을 분리하기 위해 조향 기반 전진 부스트는 비활성화
        float driftBoost = 0f;
        Vector3 desiredVelocity = (forwardDir * (localVelocity.z + driftBoost)) + (rightDir * localVelocity.x);

        float targetY = _rb.velocity.y;
        float maxAllowedRiseSpeed = float.PositiveInfinity;
        if (hasGround)
        {
            targetY = Mathf.Lerp(_rb.velocity.y, desiredVelocity.y, verticalVelocityBlend * Time.fixedDeltaTime);

            float clearanceError = desiredGroundClearance - _currentGroundClearance;
            float clearanceCorrection = Mathf.Clamp(
                clearanceError * clearanceAdjustStrength,
                -groundStickForce,
                maxClearanceRiseSpeed
            );
            targetY += clearanceCorrection;

            if (suppressSeamBounce)
            {
                maxAllowedRiseSpeed = Mathf.Max(groundedMaxRiseSpeed, maxClearanceRiseSpeed, desiredVelocity.y + groundedRiseAllowance);
                targetY = Mathf.Min(targetY, maxAllowedRiseSpeed);
            }
            targetY = Mathf.Max(targetY, -groundStickForce);
        }

        Vector3 targetVelocity = new Vector3(desiredVelocity.x, targetY, desiredVelocity.z);
        _rb.velocity = Vector3.Lerp(_rb.velocity, targetVelocity, velocityBlend * Time.fixedDeltaTime);
        if (hasGround && suppressSeamBounce && _rb.velocity.y > maxAllowedRiseSpeed)
        {
            Vector3 clampedVelocity = _rb.velocity;
            clampedVelocity.y = maxAllowedRiseSpeed;
            _rb.velocity = clampedVelocity;
        }
    }

    private void ApplyRotation(float steerInput, bool hasGround, Vector3 groundNormal)
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yawAngle, 0f);
        Quaternion targetRotation = yawRotation;

        if (alignPitchToRoad && hasGround && TryGetRoadPitchAngle(yawRotation, out float pitchAngle))
        {
            targetRotation = yawRotation * Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        Quaternion smoothedRotation = Quaternion.Slerp(_rb.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
        if (steerInput == 0f && Quaternion.Angle(smoothedRotation, targetRotation) <= neutralSnapAngle)
        {
            smoothedRotation = targetRotation;
        }

        _rb.MoveRotation(smoothedRotation);
    }

    private bool TryGetGroundNormal(out Vector3 groundNormal)
    {
        groundNormal = Vector3.up;

        Bounds bounds = _bodyCollider != null ? _bodyCollider.bounds : new Bounds(transform.position, Vector3.one);
        Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + groundProbeUpOffset, bounds.center.z);

        bool hit = Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hitInfo,
            groundCheckDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hit)
        {
            _currentGroundClearance = groundCheckDistance;
            return false;
        }

        float sideOffset = Mathf.Max(bounds.extents.x * 0.45f, 0.2f);
        Vector3 side = transform.right * sideOffset;
        Vector3 leftOrigin = origin - side;
        Vector3 rightOrigin = origin + side;

        Vector3 normalSum = hitInfo.normal;
        int hitCount = 1;
        float clearanceSum = Mathf.Max(hitInfo.distance - groundProbeUpOffset, 0f);
        int clearanceSamples = 1;

        if (Physics.Raycast(leftOrigin, Vector3.down, out RaycastHit leftHit, groundCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            normalSum += leftHit.normal;
            hitCount++;
            clearanceSum += Mathf.Max(leftHit.distance - groundProbeUpOffset, 0f);
            clearanceSamples++;
        }

        if (Physics.Raycast(rightOrigin, Vector3.down, out RaycastHit rightHit, groundCheckDistance, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            normalSum += rightHit.normal;
            hitCount++;
            clearanceSum += Mathf.Max(rightHit.distance - groundProbeUpOffset, 0f);
            clearanceSamples++;
        }

        _currentGroundClearance = clearanceSum / Mathf.Max(1, clearanceSamples);
        Vector3 averagedNormal = (normalSum / hitCount).normalized;
        _smoothedGroundNormal = Vector3.Slerp(_smoothedGroundNormal, averagedNormal, groundNormalLerp).normalized;
        groundNormal = _smoothedGroundNormal;
        return true;
    }

    private bool TryGetRoadPitchAngle(Quaternion yawRotation, out float pitchAngle)
    {
        pitchAngle = 0f;
        if (_bodyCollider == null) return false;

        Bounds bounds = _bodyCollider.bounds;
        Vector3 center = bounds.center;
        Vector3 forward = yawRotation * Vector3.forward;

        Vector3 frontOrigin = center + (forward * pitchSampleDistance) + (Vector3.up * pitchRayStartHeight);
        Vector3 rearOrigin = center - (forward * pitchSampleDistance) + (Vector3.up * pitchRayStartHeight);

        bool frontHit = Physics.Raycast(
            frontOrigin,
            Vector3.down,
            out RaycastHit frontInfo,
            pitchRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        bool rearHit = Physics.Raycast(
            rearOrigin,
            Vector3.down,
            out RaycastHit rearInfo,
            pitchRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (!frontHit || !rearHit) return false;

        Vector3 horizontalDelta = Vector3.ProjectOnPlane(frontInfo.point - rearInfo.point, Vector3.up);
        float horizontalDistance = horizontalDelta.magnitude;
        if (horizontalDistance < 0.01f) return false;

        float heightDelta = frontInfo.point.y - rearInfo.point.y;
        pitchAngle = -Mathf.Atan2(heightDelta, horizontalDistance) * Mathf.Rad2Deg;
        pitchAngle = Mathf.Clamp(pitchAngle, -maxPitchAngle, maxPitchAngle);
        return true;
    }

    private void ApplyStepAssist()
    {
        if (!enableStepAssist) return;
        if (Mathf.Abs(_rb.velocity.y) > 0.8f) return;

        Vector3 planarVelocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
        if (planarVelocity.magnitude < minStepForwardSpeed) return;

        Vector3 moveDir = planarVelocity.normalized;
        Bounds bounds = _bodyCollider != null ? _bodyCollider.bounds : new Bounds(transform.position, Vector3.one);

        float bodyHalfWidth = Mathf.Max(bounds.extents.x, 0.2f);
        float baseY = bounds.min.y + stepCheckLowerHeight;

        Vector3 lowerOrigin = new Vector3(bounds.center.x, baseY, bounds.center.z) + (moveDir * bodyHalfWidth);
        Vector3 upperOrigin = lowerOrigin + (Vector3.up * stepHeight);

        bool hitLower = Physics.Raycast(
            lowerOrigin,
            moveDir,
            out RaycastHit lowerHit,
            stepCheckForwardDistance,
            stepLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitLower) return;
        if (lowerHit.collider.attachedRigidbody == _rb) return;
        if (lowerHit.collider.CompareTag("Obstacle")) return;

        bool hitUpper = Physics.Raycast(
            upperOrigin,
            moveDir,
            stepCheckForwardDistance,
            stepLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitUpper) return;

        float upStep = Mathf.Min(stepHeight, stepAssistSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + (Vector3.up * upStep));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("FinishLine"))
            GameManager.Instance?.CompleteStage();
        else if (other.CompareTag("Obstacle"))
            GameManager.Instance?.EndGame();
    }
}
