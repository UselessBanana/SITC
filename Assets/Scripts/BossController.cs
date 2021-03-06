﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

public class BossController : MonoBehaviour
{
    private enum BossType
    {
        OCTOPUS
    }
    [SerializeField]
    private BossType bossType = BossType.OCTOPUS;

    public delegate void DamageAction(int pid, float health, float current_health, float amount);
    public static event DamageAction OnDamage;

    //General Variables
    InputController iC;
    public EntityManager em = null;
    Vector2 leftStick = Vector2.zero;
    Vector2 rightStick = Vector2.zero;
    bool leftStickActive = true;
    bool rightStickActive = true;
    public int playerIndex = 0;
    private Transform entities;
    Rigidbody leftArmRB;
    Rigidbody rightArmRB;
    public bool usingKeyboard = false;
    public Transform SquidlingPaths;

    //Editable General Variables
    [SerializeField]
    Transform leftArmMovePoint;
    [SerializeField]
    Transform rightArmMovePoint;
    [SerializeField]
    private float armSpeed = 5f;
    [SerializeField]
    float attackCooldownLength = 2.0f;
    [SerializeField]
    float attackTime = 1.0f;
    public float baseHealth = 100.0f;
    public float currentHealth = 100.0f;

    //Raycast Specific Variables
    RaycastHit rayhit;

    //Octopus Specific Variables
    bool leftObjHeld = false;
    bool rightObjHeld = false;
    [SerializeField]
    Transform leftTenticleObjHolder;
    [SerializeField]
    Transform rightTenticleObjHolder;
    SlamController leftCollider;
    SlamController rightCollider;
    [SerializeField]
    List<Transform> leftTenticleObjects = new List<Transform>();
    [SerializeField]
    List<Transform> rightTenticleObjects = new List<Transform>();

    //Particle Emitter for the explosion attached to the arm.
    ParticleEmitter explosion;

    GameObject leftReticule;
    GameObject rightReticule;

    Vector3 leftPos;
    Vector3 rightPos;
    public GameObject bossEye;


    void Start()
    {
        iC = GetComponent<InputController>();
        iC.SetPlayer(playerIndex);
        leftCollider = leftArmMovePoint.GetComponent<SlamController>();
        rightCollider = rightArmMovePoint.GetComponent<SlamController>();
        leftArmRB = leftArmMovePoint.GetComponent<Rigidbody>();
        rightArmRB = rightArmMovePoint.GetComponent<Rigidbody>();
        entities = transform.parent.transform;

        //Instantiation of objects to prevent overhead.
        leftReticule = GameObject.Find("LeftReticule");
        rightReticule = GameObject.Find("RightReticule");

        explosion = this.gameObject.AddComponent<ParticleEmitter>();
        //this.gameObject.AddComponent<BoxCollider>();

        switch (bossType)
        {
            case BossType.OCTOPUS:
                {
                    rightTenticleObjects = new List<Transform>();
                    leftTenticleObjects = new List<Transform>();
                    break;
                }
        }
    }

    void Update()
    {
        InputHandler();

        switch (bossType)
        {
            case BossType.OCTOPUS:
                {
                    //OctopusSpecificInput();
                    break;
                }
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            Damage(25);
        }

        if (GameManager.instance.state == GameManager.GameState.GAMEOVER && (iC.PressedStart() || Input.GetKeyDown(KeyCode.Return)))
        {
            SceneManager.LoadScene(0);
        }
    }

    void FixedUpdate()
    {
        int closestPlayer = 4;
        float currentDistance = 0.0f;
        if (em.players[0] != gameObject && em.players[3] != null)
        {
            if (em.players[0].GetComponent<PlayerController>().playerActive && (Vector3.Distance(bossEye.transform.position, em.players[0].transform.position) < currentDistance || currentDistance == 0.0f))
            {
                currentDistance = Vector3.Distance(bossEye.transform.position, em.players[0].transform.position);
                closestPlayer = 0;
            }
        }
        if (em.players[1] != gameObject && em.players[3] != null)
        {
            if (em.players[1].GetComponent<PlayerController>().playerActive && (Vector3.Distance(bossEye.transform.position, em.players[1].transform.position) < currentDistance || currentDistance == 0.0f))
            {
                currentDistance = Vector3.Distance(bossEye.transform.position, em.players[1].transform.position);
                closestPlayer = 1;
            }
        }
        if (em.players[2] != gameObject && em.players[3] != null)
        {
            if (em.players[2].GetComponent<PlayerController>().playerActive && (Vector3.Distance(bossEye.transform.position, em.players[2].transform.position) < currentDistance || currentDistance == 0.0f))
            {
                currentDistance = Vector3.Distance(bossEye.transform.position, em.players[2].transform.position);
                closestPlayer = 2;
            }
        }
        if (em.players[3] != gameObject && em.players[3] != null)
        {
            if (em.players[3].GetComponent<PlayerController>().playerActive && (Vector3.Distance(bossEye.transform.position, em.players[3].transform.position) < currentDistance || currentDistance == 0.0f))
            {
                currentDistance = Vector3.Distance(bossEye.transform.position, em.players[3].transform.position);
                closestPlayer = 3;
            }
        }
        if (closestPlayer != 4)
        {
            bossEye.transform.LookAt(em.players[closestPlayer].transform.position);
        }
        else
        {
            bossEye.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

        if (transform.rotation.y != 180.0f)
        {
            transform.rotation = new Quaternion(transform.rotation.x, 180.0f, transform.rotation.z, transform.rotation.w);
        }
        #region Update tracking

        CalculateReticule(leftArmMovePoint.position, 0);
        CalculateReticule(rightArmMovePoint.position, 1);

        #endregion

        #region Movement Code

        if (leftStickActive)
            leftArmMovePoint.transform.Translate(new Vector3(leftStick.x * armSpeed * Time.deltaTime, leftStick.y * armSpeed * Time.deltaTime, 0f), Space.World);
        if (rightStickActive)
            rightArmMovePoint.transform.Translate(new Vector3(rightStick.x * armSpeed * Time.deltaTime, rightStick.y * armSpeed * Time.deltaTime, 0f), Space.World);

        // if (leftArmMovePoint.transform.position.y < 0)
        // {
        //     Vector3 tempPos = leftArmMovePoint.transform.position;
        //     tempPos.y = 0;
        //     leftArmMovePoint.transform.position = tempPos;
        // }
        // else if (leftArmMovePoint.transform.position.y > 32)
        // {
        //     Vector3 tempPos = leftArmMovePoint.transform.position;
        //     tempPos.y = 32;
        //     leftArmMovePoint.transform.position = tempPos;
        // }
        // if (rightArmMovePoint.transform.position.y < 0)
        // {
        //     Vector3 tempPos = rightArmMovePoint.transform.position;
        //     tempPos.y = 0;
        //     rightArmMovePoint.transform.position = tempPos;
        // }
        // else if (rightArmMovePoint.transform.position.y > 32)
        // {
        //     Vector3 tempPos = rightArmMovePoint.transform.position;
        //     tempPos.y = 32;
        //     rightArmMovePoint.transform.position = tempPos;
        // }

        #endregion

        #region Limitations

        // X Limitations
        // if (leftArmMovePoint.transform.position.x < 10)
        // {
        //     Vector3 tempPos = leftArmMovePoint.transform.position;
        //     tempPos.x = 10;
        //     leftArmMovePoint.transform.position = tempPos;
        // }
        // else if (leftArmMovePoint.transform.position.x > 52)
        // {
        //     Vector3 tempPos = leftArmMovePoint.transform.position;
        //     tempPos.x = 52;
        //     leftArmMovePoint.transform.position = tempPos;
        // }
        // if (rightArmMovePoint.transform.position.x < 12)
        // {
        //     Vector3 tempPos = rightArmMovePoint.transform.position;
        //     tempPos.x = 12;
        //     rightArmMovePoint.transform.position = tempPos;
        // }
        // else if (rightArmMovePoint.transform.position.x > 57)
        // {
        //     Vector3 tempPos = rightArmMovePoint.transform.position;
        //     tempPos.x = 57;
        //     rightArmMovePoint.transform.position = tempPos;
        // }

        #endregion

        //Set up for boss specific FixedUpdate
        switch (bossType)
        {
            case BossType.OCTOPUS:
                {
                    break;
                }
        }
    }

    public void Damage(int amount)
    {
        //if (baseHealth - amount <= 0.0f)
        //{
        //    Debug.Log("The boss has been slain.");
        //} 
        //else
        //{
        //    baseHealth -= amount;
        //    if (OnDamage != null)
        //    {
        //        OnDamage(3, baseHealth, currentHealth, amount);    // Always 3 because our boss is the 4th player
        //    }
        //} 
        currentHealth -= amount;

        // if (currentHealth <= 50)
        // {
        //     ChangePlaces();
        // }
        em.ui.UpdateBossHealth(currentHealth, baseHealth);
        em.CheckWinState();
    }

    // CHANGE PLACES!
    public void ChangePlaces()
    {
        GameManager.instance.cm.Play(1);
        CutsceneManager.CloseLetterbox += CutsceneManager_CloseLetterbox;
    }

    private void CutsceneManager_CloseLetterbox()
    {
        StartCoroutine(ReturnAfterTime(2.0f));
        CutsceneManager.CloseLetterbox -= CutsceneManager_CloseLetterbox;
    }

    IEnumerator ReturnAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        GameManager.instance.cm.Play(0, false);
    }

    IEnumerator SlamAttack(bool attackWithLeft)
    {
        if (attackWithLeft)
        {
            leftStickActive = false;
            leftArmRB.isKinematic = false;
            leftArmRB.AddForce(new Vector3(0, -0.002f, 0));
            leftCollider.isActive = true;
        }
        else
        {
            rightStickActive = false;
            rightArmRB.isKinematic = false;
            rightArmRB.AddForce(new Vector3(0, -0.002f, 0));
            rightCollider.isActive = true;
        }

        // Wait for the attack to smash
        yield return new WaitForSeconds(attackTime);

        if (attackWithLeft)
        {
            leftCollider.isActive = false;
            //explosion.CreateExplosion(leftArmMovePoint.transform.position);
        }
        else
        {
            rightCollider.isActive = false;
            //explosion.CreateExplosion(rightArmMovePoint.transform.position);
        }
        // Give the player back control of the arm when the cooldown expires
        yield return new WaitForSeconds(attackCooldownLength - attackTime);

        if (attackWithLeft)
        {
            leftArmRB.isKinematic = true;
            leftStickActive = true;

        }
        else
        {
            rightArmRB.isKinematic = true;
            rightStickActive = true;
        }
    }

    void CalculateReticule(Vector3 armPosition, int tentacleid)
    {
        Physics.Raycast(new Ray(armPosition, Vector3.down), out rayhit);
        // Convert World Position of hit to screen point to achieve accurate representation of reticle.
        // Enable this for when UI is enabled.
        // Vector3 screenPos = Camera.main.WorldToScreenPoint(rayhit.point);

        if (rayhit.collider != null)
        {
            //Hit Something.
            if (rayhit.collider.tag == "Wall" || rayhit.collider.tag == "Player")
            {
                //That Something is called "Wall" or "Player".
                if (leftReticule != null || rightReticule != null)
                {
                    //The reticules are declared.
                    switch (tentacleid)
                    {
                        case 0:
                            //Debug.DrawLine(leftArmMovePoint.position, rayhit.point, Color.red, 5.0f);
                            leftPos = rayhit.point + new Vector3(0.0f, 5.0f, 0.0f);
                            //Bob Logic.
                            leftPos.y = leftPos.y + (float)Mathf.Sin(Time.time) * 1.0f;
                            //Calculate the new position.
                            leftReticule.transform.position = leftPos;
                            //Mathf.Clamp(leftPos.y, rayhit.point.y + 5.0f, rayhit.point.y + 10.0f);

                            /**if (addLeftBob)
                            {
                                StartCoroutine(WaitToSnap(3.0f));
                                leftReticule.AddComponent<Bob>();
                                addLeftBob = false;
                            }*/

                            //Debug.Log(rayhit.point.ToString());
                            //leftReticule.transform.position = screenPos;
                            break;
                        case 1:
                            rightPos = rayhit.point + new Vector3(0.0f, 5.0f, 0.0f);
                            rightPos.y = rightPos.y + (float)Mathf.Sin(Time.time) * 1.0f;
                            rightReticule.transform.position = rightPos;
                            break;
                        default:
                            Debug.Log("Created tentacle with id: " + tentacleid);
                            break;
                    }
                }
                else
                {
                    switch (tentacleid)
                    {
                        case 0:
                            leftReticule.transform.position = rayhit.point;
                            //leftReticule.transform.rotation = Quaternion.Euler(80.0f,0.0f,0.0f);
                            //leftReticule.transform.position = screenPos;
                            break;
                        case 1:
                            rightReticule.transform.position = rayhit.point;
                            //rightReticule.transform.rotation = Quaternion.Euler(80.0f,0.0f,0.0f);
                            //rightReticule.transform.position = screenPos;
                            break;
                        default:
                            Debug.Log("Tracking tentacle...");
                            break;
                    }
                }
            }
        }
    }

    private IEnumerator WaitToSnap(float duration)
    {
        yield return new WaitForSeconds(duration);
    }

    void InputHandler()
    {
        if (leftStickActive)
        {
            if (!usingKeyboard)
            {
                leftStick = new Vector2(iC.LeftHorizontal(), iC.LeftVertical());

                if (iC.LeftTrigger() > 0)
                    StartCoroutine(SlamAttack(true));
            }
            else
            {
                leftStick = new Vector2(Input.GetAxis("LeftHorizontal"), Input.GetAxis("LeftVertical"));

                if (Input.GetButtonDown("LeftSlam"))
                    StartCoroutine(SlamAttack(true));
            }
        }
        if (rightStickActive)
        {
            if (!usingKeyboard)
            {
                rightStick = new Vector2(iC.RightHorizontal(), iC.RightVertical());

                if (iC.RightTrigger() > 0)
                    StartCoroutine(SlamAttack(false));
            }
            else
            {
                rightStick = new Vector2(Input.GetAxis("RightHorizontal"), Input.GetAxis("RightVertical"));

                if (Input.GetButtonDown("RightSlam"))
                    StartCoroutine(SlamAttack(false));
            }
        }
    }

    #region Octopus Methods

    void OctopusSpecificInput()
    {
        if (!usingKeyboard)
        {
            if (iC.PressedLeftShoulder() && leftStickActive)
            {
                if (leftObjHeld && leftTenticleObjHolder.childCount == 0)
                    leftObjHeld = false;
                if (!leftObjHeld)
                    PickupObject(true);
                else
                    DropObject(true);
            }
            if (iC.PressedRightShoulder() && rightStickActive)
            {
                if (rightObjHeld && rightTenticleObjHolder.childCount == 0)
                    rightObjHeld = false;
                if (!rightObjHeld)
                    PickupObject(false);
                else
                    DropObject(false);
            }
        }
        else
        {
            if (Input.GetButtonDown("LeftPickup") && leftStickActive)
            {
                if (leftObjHeld && leftTenticleObjHolder.childCount == 0)
                    leftObjHeld = false;
                if (!leftObjHeld)
                    PickupObject(true);
                else
                    DropObject(true);
            }
            if (Input.GetButtonDown("RightPickup") && rightStickActive)
            {
                if (rightObjHeld && rightTenticleObjHolder.childCount == 0)
                    rightObjHeld = false;
                if (!rightObjHeld)
                    PickupObject(false);
                else
                    DropObject(false);
            }
        }
    }

    public void AddInteractiveObject(Transform trans, bool leftTenticle)
    {
        if (leftTenticle)
        {
            leftTenticleObjects.Add(trans);
        }
        else
        {
            rightTenticleObjects.Add(trans);
        }
    }

    public void RemoveInteractiveObject(Transform trans, bool leftTenticle)
    {
        if (leftTenticle)
        {
            leftTenticleObjects.Remove(trans);
        }
        else
        {
            rightTenticleObjects.Remove(trans);
        }
    }

    void PickupObject(bool leftTenticle)
    {
        if (leftTenticle)
        {
            if (leftTenticleObjects.Count > 0)
            {
                leftTenticleObjects[0].SetParent(leftTenticleObjHolder);
                leftTenticleObjects[0].GetComponent<Rigidbody>().useGravity = false;
                leftTenticleObjects[0].GetComponent<Rigidbody>().isKinematic = true;
                RemoveInteractiveObject(leftTenticleObjects[0], true);
                leftObjHeld = true;
            }
        }
        else
        {
            if (rightTenticleObjects.Count > 0)
            {
                rightTenticleObjects[0].SetParent(rightTenticleObjHolder);
                rightTenticleObjects[0].GetComponent<Rigidbody>().useGravity = false;
                rightTenticleObjects[0].GetComponent<Rigidbody>().isKinematic = true;
                RemoveInteractiveObject(rightTenticleObjects[0], false);
                rightObjHeld = true;
            }
        }
    }

    void DropObject(bool leftTenticle)
    {
        if (leftTenticle)
        {
            if (leftTenticleObjHolder.childCount > 0)
            {
                leftTenticleObjHolder.GetChild(0).GetComponent<Rigidbody>().useGravity = true;
                leftTenticleObjHolder.GetChild(0).GetComponent<Rigidbody>().isKinematic = false;
                RemoveInteractiveObject(leftTenticleObjects[0], true);
                leftTenticleObjHolder.GetChild(0).SetParent(entities);
                leftObjHeld = false;
            }
        }
        else
        {
            if (rightTenticleObjHolder.childCount > 0)
            {
                rightTenticleObjHolder.GetChild(0).GetComponent<Rigidbody>().useGravity = true;
                rightTenticleObjHolder.GetChild(0).GetComponent<Rigidbody>().isKinematic = false;
                RemoveInteractiveObject(rightTenticleObjects[0], false);
                rightTenticleObjHolder.GetChild(0).SetParent(entities);
                rightObjHeld = false;
            }
        }
    }

    #endregion

    /**
    public Transform GetLeftTentacle()
    {
        return leftArmMovePoint;
    }

    public Transform GetRightTentacle()
    {
        return rightArmMovePoint;
    }*/
}
