using UnityEngine;
using UnityEngine.AI;

namespace State.AICAC
{
    public class BaseMoveStateAICAC : _StateAICAC
    {
        [SerializeField] GlobalRefAICAC globalRef;
        BaseMoveParameterAICAC baseMoveAICACSO;

        [SerializeField] Transform sphereDebug;

        [Header("Nav Link")]
        [SerializeField] float maxDurationNavLink;
        [SerializeField] bool linkIsActive;
        bool triggerNavLink;
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

        [Header("Rate Calcule Path")]
        [SerializeField] float maxRateRepath;
        [SerializeField] float currentRateRepath;

        bool activeSurround;

        public override void InitState(StateControllerAICAC stateController)
        {
            base.InitState(stateController);

            state = StateControllerAICAC.AIState.BaseMove;
        }

        private void Awake()
        {
            baseMoveAICACSO = globalRef.baseMoveAICACSO;
        }

        private void Update()
        {

            //sphereDebug.position = destination;
            //globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Walkable"));
            //globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Not Walkable"));
            // globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Jump"));

            SmoothLookAt();
            ManageCurrentNavMeshLink();
            BaseMovement();

        }

        void OnEnable()
        {
            activeSurround = true;
        }

        void ActiveJump()
        {
            globalRef.agent.areaMask |= (1 << NavMesh.GetAreaFromName("Jump"));
        }
        void ManageCurrentNavMeshLink()
        {
            if (globalRef.agent.isOnOffMeshLink)
            {
                if (maxDurationNavLink > 0)
                {
                    globalRef.agent.ActivateCurrentOffMeshLink(false);
                    linkIsActive = false;
                    maxDurationNavLink -= Time.deltaTime;
                }
                else
                {
                    linkIsActive = true;
                    globalRef.agent.ActivateCurrentOffMeshLink(true);
                }

                globalRef.agent.speed = 3;
                if (navLink == null)
                    navLink = globalRef.agent.navMeshOwner as NavMeshLink;

            }
            else
            {
                if (navLink != null)
                {
                    navLink.UpdateLink();
                    navLink = null;
                }
                maxDurationNavLink = globalRef.agentLinkMover._duration;
            }
        }

        void BaseMovement()
        {
            dir = CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer - globalRef.transform.position;
            left = Vector3.Cross(dir, Vector3.up).normalized;

            if (globalRef.agent.isOnOffMeshLink)
            {
                link = globalRef.agent.navMeshOwner as NavMeshLink;
                
                if (!triggerNavLink)
                {
                    linkDestination = link.transform.position - transform.position;
                    triggerNavLink = true;
                }
            }
            else
            {
                offset = Mathf.Lerp(offset, globalRef.offsetDestination, baseMoveAICACSO.offsetTransitionSmooth * Time.deltaTime);
                offset = Mathf.Clamp(offset, -Mathf.Abs(globalRef.offsetDestination), Mathf.Abs(globalRef.offsetDestination));


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
                    SlowSpeed(globalRef.isInEylau);

                    Vector3 playerPosAnticip = CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer + left * offset;
                    //destination = CheckNavMeshPoint(playerPosAnticip + (transform.forward * baseMoveAICACSO.lenghtBack));
                    globalRef.agent.isStopped = false;
                    //globalRef.agent.SetDestination(destination);
                    if (Vector3.Distance(destination, globalRef.transform.position) > 1f && activeSurround)
                        destination = CheckNavMeshPoint(globalRef.destination);
                    else
                    {
                        activeSurround = false;
                        destination = CheckNavMeshPoint(CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer);
                    }

                    globalRef.agent.SetDestination(destination);
                    currentRateRepath = maxRateRepath;
                }
            }

            if (Vector3.Distance(CheckPlayerDownPos.instanceCheckPlayerPos.positionPlayer, globalRef.transform.position) < baseMoveAICACSO.attackRange)//(globalRef.distPlayer < baseMoveAICACSO.attackRange)
            {
                stateControllerAICAC.SetActiveState(StateControllerAICAC.AIState.BaseAttack);
            }
            else
            {
                if (!globalRef.agent.isOnOffMeshLink)
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
                if (globalRef.distPlayer >= baseMoveAICACSO.distCanRun)
                {
                    if (globalRef.agent.speed < baseMoveAICACSO.runSpeed)
                    {
                        globalRef.agent.speed += baseMoveAICACSO.smoothSpeedRun * Time.deltaTime;
                    }
                    else
                        globalRef.agent.speed = baseMoveAICACSO.runSpeed;
                }
                else if (globalRef.distPlayer <= baseMoveAICACSO.distStopRun)
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
            if (globalRef.agent.isOnOffMeshLink)
            {
                direction = linkDestination - transform.position;

                relativePos.x = linkDestination.x;
                relativePos.y = 0;
                relativePos.z = linkDestination.z;

            }
            else
            {
                // direction = globalRef.transform.position + globalRef.agent.desiredVelocity;
                direction = globalRef.agent.desiredVelocity;

                relativePos.x = direction.x;
                relativePos.y = 0;
                relativePos.z = direction.z;
            }

            SlowRotation(globalRef.isInEylau);

            Quaternion rotation = Quaternion.Slerp(globalRef.transform.rotation, Quaternion.LookRotation(relativePos, Vector3.up), baseMoveAICACSO.speedRot);
            globalRef.transform.rotation = rotation;
        }
        void SlowRotation(bool active)
        {
            if(active)
            {
                if (baseMoveAICACSO.speedRot < baseMoveAICACSO.maxSpeedRot)
                {
                    globalRef.slowSpeedRot = baseMoveAICACSO.smoothRot* globalRef.slowRatio;
                    baseMoveAICACSO.speedRot += Time.deltaTime / globalRef.slowSpeedRot;
                }
                else
                {
                    baseMoveAICACSO.speedRot = baseMoveAICACSO.maxSpeedRot;
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
            baseMoveAICACSO.speedRot = 0;
            currentRateRepath = 0;
        }
    }
}