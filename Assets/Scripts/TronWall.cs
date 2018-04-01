﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using System.Linq;

public class TronWall : MonoBehaviour {

    public float wallDestroyTime = .3f;
    public float particleSystemLifetime = .5f;

    float lifeLength {get; set;}
    TeamManager team;
    LineRenderer lineRenderer;
    Vector3[] linePoints = new Vector3[2];
    PlayerTronMechanic creator;
    Coroutine stretchWallCoroutine;
    EdgeCollider2D edgeCollider;
    float tronWallOffset;
    ParticleSystem ps;

    void Start() {
        ps = this.EnsureComponent<ParticleSystem>();
    }

    public void Initialize (PlayerTronMechanic creator, float lifeLength, TeamManager team,
                            float tronWallOffset) {
        this.lifeLength = lifeLength;
        this.team = team;
        this.creator = creator;
        this.tronWallOffset = tronWallOffset;

        lineRenderer = this.EnsureComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        linePoints[0] = creator.transform.position - ((creator.transform.position - transform.position)).normalized * tronWallOffset;

        edgeCollider = this.EnsureComponent<EdgeCollider2D>();

        lineRenderer.material = team.resources.wallMaterial;
        stretchWallCoroutine = StartCoroutine(StretchWall());
    }

    IEnumerator StretchWall() {
        while (true) {
            var endPoint = creator.transform.position - ((creator.transform.position - transform.position)).normalized * tronWallOffset;
            linePoints[1] = endPoint;
            SetRendererAndColliderPoints();
            yield return new WaitForFixedUpdate();
        }
    }

    public void PlaceWall() {
        if (stretchWallCoroutine != null) {
            StopCoroutine(stretchWallCoroutine);
            stretchWallCoroutine = null;
            this.TimeDelayCall(() => StartCoroutine(Collapse()), lifeLength);
        }
    }

    void SetRendererAndColliderPoints() {
        lineRenderer.SetPositions(linePoints);
        edgeCollider.points = linePoints.
            Select(point => (Vector2) transform.InverseTransformPoint(point)).ToArray();
    }

    public void KillSelf() {
        creator.StopWatching(this);
        Destroy(gameObject);
    }

    IEnumerator Collapse() {
        creator.StopWatching(this);
        var elapsedTime = 0f;
        var startingPoint = linePoints[0];
        while (elapsedTime < wallDestroyTime) {
            linePoints[0] = Vector3.Lerp(startingPoint, linePoints[1], elapsedTime / wallDestroyTime);
            SetRendererAndColliderPoints();
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        lineRenderer.enabled = false;
        transform.position = linePoints[1];
        var main = ps.main;
        main.startColor = team.teamColor.color;
        main.startLifetime = particleSystemLifetime;
        ps.Play();
        this.TimeDelayCall(() => Destroy(gameObject), particleSystemLifetime);
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        var other = collision.gameObject;
        var player = other.GetComponent<Player>();
        var stateManager = other.GetComponent<PlayerStateManager>();

        if (stretchWallCoroutine != null) {
            // Check if it was your teammate
            var otherPlayer = other.GetComponent<Player>();
            if (otherPlayer != null &&
                otherPlayer.team.teamColor == team.teamColor) {
                return;
            }

            creator.HandleWallCollision();
            Destroy(gameObject);
            return;
        }

        var ball = other.GetComponent<Ball>();
        if (ball != null) {
            KillSelf();
        } else if ((player != null) && (stateManager != null) &&
            (stateManager.currentState == State.Dash)) {
            KillSelf();
            var playerStun = other.EnsureComponent<PlayerStun>();
            stateManager.AttemptStun(() =>
                    {playerStun.StartStun(Vector2.zero, creator.wallBreakerStunTime);
                     other.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
                    },
                                     playerStun.StopStunned);
        }

    }
}
