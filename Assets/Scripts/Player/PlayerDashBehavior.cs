﻿using System.Collections;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour
{
    public GameObject dashEffectPrefab;
    public GameObject dashAimerPrefab;
    public bool onlyStunBallCarriers = true;
    public bool onlyStealOnBallHit = false;
    public string[] stopDashOnCollisionWith;
    public float maxChargeTime = 1.0f;
    public float chargeRate = 1.0f;
    public float dashSpeed = 50.0f;
    public float cooldown = 0.5f;
    public float stealKnockbackAmount = 100f;
    public float stealKnockbackLength = .5f;
    public float wallHitStunTime = 0.05f;
    private PlayerStateManager stateManager;
    private PlayerMovement playerMovement;
    private Player player;
    private Rigidbody2D rb;
    private Coroutine chargeCoroutine;
    private Coroutine dashCoroutine;
    private GameObject dashEffect;
    private GameObject dashAimer;
    private float lastDashTime;
    private float chargeAmount = 0;

    private void Start()
    {
        player = this.EnsureComponent<Player>();
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();

        stateManager.OnStateChange += HandleNewPlayerState;

        GameManager.Instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedDash, DashButtonPressed, this.gameObject);
        GameManager.Instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedDash, DashButtonReleased, this.gameObject);
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        // Cleanup for old state
        if (oldState == State.ChargeDash)
        {
            StopChargeDash();
        } else if (oldState == State.Dash)
        {
            StopDash();
        }

        // Handle new state
        if (newState == State.Dash)
        {
            if (oldState != State.ChargeDash)
            {
                Debug.LogError("Entered Dash state but previous state was not ChargeDash. How?!");
            }
            StartDash();
        } else if (newState == State.ChargeDash)
        {
            StartChargeDash();
        }
    }

    public void SetPrefabColors()
    {
        if (player.Team != null)
        {
            EffectSpawner chargeEffectSpawner = this.FindEffect(EffectType.DashCharge);
            dashEffectPrefab = player.Team.resources.dashEffectPrefab;
            chargeEffectSpawner.effectPrefab = player.Team.resources.dashChargeEffectPrefab;
            dashAimerPrefab = player.Team.resources.dashAimerPrefab;
        }
    }

    private void StartChargeDash()
    {
        chargeCoroutine = StartCoroutine(ChargeDash());
    }

    private void StopChargeDash()
    {
        if (chargeCoroutine != null)
        {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
            Destroy(dashAimer);
        }
    }

    private void DashButtonPressed()
    {
        if (Time.time - lastDashTime >= cooldown && stateManager.CurrentState == State.NormalMovement)
        {
            stateManager.TransitionToState(State.ChargeDash);
        }
    }

    private void DashButtonReleased()
    {
        if (stateManager.CurrentState == State.ChargeDash)
        {
            DashInformation info = stateManager.GetStateInformationForWriting<DashInformation>(State.Dash);
            info.StartPosition = playerMovement.CurrentPosition;
            info.Velocity = (Quaternion.AngleAxis(playerMovement.CurrentRigidBodyRotation, Vector3.forward) * Vector3.right).normalized * dashSpeed * (1.0f + chargeAmount);
            stateManager.TransitionToState(State.Dash, info);
        }
    }

    private IEnumerator ChargeDash()
    {
        // TODO dkonik: Reuse this, don't instantiate every time
        dashAimer = Instantiate(dashAimerPrefab, playerMovement.CurrentPosition, playerMovement.CurrentRotation, transform);

        chargeAmount = 0.0f;

        while (true)
        {
            chargeAmount += chargeRate * Time.deltaTime;
            yield return null;
        }
    }

    private void StartDash()
    {
        dashCoroutine = StartCoroutine(Dash());
        lastDashTime = Time.time;
    }

    private void StopDash()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
            Destroy(dashEffect, 1.0f);
        }
    }

    private IEnumerator Dash()
    {
        // TODO anyone: This is where we could do something like handling turning off of the 
        // photon transform view component, since we know which way the ball will be heading for
        // a little bit.

        DashInformation information = stateManager.CurrentStateInformation_Exn<DashInformation>();

        float dashDuration = Mathf.Min(information.Velocity.magnitude, 0.5f);
        AudioManager.instance.DashSound.Play();


        // Set duration of particle system for each dash trail.
        // TODO dkonik: Do not instantiate every time
        dashEffect = Instantiate(dashEffectPrefab, playerMovement.CurrentPosition, playerMovement.CurrentRotation, transform);

        foreach (ParticleSystem ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
            ParticleSystem.MainModule main = ps.main;
            main.duration = dashDuration;
            ps.Play();
        }

        float startTime = Time.time;
        while (Time.time - startTime <= dashDuration)
        {
            yield return null;
        }

        foreach (ParticleSystem ps in dashEffect.GetComponentsInChildren<ParticleSystem>())
        {
            ps.Stop();
        }

        stateManager.TransitionToState(State.NormalMovement);
    }

    private Ball TrySteal(Player otherPlayer)
    {
        BallCarrier otherCarrier = otherPlayer.gameObject.GetComponent<BallCarrier>();
        return otherCarrier?.Ball;
    }

    private void StunAndSteal(GameObject otherGameObject)
    {
        bool hitBall = otherGameObject.GetComponent<Ball>() != null;
        Player otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            (otherPlayer.Team?.TeamColor != player.Team?.TeamColor
             || otherPlayer.Team == null || player.Team == null))
        {
            Ball ball = TrySteal(otherPlayer);

            bool shouldSteal = ball != null && (!onlyStealOnBallHit || hitBall);
            if (shouldSteal || (ball == null && !onlyStunBallCarriers))
            {
                otherPlayer.StateManager.StunNetworked(
                    otherPlayer.PlayerMovement.CurrentPosition,
                    playerMovement.CurrentVelocity.normalized * stealKnockbackAmount,
                    stealKnockbackLength,
                    true);
            }

            if (shouldSteal)
            {
                AudioManager.instance.StealSound.Play(.5f);
                PossessBallInformation info = stateManager.GetStateInformationForWriting<PossessBallInformation>(State.Possession);
                info.StoleBall = true;
                info.VictimPlayerNumber = otherPlayer.playerNumber;
                stateManager.TransitionToState(State.Possession, info);
            }
        }
    }

    private Player GetAssociatedPlayer(GameObject gameObject)
    {
        Ball ball = gameObject.GetComponent<Ball>();
        if (ball != null)
        {
            return ball.Owner;
        }
        return gameObject.GetComponent<Player>();
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        StunAndSteal(collider.gameObject);
    }

    public void OnTriggerStay2D(Collider2D collider)
    {
        StunAndSteal(collider.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        if (stateManager.CurrentState != State.Dash)
        {
            return;
        }

        int layerMask = LayerMask.GetMask(stopDashOnCollisionWith);
        if (layerMask == (layerMask | 1 << other.layer))
        {
            // TODO dkonik: We used to have a TimeDelayCall here...I'm not sure why
            // but make sure this works without it
            stateManager.TransitionToState(State.NormalMovement);
        }
        else
        {
            StunAndSteal(other);
        }

    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

}
