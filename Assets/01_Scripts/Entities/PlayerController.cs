using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Score")]
    public bool enableScore = true;
    [Min(0.1f)] public float scoreZDistanceStep = 5f;
    [Min(0.1f)] public float scoreXDistanceStep = 1f;
    public int scoreZPoints = 2;
    public int scoreXPoints = 1;

    [Header("Score UI")]
    public bool autoCreateScoreUI = true;
    public Vector2 scoreAnchoredPos = new Vector2(30f, -30f);
    public Vector2 scoreSize = new Vector2(320f, 80f);
    public int scoreFontSize = 44;
    public Color scoreColor = Color.white;
    public string scoreFormat = "점수: {0}";

    [Header("Out of Bounds")]
    public float outOfBoundsNoGroundTime = 0.8f;
    public float outOfBoundsBelowStartY = -5f;
    public bool autoCreateOutOfBoundsUI = true;
    public Vector2 outOfBoundsPanelSize = new Vector2(760f, 420f);
    public int outOfBoundsTitleFontSize = 52;
    public int outOfBoundsScoreFontSize = 44;
    public int outOfBoundsButtonFontSize = 40;
    public Color outOfBoundsPanelColor = new Color(0f, 0f, 0f, 0.7f);
    public Color outOfBoundsTextColor = Color.white;
    public string outOfBoundsTitleText = "이탈";
    public string outOfBoundsScoreFormat = "누적 점수: {0}";
    public string outOfBoundsButtonText = "처음으로";

    [Header("Stage Clear UI")]
    public bool autoCreateStageClearUI = true;
    public Vector2 stageClearPanelSize = new Vector2(760f, 420f);
    public int stageClearTitleFontSize = 52;
    public int stageClearButtonFontSize = 40;
    public Color stageClearPanelColor = new Color(0f, 0f, 0f, 0.7f);
    public Color stageClearTextColor = Color.white;
    public string stageClearTitleText = "스테이지 클리어";
    public string stageClearButtonText = "다음 단계";

    [Header("Airborne Clamp")]
    public bool preventAirborneLaunch = true;
    [Min(0f)] public float maxAirborneRiseSpeed = 0f;

    [Header("Front Impact Lock")]
    [Min(0f)] public float frontImpactPitchLockDuration = 0.2f;

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
    private Text _scoreText;
    private int _lastDisplayedScore = -1;
    private int _score;
    private float _scoreAccumZ;
    private float _scoreAccumX;
    private Vector3 _lastScorePosition;
    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private float _noGroundTimer;
    private bool _isOutOfBounds;
    private bool _isStageCleared;
    private float _frontImpactPitchLockTimer;
    private GameObject _outOfBoundsPanel;
    private Text _outOfBoundsScoreText;
    private Button _outOfBoundsHomeButton;
    private GameObject _stageClearPanel;
    private Button _stageClearNextButton;

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
        scoreFontSize = Mathf.Max(12, scoreFontSize);
        outOfBoundsTitleFontSize = Mathf.Max(12, outOfBoundsTitleFontSize);
        outOfBoundsScoreFontSize = Mathf.Max(12, outOfBoundsScoreFontSize);
        outOfBoundsButtonFontSize = Mathf.Max(12, outOfBoundsButtonFontSize);
        stageClearTitleFontSize = Mathf.Max(12, stageClearTitleFontSize);
        stageClearButtonFontSize = Mathf.Max(12, stageClearButtonFontSize);
        scoreZDistanceStep = Mathf.Max(0.1f, scoreZDistanceStep);
        scoreXDistanceStep = Mathf.Max(0.1f, scoreXDistanceStep);
        scoreZPoints = Mathf.Max(0, scoreZPoints);
        scoreXPoints = Mathf.Max(0, scoreXPoints);
        outOfBoundsNoGroundTime = Mathf.Max(0.05f, outOfBoundsNoGroundTime);
        outOfBoundsBelowStartY = Mathf.Min(outOfBoundsBelowStartY, 0f);
        frontImpactPitchLockDuration = Mathf.Max(0f, frontImpactPitchLockDuration);
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        AdjustBodyColliderToWheelHeight();
        InitializeWheelVisuals();
        EnsureDigitalSpeedometer();
        EnsureScoreText();
        EnsureOutOfBoundsPanel();
        ResetScoreTracking();

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
        SetGameplayUIVisible(GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing);
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            _rb.velocity = Vector3.zero;
            return;
        }

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

        if (_frontImpactPitchLockTimer > 0f)
        {
            _frontImpactPitchLockTimer = Mathf.Max(0f, _frontImpactPitchLockTimer - Time.fixedDeltaTime);
        }

        float steerInput = GetSteerInput();
        bool driftInput = autoDriftBySteer && Mathf.Abs(steerInput) >= autoDriftSteerThreshold;

        bool hasGround = TryGetGroundNormal(out Vector3 groundNormal);
        UpdateScoreTracking();
        UpdateOutOfBounds(hasGround);
        if (_isOutOfBounds) return;
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
        else if (!hasGround && preventAirborneLaunch && _rb.velocity.y > maxAirborneRiseSpeed)
        {
            Vector3 clampedVelocity = _rb.velocity;
            clampedVelocity.y = maxAirborneRiseSpeed;
            _rb.velocity = clampedVelocity;
        }

        if (_frontImpactPitchLockTimer > 0f && _rb.velocity.y > 0f)
        {
            Vector3 clampedVelocity = _rb.velocity;
            clampedVelocity.y = 0f;
            _rb.velocity = clampedVelocity;
        }
    }

    private void ApplyRotation(float steerInput, bool hasGround, Vector3 groundNormal)
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yawAngle, 0f);
        Quaternion targetRotation = yawRotation;

        if (_frontImpactPitchLockTimer <= 0f && alignPitchToRoad && hasGround && TryGetRoadPitchAngle(yawRotation, out float pitchAngle))
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

        Canvas canvas = GetOrCreateHudCanvas();
        Text[] existingTexts = canvas.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < existingTexts.Length; i++)
        {
            if (existingTexts[i].name == "DigitalSpeedometerText")
            {
                _digitalSpeedText = existingTexts[i];
                return;
            }
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

    private Canvas GetOrCreateHudCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            return canvas;
        }

        GameObject canvasGo = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        return canvas;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void EnsureScoreText()
    {
        if (!autoCreateScoreUI || _scoreText != null)
        {
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();
        Text[] texts = canvas.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].name == "ScoreText")
            {
                _scoreText = texts[i];
                UpdateScoreText();
                return;
            }
        }

        GameObject scoreGo = new GameObject("ScoreText", typeof(RectTransform), typeof(Text));
        RectTransform rect = scoreGo.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = scoreAnchoredPos;
        rect.sizeDelta = scoreSize;

        _scoreText = scoreGo.GetComponent<Text>();
        _scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _scoreText.alignment = TextAnchor.UpperLeft;
        _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _scoreText.verticalOverflow = VerticalWrapMode.Overflow;
        _scoreText.fontSize = scoreFontSize;
        _scoreText.color = scoreColor;
        _scoreText.raycastTarget = false;
        UpdateScoreText();
    }

    private void UpdateScoreTracking()
    {
        if (!enableScore || _isOutOfBounds)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        Vector3 delta = currentPosition - _lastScorePosition;
        _lastScorePosition = currentPosition;

        _scoreAccumZ += Mathf.Abs(delta.z);
        _scoreAccumX += Mathf.Abs(delta.x);

        int addScore = 0;
        int zSteps = Mathf.FloorToInt(_scoreAccumZ / scoreZDistanceStep);
        if (zSteps > 0)
        {
            addScore += zSteps * scoreZPoints;
            _scoreAccumZ -= zSteps * scoreZDistanceStep;
        }

        int xSteps = Mathf.FloorToInt(_scoreAccumX / scoreXDistanceStep);
        if (xSteps > 0)
        {
            addScore += xSteps * scoreXPoints;
            _scoreAccumX -= xSteps * scoreXDistanceStep;
        }

        if (addScore != 0)
        {
            _score += addScore;
            UpdateScoreText();
        }
    }

    public void AddScore(int amount)
    {
        if (!enableScore || amount == 0)
        {
            return;
        }

        EnsureScoreText();
        _score = Mathf.Max(0, _score + amount);
        UpdateScoreText();

        if (_outOfBoundsScoreText != null)
        {
            _outOfBoundsScoreText.text = string.Format(outOfBoundsScoreFormat, _score);
        }
    }

    private void UpdateScoreText()
    {
        if (_scoreText == null)
        {
            return;
        }

        if (_score == _lastDisplayedScore)
        {
            return;
        }

        _scoreText.text = string.Format(scoreFormat, _score);
        _lastDisplayedScore = _score;
    }

    private void ResetScoreTracking()
    {
        _score = 0;
        _scoreAccumZ = 0f;
        _scoreAccumX = 0f;
        _lastScorePosition = transform.position;
        _lastDisplayedScore = -1;
        UpdateScoreText();
    }

    private void EnsureOutOfBoundsPanel()
    {
        if (!autoCreateOutOfBoundsUI || _outOfBoundsPanel != null)
        {
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();
        RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == "OutOfBoundsPanel")
            {
                _outOfBoundsPanel = rects[i].gameObject;
                Text[] texts = _outOfBoundsPanel.GetComponentsInChildren<Text>(true);
                for (int j = 0; j < texts.Length; j++)
                {
                    if (texts[j].name == "OutOfBoundsScoreText")
                    {
                        _outOfBoundsScoreText = texts[j];
                        break;
                    }
                }

                Button[] buttons = _outOfBoundsPanel.GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    if (buttons[j].name == "OutOfBoundsHomeButton")
                    {
                        _outOfBoundsHomeButton = buttons[j];
                        break;
                    }
                }
                if (_outOfBoundsHomeButton != null)
                {
                    _outOfBoundsHomeButton.onClick.RemoveAllListeners();
                    _outOfBoundsHomeButton.onClick.AddListener(ReturnToMainMenu);
                }
                _outOfBoundsPanel.SetActive(false);
                return;
            }
        }

        EnsureEventSystem();

        GameObject panelGo = new GameObject("OutOfBoundsPanel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = outOfBoundsPanelSize;

        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.color = outOfBoundsPanelColor;
        panelImage.raycastTarget = true;

        GameObject titleGo = new GameObject("OutOfBoundsTitle", typeof(RectTransform), typeof(Text));
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(outOfBoundsPanelSize.x - 80f, 90f);

        Text titleText = titleGo.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;
        titleText.fontSize = outOfBoundsTitleFontSize;
        titleText.color = outOfBoundsTextColor;
        titleText.raycastTarget = false;
        titleText.text = outOfBoundsTitleText;

        GameObject scoreGo = new GameObject("OutOfBoundsScoreText", typeof(RectTransform), typeof(Text));
        RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
        scoreRect.SetParent(panelRect, false);
        scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRect.pivot = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(0f, -10f);
        scoreRect.sizeDelta = new Vector2(outOfBoundsPanelSize.x - 80f, 80f);

        _outOfBoundsScoreText = scoreGo.GetComponent<Text>();
        _outOfBoundsScoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _outOfBoundsScoreText.alignment = TextAnchor.MiddleCenter;
        _outOfBoundsScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
        _outOfBoundsScoreText.verticalOverflow = VerticalWrapMode.Overflow;
        _outOfBoundsScoreText.fontSize = outOfBoundsScoreFontSize;
        _outOfBoundsScoreText.color = outOfBoundsTextColor;
        _outOfBoundsScoreText.raycastTarget = false;
        _outOfBoundsScoreText.text = string.Format(outOfBoundsScoreFormat, _score);

        GameObject buttonGo = new GameObject("OutOfBoundsHomeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.SetParent(panelRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 30f);
        buttonRect.sizeDelta = new Vector2(260f, 80f);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.95f);
        _outOfBoundsHomeButton = buttonGo.GetComponent<Button>();
        _outOfBoundsHomeButton.targetGraphic = buttonImage;
        _outOfBoundsHomeButton.onClick.AddListener(ReturnToMainMenu);

        GameObject buttonTextGo = new GameObject("OutOfBoundsHomeButtonText", typeof(RectTransform), typeof(Text));
        RectTransform buttonTextRect = buttonTextGo.GetComponent<RectTransform>();
        buttonTextRect.SetParent(buttonRect, false);
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        Text buttonText = buttonTextGo.GetComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
        buttonText.verticalOverflow = VerticalWrapMode.Overflow;
        buttonText.fontSize = outOfBoundsButtonFontSize;
        buttonText.color = Color.black;
        buttonText.raycastTarget = false;
        buttonText.text = outOfBoundsButtonText;

        _outOfBoundsPanel = panelGo;
        _outOfBoundsPanel.SetActive(false);
    }

    private void EnsureStageClearPanel()
    {
        if (!autoCreateStageClearUI || _stageClearPanel != null)
        {
            return;
        }

        Canvas canvas = GetOrCreateHudCanvas();
        RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i].name == "StageClearPanel")
            {
                _stageClearPanel = rects[i].gameObject;
                Button[] buttons = _stageClearPanel.GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    if (buttons[j].name == "StageClearNextButton")
                    {
                        _stageClearNextButton = buttons[j];
                        break;
                    }
                }
                if (_stageClearNextButton != null)
                {
                    _stageClearNextButton.onClick.RemoveAllListeners();
                    _stageClearNextButton.onClick.AddListener(GoToNextStage);
                }
                _stageClearPanel.SetActive(false);
                return;
            }
        }

        EnsureEventSystem();

        GameObject panelGo = new GameObject("StageClearPanel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = stageClearPanelSize;

        Image panelImage = panelGo.GetComponent<Image>();
        panelImage.color = stageClearPanelColor;
        panelImage.raycastTarget = true;

        GameObject titleGo = new GameObject("StageClearTitle", typeof(RectTransform), typeof(Text));
        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.SetParent(panelRect, false);
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -30f);
        titleRect.sizeDelta = new Vector2(stageClearPanelSize.x - 80f, 90f);

        Text titleText = titleGo.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        titleText.verticalOverflow = VerticalWrapMode.Overflow;
        titleText.fontSize = stageClearTitleFontSize;
        titleText.color = stageClearTextColor;
        titleText.raycastTarget = false;
        titleText.text = stageClearTitleText;

        GameObject buttonGo = new GameObject("StageClearNextButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
        buttonRect.SetParent(panelRect, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 30f);
        buttonRect.sizeDelta = new Vector2(260f, 80f);

        Image buttonImage = buttonGo.GetComponent<Image>();
        buttonImage.color = new Color(1f, 1f, 1f, 0.95f);
        _stageClearNextButton = buttonGo.GetComponent<Button>();
        _stageClearNextButton.targetGraphic = buttonImage;
        _stageClearNextButton.onClick.AddListener(GoToNextStage);

        GameObject buttonTextGo = new GameObject("StageClearNextButtonText", typeof(RectTransform), typeof(Text));
        RectTransform buttonTextRect = buttonTextGo.GetComponent<RectTransform>();
        buttonTextRect.SetParent(buttonRect, false);
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        Text buttonText = buttonTextGo.GetComponent<Text>();
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
        buttonText.verticalOverflow = VerticalWrapMode.Overflow;
        buttonText.fontSize = stageClearButtonFontSize;
        buttonText.color = Color.black;
        buttonText.raycastTarget = false;
        buttonText.text = stageClearButtonText;

        _stageClearPanel = panelGo;
        _stageClearPanel.SetActive(false);
    }

    public void SetStageClearUIVisible(bool visible)
    {
        if (visible)
        {
            EnsureStageClearPanel();
        }

        if (_stageClearPanel != null)
        {
            _stageClearPanel.SetActive(visible);
        }
    }

    private void UpdateOutOfBounds(bool hasGround)
    {
        if (_isOutOfBounds)
        {
            return;
        }

        _noGroundTimer = hasGround ? 0f : _noGroundTimer + Time.fixedDeltaTime;
        float minAllowedY = _startPosition.y + outOfBoundsBelowStartY;
        if (_noGroundTimer >= outOfBoundsNoGroundTime || transform.position.y <= minAllowedY)
        {
            TriggerOutOfBounds();
        }
    }

    private void TriggerOutOfBounds()
    {
        _isOutOfBounds = true;
        _noGroundTimer = 0f;

        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        GameManager.Instance?.SetGameOver("플레이어가 코스를 이탈했습니다.");
        ShowOutOfBoundsUI();
    }

    private void TriggerStageClear()
    {
        if (_isStageCleared)
        {
            return;
        }

        _isStageCleared = true;
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }
    }

    private void ShowOutOfBoundsUI()
    {
        if (_outOfBoundsPanel == null)
        {
            return;
        }

        if (_outOfBoundsScoreText != null)
        {
            _outOfBoundsScoreText.text = string.Format(outOfBoundsScoreFormat, _score);
        }

        _outOfBoundsPanel.SetActive(true);
    }

    private void ReturnToMainMenu()
    {
        ResetToStartState();

        GameManager.Instance?.GoToMainMenu();
    }

    private void GoToNextStage()
    {
        if (_stageClearPanel != null)
        {
            _stageClearPanel.SetActive(false);
        }

        GameManager.Instance?.GoToNextStage();
    }

    public void ResetToStartState()
    {
        if (_outOfBoundsPanel != null)
        {
            _outOfBoundsPanel.SetActive(false);
        }
        if (_stageClearPanel != null)
        {
            _stageClearPanel.SetActive(false);
        }

        _isOutOfBounds = false;
        _isStageCleared = false;
        _noGroundTimer = 0f;

        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        transform.position = _startPosition;
        transform.rotation = _startRotation;
        _yawAngle = _startRotation.eulerAngles.y;
        _initialYawAngle = _yawAngle;
        _frontImpactPitchLockTimer = 0f;
        ResetScoreTracking();
    }

    public void SetGameplayUIVisible(bool visible)
    {
        if (_digitalSpeedText != null)
        {
            _digitalSpeedText.gameObject.SetActive(visible);
        }

        if (_scoreText != null)
        {
            _scoreText.gameObject.SetActive(visible);
        }

        if (_outOfBoundsPanel != null)
        {
            _outOfBoundsPanel.SetActive(visible && _isOutOfBounds);
        }
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
        {
            TriggerStageClear();
            GameManager.Instance?.CompleteStage();
        }
        else if (HasColliderTag(other, "Obstacle"))
            GameManager.Instance?.EndGame();
    }

    private void OnCollisionEnter(Collision collision)
    {
        SuppressBounceOnCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        SuppressBounceOnCollision(collision);
    }

    private void SuppressBounceOnCollision(Collision collision)
    {
        if (_rb == null || collision == null || collision.contactCount == 0)
        {
            return;
        }

        Vector3 contactNormalSum = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
        {
            contactNormalSum += collision.GetContact(i).normal;
        }

        Vector3 averageNormal = contactNormalSum.normalized;
        Vector3 velocity = _rb.velocity;
        Vector3 resolvedVelocity = Vector3.ProjectOnPlane(velocity, averageNormal);

        if (averageNormal.y > 0.2f && resolvedVelocity.y > 0f)
        {
            resolvedVelocity.y = 0f;
        }

        if (resolvedVelocity.y > maxAirborneRiseSpeed && !collision.collider.isTrigger)
        {
            resolvedVelocity.y = maxAirborneRiseSpeed;
        }

        _rb.velocity = resolvedVelocity;

        if (HasFrontImpact(collision))
        {
            _frontImpactPitchLockTimer = frontImpactPitchLockDuration;

            Vector3 angularVelocity = _rb.angularVelocity;
            if (angularVelocity.x > 0f)
            {
                angularVelocity.x = 0f;
            }

            if (angularVelocity.z != 0f)
            {
                angularVelocity.z = 0f;
            }

            _rb.angularVelocity = angularVelocity;
        }
    }

    private bool HasFrontImpact(Collision collision)
    {
        if (collision == null)
        {
            return false;
        }

        if (HasColliderTag(collision.collider, "Obstacle"))
        {
            return true;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Vector3 localPoint = transform.InverseTransformPoint(contact.point);
            if (localPoint.z > 0f && contact.normal.y < 0.6f)
            {
                return true;
            }
        }

        return false;
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
