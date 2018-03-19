﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

using IC = InControl;

public class BallCarrier : MonoBehaviour {

    public float coolDownTime = .1f;
    public Ball ball { private set; get;}
    public float ballTurnSpeed = 10f;
    public bool chargedBallStuns = false;

    float ballOffsetFromCenter = .5f;
    PlayerMovement playerMovement;
    PlayerStateManager stateManager;
    Coroutine carryBallCoroutine;
    bool isCoolingDown = false;
    BoxCollider2D ballGrabber;

    public bool IsCarryingBall() {
        return ball != null;
    }

    void Start() {
        playerMovement = GetComponent<PlayerMovement>();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerMovement != null && stateManager != null) {
            stateManager.CallOnStateEnter(
                State.Posession, playerMovement.FreezePlayer);
            stateManager.CallOnStateExit(
                State.Posession, playerMovement.UnFreezePlayer);
        }
        ballGrabber = GetComponent<BoxCollider2D>();
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public void StartCarryingBall(Ball ball) {
        CalculateOffset(ball);
        ball.charged = false;
        Utility.TutEvent("BallPickup", this);
        carryBallCoroutine = StartCoroutine(CarryBall(ball));
    }

    void CalculateOffset(Ball ball) {
        var ballRadius = ball.GetComponent<CircleCollider2D>()?.bounds.extents.x;
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && ballRadius != null) {
            ballOffsetFromCenter = renderer.sprite.bounds.extents.x + ballRadius.Value;
        }
    }

    IEnumerator CarryBall(Ball ball) {
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        this.ball = ball;
        ball.owner = this;

        while (true) {
            playerMovement?.RotatePlayer();
            PlaceBallAtNose();
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator CoolDownTimer() {
        isCoolingDown = true;
        yield return new WaitForSeconds(coolDownTime);
        isCoolingDown = false;
    }

    public void DropBall() {
        if (ball != null) {

            StopCoroutine(carryBallCoroutine);
            carryBallCoroutine = null;

            // Reset references
            ball.owner = null;
            ball = null;
            StartCoroutine(CoolDownTimer());
        }
    }

    Vector2 NosePosition(Ball ball) {
        var newPosition = transform.position +
            (1.03f * transform.right * ballOffsetFromCenter);
        return newPosition;
    }

    void PlaceBallAtNose() {
        if (ball != null) {
            var rigidbody = ball.GetComponent<Rigidbody2D>();
            Vector2 newPosition = CircularLerp(
                ball.transform.position, NosePosition(ball), transform.position,
                ballOffsetFromCenter, Time.deltaTime, ballTurnSpeed);
            rigidbody.MovePosition(newPosition);
        }
    }

    Vector2 CircularLerp(Vector2 start, Vector2 end, Vector2 center, float radius,
                      float timeDelta, float speed) {
        float angularDistance = timeDelta * speed;
        var centeredStart = start - center;
        var centerToStartDirection = centeredStart.normalized;

        var centeredEndDirection = (end - center).normalized;
        var angle = Vector2.SignedAngle(centerToStartDirection, centeredEndDirection);
        var arcDistance = radius * 2 * Mathf.PI * Mathf.Abs(angle / 360);
        var percentArc = Mathf.Clamp(angularDistance / arcDistance, 0, 1);

        var rotation = Quaternion.AngleAxis(angle * percentArc, Vector3.forward);
        var centeredResult = rotation * centerToStartDirection;
        centeredResult *= radius;
        return (Vector2) centeredResult + center;
    }

    void HandleCollision(GameObject thing) {
        var ball = thing.GetComponent<Ball>();
        if (ball == null || !ball.IsOwnable() || isCoolingDown) {
            return;
        }
        if (stateManager != null) {
            var last_team = ball.lastOwner?.GetComponent<Player>().team;
            var this_team = GetComponent<Player>().team;
            if (chargedBallStuns && ball.charged && last_team != this_team) {
                var stun = GetComponent<PlayerStun>();
                var direction = transform.position - ball.transform.position;
                var knockback = ball.GetComponent<Rigidbody2D>().velocity.magnitude * direction;
                stateManager.AttemptStun(() => stun.StartStun(knockback), stun.StopStunned);
            } else {
                stateManager.AttemptPossession(() => StartCarryingBall(ball), DropBall);
            }
        } else {
            StartCoroutine(CoroutineUtility.RunThenCallback(CarryBall(ball), DropBall));
        }
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        HandleCollision(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other) {
        HandleCollision(other.gameObject);
    }
}
