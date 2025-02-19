﻿using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Newtonsoft.Json.Linq;

[RequireComponent(typeof(NPCHealth))]
public class ZombieBehaviour : MonoBehaviour, ISaveable
{
    public enum npcStates { Patrol, Walk, Run, Attack }

    private NPCHealth npcHealth;
    private NavMeshAgent _agent;
    private GameObject playerObject;
    private RandomHelper rand = new RandomHelper();

    private AnimatorStateInfo stateInfo;
    private AnimatorTransitionInfo transInfo;

    private GameObject PlayerCam;

    [Header("Configuração Principal")]
    public Animator _Animator;
    public LayerMask searchMask;
    public LayerMask attractionMask;
    public PatrolPoint[] patrolPoints;

    [Header("Sensores")]
    public WaypointGroup Waypoints;
    public Vector3 NPCHead;

    [Header("Sensor Configs")]
    [Range(0, 179)]
    public float FOVAngle = 110;
    public float attackColliderHeight = 0.3f;
    public float smoothLookAt = 3f;
    public float smoothStopAgent = 2f;
    public bool enableLookAt;

    [Header("AI Configs")]
    [Range(0, 2)]
    public int intelligence;
    [Tooltip("Tempo de Patrulha (From-To)")]
    public Vector2 patrolTime = new Vector2(1, 2);
    public float patrolPointTime = 6f;
    public float walkSpeed = 0.4f;
    public float runSpeed = 5.5f;
    public float turnStateCutTime;
    public bool walkInPlace;
    public bool randomPatrol;
    public bool waypointPatrol;
    public bool enableGizmos;

    [Header("AI Dano Configs")]
    [Tooltip("Dano de Ataque dado ao Jogador (From -To)")]
    public Vector2 AttackDamage;

    [Header("Distancia Configs")]
    public float stoppingDistance = 0.6f;
    public float changeStateDistance = 1f;
    public float seeDistance = 15f;
    public float chaseDistance = 4f;
    public float chaseAttackDistance = 1.5f;
    public float attackDistance = 1.7f;
    public float attractionDistance = 15f;
    public float patrolPointDetect = 2f;

    [Header("AI Sons")]
    public AudioClip[] AttackSound;

    private bool isAttracted;
    private bool isBlocked;
    private bool isDead;
    private bool lookAt;
    private bool rootPosition;
    private bool playerChased;
    private bool goLastPos;
    private bool pathComplete;

    private bool playerDead;
    private bool patrolPending;
    private bool disablePending;
    private bool isTurning;
    private bool patrol;
    private bool turn;
    private bool load;

    private int waypoint;
    private Vector3 lastPlayerPos;
    private Vector3 patrolPointPos;
    private Vector3 playerHeadPos;
    private Vector3 npcHeadPos;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        playerObject = Camera.main.transform.root.gameObject;
        patrolPoints = FindObjectsOfType<PatrolPoint>();

        if (GetComponent<NPCHealth>())
        {
            npcHealth = GetComponent<NPCHealth>();
        }

        PlayerCam = Camera.main.gameObject;
    }

    void Start()
    {
        _agent.stoppingDistance = stoppingDistance;
        _agent.updatePosition = false;
        _agent.updateRotation = true;
        _agent.isStopped = false;
        rootPosition = true;
    }

    void Update()
    {
        playerDead = playerObject.GetComponentInChildren<HealthManager>().isDead;
        stateInfo = _Animator.GetCurrentAnimatorStateInfo(0);
        transInfo = _Animator.GetAnimatorTransitionInfo(0);

        Vector3 camPos = PlayerCam.transform.position;
        playerHeadPos = new Vector3(PlayerCam.transform.root.position.x, camPos.y, PlayerCam.transform.root.position.z);

        Vector3 worldDeltaPosition = _agent.nextPosition - transform.position;
        _agent.nextPosition = transform.position + 0.9f * worldDeltaPosition;

        if (!npcHealth || isDead || load) return;

        npcHeadPos = transform.position;
        npcHeadPos += NPCHead;

        if (SearchForPlayer())
        {
            lastPlayerPos = playerObject.transform.position;
            SetNewDestination(playerObject.transform.position);

            rootPosition = false;
            _agent.speed = runSpeed;

            if (!disablePending) { DisablePendingAnimations(); disablePending = true; }

            if (PlayerDistance() <= attackDistance)
            {
                SetAnimatorState(npcStates.Attack, true, false);
                lookAt = enableLookAt;
            }
            else
            {
                SetAnimatorState(npcStates.Attack, false, false);
                lookAt = false;
            }

            if (PlayerDistance() >= chaseAttackDistance)
            {
                SetAnimatorState(npcStates.Run, true, false);
                SetAnimatorState(npcStates.Patrol, false, false);
                StopAgent(false);
            }
            else
            {
                SetAnimatorState(npcStates.Run, false, false);
                SetAnimatorState(npcStates.Patrol, true, false);
                StopAgent(true);
            }

            playerChased = true;
            goLastPos = false;
        }
        else
        {
            StopAgent(false);

            if (playerChased)
            {
                if (!goLastPos)
                {
                    SetNewDestination(lastPlayerPos);
                    goLastPos = true;
                }

                if (patrolPoints.Length > 0)
                {
                    foreach (var point in patrolPoints)
                    {
                        if (point.InTrigger)
                        {
                            patrolPointPos = point.transform.position;
                        }
                    }
                }

                if (PathCompleted())
                {
                    if (intelligence > 0)
                    {
                        if (patrolPointPos != Vector3.zero)
                        {
                            float distance = Vector3.Distance(lastPlayerPos, patrolPointPos);
                            Debug.Log("Distância atual: " + distance + " Detectar em: " + patrolPointDetect);

                            if (distance <= patrolPointDetect)
                            {
                                Debug.Log("Configurando o Destino para o Ponto de Patrulha");

                                patrol = true;
                                rootPosition = true;

                                StopAllCoroutines();
                                StartCoroutine(PatrolPointSequence());

                                foreach (var i in patrolPoints)
                                {
                                    i.zombieInTrigger = false;
                                }
                            }
                        }
                    }

                    playerChased = false;
                    isAttracted = false;
                }
            }
            else
            {
                if (!patrol)
                {
                    if (!isAttracted)
                    {
                        disablePending = false;
                        playerChased = false;
                        rootPosition = !walkInPlace;
                        _agent.speed = walkSpeed;
                        WaypointSequence();
                    }
                    else if (isAttracted && !turn && !isTurning)
                    {
                        StartCoroutine(Patrol(7));
                    }
                }
            }

            if (turn)
            {
                rootPosition = true;
            }
        }

        //Atraia Zumbi com um tiro
        if (intelligence > 0 && npcHealth.damageTaken && !isAttracted)
        {
            AttractZombie(playerObject.transform.position);
            isAttracted = true;
        }

        //Atrair Zumbi pelo Efeito de Impacto
        if (intelligence > 1 && !isAttracted)
        {
            Vector3 attraction = SoundDetection();

            if (attraction != Vector3.zero)
            {
                SoundAttract(attraction);
                isAttracted = true;
                patrol = true;
            }
        }
    }

    private void OnAnimatorMove()
    {
        Vector3 position = _Animator.rootPosition;
        position.y = _agent.nextPosition.y;

        _agent.updatePosition = !rootPosition;

        if (rootPosition)
        {
            transform.position = position;
            transform.rotation = _Animator.rootRotation;
        }
        else if (lookAt)
        {
            Quaternion rotation = Quaternion.LookRotation(playerObject.transform.position - transform.position);
            Quaternion rot = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * smoothLookAt);
            rot.x = 0; rot.z = 0;
            transform.rotation = rot;
        }
    }


    void WaypointSequence()
    {
        if (PathCompleted())
        {
            if (waypointPatrol)
            {
                if (!patrolPending)
                {
                    WalkToDestination(Waypoints.Waypoints[NextWaypoint()].position);
                }
                else
                {
                    StartCoroutine(Patrol(Mathf.RoundToInt(Random.Range(patrolTime.x, patrolTime.y))));
                }

                patrolPending = true;
            }
            else
            {
                WalkToDestination(Waypoints.Waypoints[NextWaypoint()].position);
            }
        }
    }

    private void StartTurn(Vector3 target)
    {
        Vector3 relative = transform.InverseTransformPoint(target);
        int angle = Mathf.RoundToInt(Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg);
        StartCoroutine(TurnAngle(angle));
    }

    IEnumerator TurnAngle(float angle)
    {
        if (!isTurning)
        {
            turn = true;
            _agent.updateRotation = false;
            DisablePendingAnimations();
            _Animator.SetFloat("Angle", angle);
            _Animator.SetTrigger("Turn");
            isTurning = true;
        }

        if (angle >= 45 || angle <= -45)
        {
            yield return new WaitForSeconds(stateInfo.length - turnStateCutTime);
        }

        turn = false;
        isTurning = false;

        yield return null;
    }

    IEnumerator Patrol(int wait)
    {
        SetAnimatorState(npcStates.Patrol, true, true);
        patrol = true;

        yield return new WaitForSeconds(wait);

        patrol = false;
        patrolPending = false;
        isAttracted = false;

        yield return null;
    }

    private int NextWaypoint()
    {
        if (randomPatrol && Waypoints.Waypoints.Count > 1)
        {
            return waypoint = rand.Range(0, Waypoints.Waypoints.Count);
        }
        else
        {
            return waypoint == Waypoints.Waypoints.Count - 1 ? 0 : waypoint + 1;
        }
    }

    private void WalkToDestination(Vector3 destination)
    {
        pathComplete = false;
        rootPosition = !walkInPlace;
        SetNewDestination(destination);
        _agent.speed = walkSpeed;
        SetAnimatorState(npcStates.Walk, true, true);
    }

    private void RunToDestination(Vector3 destination)
    {
        rootPosition = false;
        _agent.speed = runSpeed;
        SetNewDestination(destination);
        SetAnimatorState(npcStates.Run, true, true);
    }

    private void AttractZombie(Vector3 AttractPos)
    {
        StartTurn(AttractPos);
        npcHealth.damageTaken = false;
    }

    private void SoundAttract(Vector3 target)
    {
        isAttracted = true;
        patrol = false;
        patrolPending = false;
        StopAllCoroutines();
        StartTurn(target);
        StartCoroutine(SoundAttractSequence(target));
    }

    IEnumerator SoundAttractSequence(Vector3 target)
    {
        yield return new WaitUntil(() => !turn);

        if (DistanceTo(target) >= 3)
        {
            RunToDestination(target);
        }
        else
        {
            WalkToDestination(target);
        }

        yield return new WaitUntil(() => DistanceTo(target) <= changeStateDistance);

        SetAnimatorState(npcStates.Patrol, true, true);

        yield return new WaitForSeconds(4);

        StartCoroutine(TurnAngle(RandomAngle(90, 180)));

        yield return new WaitUntil(() => !turn);

        SetAnimatorState(npcStates.Patrol, true, true);

        yield return new WaitForSeconds(4);

        patrol = false;
        isAttracted = false;
        patrolPending = false;

        yield return null;
    }

    private float RandomAngle(int from, int to)
    {
        int dirOrder = Random.Range(-1, 1);

        if (dirOrder >= 0)
        {
            return Random.Range(from, to);
        }
        else
        {
            return Random.Range(-to, -from);
        }
    }

    IEnumerator PatrolPointSequence()
    {
        pathComplete = false;

        SetAnimatorState(npcStates.Patrol, true, true);

        yield return new WaitForSeconds(3);

        WalkToDestination(patrolPointPos);

        yield return new WaitUntil(() => PathCompleted());

        SetAnimatorState(npcStates.Patrol, true, true);

        yield return new WaitForSeconds(patrolPointTime);

        patrol = false;
        patrolPointPos = Vector3.zero;
        yield return null;
    }

    void SetNewDestination(Vector3 destination)
    {
        pathComplete = false;
        _agent.isStopped = false;
        _agent.updateRotation = true;
        _agent.SetDestination(destination);
    }

    /// <summary>
    /// Ataque e aplique dano ao jogador
    /// </summary>
    public void AttackPlayer()
    {
        if (inAttackCollider())
        {
            float randomDamage = Random.Range(AttackDamage.x, AttackDamage.y);
            playerObject.GetComponent<HealthManager>().ApplyDamage(randomDamage);
        }

        AudioSource.PlayClipAtPoint(AttackSound[rand.Range(0, AttackSound.Length)], transform.position);
    }

    private bool inAttackCollider()
    {
        Vector3 colliderPos = transform.position;
        colliderPos.z -= 0.35f;
        colliderPos.y += GetComponent<CapsuleCollider>().height / 2;
        RaycastHit[] hit = Physics.SphereCastAll(colliderPos, attackColliderHeight, transform.forward);

        if (playerDead) return false;

        foreach (var i in hit)
        {
            if (i.collider.gameObject == playerObject)
            {
                return true;
            }
        }

        return false;
    }

    public void StateMachine(bool enabled)
    {
        if (!enabled)
        {
            isDead = true;
            _Animator.enabled = false;
            _agent.isStopped = true;
            GetComponent<CapsuleCollider>().enabled = false;
        }
        else
        {
            isDead = false;
            _Animator.enabled = true;
            _agent.isStopped = false;
            GetComponent<CapsuleCollider>().enabled = true;
        }
    }

    void SetAnimatorState(npcStates npcState, bool state, bool disableOthers)
    {
        if (disableOthers)
        {
            _Animator.SetBool("Patrol", false);
            _Animator.SetBool("isRunning", false);
            _Animator.SetBool("isWalking", false);
            _Animator.SetBool("isAttacking", false);
        }

        switch (npcState)
        {
            case npcStates.Patrol:
                _Animator.SetBool("Patrol", state);
                break;
            case npcStates.Walk:
                _Animator.SetBool("isWalking", state);
                break;
            case npcStates.Run:
                _Animator.SetBool("isRunning", state);
                break;
            case npcStates.Attack:
                _Animator.SetBool("isAttacking", state);
                break;
        }
    }

    void DisablePendingAnimations()
    {
        _Animator.SetBool("Patrol", false);
        _Animator.SetBool("isRunning", false);
        _Animator.SetBool("isWalking", false);
        _Animator.SetBool("isAttacking", false);
    }

    /// <summary>
    /// Verifique se o Zombie é detectado pelo Player
    /// </summary>
    private bool SearchForPlayer()
    {
        RaycastHit hit;

        if (playerDead) return false;

        if (Physics.Linecast(npcHeadPos, playerHeadPos, out hit, searchMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.CompareTag("Player"))
            {
                isBlocked = false;
                return isInSeeDistance() && isDetected();
            }
        }
        else
        {
            isBlocked = true;
            return isInSeeDistance() && isDetected();
        }

        return false;
    }

    private bool isInSeeDistance()
    {
        return PlayerDistance() <= seeDistance;
    }

    private bool isDetected()
    {
        if (isInNpcFOV())
        {
            return true;
        }
        else if (playerChased)
        {
            if (PlayerDistance() <= chaseDistance)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 SoundDetection()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit[] soundHit = Physics.SphereCastAll(ray, attractionDistance, attractionDistance, attractionMask);
        RaycastHit hit;
        ImpactEffect impactEffect = null;

        foreach (var item in soundHit)
        {
            if (item.collider.GetComponent<ImpactEffect>())
            {
                impactEffect = item.collider.GetComponent<ImpactEffect>();
            }

            if (impactEffect && impactEffect.Impact)
            {
                impactEffect.Impact = false;

                if (Physics.Linecast(npcHeadPos, impactEffect.transform.position, out hit, searchMask, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider.gameObject.GetComponent<ImpactEffect>())
                    {
                        return impactEffect.gameObject.transform.position;
                    }
                }
            }
        }

        return Vector3.zero;
    }

    private float PlayerDistance()
    {
        return Vector3.Distance(transform.position, playerObject.transform.position);
    }

    private float DistanceTo(Vector3 target)
    {
        return Vector3.Distance(transform.position, target);
    }

    private bool isInNpcFOV()
    {
        float checkAngle = Mathf.Min(FOVAngle, 359.9999f) / 2;

        float dot = Vector3.Dot(transform.forward, (playerObject.transform.position - transform.position).normalized);

        float viewAngle = (1 - dot) * 90;

        if (viewAngle <= checkAngle)
            return true;
        else
            return false;
    }

    private bool PathCompleted()
    {
        if (!pathComplete)
        {
            if (Vector3.Distance(_agent.destination, _agent.transform.position) <= (_agent.stoppingDistance + 0.5f))
            {
                if (_agent.velocity.sqrMagnitude <= 0.05f)
                {
                    pathComplete = true;
                    return true;
                }
            }
        }
        else
        {
            return true;
        }

        return false;
    }

    private void StopAgent(bool state)
    {
        if (state == true) { _agent.velocity = Vector3.Lerp(_agent.velocity, Vector3.zero, Time.deltaTime * (smoothStopAgent * 10)); }
        _agent.isStopped = state;
    }

    private void OnDrawGizmosSelected()
    {
        float rayRange = 10.0f;
        float halfFOV = FOVAngle / 2.0f;

        if (!enableGizmos) return;

        if (Application.isPlaying)
        {
            Vector3 dir = npcHeadPos - playerHeadPos;
            Gizmos.DrawRay(npcHeadPos, -dir);
        }

        Vector3 colliderPos = transform.position;
        colliderPos.z -= 0.35f;
        colliderPos.y += GetComponent<CapsuleCollider>().height / 2;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(colliderPos, attackColliderHeight);

        Vector3 pos = transform.position;
        pos += NPCHead;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pos, 0.05f);

        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);

        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, leftRayDirection * rayRange);
        Gizmos.DrawRay(transform.position, rightRayDirection * rayRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attractionDistance);
    }

    public Dictionary<string, object> OnSave()
    {
        return new Dictionary<string, object>()
        {
            { "position", transform.localPosition },
            { "patrolPointPos", patrolPointPos },
            { "lastSeenPos", lastPlayerPos },
            { "rotation", transform.localEulerAngles.y },
            { "isDead", isDead },
            { "npcHealth", npcHealth.Health },
            { "isAttracted", isAttracted },
            { "path", waypoint },
            { "patrolPending", patrolPending },
            { "patrol", patrol },
            { "playerChased", playerChased },
            { "turn", turn }
        };
    }

    public void OnLoad(JToken token)
    {
        load = true;
        isDead = (bool)token["isDead"];
        bool m_patrol = (bool)token["patrol"];
        patrolPending = (bool)token["patrolPending"];
        playerChased = (bool)token["playerChased"];
        turn = (bool)token["turn"];

        if (isDead)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.localPosition = token["position"].ToObject<Vector3>();
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, (float)token["rotation"], transform.eulerAngles.z);

            GetComponent<NPCHealth>().Health = (int)token["npcHealth"];

            patrolPointPos = token["patrolPointPos"].ToObject<Vector3>();
            lastPlayerPos = token["lastSeenPos"].ToObject<Vector3>();

            isAttracted = (bool)token["isAttracted"];
            waypoint = (int)token["path"];

            if (patrolPending && !m_patrol && !isAttracted)
            {
                WalkToDestination(Waypoints.Waypoints[waypoint].position);
            }
        }

        load = false;
    }
}
