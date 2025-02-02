using UnityEngine;
using UnityEngine.AI;

namespace State.AICAC
{
    public class BaseMoveStateAICAC : _StateAICAC
    {
        [SerializeField] GlobalRefAICAC globalRef;
        BaseMoveParameterAICAC baseMoveAICACSO;

        [Header("Nav Link")]
        [SerializeField] float maxDurationNavLink;
        bool triggerNavLink;
        public bool isOnNavLink;
        NavMeshLink link;
        NavMeshLink navLink;
        NavMeshHit closestHit;
        Vector3 linkDestination;

        [Header("Direction Movement")]
        [SerializeField] float offset;
        [SerializeField] LayerMask mask;
        Vector3 destination;
        Vector3 dir;
        Vector3 left;

        [Header("LookAt")]
        Vector3 direction;
        Vector3 relativePos;
        bool lookForwardJump;

        [Header("Rate Calcule Path")]
        [SerializeField] float maxRateRepath;
        [SerializeField] float currentRateRepath;

        [SerializeField] bool activeSurround;

        [SerializeField] float distToCirclePos;

        public override void InitState(StateControllerAICAC stateController)
        {
            base.InitState(stateController);

            state = StateControllerAICAC.AIState.BaseMove;
        }

        void OnEnable()
        {
            if (baseMoveAICACSO != null)
                baseMoveAICACSO.currentCoolDownAttack = Random.Range(baseMoveAICACSO.maxCoolDownAttack.x, baseMoveAICACSO.maxCoolDownAttack.y);

            if (globalRef != null && globalRef.myAnimator != null)
                AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "Walk");
        }

        private void Start()
        {
            baseMoveAICACSO = globalRef.baseMoveAICACSO;
        }

        private void Update()
        {
            //sphereDebug.position = destination;
            //globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Walkable"));
            //globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Not Walkable"));
            // globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Jump"));
            distToCirclePos = Vector3.Distance(destination, globalRef.transform.position);
            distToCirclePos = Vector3.Distance(globalRef.playerTransform.position, globalRef.transform.position);


            SmoothLookAt();
            ManageCurrentNavMeshLink();
            BaseMovement();
        }

        void CoolDownAttack()
        {
            if(baseMoveAICACSO.currentCoolDownAttack >0)
            {
                baseMoveAICACSO.currentCoolDownAttack -= Time.deltaTime;
                destination = CheckNavMeshPoint(globalRef.destinationSurround);

                if (Vector3.Distance(destination, globalRef.transform.position) < baseMoveAICACSO.distStopSurround || globalRef.agent.velocity.magnitude <1f)
                {
                    baseMoveAICACSO.currentCoolDownAttack = 0;
                }
            }
            else
            {
                baseMoveAICACSO.currentCoolDownAttack = Random.Range(baseMoveAICACSO.maxCoolDownAttack.x, baseMoveAICACSO.maxCoolDownAttack.y);
            }
        }

        void ActiveJump()
        {
            globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Jump"));
        }
        void ManageCurrentNavMeshLink()
        {
            if (globalRef.agent.isOnOffMeshLink)
            {
                lookForwardJump = true;
                globalRef.agent.autoTraverseOffMeshLink = false;

                if (navLink == null)
                {
                    globalRef.agent.ActivateCurrentOffMeshLink(false);
                    navLink = globalRef.agent.navMeshOwner as NavMeshLink;
                    globalRef.agentLinkMover.m_Curve.AddKey(0.5f, Mathf.Abs((navLink.endPoint.y - navLink.startPoint.y)/1.5f));
                    globalRef.agentLinkMover._height = Mathf.Abs((navLink.endPoint.y - navLink.startPoint.y) / 1.5f);
                }

                if (!isOnNavLink)
                {
                    isOnNavLink = true;
                    globalRef.agent.speed = 0;
                    AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "StartJump");
                }

                if (maxDurationNavLink > 0) // jump Current duration
                {
                    maxDurationNavLink -= Time.deltaTime;
                }
                else // jump End duration
                {
                    globalRef.agent.ActivateCurrentOffMeshLink(true);
                    AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "EndJump");
                }
            }
            else
            {
                lookForwardJump = false;

                if (navLink != null)
                {
                    globalRef.animEventAICAC.EndJump();
                    navLink.UpdateLink();
                    navLink = null;
                    maxDurationNavLink = globalRef.agentLinkMover._duration;
                }
            }
        }

        void BaseMovement()
        {
            if (isOnNavLink)
            {
                if (!triggerNavLink)
                {
                    linkDestination = navLink.transform.position - transform.position;
                    triggerNavLink = true;
                }
            }
            else
            {
                if(globalRef.offsetDestination !=0)
                {
                    offset = Mathf.Lerp(offset, globalRef.offsetDestination, baseMoveAICACSO.offsetTransitionSmooth * Time.deltaTime);
                    offset = Mathf.Clamp(offset, -Mathf.Abs(globalRef.offsetDestination), Mathf.Abs(globalRef.offsetDestination));
                }

                if (triggerNavLink)
                {
                    globalRef.agent.areaMask &= ~(1 << NavMesh.GetAreaFromName("Jump"));
                    triggerNavLink = false;
                    Invoke("ActiveJump", baseMoveAICACSO.jumpRate);
                }
            }

            if (globalRef.agent.enabled && globalRef != null)
            {
                if(currentRateRepath >0)
                {
                    currentRateRepath -= Time.deltaTime;
                }
                else
                {
                    if (activeSurround)
                    {
                        if (Vector3.Distance(globalRef.destinationSurround, globalRef.transform.position) > baseMoveAICACSO.distStopSurround)
                            destination = CheckNavMeshPoint(globalRef.destinationSurround);
                        else
                            activeSurround = false;
                    }
                    else
                    {
                        activeSurround = false;

                        if (baseMoveAICACSO.currentCoolDownAttack > 0)
                            CoolDownAttack();
                        else
                        {
                            dir = CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer - globalRef.transform.position;
                            left = Vector3.Cross(dir, Vector3.up).normalized;
                            Vector3 playerPosAnticip = CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer + (left * offset);
                            destination = CheckNavMeshPoint(playerPosAnticip);
                        }

                        if (Vector3.Distance(globalRef.playerTransform.position, globalRef.transform.position) > (globalRef.surroundManager.radius + baseMoveAICACSO.distStopSurround))
                        {
                            activeSurround = true;
                        }
                    }

                    if(!isOnNavLink)
                    {
                        SlowSpeed(globalRef.isInEylau);
                        globalRef.agent.SetDestination(destination);
                    }
                    currentRateRepath = maxRateRepath;
                }
            }

            if (Vector3.Distance(CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer, globalRef.transform.position) < baseMoveAICACSO.attackRange)//(globalRef.distPlayer < baseMoveAICACSO.attackRange)
            {
                if (!isOnNavLink)
                {
                    stateControllerAICAC.SetActiveState(StateControllerAICAC.AIState.BaseAttack);
                }
            }
            else
            {
                if (!isOnNavLink)
                    SpeedAdjusting();
            }
        }
        Vector3 CheckNavMeshPoint(Vector3 _destination)
        {
            if (NavMesh.SamplePosition(_destination, out closestHit, 1, 1))
            {
                _destination = closestHit.position;
            }
            return _destination;
        }
        void SpeedAdjusting()
        {
            if (!baseMoveAICACSO.activeAnticipDestination)
            {
                if (Vector3.Distance(destination, globalRef.transform.position) >= baseMoveAICACSO.distCanRun)
                {
                    if (globalRef.agent.speed < baseMoveAICACSO.runSpeed)
                    {
                        globalRef.agent.speed += baseMoveAICACSO.smoothSpeedRun * Time.deltaTime;
                    }
                    else
                        globalRef.agent.speed = baseMoveAICACSO.runSpeed;
                }
                else if (Vector3.Distance(destination, globalRef.transform.position) <= baseMoveAICACSO.distStopRun)
                {
                    if (globalRef.agent.speed > baseMoveAICACSO.baseSpeed)
                        globalRef.agent.speed -= baseMoveAICACSO.smoothSpeedbase * Time.deltaTime;
                    else
                        globalRef.agent.speed = baseMoveAICACSO.baseSpeed;
                }
                else
                {
                    if (globalRef.agent.speed < baseMoveAICACSO.baseSpeed)
                    {
                        globalRef.agent.speed += baseMoveAICACSO.smoothSpeedbase * Time.deltaTime;
                    }
                    else
                        globalRef.agent.speed = baseMoveAICACSO.baseSpeed;
                }
            }
            else
            {
                if (globalRef.agent.speed < baseMoveAICACSO.anticipSpeed)
                    globalRef.agent.speed += baseMoveAICACSO.smoothSpeedAnticip * Time.deltaTime;
                else
                    globalRef.agent.speed = baseMoveAICACSO.anticipSpeed;
            }
        }

        void SlowSpeed(bool active)
        {
            if(active)
            {
                globalRef.slowSpeedRot = globalRef.agent.speed / globalRef.slowRatio;
                globalRef.agent.speed = globalRef.slowSpeedRot;
            }
            else
            {
                if(globalRef.agent.speed == globalRef.slowSpeedRot)
                    globalRef.agent.speed *= globalRef.slowRatio;
            }
        }

        void SmoothLookAt()
        {
            if(lookForwardJump)
            {
                relativePos.x = linkDestination.x;
                relativePos.y = 0;
                relativePos.z = linkDestination.z;

                SlowRotation(globalRef.isInEylau);
                Quaternion rotation = Quaternion.Slerp(globalRef.transform.rotation, Quaternion.LookRotation(relativePos, Vector3.up), baseMoveAICACSO.speedRot);
                globalRef.transform.rotation = rotation;
            }
            else if(!isOnNavLink)
            {
                // direction = globalRef.transform.position + globalRef.agent.desiredVelocity;
                direction = globalRef.agent.desiredVelocity;

                relativePos.x = direction.x;
                relativePos.y = 0;
                relativePos.z = direction.z;

                SlowRotation(globalRef.isInEylau);
                Quaternion rotation = Quaternion.Slerp(globalRef.transform.rotation, Quaternion.LookRotation(relativePos, Vector3.up), baseMoveAICACSO.speedRot);
                globalRef.transform.rotation = rotation;
            }
        }
        void SlowRotation(bool active)
        {
            if(active)
            {
                if (baseMoveAICACSO.speedRot < (baseMoveAICACSO.maxSpeedRot / globalRef.slowRatio))
                {
                    baseMoveAICACSO.speedRot += Time.deltaTime / (baseMoveAICACSO.smoothRot * globalRef.slowRatio);
                }
                else
                {
                    baseMoveAICACSO.speedRot = (baseMoveAICACSO.maxSpeedRot / globalRef.slowRatio);
                }
            }
            else
            {
                if (baseMoveAICACSO.speedRot < baseMoveAICACSO.maxSpeedRot)
                {
                    baseMoveAICACSO.speedRot += Time.deltaTime / baseMoveAICACSO.smoothRot;
                }
                else
                {
                    baseMoveAICACSO.speedRot = baseMoveAICACSO.maxSpeedRot;
                }
            }
        }

        private void OnDisable()
        {
            globalRef.baseAttackAICACSO.isAttacking = false;
            currentRateRepath = 0;

            if (baseMoveAICACSO != null)
                baseMoveAICACSO.speedRot = 0;
        }
    }
}