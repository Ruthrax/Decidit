using UnityEngine;
using UnityEngine.AI;

namespace State.AICAC
{
    public class BaseMoveStateAICAC : _StateAICAC
    {
        [SerializeField] GlobalRefAICAC globalRef;

        [SerializeField] float maxDurationNavLink;
        [SerializeField] bool linkIsActive;
        NavMeshLink navLink;

        public override void InitState(StateControllerAICAC stateController)
        {
            base.InitState(stateController);

            state = StateControllerAICAC.AIState.BaseMove;
        }

        private void Update()
        {
            if (state == StateControllerAICAC.AIState.BaseMove)
            {
                SmoothLookAt();
                BaseMovement();
            }

            if (globalRef.agent.isOnOffMeshLink)
            {
                globalRef.agent.speed = 3;

                if(navLink == null)
                    navLink = globalRef.agent.navMeshOwner as NavMeshLink;

                if (maxDurationNavLink >= 0.1f)
                {
                    globalRef.agent.ActivateCurrentOffMeshLink(false);
                    linkIsActive = false;
                    Debug.Log(linkIsActive);
                    maxDurationNavLink -= Time.deltaTime;
                }
                else
                {
                    linkIsActive = true;
                    Debug.Log(linkIsActive);
                    globalRef.agent.ActivateCurrentOffMeshLink(true);
                }
            }
            else
            {
                if(navLink != null)
                {
                    navLink.UpdateLink();
                    navLink = null;
                }
                maxDurationNavLink = globalRef.agentLinkMover._duration;
            }
        }

        void BaseMovement()
        {
            Vector3 dir = globalRef.playerTransform.position - globalRef.transform.position;
            Vector3 left = Vector3.Cross(dir, Vector3.up).normalized;

            Vector3 destination = globalRef.playerTransform.position + left * globalRef.offsetDestination;

            globalRef.debugDestination = CheckNavMeshPoint(destination);

            globalRef.agent.SetDestination(CheckNavMeshPoint(destination));

            if (globalRef.distPlayer < globalRef.baseMoveAICACSO.attackRange)
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
            NavMeshHit closestHit;
            if (NavMesh.SamplePosition(_destination, out closestHit, 1, 1))
            {
                _destination = closestHit.position;
                return _destination;
            }
            else
            {
                _destination = globalRef.playerTransform.position;
                return _destination;
            }
        }
        void SpeedAdjusting()
        {
            if (!globalRef.baseMoveAICACSO.activeAnticipDestination)
            {
                if (globalRef.distPlayer >= globalRef.baseMoveAICACSO.distCanRun)
                {
                    if (globalRef.agent.speed < globalRef.baseMoveAICACSO.runSpeed)
                    {
                        globalRef.agent.speed += globalRef.baseMoveAICACSO.smoothSpeedRun * Time.deltaTime;
                    }
                    else
                        globalRef.agent.speed = globalRef.baseMoveAICACSO.runSpeed;
                }
                else if (globalRef.distPlayer <= globalRef.baseMoveAICACSO.distStopRun)
                {
                    if (globalRef.agent.speed > globalRef.baseMoveAICACSO.baseSpeed)
                        globalRef.agent.speed -= globalRef.baseMoveAICACSO.smoothSpeedbase * Time.deltaTime;
                    else
                        globalRef.agent.speed = globalRef.baseMoveAICACSO.baseSpeed;
                }
                else
                {
                    if (globalRef.agent.speed < globalRef.baseMoveAICACSO.baseSpeed)
                    {
                        globalRef.agent.speed += globalRef.baseMoveAICACSO.smoothSpeedbase * Time.deltaTime;
                    }
                    else
                        globalRef.agent.speed = globalRef.baseMoveAICACSO.baseSpeed;
                }
            }
            else
            {
                if (globalRef.agent.speed < globalRef.baseMoveAICACSO.anticipSpeed)
                    globalRef.agent.speed += globalRef.baseMoveAICACSO.smoothSpeedAnticip * Time.deltaTime;
                else
                    globalRef.agent.speed = globalRef.baseMoveAICACSO.anticipSpeed;
            }
        }

        void SmoothLookAt()
        {
            Vector3 direction;
            Vector3 relativePos;

            direction = globalRef.agent.destination;
            relativePos.x = direction.x - globalRef.transform.position.x;
            relativePos.y = 0;
            relativePos.z = direction.z - globalRef.transform.position.z;

            if (globalRef.baseMoveAICACSO.speedRot < globalRef.baseMoveAICACSO.maxSpeedRot)
                globalRef.baseMoveAICACSO.speedRot += Time.deltaTime / globalRef.baseMoveAICACSO.smoothRot;
            else
            {
                globalRef.baseMoveAICACSO.speedRot = globalRef.baseMoveAICACSO.maxSpeedRot;
            }

            Quaternion rotation = Quaternion.Slerp(globalRef.transform.rotation, Quaternion.LookRotation(relativePos, Vector3.up), globalRef.baseMoveAICACSO.speedRot);
            globalRef.transform.rotation = rotation;
        }

        private void OnDisable()
        {
            globalRef.baseAttackAICACSO.isAttacking = false;
            globalRef.baseMoveAICACSO.speedRot = 0;
        }
    }
}