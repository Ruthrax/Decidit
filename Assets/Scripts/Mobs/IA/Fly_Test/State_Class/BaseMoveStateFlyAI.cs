using UnityEngine;
using UnityEngine.AI;

namespace State.FlyAI
{
    public class BaseMoveStateFlyAI : _StateFlyAI
    {
        [SerializeField] GlobalRefFlyAI globalRef;
        BaseMoveFlySO baseMoveFlySO;

        [SerializeField] GameObject flyAI;
        [SerializeField] Transform childflyAI;

        [SerializeField] bool dodgeObstacle;
        [SerializeField] bool right;
        [SerializeField] float offset;

        public override void InitState(StateControllerFlyAI stateController)
        {
            base.InitState(stateController);

            state = StateControllerFlyAI.AIState.BaseMove;
        }

        private void Start()
        {
            baseMoveFlySO = globalRef.baseMoveFlySO;
            baseMoveFlySO.currentRateAttack = (int)Random.Range(baseMoveFlySO.maxRateAttack.x, baseMoveFlySO.maxRateAttack.y);
        }

        private void Update()
        {
            SmoothLookAtYAxisPatrol();
        }

        private void FixedUpdate()
        {
            CheckObstacle();
        }

        void CheckObstacle()
        {
            RaycastHit hit;

            hit = RaycastAIManager.instanceRaycast.RaycastAI(transform.position, baseMoveFlySO.destinationFinal - flyAI.transform.position, baseMoveFlySO.maskObstacle, 
                Color.red,Vector3.Distance(flyAI.transform.position, baseMoveFlySO.destinationFinal));

            if(hit.transform != null)
            {
                dodgeObstacle = true;
                SearchNewPos();
            }
            else
            {
                if(dodgeObstacle)
                {
                    dodgeObstacle = false;
                }
            }
        }

        public void SmoothLookAtYAxisPatrol()
        {
            Vector3 relativePos;

            relativePos.x = baseMoveFlySO.destinationFinal.x - flyAI.transform.position.x;
            relativePos.y = baseMoveFlySO.destinationFinal.y - flyAI.transform.position.y;
            relativePos.z = baseMoveFlySO.destinationFinal.z - flyAI.transform.position.z;

            Quaternion rotation = Quaternion.Slerp(childflyAI.localRotation, Quaternion.LookRotation(relativePos, Vector3.up), baseMoveFlySO.speedRotationAIPatrol);
            childflyAI.localRotation = rotation;

            if (baseMoveFlySO.speedRotationAIPatrol < baseMoveFlySO.maxSpeedRotationAIPatrol)
            {
                baseMoveFlySO.speedRotationAIPatrol += (Time.deltaTime / baseMoveFlySO.smoothRotationPatrol);
                baseMoveFlySO.lerpSpeedYValuePatrol += (Time.deltaTime / baseMoveFlySO.ySpeedSmootherPatrol);
            }

            ApplyFlyingMove();
            DelayBeforeAttack();
        }
        ////////////// Set Destination \\\\\\\\\\\\\\\\\\\\\
        Vector3 SearchNewPos() // d�fini la position al�atoire choisi dans la fonction "RandomPointInBounds()" si la distance entre le point et l'IA est suffisament grande
        {
            if (baseMoveFlySO.distDestinationFinal < 20 || dodgeObstacle)
            {
                baseMoveFlySO.destinationFinal = RandomPointInBounds(globalRef.myCollider.bounds);

                baseMoveFlySO.newPosIsSet = false;
                baseMoveFlySO.speedRotationAIPatrol = 0;
                baseMoveFlySO.currentSpeedYPatrol = 0;
                baseMoveFlySO.lerpSpeedYValuePatrol = 0;
                return baseMoveFlySO.newPos = new Vector3(baseMoveFlySO.destinationFinal.x, baseMoveFlySO.destinationFinal.z);
            }
            else
            {
                baseMoveFlySO.newPosIsSet = true;
                baseMoveFlySO.speedRotationAIPatrol = 0;
                baseMoveFlySO.currentSpeedYPatrol = 0;
                baseMoveFlySO.lerpSpeedYValuePatrol = 0;

                Vector3 destinationFinal2D = new Vector2(baseMoveFlySO.destinationFinal.x, baseMoveFlySO.destinationFinal.z);
                Vector3 transformPos2D = new Vector2(flyAI.transform.position.x, flyAI.transform.position.z);

                baseMoveFlySO.timeGoToDestinationPatrol = Vector3.Distance(destinationFinal2D, transformPos2D) / globalRef.agent.speed;
                baseMoveFlySO.maxSpeedYTranslationPatrol = Mathf.Abs(baseMoveFlySO.destinationFinal.y - flyAI.transform.position.y) / baseMoveFlySO.timeGoToDestinationPatrol;

                return baseMoveFlySO.newPos = new Vector3(baseMoveFlySO.destinationFinal.x, baseMoveFlySO.destinationFinal.z);
            }
        }
        Vector3 RandomPointInBounds(Bounds bounds) // renvoie une position al�atoire dans un trigger collider
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        void ApplyFlyingMove()
        {
            baseMoveFlySO.distDestinationFinal = Vector3.Distance(flyAI.transform.position, baseMoveFlySO.destinationFinal);
            globalRef.agent.speed = baseMoveFlySO.baseMoveSpeed;

            if (baseMoveFlySO.newPosIsSet)
            {
                if (baseMoveFlySO.distDestinationFinal > 7)
                {
                    globalRef.agent.SetDestination(CheckNavMeshPoint(new Vector3(flyAI.transform.position.x, 0, flyAI.transform.position.z) + childflyAI.TransformDirection(Vector3.forward)));
                    baseMoveFlySO.currentSpeedYPatrol = Mathf.Lerp(baseMoveFlySO.currentSpeedYPatrol, baseMoveFlySO.maxSpeedYTranslationPatrol, baseMoveFlySO.lerpSpeedYValuePatrol);

                    if (Mathf.Abs(flyAI.transform.position.y - baseMoveFlySO.destinationFinal.y) > 1)
                    {
                        if (flyAI.transform.position.y < baseMoveFlySO.destinationFinal.y)
                        {
                            globalRef.agent.baseOffset += baseMoveFlySO.currentSpeedYPatrol * Time.deltaTime;
                        }
                        else
                        {
                            globalRef.agent.baseOffset -= baseMoveFlySO.currentSpeedYPatrol * Time.deltaTime;
                        }
                    }
                }
                else
                    baseMoveFlySO.newPosIsSet = false;
            }
            else
                SearchNewPos();
        }
        Vector3 CheckNavMeshPoint(Vector3 newDestination)
        {
            NavMeshHit closestHit;
            if (NavMesh.SamplePosition(newDestination, out closestHit, 1, 1))
            {
                newDestination = closestHit.position;
            }
            return newDestination;
        }

        void DelayBeforeAttack()
        {
            if (baseMoveFlySO.currentRateAttack > 0)
            {
                baseMoveFlySO.currentRateAttack -= Time.deltaTime;
            }
            else
            {
                stateControllerFlyAI.SetActiveState(StateControllerFlyAI.AIState.LockPlayer);
                //flyAINavMesh.SwitchToNewState(1);
            }
        }

        public void ForceLaunchAttack()
        {
            baseMoveFlySO.currentRateAttack = 0;
        }

        void OnDisable()
        {
            baseMoveFlySO.currentRateAttack = Random.Range(baseMoveFlySO.maxRateAttack.x, baseMoveFlySO.maxRateAttack.y);
            baseMoveFlySO.timeGoToDestinationPatrol = 0;
            baseMoveFlySO.maxSpeedYTranslationPatrol = 0;
            baseMoveFlySO.currentSpeedYPatrol = 0;
            baseMoveFlySO.lerpSpeedYValuePatrol = 0;
            baseMoveFlySO.speedRotationAIPatrol = 0;

            baseMoveFlySO.newPosIsSet = false;
        }

    }
}