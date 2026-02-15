using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using TheFiend;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
public class TheFiendAI : EnemyAI
{
    public NetworkVariable<int> StateOfMind;

    public NetworkVariable<int> Funky = new NetworkVariable<int>(1);

    private Animator animator;

    public GameObject Main;

    public GameObject Neck;

    public GameObject Spine;

    public GameObject LeftHand;

    public GameObject RightHand;

    public MeshRenderer MapDot;

    public SkinnedMeshRenderer skinnedMesh;

    private float OldYScale;

    public System.Random enemyRandom;

    public AudioClip[] audioClips;

    public AudioClip StepClip;

    private AudioSource AS;

    private AudioSource AS2;

    private Vector3 FavSpot;

    private bool ResetNode;

    private bool EatingPlayer;

    public NetworkVariable<bool> Seeking;

    public NetworkVariable<bool> Invis;

    public NetworkVariable<bool> RageMode;

    public NetworkVariable<bool> GlobalCD;

    public NetworkVariable<bool> StandingMode;

    public NetworkVariable<bool> IsDying = new NetworkVariable<bool>(value: false);

    public NetworkVariable<bool> LungApparatusWillRage = new NetworkVariable<bool>(TheFiend.Config.WillRageAfterApparatusConfig);//bro, this was NEVER fucking set, why
    private Coroutine rotateCoroutine;
    public Quaternion OldR;

    public bool Step;

    private Vector3 LastPos;

    private Vector3 Node;

    private int LightTriggerTimes;

    private NavMeshPath path;

    private GameObject Head;

    private GameObject breakerBox;

    private RoundManager roundManager;

    public TimeOfDay timeOfDay;

    private LungProp LungApparatus;//lets just have this as the lungProp itself, not a gameobject

    private Vector3 LungApparatusPosition;

    public GameObject TargetLook;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            LungApparatusWillRage.Value =
                TheFiend.Config.WillRageAfterApparatusConfig;
        }
    }
    public void Awake()
    {
        path = new NavMeshPath();
        FavSpot = transform.position;
        Head = Neck.transform.Find("mixamorig:Head").gameObject;
        OldR = Neck.transform.localRotation;
        animator = GetComponent<Animator>();
        animator.Play("Idle");
        AS = GetComponent<AudioSource>();
        AS2 = Spine.GetComponent<AudioSource>();
        try
        {
            breakerBox = FindObjectOfType<BreakerBox>().gameObject;
        }
        catch
        {
            breakerBox = null;
        }
        roundManager = FindObjectOfType<RoundManager>();
        timeOfDay = FindObjectOfType<TimeOfDay>();
        MapDot.material.color = Color.red;
        if (LungApparatus == null) LungApparatus = FindObjectOfType<LungProp>();
        AudioMixerGroup outputAudioMixerGroup = SoundManager.Instance.diageticMixer.FindMatchingGroups("SFX")[0];
        AS.outputAudioMixerGroup = outputAudioMixerGroup;
    }

    public override void Start()
    {
        base.Start();
        OldYScale = Main.transform.position.y;
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
        if(LungApparatus != null)
        {
            if (LungApparatus.isLungDocked)
            {
                TheFiendPlugin.logger.LogInfo("Lung Apparatus Found");
                LungApparatusPosition = LungApparatus.transform.position;
            }
        }
        else if(LungApparatus == null) { TheFiendPlugin.logger.LogInfo("Failed to find an Apparatus?"); }
    }

    public void FixedUpdate()
    {
        if (Step && !Invis.Value)
        {
            AS2.pitch = UnityEngine.Random.Range(0.6f, 1f);
            AS2.PlayOneShot(StepClip);
        }
    }

    public void LateUpdate()
    {
        if (TargetLook != null)
        {
            Neck.transform.LookAt(TargetLook.transform, Vector3.up);
        }
        else
        {
            Neck.transform.localRotation = OldR;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (stunnedByPlayer != null)
        {
            AS.clip = audioClips[2];
            AS.loop = false;
            AS.Play();

            IsDying.Value = true;
            skinnedMesh.enabled = false;

            Destroy(gameObject, 4f);
            stunnedByPlayer = null;
        }

        if (IsDying.Value)
        {
            base.SyncPositionToClients();
            return;
        }

        if (timeOfDay.hour >= 15)
            Funky.Value = 2;

        AS.volume = (Seeking.Value || StateOfMind.Value == 3) ? 0f : TheFiend.Config.VolumeConfig;

        skinnedMesh.enabled = !Invis.Value;

        int funky = Mathf.Max(1, Funky.Value);

        if (UnityEngine.Random.Range(1, 10000) == 1)
            TeleportServerRpc();

        if (StateOfMind.Value == 3 && UnityEngine.Random.Range(1, 10000 / funky) == 1)
            HideOnCellingServerRpc();

        if (!Seeking.Value && UnityEngine.Random.Range(1, 10000 / funky) == 1) ToggleSeekingServerRpc();

        if (GlobalCD.Value)
        {
            base.SyncPositionToClients();
            return;
        }

        if (StateOfMind.Value < 3)
            StateOfMind.Value = 0;

        if (breakerBox && !Seeking.Value)
        {
            Transform mesh = breakerBox.transform.Find("Mesh");
            if (mesh != null)
            {
                float dist = Vector3.Distance(Main.transform.position, mesh.position);

                if (dist <= 5f)
                {
                    if (Physics.Raycast(Neck.transform.position,mesh.position - Neck.transform.position,out var hitInfo,float.PositiveInfinity,~LayerMask.GetMask("Enemies")) && Vector3.Distance(hitInfo.point, mesh.position) < 2f)
                    {
                        StateOfMind.Value = 4;
                        TargetLook = mesh.gameObject;

                        if (dist <= 2f)
                            BreakerBoxBreakServerRpc();
                    }
                    else
                    {
                        StateOfMind.Value = 0;
                    }
                }
            }
        }

        bool hasTarget = TargetClosestPlayer(100f, false, 70f);

        if (hasTarget) TargetLook = targetPlayer.gameObject;

        if (hasTarget && targetPlayer.currentlyHeldObject && targetPlayer.currentlyHeldObject.gameObject.name.Contains("FlashlightItem"))
        {
            Transform lightTransform = targetPlayer.currentlyHeldObject.transform.Find("Light");

            if (lightTransform != null)
            {
                Light light = lightTransform.GetComponent<Light>();

                if (light != null && light.enabled && Vector3.Distance(Head.transform.position, lightTransform.position) <= 2.5f)
                {
                    FearedServerRpc(false, true);
                    LightTriggerTimes++;
                }
            }
        }

        if (hasTarget && StateOfMind.Value == 3 && !Seeking.Value && Vector3.Distance(transform.position,targetPlayer.transform.position) <= 4f)
        {
            HideOnCellingServerRpc();
        }
        //if (!LungApparatusWillRage.Value) TheFiendPlugin.logger.LogInfo("The NetworkVariable LungApparatusWillRage is false");
        if (LungApparatus && (LungApparatusWillRage.Value || TheFiend.Config.WillRageAfterApparatusConfig) && !Invis.Value && !LungApparatus.isLungDocked)
        {
            Transform lightTransform = LungApparatus.transform.Find("Point Light");
            if (lightTransform != null)
            {
                Light light = lightTransform.GetComponent<Light>();
                light?.color = Color.red;
            }

            LungApparatus.scrapValue = 300;
            LungApparatus = null;
            StartCoroutine(Rage());
        }

        if (!EatingPlayer && StateOfMind.Value <= 2 && !StandingMode.Value)
        {
            if (hasTarget)
            {
                ResetNode = true;

                if (agent.remainingDistance > 10f && !RageMode.Value)
                {
                    OldYScale = Main.transform.position.y;

                    if (!Seeking.Value)
                    {
                        StateOfMind.Value = 1;
                        agent.speed = 3 + (funky - 1);
                        animator.Play("Walk");

                        if ((CheckDoor() && UnityEngine.Random.Range(1, 100) == 1 && StateOfMind.Value != 3) || (!CheckDoor() && UnityEngine.Random.Range(1, 1000) == 1 && StateOfMind.Value != 3))
                        {
                            HideOnCellingServerRpc();
                        }
                    }
                    else
                    {
                        agent.speed = 1f;
                        animator.Play("Seeking");
                        BreakDoorServerRpc();
                    }

                    if (UnityEngine.Random.Range(1, TheFiend.Config.FlickerRngConfig) == 1)
                    {
                        roundManager.FlickerLights(true, true);
                    }
                }
                else if (!Seeking.Value)
                {
                    StateOfMind.Value = 2;

                    if (CheckLineOfSightForPlayer(45f, 60, -1) != null) targetPlayer.JumpToFearLevel(0.9f, true);

                    agent.speed = RageMode.Value ? 20 * funky : 9 * funky;
                    animator.Play("Run");
                    BreakDoorServerRpc();
                }

                SetDestinationToPosition(targetPlayer.transform.position, false);

                if (Seeking.Value)
                {
                    var players = FindObjectsOfType<PlayerControllerB>();

                    foreach (var player in players)
                    {
                        if (player.HasLineOfSightToPosition(Neck.transform.position,45f,60, -1f))
                        {
                            ToggleSeekingServerRpc();
                            FearedServerRpc(true);
                            break;
                        }
                    }
                }
            }
            else
            {
                agent.speed = 3f;

                if (ResetNode)
                {
                    WonderVectorServerRpc(60f);
                    ResetNode = false;
                }

                if (Node != Vector3.zero)
                    SetDestinationToPosition(Node, false);
                else
                    ResetNode = true;

                if (agent.remainingDistance <= 0.5f || UnityEngine.Random.Range(1, 100) == 1)
                {
                    ResetNode = true;
                }

                TargetLook = null;
            }

            if (agent.remainingDistance == 0f && StateOfMind.Value == 0 && !RageMode.Value)
            {
                animator.Play("Idle");
            }
            else if (!Seeking.Value && StateOfMind.Value == 1)
            {
                animator.Play("Walk");
            }
        }

        if (StateOfMind.Value == 4 && TargetLook != null)
        {
            animator.Play("Walk");
            SetDestinationToPosition(TargetLook.transform.position, false);
        }

        base.SyncPositionToClients();
    }

    [ServerRpc]
    public void ToggleSeekingServerRpc()
    {
        Seeking.Value = !Seeking.Value;
    }

    private void OnTriggerStay(Collider collision)
    {
        if (collision.gameObject.GetComponent<PlayerControllerB>() && StateOfMind.Value != 3 && !GlobalCD.Value && !Invis.Value && !IsDying.Value && !EatingPlayer && Vector3.Distance(transform.position, collision.gameObject.transform.position) < 4f)
        {
            GrabServerRpc(collision.gameObject.GetComponent<PlayerControllerB>());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SceamServerRpc()
    {
        AS.Stop();
        AS.clip = audioClips[UnityEngine.Random.Range(1, 2)];
        AS.loop = false;
        AS.Play();
        SceamClientRpc();
    }

    [ClientRpc]
    public void SceamClientRpc()
    {
        AS.Stop();
        AS.clip = audioClips[UnityEngine.Random.Range(1, 2)];
        AS.loop = false;
        AS.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void IdleSoundServerRpc()
    {

        AS.Stop();
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
        IdleSoundClientRpc();
    }

    [ClientRpc]
    public void IdleSoundClientRpc()
    {

        AS.Stop();
        AS.clip = audioClips[0];
        AS.loop = true;
        AS.Play();
    }

    [ServerRpc(RequireOwnership = false)]
    public void GrabServerRpc(NetworkBehaviourReference PlayerControllerBRef)
    {
        if (PlayerControllerBRef.TryGet<PlayerControllerB>(out PlayerControllerB networkBehaviour, null))
        {
            GrabClientRpc(networkBehaviour.gameObject);
        }
    }

    [ClientRpc]
    public void GrabClientRpc(NetworkObjectReference networkObject)
    {
        if (networkObject.TryGet(out var networkObject2))
        {
            StartCoroutine(Grabbing(networkObject2.gameObject));
        }
    }

    public IEnumerator Grabbing(GameObject Player)
    {
        if (EatingPlayer || !Player)
        {
            yield break;
        }
        PlayerControllerB PCB = Player.GetComponent<PlayerControllerB>();
        if (PCB.health <= 50)
        {
            EatingPlayer = true;
            TargetLook = null;
            agent.speed = 0f;
            SetDestinationToPosition(agent.transform.position, false);
            float oldspeed = PCB.movementSpeed;
            PCB.movementSpeed = 0f;
            animator.Play("Grab");
            transform.LookAt(Player.transform.position, Vector3.up);
            rotateCoroutine = StartCoroutine(RotatePlayerToMe(PCB));
            SceamServerRpc();
            yield return new WaitForSeconds(1.7f);
            PCB.KillPlayer(Main.transform.forward * 30f, true, (CauseOfDeath)6, 1, default(Vector3));
            if (IsOwner)
            {
                PCB.movementSpeed = oldspeed;
            }
            yield return new WaitForSeconds(1f);
            if (rotateCoroutine != null)
            {
                StopCoroutine(rotateCoroutine);
            }
            IdleSoundServerRpc();
            animator.Play("Idle");
            yield return new WaitForSeconds(3f);
            yield return new WaitForSeconds(2f);
            RageMode.Value = false;
            if (UnityEngine.Random.Range(1, 30) == 1)
            {
                HideOnCellingServerRpc();
            }

            EatingPlayer = false;
        }
        else
        {
            PCB.DamagePlayer(50, true, true, (CauseOfDeath)0, 0, false, default(Vector3));
            PCB.externalForceAutoFade += Main.transform.forward * 30f;
            animator.Play("Craw");
            GlobalCD.Value = true;
            PCB.movementAudio.PlayOneShot(audioClips[6], 1f);
            StartCooldown(1f);
        }
    }

    private IEnumerator RotatePlayerToMe(PlayerControllerB PCB)
    {
        while (PCB != null && !PCB.isPlayerDead && PCB.health > 0)
        {
            if (PCB.IsOwner)
            {
                Vector3 direction = transform.position - PCB.transform.position;
                PlayerSmoothLookAt(direction, PCB);
            }

            yield return null;
        }
    }

    private void PlayerSmoothLookAt(Vector3 newDirection, PlayerControllerB PCB)
    {
        PCB.gameObject.transform.rotation = Quaternion.Lerp(PCB.gameObject.transform.rotation, Quaternion.LookRotation(newDirection), Time.deltaTime * 5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideOnCellingServerRpc()
    {
        if (StateOfMind.Value != 3)
        {
            if (StateOfMind.Value <= 3)
            {
                StateOfMind.Value = 3;
                LastPos = Main.transform.position;
                OldYScale = Main.transform.position.y;
                Physics.Raycast(Main.transform.position, transform.TransformDirection(Vector3.up), out var hitInfo, float.PositiveInfinity, ~LayerMask.GetMask("Enmies"));
                animator.Play("Hide");
                AS.Stop();
                agent.speed = 0f;
                SetYLevelClientRpc(hitInfo.point.y);
                MapDot.enabled = false;
            }
        }
        else if (!StandingMode.Value)
        {
            StartCoroutine(Stand());
        }
    }

    [ClientRpc]
    public void SetYLevelClientRpc(float y)
    {
        Main.transform.position = new Vector3(Main.transform.position.x, y, Main.transform.position.z);
    }

    public IEnumerator Stand()
    {
        StandingMode.Value = true;
        MapDot.enabled = true;
        Rigidbody rig = Main.AddComponent<Rigidbody>();
        rig.detectCollisions = false;
        while (Vector3.Distance(Main.transform.position, LastPos) > 1.5f)
        {
            yield return null;
        }
        Destroy(rig);
        animator.Play("UnHide");
        yield return new WaitForSeconds(0.2f);
        SetYLevelClientRpc(OldYScale);
        animator.Play("Idle");
        SceamServerRpc();
        yield return new WaitForSeconds(2f);
        IdleSoundServerRpc();
        BreakDoorServerRpc();
        StateOfMind.Value = 1;
        StandingMode.Value = false;
    }

    [ServerRpc]
    public void BreakDoorServerRpc()
    {
        try
        {
            DoorLock[]? array = FindObjectsOfType(typeof(DoorLock)) as DoorLock[];
            foreach (DoorLock val in array)
            {
                GameObject gameObject = val.transform.parent.transform.parent.transform.parent.gameObject;
                if (!gameObject.GetComponent<Rigidbody>() && Vector3.Distance(transform.position, gameObject.transform.position) <= 4f)
                {
                    BashDoorClientRpc(gameObject, targetPlayer.transform.position - transform.position.normalized * 20f);
                }
            }
        }
        catch
        {
        }
    }

    [ClientRpc]
    public void BashDoorClientRpc(NetworkObjectReference netObjRef, Vector3 Position)
    {
        if (netObjRef.TryGet(out var networkObject))
        {
            GameObject gameObject = networkObject.gameObject;
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 60f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.volume = 3f;
            StartCoroutine(TurnOffC(rigidbody, 0.12f));
            rigidbody.AddForce(Position, ForceMode.Impulse);
            audioSource.PlayOneShot(audioClips[3]);
        }
    }

    public bool CheckDoor()
    {
        DoorLock[]? array = FindObjectsOfType(typeof(DoorLock)) as DoorLock[];
        foreach (DoorLock val in array)
        {
            GameObject gameObject = val.transform.parent.transform.parent.gameObject;
            if (Vector3.Distance(transform.position, gameObject.transform.position) <= 4f)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator TurnOffC(Rigidbody rigidbody, float time)
    {
        rigidbody.detectCollisions = false;
        yield return new WaitForSeconds(time);
        rigidbody.detectCollisions = true;
        Destroy(rigidbody.gameObject, 5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void FearedServerRpc(bool TempRage, bool uselight = false)
    {
        GlobalCD.Value = true;
        FearedClientRpc();
        StartCoroutine(CD(5f));
        float tempRage = 3f;
        if (uselight)
        {
            tempRage = LightTriggerTimes * 2;
        }
        if (TempRage)
        {
            StartCoroutine(SetTempRage(tempRage));
        }
    }

    public IEnumerator Rage()
    {
        GlobalCD.Value = true;
        yield return new WaitForSeconds(0.2f);
        animator.Play("Rage");
        PlayerControllerB[]? array = FindObjectsOfType(typeof(PlayerControllerB)) as PlayerControllerB[];
        foreach (PlayerControllerB player in array)
        {
            player.JumpToFearLevel(0.9f, true);
        }
        AS.maxDistance = 500f;
        AS.Stop();
        AS.clip = audioClips[5];
        AS.loop = false;
        AS.Play();
        TheFiendPlugin.logger.LogInfo("We should be raging");
        yield return new WaitForSeconds(9f);
        ToggleRageServerRpc(TheRageValue: true);
        AS.maxDistance = 30f;
        GlobalCD.Value = false;
        yield return new WaitForSeconds(20f);
        ToggleRageServerRpc(TheRageValue: false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleRageServerRpc(bool TheRageValue)
    {
        RageMode.Value = TheRageValue;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TeleportServerRpc()
    {
        Invis.Value = true;
        List<PlayerControllerB> list = new List<PlayerControllerB>();
        PlayerControllerB[]? array = FindObjectsOfType(typeof(PlayerControllerB)) as PlayerControllerB[];
        foreach (PlayerControllerB val in array)
        {
            if (val.isInsideFactory)
            {
                list.Add(val);
            }
        }
        if (list.Count > 0)
        {
            transform.position = list[UnityEngine.Random.Range(1, list.Count)].gameObject.transform.position;
        }
        GlobalCD.Value = true;
        StartCoroutine(CD(25f, UnInvis: true));
    }

    [ClientRpc]
    public void FearedClientRpc()
    {
        animator.Play("CoverFace");
        agent.speed = 0f;
        AS.Stop();
        AS.clip = audioClips[4];
        AS.loop = false;
        AS.Play();
    }

    public void StartCooldown(float time, bool UnInvis = false)
    {
        StartCoroutine(CD(time, UnInvis));
    }

    private IEnumerator CD(float time, bool UnInvis = false)
    {
        agent.speed = 0f;
        yield return new WaitForSeconds(time);
        GlobalCD.Value = false;
        if (UnInvis)
        {
            Invis.Value = false;
        }
    }

    private IEnumerator StateMindCD(float time, int typenow)
    {
        yield return new WaitForSeconds(time);
        StateOfMind.Value = typenow;
    }

    private IEnumerator SetTempRage(float time)
    {
        ToggleRageServerRpc(TheRageValue: true);
        yield return new WaitForSeconds(time);
        ToggleRageServerRpc(TheRageValue: false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void WonderVectorServerRpc(float Range)
    {
        Vector3 vector = transform.position + new Vector3(UnityEngine.Random.Range(0f - Range, Range), 0f, UnityEngine.Random.Range(0f - Range, Range));
        if (agent.CalculatePath(vector, path))
        {
            Node = vector;
        }
        else
        {
            Node = Vector3.zero;
        }
    }

    [ServerRpc]
    public void BreakerBoxBreakServerRpc()
    {
        if (!breakerBox.GetComponent<Rigidbody>())
        {
            BreakerBoxBreakClientRpc(breakerBox);
        }
    }

    [ClientRpc]
    public void BreakerBoxBreakClientRpc(NetworkObjectReference networkObjectReference)
    {
        if (networkObjectReference.TryGet(out var networkObject))
        {
            GameObject gameObject = networkObject.gameObject;
            gameObject.transform.Find("Mesh").transform.Find("PowerBoxDoor").gameObject.AddComponent<Rigidbody>();
            Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
            StartCoroutine(TurnOffC(rigidbody, 0.1f));
            rigidbody.AddForce((Neck.transform.position - transform.position).normalized * 15f, ForceMode.Impulse);
            gameObject.GetComponent<AudioSource>().PlayOneShot(audioClips[3]);
            Destroy(gameObject, 5f);
            gameObject = null;
            roundManager.PowerSwitchOffClientRpc();
            StartCoroutine(StateMindCD(1f, 0));
            animator.Play("Grab");
        }
    }
}
