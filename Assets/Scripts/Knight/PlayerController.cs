using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEngine;

namespace Player
{

    public class PlayerController : MonoBehaviour
    {
        public Vector3 Velocity { get; private set; }
        public float x;
        public FrameInput Input { get; private set; }
        public bool JumpingThisFrame { get; private set; }
        public bool LandingThisFrame { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public bool Grounded => _colDown;
        public bool hurtthisFrame;

        private Vector3 _lastPosition;
        private float _currentHorizontalSpeed, _currentVerticalSpeed;
        private bool RestoreTime = false;
        private bool _active;

        void Awake() => Invoke(nameof(Activate), 0.5f);
        void Activate() => _active = true;

        void Update()
        {
            EventCenter.Instance.EventTrigger<Transform>("EventPlayerPosChange", transform);

            if (!_active) return;
            // Calculate velocity
            Velocity = (transform.position - _lastPosition) / Time.deltaTime;
            _lastPosition = transform.position;
            GatherInput(); // ��ȡ����

            RunCollisionChecks(); // ��ײ���
            isHurt();

            CalculateWalk(); // ˮƽ�ƶ�������ˮƽ�ٶȣ�

            // ��׹/���������㴹ֱ�ٶȣ�
            CalculateJumpApex();
            CalculateGravity();

            CalculateJump(); // ��Ծ�����ô�ֱ�ٶȣ�

            MoveCharacter(); // �ƶ���ɫ



        }

        #region Gather Input
        private void GatherInput()
        {
            Input = new FrameInput
            {
                JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
                JumpUp = UnityEngine.Input.GetButtonUp("Jump"),
                X = UnityEngine.Input.GetAxisRaw("Horizontal"),
                Attack = UnityEngine.Input.GetButtonDown("Attack")
            };
            if (Input.JumpDown)
            {
                _lastJumpPressed = Time.time;
            }
        }
        #region Collision
        [Header("COLLISION")]

        [SerializeField, Tooltip("��ɫ��ײ����")]
        private Bounds _characterBounds;
        [SerializeField, Tooltip("���߼���Layer")]
        private LayerMask _groundLayer;
        [SerializeField, Tooltip("ÿ�����������������")]
        private int _detectorCount = 3;
        [SerializeField, Tooltip("���߼�����")]
        private float _detectionRayLength = 0.1f;
        [SerializeField, Tooltip("�������������Ե�Ļ�������С")]
        [Range(0.1f, 0.3f)]
        private float _rayBuffer = 0.1f; //������ֵ���Ծ�����������������ײ���ذ�

        private RayRange _raysUp, _raysRight, _raysDown, _raysLeft; // �ĸ������RayRange����
        private bool _colUp, _colRight, _colDown, _colLeft; // �ֱ��ʾ�ĸ������Ƿ�����ײ
        private float _timeLeftGrounded; // ��¼�뿪����ʱ��ʱ��

        private void RunCollisionChecks()
        {
            //��ʼ���ĸ������ϵ�RagRange����
            CalculateRayRanged();

            //Ground
            LandingThisFrame = false;
            var groundCheck = RunDetection(_raysDown);
            if (_colDown && !groundCheck) _timeLeftGrounded = Time.time; //���·�����ײ���� ��ǰ֡���µ����߼��ֵΪfalse��˵���������������뿪ƽ̨��Ե�ĵ�һ֡
            else if (!_colDown && groundCheck)
            {
                _coyoteUsable = true; // Only trigger when first touching
                LandingThisFrame = true;
            } //��صĵ�һ֡

            _colDown = groundCheck;
            _colUp = RunDetection(_raysUp);
            _colLeft = RunDetection(_raysLeft);
            _colRight = RunDetection(_raysRight);

            bool RunDetection(RayRange range)
            {
                return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, _detectionRayLength, _groundLayer));
            }
        }

        /// <summary>
        /// �����ĸ������Ϸ������ߵķ�Χ��
        /// ��ȷ�����ߵ�����ߣ��յ��ߣ�����
        /// Ӱ�������_raysUp, _raysRight, _raysDown, _raysLeft
        /// </summary>
        private void CalculateRayRanged()
        {
            //���ݵ�ǰλ�úͲ������趨�ļ��д�С������һ�����У��Լ��е��ĸ��߽�Ϊ׼���޸�RayRange
            var b = new Bounds(transform.position, _characterBounds.size);

            _raysDown = new RayRange(b.min.x + _rayBuffer, b.min.y, b.max.x - _rayBuffer, b.min.y, Vector2.down);
            _raysUp = new RayRange(b.min.x + _rayBuffer, b.max.y, b.max.x - _rayBuffer, b.max.y, Vector2.up);
            _raysLeft = new RayRange(b.min.x, b.min.y + _rayBuffer, b.min.x, b.max.y - _rayBuffer, Vector2.left);
            _raysRight = new RayRange(b.max.x, b.min.y + _rayBuffer, b.max.x, b.max.y - _rayBuffer, Vector2.right);
        }
        private IEnumerable<Vector2> EvaluateRayPositions(RayRange range)
        {
            for (var i = 0; i < _detectorCount; i++)
            {
                var t = (float)i / (_detectorCount - 1);
                yield return Vector2.Lerp(range.Start, range.End, t);
            }
        }


        private void OnDrawGizmos()
        {
            // Bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + _characterBounds.center, _characterBounds.size);

            // Rays
            if (!Application.isPlaying)
            {
                CalculateRayRanged();
                Gizmos.color = Color.blue;
                foreach (var range in new List<RayRange> { _raysUp, _raysRight, _raysDown, _raysLeft })
                {
                    foreach (var point in EvaluateRayPositions(range))
                    {
                        Gizmos.DrawRay(point, range.Dir * _detectionRayLength);
                    }
                }
            }

            if (!Application.isPlaying) return;

            // Draw the future position. Handy for visualizing gravity
            Gizmos.color = Color.red;
            var move = new Vector3(_currentHorizontalSpeed, _currentVerticalSpeed) * Time.deltaTime;
            Gizmos.DrawWireCube(transform.position + _characterBounds.center + move, _characterBounds.size);
        }
        #endregion

        #region Gravity
        [Header("GRAVITY")]

        [SerializeField, Tooltip("��������ٶ�")]
        private float _fallClamp = -40f;

        [SerializeField, Tooltip("��С������ٶ�")]
        private float _minFallSpeed = 80f;

        [SerializeField, Tooltip("���������ٶ�")]
        private float _maxFallSpeed = 120f;

        private float _fallSpeed; // ��ǰ������ٶ�
        /// <summary>
        /// ʵ������
        /// ���������������ٶ�
        /// ��ʹ�����Ǵӽϸߵ�ƽ̨������ʱ������Ծ�����У�������������һֱ���ٵ����ٶȹ��죬���Բٿء�
        /// ��������ߴ�����ʱ����ˮƽ�ƶ�һЩ�ٶȲ���
        /// ��ʹ����������Ծ�����п��Ը������ĵ�������ĺ����ƶ���
        /// </summary>
        private void CalculateGravity()
        {
            if (_colDown) //˵�������
            {
                //��غ�ֱ�ٶȹ���
                if (_currentVerticalSpeed < 0) _currentVerticalSpeed = 0;
            }
            else
            {
                // ���ɿ���Ծ�����Ҵ�ʱ��������������������ٶ�ʹPlayer���ټ��ٵ�����״̬
                float fallSpeed = _endedJumpEarly && _currentVerticalSpeed > 0 ? _fallSpeed * _jumpEndEarlyGravityModifier : _fallSpeed;

                // ���ݵ�ǰ������ٶȣ��޸ĵ�ǰ��ֱ�ٶ�
                _currentVerticalSpeed -= fallSpeed * Time.deltaTime;

                // ��Ϊ����������ٶȵ����ƣ�����ʱ���ܿ����������ٶ�
                if (_currentVerticalSpeed < _fallClamp) _currentVerticalSpeed = _fallClamp;
            }
        }
        #endregion

        #region Jump
        [SerializeField, Tooltip("��Ծ���ٶ�")]
        private float _jumpHeight = 30;

        [SerializeField, Tooltip("������ʱ�ٶ�С�ڸ�ֵʱ��Ϊ�ӽ���Ծ��ߵ���")]
        private float _jumpApexThreshold = 10f;

        [SerializeField, Tooltip("�뿪ƽ̨��Ե�Կ�������ʱ��")]
        private float _coyoteTimeThreshold = 0.1f;

        [SerializeField, Tooltip("�������ǰ����ʱ���ھͿ�����Ӧ��Ծ����")]
        private float _jumpBuffer = 0.1f;

        [SerializeField, Tooltip("�ж���Ծʱ���ӵ����¼��ٶȱ���")]
        private float _jumpEndEarlyGravityModifier = 3;

        private bool _coyoteUsable; // ��û����Ծ��
        private bool _endedJumpEarly = true; // �Ƿ��ж�����Ծ
        private float _apexPoint; // ����ʱΪ0��������ߵ�ʱΪ
        private float _lastJumpPressed; // �ϴΰ�����Ծ����ʱ��
        // �Ƿ�����ƽ̨��Ե���ҿ�������
        private bool CanUseCoyote => _coyoteUsable && !_colDown && _timeLeftGrounded + _coyoteTimeThreshold > Time.time;
        // �Ƿ�����غ��Զ�����
        private bool HasBufferedJump => _colDown && _lastJumpPressed + _jumpBuffer > Time.time;

        private void CalculateJumpApex()
        {
            if (!_colDown)
            {
                // Խ�ӽ���Ծ��ߵ㣨����ֱ�ٶȽӽ�0��ʱ�����¼��ٶ�Խ��
                _apexPoint = Mathf.InverseLerp(_jumpApexThreshold, 0, Mathf.Abs(Velocity.y));
                _fallSpeed = Mathf.Lerp(_minFallSpeed, _maxFallSpeed, _apexPoint);
            }
            else
            {
                _apexPoint = 0;
            }
        }

        private void CalculateJump()
        {
            if (Input.JumpDown && CanUseCoyote || HasBufferedJump)
            {
                _currentVerticalSpeed = _jumpHeight;    // ���ó�ʼ�ٶ�
                _endedJumpEarly = false;                // ��δ�ж���Ծ
                _coyoteUsable = false;                  // �Ѿ�����Ծ�У�ʹCanUseCoyoteһ��Ϊfalse
                _timeLeftGrounded = float.MinValue;     // -3.40282347E+38��ʹCanUseCoyoteһ��Ϊfalse
                JumpingThisFrame = true;

            }
            else
            {
                JumpingThisFrame = false; // �ڵ�ǰ֡û����Ծ
            }

            // ����ɿ�����Ծ���� ���Ҵ�ʱ��Player���������� ˵�����ж���Ծ
            if (!_colDown && Input.JumpUp && !_endedJumpEarly && Velocity.y > 0)
            {
                _endedJumpEarly = true;
            }

            // �������ײ�����ϰ����ǿ���ٶ�Ϊ�㣬
            if (_colUp && _currentVerticalSpeed > 0)
            {
                _currentVerticalSpeed = 0;
            }

        }
        #endregion


        #endregion

        #region Walk
        [Header("WALKING")]

        [SerializeField, Tooltip("���ٶ�")]
        private float _acceleration = 90;

        [SerializeField, Tooltip("����ƶ��ٶ�")]
        private float _moveClamp = 13;

        [SerializeField, Tooltip("���ٶ�")]
        private float _deAcceleration = 60f;

        [SerializeField, Tooltip("����Ծ�ж����ٵļӳ�ϵ��")]
        private float _apexBonus = 2;

        /// <summary>
        /// ���ݰ�������ײ������޸�Playerˮƽ�ٶȣ�ʵ�������ƶ�
        /// Ӱ�������_currentHorizontalSpeed
        /// </summary>
        private void CalculateWalk()
        {
            if (Input.X != 0)
            {
                //set horizontal move speed;
                _currentHorizontalSpeed += Input.X * _acceleration * Time.deltaTime;

                //��������ٶ�
                _currentHorizontalSpeed = Mathf.Clamp(_currentHorizontalSpeed, -_moveClamp, _moveClamp);

                // ������Ծ�߶ȶ��ٶȸ���ӳ�
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentHorizontalSpeed += apexBonus * Time.deltaTime;
            }
            else
            {
                //�ɿ��������𽥼���
                _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, _deAcceleration * Time.deltaTime);
            }

            // �����������ײ��ǽ�ڣ����ٶ�ǿ����� 0��������ǽ
            if (_currentHorizontalSpeed > 0 && _colRight || _currentHorizontalSpeed < 0 && _colLeft)
            {
                _currentHorizontalSpeed = 0;
            }
        }


        #endregion



        #region Move
        [Header("MOVE")]

        [SerializeField, Tooltip("��ײ��⾫��")]
        private int _freeColliderIterations = 10;

        /*�ƶ�Playerʱ�����Ǹ���Player��ǰ��ˮƽ����ֱ�ٶȣ��������һ֡PlayerӦ����λ�ã�
         * ������_characterBounds�Ĵ�С���ڶ�Ӧλ�ý�����ײ��⣬�����ʱ��û�������κ����壬��ֱ�Ӱ�Player�ƶ�����Ӧλ�ü��ɡ�
         * ����Ҫ����_freeColliderIterations������һС��һС����̽��
         ͬʱ�����ƶ������У�����Ҫ���һ���ݴ���
         �ڲ�һ���Ϳ�����ƽ̨ʱ�����û���ƽ̨
         ����ʱ������һ�����ƽ̨�ı�Ե�����û����ᱻƽ̨�赲��Ծ`*/

        private void MoveCharacter()
        {
            var pos = transform.position + _characterBounds.center;
            RawMovement = new Vector3(_currentHorizontalSpeed, _currentVerticalSpeed);
            var move = RawMovement * Time.deltaTime;
            var furthestPoint = pos + move;

            // check furthest movement. If nothing hit, move and don't do extra checks
            var hit = Physics2D.OverlapBox(furthestPoint, _characterBounds.size, 0, _groundLayer);
            if (!hit)
            {
                transform.position += move;
                return;
            }

            // otherwise increment away from current pos; see what closest position we can move to
            var positionToMoveTo = transform.position;
            for (int i = 1; i < _freeColliderIterations; i++)
            {
                // increment to check all but furthestPoint - we did that already
                var t = (float)i / _freeColliderIterations;
                var posToTry = Vector2.Lerp(pos, furthestPoint, t);

                if (Physics2D.OverlapBox(posToTry, _characterBounds.size, 0, _groundLayer))
                {
                    transform.position = positionToMoveTo;

                    // We've landed on a corner or hit our head on a ledge. Nudge the player gently
                    if (i == 1)
                    {
                        if (_currentVerticalSpeed < 0) _currentVerticalSpeed = 0;
                        var dir = transform.position - hit.transform.position;
                        transform.position += dir.normalized * move.magnitude;
                    }

                    return;
                }

                positionToMoveTo = posToTry;
            }
        }

        #endregion


        #region Hurt
        [Header("isHurt")]
        [SerializeField, Tooltip("���߼���Layer")]
        private LayerMask _enemyLayer;
        private bool _enemyColUp, _enemyColRight, _enemyColDown, _enemyColLeft; // �ֱ��ʾ�ĸ������Ƿ���boss������ײ
        private float coolTime = -1;     //���˵���ȴʱ��
        private float t = 0;

        [SerializeField, Tooltip("�����ٶ�")]
        public float RestoreSpeed = 10;     // �����ٶ�
        [SerializeField, Tooltip("timescaleĿ��")]
        public float changeTime = 0.05f;    // timescaleĿ��
        [SerializeField, Tooltip("����ʱ��")]
        public float Delay = 0.1f;          // ����ʱ��
        [SerializeField, Tooltip("����ʱ���ӵ�ˮƽ�ٶȣ�����ֵ��")]
        public float HurtHorizontalSpeed = 10f;
        [SerializeField, Tooltip("����ʱ���ӵĴ�ֱ�ٶȣ�����ֵ��")]
        public float HurtVerticallSpeed = 20f;


        private void isHurt()
        {



            //��ʼ���ĸ������ϵ�RagRange����
            CalculateRayRanged();

            _enemyColDown = RunDetection(_raysDown);
            _enemyColUp = RunDetection(_raysUp);
            _enemyColLeft = RunDetection(_raysLeft);
            _enemyColRight = RunDetection(_raysRight);
            coolTime = Mathf.Abs(Time.time - t);
            // coolTime���޵�ʱ��1.3s
            //���˺�ֹͣʱ�䣬��ʩ����
            if (_enemyColUp || _enemyColDown || _enemyColLeft || _enemyColRight && coolTime > 1.3f)
            {
                t = Time.time;
                if (!RestoreTime) StopTime();

                // ʩ����; ���ķ�����enemy�ķ�����
                // ����ʱӦ������ȫ�޷��ƶ���״̬
                if (_enemyColRight)
                    _currentHorizontalSpeed = -Mathf.Abs(HurtHorizontalSpeed);
                else if (_enemyColLeft)
                    _currentHorizontalSpeed = Mathf.Abs(HurtHorizontalSpeed);
                _currentVerticalSpeed = Mathf.Abs(HurtVerticallSpeed);

                hurtthisFrame = true;
                float tmp = coolTime;
                while (tmp > -10f)
                {
                    tmp -= Time.deltaTime;
                }
            }
            else hurtthisFrame = false;
            if (RestoreTime)
            {
                if (Time.timeScale < 1f)
                {
                    Time.timeScale += Time.deltaTime * RestoreSpeed;
                }
                else
                {
                    Time.timeScale = 1f;
                    RestoreTime = false;
                }
            }

            bool RunDetection(RayRange range)
            {
                return EvaluateRayPositions(range).Any(point => Physics2D.Raycast(point, range.Dir, _detectionRayLength, _enemyLayer));
            }
        }

        //set time scale to stoptime
        private void StopTime()
        {
            if (Delay > 0)
            {
                StopCoroutine(StartTimeAgain(Delay));
                StartCoroutine(StartTimeAgain(Delay));
            }
            else
            {
                RestoreTime = true;
            }
            Time.timeScale = changeTime;

        }
        
        IEnumerator StartTimeAgain(float amt)
        {
            RestoreTime = true;
            yield return new WaitForSecondsRealtime(amt);
        }
        #endregion

        #region Attack
        private void DoAttack()
        {
            if (Input.Attack)
            {
            }
        }
        #endregion
    }



}





