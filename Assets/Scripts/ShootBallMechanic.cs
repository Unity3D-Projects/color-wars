﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class ShootBallMechanic : MonoBehaviour {

    public float shotSpeed;
    public float forcedShotTime;
    public IC.InputControlType shootButton = IC.InputControlType.Action1;

    PlayerMovement playerMovement;
    IC.InputDevice inputDevice;
    Coroutine shootTimer;
    BallCarrier ballCarrier;
    PlayerStateManager stateManager;

    IEnumerator ShootTimer() {
        float elapsedTime = 0f;
        while (elapsedTime < forcedShotTime) {
            if (inputDevice != null) {
                if (inputDevice.GetControl(shootButton).WasPressed){
                    break;
                }
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Shoot();
    }

    void Shoot() {
        var ball = ballCarrier.ball;
        ballCarrier.DropBall();
        var shotDirection = ball.transform.position - transform.position;
        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
    }

    void Update() {
        if (inputDevice == null) {
            inputDevice = playerMovement.GetInputDevice();
        }
    }

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager =  this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(
            State.Posession, () => shootTimer = StartCoroutine(ShootTimer()));
        stateManager.CallOnStateExit(
            State.Posession,
            () => {
                if (shootTimer != null) {
                    StopCoroutine(shootTimer);
                    shootTimer = null;
                }
            });
    }
}