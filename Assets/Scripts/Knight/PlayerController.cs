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
            GatherInput(); // 获取输入

            RunCollisionChecks(); // 碰撞检测
            isHurt();

            CalculateWalk(); // 水平移动（计算水平速度）

            // 下坠/重力（计算垂直速度）
            CalculateJumpApex();
            CalculateGravity();

            CalculateJump(); // 跳跃（设置垂直速度）

            MoveCharacter(); // 移动角色



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

        [SerializeField, Tooltip("角色碰撞检测盒")]
        private Bounds _characterBounds;
        [SerializeField, Tooltip("射线检测的Layer")]
        private LayerMask _groundLayer;
        [SerializeField, Tooltip("每个方向发射的射线数量")]
        private int _detectorCount = 3;
        [SerializeField, Tooltip("射线检测距离")]
        private float _detectionRayLength = 0.1f;
        [SerializeField, Tooltip("发射线区域与边缘的缓冲区大小")]
        [Range(0.1f, 0.3f)]
        private float _rayBuffer = 0.1f; //增大数值可以尽量避免侧向的射线碰撞到地板

        private RayRange _raysUp, _raysRight, _raysDown, _raysLeft; // 四个方向的RayRange参数
        private bool _colUp, _colRight, _colDown, _colLeft; // 分别表示四个方向是否发生碰撞
        private float _timeLeftGrounded; // 记录离开地面时的时间

        private void RunCollisionChecks()
        {
            //初始化四个方向上的RagRange参数
            CalculateRayRanged();

            //Ground
            LandingThisFrame = false;
            var groundCheck = RunDetection(_raysDown);
            if (_colDown && !groundCheck) _timeLeftGrounded = Time.time; //脚下发生碰撞并且 当前帧向下的射线检测值为false，说明这是起跳或者离开平台边缘的第一帧
            else if (!_colDown && groundCheck)
            {
                _coyoteUsable = true; // Only trigger when first touching
                LandingThisFrame = true;
            } //落地的第一帧

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
        /// 计算四个方向上发射射线的范围；
        /// 即确定射线的起点线，终点线，方向。
        /// 影响参数：_raysUp, _raysRight, _raysDown, _raysLeft
        /// </summary>
        private void CalculateRayRanged()
        {
            //根据当前位置和参数中设定的检测盒大小，生成一个检测盒，以检测盒的四个边界为准，修改RayRange
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

        [SerializeField, Tooltip("最大下落速度")]
        private float _fallClamp = -40f;

        [SerializeField, Tooltip("最小下落加速度")]
        private float _minFallSpeed = 80f;

        [SerializeField, Tooltip("最大下落加速度")]
        private float _maxFallSpeed = 120f;

        private float _fallSpeed; // 当前下落加速度
        /// <summary>
        /// 实现重力
        /// 限制了下落的最大速度
        /// 这使得我们从较高的平台向下跳时，在跳跃过程中，不会由于重力一直加速导致速度过快，难以操控。
        /// 在跳到最高处附近时给予水平移动一些速度补偿
        /// 这使得我们在跳跃过程中可以更流畅的调整人物的横向移动。
        /// </summary>
        private void CalculateGravity()
        {
            if (_colDown) //说明落地了
            {
                //落地后垂直速度归零
                if (_currentVerticalSpeed < 0) _currentVerticalSpeed = 0;
            }
            else
            {
                // 当松开跳跃键并且此时还在上升，调大下落加速度使Player快速减速到下落状态
                float fallSpeed = _endedJumpEarly && _currentVerticalSpeed > 0 ? _fallSpeed * _jumpEndEarlyGravityModifier : _fallSpeed;

                // 依据当前下落加速度，修改当前垂直速度
                _currentVerticalSpeed -= fallSpeed * Time.deltaTime;

                // 因为有最大下落速度的限制，向下时不能快过最大下落速度
                if (_currentVerticalSpeed < _fallClamp) _currentVerticalSpeed = _fallClamp;
            }
        }
        #endregion

        #region Jump
        [SerializeField, Tooltip("跳跃初速度")]
        private float _jumpHeight = 30;

        [SerializeField, Tooltip("当上升时速度小于该值时认为接近跳跃最高点了")]
        private float _jumpApexThreshold = 10f;

        [SerializeField, Tooltip("离开平台边缘仍可起跳的时间")]
        private float _coyoteTimeThreshold = 0.1f;

        [SerializeField, Tooltip("在离落地前多少时间内就可以响应跳跃按键")]
        private float _jumpBuffer = 0.1f;

        [SerializeField, Tooltip("中断跳跃时附加的乡下加速度倍数")]
        private float _jumpEndEarlyGravityModifier = 3;

        private bool _coyoteUsable; // 并没在跳跃中
        private bool _endedJumpEarly = true; // 是否中断了跳跃
        private float _apexPoint; // 起跳时为0，跳到最高点时为
        private float _lastJumpPressed; // 上次按下跳跃键的时间
        // 是否脱离平台边缘并且可以跳起
        private bool CanUseCoyote => _coyoteUsable && !_colDown && _timeLeftGrounded + _coyoteTimeThreshold > Time.time;
        // 是否在落地后自动跳起
        private bool HasBufferedJump => _colDown && _lastJumpPressed + _jumpBuffer > Time.time;

        private void CalculateJumpApex()
        {
            if (!_colDown)
            {
                // 越接近跳跃最高点（即垂直速度接近0）时，向下加速度越大
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
                _currentVerticalSpeed = _jumpHeight;    // 设置初始速度
                _endedJumpEarly = false;                // 并未中断跳跃
                _coyoteUsable = false;                  // 已经在跳跃中，使CanUseCoyote一定为false
                _timeLeftGrounded = float.MinValue;     // -3.40282347E+38，使CanUseCoyote一定为false
                JumpingThisFrame = true;

            }
            else
            {
                JumpingThisFrame = false; // 在当前帧没有跳跃
            }

            // 如果松开了跳跃键， 并且此时的Player还在上升， 说明是中断跳跃
            if (!_colDown && Input.JumpUp && !_endedJumpEarly && Velocity.y > 0)
            {
                _endedJumpEarly = true;
            }

            // 如果向上撞到了障碍物，得强制速度为零，
            if (_colUp && _currentVerticalSpeed > 0)
            {
                _currentVerticalSpeed = 0;
            }

        }
        #endregion


        #endregion

        #region Walk
        [Header("WALKING")]

        [SerializeField, Tooltip("加速度")]
        private float _acceleration = 90;

        [SerializeField, Tooltip("最大移动速度")]
        private float _moveClamp = 13;

        [SerializeField, Tooltip("减速度")]
        private float _deAcceleration = 60f;

        [SerializeField, Tooltip("在跳跃中对移速的加成系数")]
        private float _apexBonus = 2;

        /// <summary>
        /// 根据按键和碰撞情况，修改Player水平速度，实现左右移动
        /// 影响参数：_currentHorizontalSpeed
        /// </summary>
        private void CalculateWalk()
        {
            if (Input.X != 0)
            {
                //set horizontal move speed;
                _currentHorizontalSpeed += Input.X * _acceleration * Time.deltaTime;

                //限制最大速度
                _currentHorizontalSpeed = Mathf.Clamp(_currentHorizontalSpeed, -_moveClamp, _moveClamp);

                // 根据跳跃高度对速度给予加成
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentHorizontalSpeed += apexBonus * Time.deltaTime;
            }
            else
            {
                //松开按键后，逐渐减速
                _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, _deAcceleration * Time.deltaTime);
            }

            // 如果左右两侧撞到墙壁，则将速度强制设成 0，不允许穿墙
            if (_currentHorizontalSpeed > 0 && _colRight || _currentHorizontalSpeed < 0 && _colLeft)
            {
                _currentHorizontalSpeed = 0;
            }
        }


        #endregion



        #region Move
        [Header("MOVE")]

        [SerializeField, Tooltip("碰撞检测精度")]
        private int _freeColliderIterations = 10;

        /*移动Player时，我们根据Player当前的水平、垂直速度，计算出下一帧Player应处的位置，
         * 并依据_characterBounds的大小，在对应位置进行碰撞检测，如果此时并没有碰到任何物体，则直接把Player移动到对应位置即可。
         * 否则要根据_freeColliderIterations参数，一小步一小步试探。
         同时，在移动过程中，我们要完成一点容错处理：
         在差一点点就可跳上平台时，帮用户上平台
         起跳时碰到了一点点上平台的边缘，让用户不会被平台阻挡跳跃`*/

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
        [SerializeField, Tooltip("射线检测的Layer")]
        private LayerMask _enemyLayer;
        private bool _enemyColUp, _enemyColRight, _enemyColDown, _enemyColLeft; // 分别表示四个方向是否与boss发生碰撞
        private float coolTime = -1;     //受伤的冷却时间
        private float t = 0;

        [SerializeField, Tooltip("回正速度")]
        public float RestoreSpeed = 10;     // 回正速度
        [SerializeField, Tooltip("timescale目标")]
        public float changeTime = 0.05f;    // timescale目标
        [SerializeField, Tooltip("回正时间")]
        public float Delay = 0.1f;          // 回正时间
        [SerializeField, Tooltip("受伤时附加的水平速度（绝对值）")]
        public float HurtHorizontalSpeed = 10f;
        [SerializeField, Tooltip("受伤时附加的垂直速度（绝对值）")]
        public float HurtVerticallSpeed = 20f;


        private void isHurt()
        {



            //初始化四个方向上的RagRange参数
            CalculateRayRanged();

            _enemyColDown = RunDetection(_raysDown);
            _enemyColUp = RunDetection(_raysUp);
            _enemyColLeft = RunDetection(_raysLeft);
            _enemyColRight = RunDetection(_raysRight);
            coolTime = Mathf.Abs(Time.time - t);
            // coolTime的无敌时间1.3s
            //受伤后停止时间，并施加力
            if (_enemyColUp || _enemyColDown || _enemyColLeft || _enemyColRight && coolTime > 1.3f)
            {
                t = Time.time;
                if (!RestoreTime) StopTime();

                // 施加力; 力的方向是enemy的反方向
                // 受伤时应该是完全无法移动的状态
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





