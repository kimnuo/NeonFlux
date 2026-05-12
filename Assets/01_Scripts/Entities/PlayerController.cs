using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    private const float MaxHeadingAngleLimit = 65f;

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
    public bool reduceForwardSpeedBySteer = true;
    public AnimationCurve forwardSpeedBySteerAngle = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.72f));
    [Min(1f)] public float steerDecelerationMultiplier = 1.35f;
    public bool limitHeadingToForward = true;
    [Range(1f, MaxHeadingAngleLimit)] public float maxHeadingOffsetFromStart = MaxHeadingAngleLimit;

    [Header("Drift / Grip")]
    public bool autoDriftBySteer = true;
    public float autoDriftSteerThreshold = 0.45f;
    public float driftLateralGrip = 2.6f;
    public float normalLateralGrip = 12f;
    public float driftLateralVelocity = 7f;
    public float driftForwardBoost = 2.4f;
    public ParticleSystem driftSmokeParticle;
    public ParticleSystem driftSmokeLeftParticle;
    public ParticleSystem driftSmokeRightParticle;
    public Vector3 driftSmokeLocalOffset = new Vector3(0f, 0.2f, -1.1f);
    public float driftSmokeSideOffset = 0.45f;
    public float driftSmokeInputMaxAbs = 10f;
    public float driftSmokeStartInputAbs = 2f;
    public float driftSmokeStopInputAbs = 2f;
    public float driftSmokeHoldDuration = 1f;
    public float driftSmokeMinForwardSpeed = 0f;
    [Range(0f, 1f)] public float driftSmokeMinActiveIntensity = 0.22f;
    public float driftSmokeSlipReference = 6f;
    public float driftSmokeMaxEmission = 52f;
    public float driftSmokeIntensityRiseSpeed = 5f;
    public float driftSmokeIntensityFallSpeed = 3f;
    public float driftSmokeOutsideBoost = 0.35f;

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

    [Header("Wheel Visuals")]
    public bool autoFindWheelVisuals = true;
    public float wheelVisualRadius = 0.36f;
    public float wheelVisualSteerAngle = 28f;
    public float wheelVisualSpinMultiplier = 1f;

    [Header("Collider Height")]
    [Range(0f, 1f)] public float colliderBottomFromWheelYRatio = 0.5f;
    [Min(0f)] public float colliderBottomLift = 0.12f;

    [Header("Digital Speedometer")]
    public bool autoCreateDigitalSpeedometer = true;
    public Vector2 digitalSpeedometerAnchoredPos = new Vector2(0f, -90f);
    public Vector2 digitalSpeedometerSize = new Vector2(420f, 90f);
    public int digitalSpeedometerFontSize = 52;
    public string digitalSpeedometerUnit = " KM/H";

    private Rigidbody _rb;
    private Collider _bodyCollider;
    private float _yawAngle;
    private float _steerAngle;
    private float _initialYawAngle;
    private Vector3 _smoothedGroundNormal = Vector3.up;
    private float _currentGroundClearance;
    private bool _isDriftSmokePlaying;
    private float _driftSmokeIntensity;
    private float _driftSmokeHoldTimer;
    private float _driftSmokeHoldStartIntensity;
    private Material _driftSmokeMaterial;
    private readonly List<WheelVisualState> _wheelVisuals = new();
    public float CurrentSpeedKmh => _rb != null ? _rb.velocity.magnitude * 3.6f : 0f;
    private Text _digitalSpeedText;
    private int _lastDisplayedSpeed = -1;

    private sealed class WheelVisualState
    {
        public Transform Transform;
        public Quaternion BaseLocalRotation;
        public bool IsFrontWheel;
        public float RollAngle;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _bodyCollider = GetComponent<Collider>();
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.maxDepenetrationVelocity = maxDepenetrationSpeed;
        colliderBottomFromWheelYRatio = Mathf.Clamp01(colliderBottomFromWheelYRatio);
        colliderBottomLift = Mathf.Max(0f, colliderBottomLift);
        digitalSpeedometerFontSize = Mathf.Max(12, digitalSpeedometerFontSize);
        AdjustBodyColliderToWheelHeight();
        InitializeWheelVisuals();
        EnsureDigitalSpeedometer();

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
        maxHeadingOffsetFromStart = Mathf.Clamp(maxHeadingOffsetFromStart, 1f, MaxHeadingAngleLimit);
        driftSmokeInputMaxAbs = Mathf.Max(1f, driftSmokeInputMaxAbs);
        driftSmokeStartInputAbs = Mathf.Clamp(driftSmokeStartInputAbs, 0f, driftSmokeInputMaxAbs);
        driftSmokeStopInputAbs = Mathf.Clamp(driftSmokeStopInputAbs, 0f, driftSmokeStartInputAbs);
        steerDecelerationMultiplier = Mathf.Max(1f, steerDecelerationMultiplier);
        driftSmokeHoldDuration = Mathf.Max(0f, driftSmokeHoldDuration);
        driftSmokeMinForwardSpeed = Mathf.Max(0f, driftSmokeMinForwardSpeed);
        driftSmokeMinActiveIntensity = Mathf.Clamp01(driftSmokeMinActiveIntensity);
        driftSmokeSlipReference = Mathf.Max(0.1f, driftSmokeSlipReference);
        driftSmokeMaxEmission = Mathf.Max(0f, driftSmokeMaxEmission);
        driftSmokeIntensityRiseSpeed = Mathf.Max(0f, driftSmokeIntensityRiseSpeed);
        driftSmokeIntensityFallSpeed = Mathf.Max(0f, driftSmokeIntensityFallSpeed);
        driftSmokeOutsideBoost = Mathf.Clamp01(driftSmokeOutsideBoost);
        driftSmokeSideOffset = Mathf.Max(0f, driftSmokeSideOffset);
        _smoothedGroundNormal = Vector3.up;
        _currentGroundClearance = desiredGroundClearance;

        if (driftSmokeParticle == null && driftSmokeLeftParticle == null && driftSmokeRightParticle == null)
        {
            driftSmokeLeftParticle = CreateDefaultDriftSmokeParticle("DriftSmokeLeftParticle", new Vector3(-driftSmokeSideOffset, driftSmokeLocalOffset.y, driftSmokeLocalOffset.z));
            driftSmokeRightParticle = CreateDefaultDriftSmokeParticle("DriftSmokeRightParticle", new Vector3(driftSmokeSideOffset, driftSmokeLocalOffset.y, driftSmokeLocalOffset.z));
        }

        InitializeSmokeEmitter(driftSmokeParticle);
        InitializeSmokeEmitter(driftSmokeLeftParticle);
        InitializeSmokeEmitter(driftSmokeRightParticle);
        _isDriftSmokePlaying = false;
        _driftSmokeIntensity = 0f;
        _driftSmokeHoldTimer = 0f;
        _driftSmokeHoldStartIntensity = 0f;

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
        UpdateWheelVisuals(steerInput);
        UpdateDigitalSpeedometer();
        UpdateDriftSmoke(hasGround, steerInput);
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
            float headingLimit = Mathf.Min(maxHeadingOffsetFromStart, MaxHeadingAngleLimit);
            float relativeYaw = Mathf.DeltaAngle(_initialYawAngle, _yawAngle);
            relativeYaw = Mathf.Clamp(relativeYaw, -headingLimit, headingLimit);
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
        if (reduceForwardSpeedBySteer)
        {
            float steer01 = Mathf.Clamp01(Mathf.Abs(_steerAngle) / Mathf.Max(maxSteerAngle, 0.01f));
            float steerSpeedFactor = Mathf.Clamp(forwardSpeedBySteerAngle.Evaluate(steer01), 0.15f, 1f);
            targetForward *= steerSpeedFactor;
        }

        float autoAcceleration = Mathf.Max(acceleration, minimumAutoAcceleration);
        float forwardAdjustRate = localVelocity.z > targetForward
            ? autoAcceleration * steerDecelerationMultiplier
            : autoAcceleration;
        localVelocity.z = Mathf.MoveTowards(localVelocity.z, targetForward, forwardAdjustRate * Time.fixedDeltaTime);

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
        if (HasColliderTag(lowerHit.collider, "Obstacle")) return;

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

    private void UpdateDriftSmoke(bool hasGround, float steerInput)
    {
        if (driftSmokeParticle == null && driftSmokeLeftParticle == null && driftSmokeRightParticle == null)
        {
            return;
        }

        if (driftSmokeParticle != null)
        {
            driftSmokeParticle.transform.localPosition = driftSmokeLocalOffset;
        }
        if (driftSmokeLeftParticle != null)
        {
            driftSmokeLeftParticle.transform.localPosition = new Vector3(-driftSmokeSideOffset, driftSmokeLocalOffset.y, driftSmokeLocalOffset.z);
        }
        if (driftSmokeRightParticle != null)
        {
            driftSmokeRightParticle.transform.localPosition = new Vector3(driftSmokeSideOffset, driftSmokeLocalOffset.y, driftSmokeLocalOffset.z);
        }

        float smokeInputAbs = Mathf.Abs(steerInput) * driftSmokeInputMaxAbs;
        Quaternion yawRotation = Quaternion.Euler(0f, _yawAngle, 0f);
        Vector3 localVelocity = Quaternion.Inverse(yawRotation) * _rb.velocity;
        float forwardSpeed = Mathf.Max(localVelocity.z, 0f);
        float lateralSpeed = Mathf.Abs(localVelocity.x);

        bool steeringSmoke = _isDriftSmokePlaying
            ? smokeInputAbs >= driftSmokeStopInputAbs
            : smokeInputAbs >= driftSmokeStartInputAbs;

        bool smokeGate = hasGround;
        if (steeringSmoke && smokeGate)
        {
            _driftSmokeHoldTimer = driftSmokeHoldDuration;
            _driftSmokeHoldStartIntensity = Mathf.Max(_driftSmokeIntensity, driftSmokeMinActiveIntensity);
        }
        else
        {
            _driftSmokeHoldTimer = Mathf.Max(0f, _driftSmokeHoldTimer - Time.fixedDeltaTime);
        }

        bool holdSmoke = !steeringSmoke && _driftSmokeHoldTimer > 0f;
        bool shouldPlay = smokeGate && (steeringSmoke || holdSmoke);
        float targetIntensity = 0f;
        if (steeringSmoke && smokeGate)
        {
            float inputStart = Mathf.Max(driftSmokeStartInputAbs, 0.01f);
            float input01 = Mathf.InverseLerp(inputStart, driftSmokeInputMaxAbs, smokeInputAbs);
            float speed01 = Mathf.InverseLerp(driftSmokeMinForwardSpeed, Mathf.Max(driftSmokeMinForwardSpeed + 1f, maxForwardSpeed), forwardSpeed);
            float slip01 = Mathf.InverseLerp(0.05f, driftSmokeSlipReference, lateralSpeed);
            float speedFactor = Mathf.Lerp(0.55f, 1f, speed01);
            float slipFactor = Mathf.Lerp(0.45f, 1f, slip01);
            float baseIntensity = input01 * speedFactor * slipFactor;
            targetIntensity = Mathf.Max(baseIntensity, driftSmokeMinActiveIntensity);
        }
        else if (holdSmoke && smokeGate)
        {
            float holdProgress = driftSmokeHoldDuration > 0f ? _driftSmokeHoldTimer / driftSmokeHoldDuration : 0f;
            targetIntensity = Mathf.Lerp(0f, _driftSmokeHoldStartIntensity, holdProgress);
        }

        float smoothSpeed = targetIntensity > _driftSmokeIntensity ? driftSmokeIntensityRiseSpeed : driftSmokeIntensityFallSpeed;
        _driftSmokeIntensity = Mathf.MoveTowards(_driftSmokeIntensity, targetIntensity, smoothSpeed * Time.fixedDeltaTime);

        if (shouldPlay)
        {
            if (!_isDriftSmokePlaying)
            {
                PlaySmokeEmitter(driftSmokeParticle);
                PlaySmokeEmitter(driftSmokeLeftParticle);
                PlaySmokeEmitter(driftSmokeRightParticle);
                _isDriftSmokePlaying = true;
            }
        }
        else if (_isDriftSmokePlaying)
        {
            if (_driftSmokeIntensity <= 0.01f)
            {
                StopSmokeEmitter(driftSmokeParticle);
                StopSmokeEmitter(driftSmokeLeftParticle);
                StopSmokeEmitter(driftSmokeRightParticle);
                _isDriftSmokePlaying = false;
            }
        }

        float outsideBoost = 1f + (driftSmokeOutsideBoost * _driftSmokeIntensity);
        float insideScale = Mathf.Clamp01(1f - (driftSmokeOutsideBoost * 0.7f * _driftSmokeIntensity));
        float leftMultiplier = 1f;
        float rightMultiplier = 1f;
        if (steerInput > 0f)
        {
            leftMultiplier = outsideBoost;
            rightMultiplier = insideScale;
        }
        else if (steerInput < 0f)
        {
            leftMultiplier = insideScale;
            rightMultiplier = outsideBoost;
        }

        UpdateSmokeEmitterRate(driftSmokeParticle, driftSmokeMaxEmission * _driftSmokeIntensity);
        UpdateSmokeEmitterRate(driftSmokeLeftParticle, driftSmokeMaxEmission * _driftSmokeIntensity * leftMultiplier);
        UpdateSmokeEmitterRate(driftSmokeRightParticle, driftSmokeMaxEmission * _driftSmokeIntensity * rightMultiplier);
    }

    private ParticleSystem CreateDefaultDriftSmokeParticle(string objectName, Vector3 localOffset)
    {
        GameObject smokeObject = new GameObject(objectName);
        smokeObject.transform.SetParent(transform, false);
        smokeObject.transform.localPosition = localOffset;
        smokeObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem smoke = smokeObject.AddComponent<ParticleSystem>();
        var main = smoke.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = 0.55f;
        main.startSpeed = 1.2f;
        main.startSize = 0.7f;
        main.startColor = new Color(1f, 1f, 1f, 0.75f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 120;

        var emission = smoke.emission;
        emission.rateOverTime = 35f;

        var shape = smoke.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.22f;

        var colorOverLifetime = smoke.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.75f, 0f),
                new GradientAlphaKey(0.25f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = colorGradient;

        var sizeOverLifetime = smoke.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.2f));

        var renderer = smoke.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material smokeMaterial = GetOrCreateDriftSmokeMaterial();
        if (smokeMaterial != null)
        {
            renderer.sharedMaterial = smokeMaterial;
        }

        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return smoke;
    }

    private Material GetOrCreateDriftSmokeMaterial()
    {
        if (_driftSmokeMaterial != null)
        {
            return _driftSmokeMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null)
        {
            return null;
        }

        _driftSmokeMaterial = new Material(shader)
        {
            name = "PlayerDriftSmoke_White"
        };

        if (_driftSmokeMaterial.HasProperty("_BaseColor"))
        {
            _driftSmokeMaterial.SetColor("_BaseColor", Color.white);
        }
        if (_driftSmokeMaterial.HasProperty("_Color"))
        {
            _driftSmokeMaterial.SetColor("_Color", Color.white);
        }

        return _driftSmokeMaterial;
    }

    private void AdjustBodyColliderToWheelHeight()
    {
        if (_bodyCollider is not BoxCollider boxCollider)
        {
            return;
        }

        if (!TryGetAverageWheelLocalY(out float wheelY))
        {
            return;
        }

        float targetBottomY = (wheelY * colliderBottomFromWheelYRatio) + colliderBottomLift;
        Vector3 center = boxCollider.center;
        center.y = targetBottomY + (boxCollider.size.y * 0.5f);
        boxCollider.center = center;
    }

    private bool TryGetAverageWheelLocalY(out float averageWheelY)
    {
        averageWheelY = 0f;
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        float ySum = 0f;
        int wheelCount = 0;

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == transform)
            {
                continue;
            }

            string name = current.name;
            if (!name.Contains("wheel", StringComparison.OrdinalIgnoreCase) &&
                !name.Contains("tire", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ySum += current.localPosition.y;
            wheelCount++;
        }

        if (wheelCount == 0)
        {
            return false;
        }

        averageWheelY = ySum / wheelCount;
        return true;
    }

    private void InitializeSmokeEmitter(ParticleSystem smokeEmitter)
    {
        if (smokeEmitter == null)
        {
            return;
        }

        UpdateSmokeEmitterRate(smokeEmitter, 0f);
        smokeEmitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void InitializeWheelVisuals()
    {
        _wheelVisuals.Clear();
        if (!autoFindWheelVisuals)
        {
            return;
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == transform) continue;

            string name = current.name;
            bool isWheel = name.Contains("wheel", StringComparison.OrdinalIgnoreCase) ||
                           name.Contains("tire", StringComparison.OrdinalIgnoreCase);
            if (!isWheel) continue;

            _wheelVisuals.Add(new WheelVisualState
            {
                Transform = current,
                BaseLocalRotation = current.localRotation,
                IsFrontWheel = current.localPosition.z > 0f,
                RollAngle = 0f
            });
        }
    }

    private void UpdateWheelVisuals(float steerInput)
    {
        if (_wheelVisuals.Count == 0)
        {
            return;
        }

        float radius = Mathf.Max(0.05f, wheelVisualRadius);
        float speedAlongForward = Vector3.Dot(_rb.velocity, transform.forward);
        float degreesPerSecond = (speedAlongForward / (2f * Mathf.PI * radius)) * 360f * wheelVisualSpinMultiplier;
        float steerYaw = steerInput * wheelVisualSteerAngle;

        for (int i = 0; i < _wheelVisuals.Count; i++)
        {
            WheelVisualState wheel = _wheelVisuals[i];
            if (wheel.Transform == null) continue;

            wheel.RollAngle = Mathf.Repeat(wheel.RollAngle + (degreesPerSecond * Time.fixedDeltaTime), 360f);
            float yawAngle = wheel.IsFrontWheel ? steerYaw : 0f;
            wheel.Transform.localRotation = wheel.BaseLocalRotation * Quaternion.Euler(wheel.RollAngle, yawAngle, 0f);
        }
    }

    private void EnsureDigitalSpeedometer()
    {
        if (!autoCreateDigitalSpeedometer)
        {
            return;
        }

        if (_digitalSpeedText != null)
        {
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }

        Text existing = canvas.GetComponentInChildren<Text>(true);
        if (existing != null && existing.name == "DigitalSpeedometerText")
        {
            _digitalSpeedText = existing;
            return;
        }

        GameObject speedGo = new GameObject("DigitalSpeedometerText", typeof(RectTransform), typeof(Text));
        RectTransform rect = speedGo.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = digitalSpeedometerAnchoredPos;
        rect.sizeDelta = digitalSpeedometerSize;

        _digitalSpeedText = speedGo.GetComponent<Text>();
        _digitalSpeedText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _digitalSpeedText.alignment = TextAnchor.MiddleCenter;
        _digitalSpeedText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _digitalSpeedText.verticalOverflow = VerticalWrapMode.Overflow;
        _digitalSpeedText.fontSize = digitalSpeedometerFontSize;
        _digitalSpeedText.color = Color.white;
        _digitalSpeedText.raycastTarget = false;
        _digitalSpeedText.text = "0" + digitalSpeedometerUnit;
    }

    private void UpdateDigitalSpeedometer()
    {
        if (_digitalSpeedText == null)
        {
            return;
        }

        int speed = Mathf.RoundToInt(CurrentSpeedKmh);
        if (speed == _lastDisplayedSpeed)
        {
            return;
        }

        _digitalSpeedText.text = speed + digitalSpeedometerUnit;
        _lastDisplayedSpeed = speed;
    }

    private void PlaySmokeEmitter(ParticleSystem smokeEmitter)
    {
        if (smokeEmitter == null)
        {
            return;
        }

        if (!smokeEmitter.isPlaying)
        {
            smokeEmitter.Play();
        }
    }

    private void StopSmokeEmitter(ParticleSystem smokeEmitter)
    {
        if (smokeEmitter == null)
        {
            return;
        }

        smokeEmitter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void UpdateSmokeEmitterRate(ParticleSystem smokeEmitter, float emissionRate)
    {
        if (smokeEmitter == null)
        {
            return;
        }

        var emission = smokeEmitter.emission;
        emission.rateOverTime = emissionRate;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasColliderTag(other, "FinishLine"))
            GameManager.Instance?.CompleteStage();
        else if (HasColliderTag(other, "Obstacle"))
            GameManager.Instance?.EndGame();
    }

    private static bool HasColliderTag(Collider target, string expectedTag)
    {
        if (target == null)
        {
            return false;
        }

        return string.Equals(target.tag, expectedTag, StringComparison.Ordinal);
    }
}
