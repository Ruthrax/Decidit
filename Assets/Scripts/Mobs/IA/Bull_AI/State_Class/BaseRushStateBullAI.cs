using UnityEngine;
using State.AICAC;

namespace State.AIBull
{
    public class BaseRushStateBullAI : _StateBull
    {
        [SerializeField] GlobalRefBullAI globalRef;
        RushBullParameterSO rushBullSO;
        [SerializeField] Material_Instances material_Instances;
        [SerializeField] bool lockPlayer;
        [SerializeField] bool canStartRush;

        [SerializeField] float distDestination;
        [SerializeField] float distDetectObstacle;
        [SerializeField] float distDetectGround;
        [SerializeField] float distFallStopRush;

        [Header("Rush Movement")]
        public Vector3 captureBasePosDistance;

        [Header("Position 2D")]
        Vector2 posPlayer;
        Vector2 posAI;

        public override void InitState(StateControllerBull stateController)
        {
            base.InitState(stateController);

            state = StateControllerBull.AIState.Rush;
        }

        private void OnEnable()
        {
            try
            {
                globalRef.agent.enabled = false;
                AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "PreAttack");
                //SoundManager.instance.PlaySoundMobOneShot(globalRef.audioSourceBull, SoundManager.instance.soundAndVolumeRushMob[0]);
                //Play SOUND PRE ATTACK RUSHER

                if (rushBullSO == null)
                    rushBullSO = globalRef.rushBullSO;
            }
            catch
            {
                //Debug.LogWarning("Missing Ref");
            }
        }

        private void Update()
        {
            SetDestination();
            SmoothLookAtPlayer();

            if (!canStartRush)
            {
                if (material_Instances.Material.color != material_Instances.ColorPreAtatck)
                    ShowSoonAttack(true);
            }
            else
            {
                if(material_Instances.Material.color == material_Instances.ColorPreAtatck)
                    ShowSoonAttack(false);
            }

            if(canStartRush)
            {
                RushMovement();
                RushDuration();
            }
        }
        private void FixedUpdate()
        {
            CheckObstacle();
        }

        void SetDestination()
        {
            if (!canStartRush && !lockPlayer)
            {
                rushBullSO.rushDestination = globalRef.playerTransform.position;
            }
            else
            {
                if (!lockPlayer)
                {
                    rushBullSO.rushDestination = globalRef.playerTransform.position + globalRef.transform.forward * rushBullSO.rushInertieSetDistance;
                    lockPlayer = true;
                    globalRef.launchRush = false;
                    Invoke("CheckSpeed", 1f);
                }
            }
        }

        void RushMovement()
        {
            rushBullSO.targetPos = new Vector2(rushBullSO.rushDestination.x, rushBullSO.rushDestination.z);
            posAI = new Vector2(globalRef.transform.position.x, globalRef.transform.position.z);

            rushBullSO.direction = rushBullSO.targetPos - posAI;
            rushBullSO.direction = rushBullSO.direction.normalized * rushBullSO.speedMove;

            SetGravity();
            SlowSpeed(globalRef.isInEylau);
            rushBullSO.move = new Vector3(rushBullSO.direction.x, rushBullSO.directionYSlope.y + rushBullSO.AIVelocity.y, rushBullSO.direction.y);
            globalRef.characterController.Move(rushBullSO.move * Time.deltaTime);

            globalRef.detectOtherAICollider.enabled = true;
            globalRef.hitBox.gameObject.SetActive(true);
        }
        void SetGravity()
        {
            if (!rushBullSO.isGround)
            {
                rushBullSO.fallingTime += Time.deltaTime;
                rushBullSO.effectiveGravity = rushBullSO.gravity * rushBullSO.fallingTime;
                rushBullSO.AIVelocity.y += rushBullSO.effectiveGravity;
            }
            else
            {
                rushBullSO.AIVelocity.y = 0;
            }
        }
        void SlowSpeed(bool active)
        {
            if (active)
            {
                globalRef.slowSpeed = rushBullSO.speedMove / globalRef.slowRatio;
                rushBullSO.direction = rushBullSO.direction.normalized * globalRef.slowSpeed;
            }
            else
            {
                rushBullSO.direction = rushBullSO.direction.normalized * rushBullSO.speedMove;
            }
        }

        void RushDuration()
        {
            posPlayer = new Vector2(globalRef.transform.position.x, globalRef.transform.position.z);
            distDestination = Vector3.Distance(posPlayer, rushBullSO.targetPos);

            if (distDestination <= 1)
            {
                //Debug.Log("Distance Stop Rush");
                StopRush();
            }

            rushBullSO.distRush = Vector3.Distance(captureBasePosDistance, globalRef.transform.position);
            if (rushBullSO.distRush >= rushBullSO.rushDistance)
            {
                StopRush();
            }
        }
        void CheckSpeed()
        {
            if(globalRef.characterController.velocity.magnitude ==0)
                StopRush();
        }


        void CheckObstacle()
        {
            //Check obstacle Ground
            rushBullSO.hitGround = RaycastAIManager.instanceRaycast.RaycastAI(globalRef.transform.position, -globalRef.transform.up, rushBullSO.maskCheckObstacle, Color.red, 100f);
            rushBullSO.directionYSlope = rushBullSO.move;

            if (Vector3.Angle(transform.up, rushBullSO.hitGround.normal) < globalRef.characterController.slopeLimit)
                rushBullSO.directionYSlope = (rushBullSO.directionYSlope - 
                    (Vector3.Dot(rushBullSO.directionYSlope, rushBullSO.hitGround.normal)) * rushBullSO.hitGround.normal);

            if (rushBullSO.hitGround.transform != null)
            {
                if(rushBullSO.isGround && rushBullSO.hitGround.distance > distFallStopRush)
                {
                    rushBullSO.isFall = true;
                }
                if(rushBullSO.hitGround.distance <= distDetectGround)
                {
                    rushBullSO.isGround = true;
                }
                else
                {
                    rushBullSO.isGround = false;
                }
            }
            else
            {
               // Debug.Log("Is Falling");
                rushBullSO.isGround = false;
            }

            //Check obstacle Wall
            rushBullSO.hitObstacle = RaycastAIManager.instanceRaycast.RaycastAI(globalRef.transform.position, globalRef.transform.forward, rushBullSO.maskCheckObstacle, Color.red, distDetectObstacle);
            if (rushBullSO.hitObstacle.transform != null)
            {
              //  Debug.Log("Obstacle Stop Rush");
              if (!rushBullSO.hitObstacle.transform.CompareTag("Ennemi"))
                StopRush();
            }
        }

        void SmoothLookAtPlayer()
        {
            rushBullSO.directionLookAt = rushBullSO.rushDestination;

            rushBullSO.relativePos.x = rushBullSO.directionLookAt.x - globalRef.transform.position.x;
            rushBullSO.relativePos.y = 0;
            rushBullSO.relativePos.z = rushBullSO.directionLookAt.z - globalRef.transform.position.z;

            SlowRotation(globalRef.isInEylau);
            Quaternion rotation = Quaternion.Slerp(globalRef.transform.rotation, Quaternion.LookRotation(rushBullSO.relativePos, Vector3.up), rushBullSO.speedRot);
            globalRef.transform.rotation = rotation;
        }
        void SlowRotation(bool active)
        {
            switch (active)
            {
                case true:
                    if (rushBullSO.speedRot < rushBullSO.maxSpeedRot)
                    {
                        globalRef.slowSpeedRot = globalRef.coolDownRushBullSO.smoothRot * globalRef.slowRatio;
                        rushBullSO.speedRot += Time.deltaTime / globalRef.slowSpeedRot;
                    }
                    else
                    {
                        if (!canStartRush)
                        {
                            ShowSoonAttack(false);
                            rushBullSO.speedRot = rushBullSO.maxSpeedRot;
                            AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "Rush");
                            canStartRush = true;
                        }
                    }
                    break;


                case false:
                    if (rushBullSO.speedRot < rushBullSO.maxSpeedRot)
                    {
                        rushBullSO.speedRot += Time.deltaTime / rushBullSO.smoothRot;
                    }
                    else
                    {
                        if (!canStartRush)
                        {
                            ShowSoonAttack(false);
                            rushBullSO.speedRot = rushBullSO.maxSpeedRot;
                            AnimatorManager.instance.SetAnimation(globalRef.myAnimator, globalRef.globalRefAnimator, "Rush");
                            canStartRush = true;
                        }
                    }
                    break;
            }
        }

        void ShowSoonAttack(bool active)
        {
            if(active)
            {
                material_Instances.Material.color = material_Instances.ColorPreAtatck;
                material_Instances.ChangeColorTexture(material_Instances.ColorPreAtatck);
            }
            else
            {
                material_Instances.Material.color = material_Instances.ColorBase;
                material_Instances.ChangeColorTexture(material_Instances.ColorBase);
            }
        }

        void StopRush()
        {
            stateController.SetActiveState(StateControllerBull.AIState.Idle);
        }
        private void OnDisable()
        {
            if(rushBullSO != null)
            {
                rushBullSO.isFall = false;
                rushBullSO.isGround = true;
                rushBullSO.speedRot = 0;
                rushBullSO.stopLockPlayer = false;
                rushBullSO.ennemiInCollider.Clear();
            }
            globalRef.launchRush = false;
            canStartRush = false;
            lockPlayer = false;
            globalRef.detectOtherAICollider.enabled = false;
            globalRef.hitBox.gameObject.SetActive(false);
            globalRef.agent.enabled = true;

        }

        private void OnTriggerEnter(Collider other)
        {
            if (gameObject.activeInHierarchy && other.name.Contains("AICAC"))
            {
                if (!rushBullSO.ennemiInCollider.Contains(other.gameObject) || rushBullSO.ennemiInCollider == null)
                    rushBullSO.ennemiInCollider.Add(other.gameObject);

                if (rushBullSO.ennemiInCollider != null)
                {
                    for (int i = 0; i < rushBullSO.ennemiInCollider.Count; i++)
                    {
                        GlobalRefAICAC globalRefAICAC = rushBullSO.ennemiInCollider[i].GetComponent<GlobalRefAICAC>();

                        rushBullSO.hitAICAC = RaycastAIManager.instanceRaycast.RaycastAI(transform.position, transform.forward, globalRef.ennemiMask, Color.red, 10f);
                        float angle;
                        angle = Vector3.SignedAngle(transform.forward, rushBullSO.hitAICAC.normal, Vector3.up);

                        if (globalRef.characterController.velocity.magnitude > 0)
                        {
                            if (angle > 0)
                            {
                                globalRefAICAC.dodgeAICACSO.targetObjectToDodge = this.transform;
                                globalRefAICAC.dodgeAICACSO.leftDodge = true;
                                globalRefAICAC.ActiveStateDodge();
                            }
                            else
                            {
                                globalRefAICAC.dodgeAICACSO.targetObjectToDodge = this.transform;
                                globalRefAICAC.dodgeAICACSO.rightDodge = true;
                                globalRefAICAC.ActiveStateDodge();
                            }
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Ennemi"))
            {
                rushBullSO.ennemiInCollider.Remove(other.gameObject);
            }
        }


        public float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
        {
            Vector3 perp = Vector3.Cross(fwd, targetDir);
            float dir = Vector3.Dot(perp, up);

            if (dir > 0.0f)
            {
                return 1.0f;
            }
            else if (dir < 0.0f)
            {
                return -1.0f;
            }
            else
            {
                return 0.0f;
            }
        }
    }
}