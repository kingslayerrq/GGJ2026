using System;
using Environments.Ocean;
using UnityEngine;

namespace Seahorse
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class SeahorseMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SeahorseController seahorseController;

        [Header("Swim")] 
        [SerializeField] private float maxSpeed;
        [SerializeField] private float acceleration;

        public Rigidbody2D rb;
        
        [Header("Debug")]
        [SerializeField] private float debugSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            seahorseController = GetComponent<SeahorseController>();
            
            rb.gravityScale = 0; // no falling
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 vel = rb.linearVelocity;
            Vector2 move = seahorseController != null ? seahorseController.Move : Vector2.zero;
            
            Vector2 targetVel = move * maxSpeed;
            vel = Vector2.MoveTowards(vel, targetVel, acceleration * dt);
            
            vel = Vector2.ClampMagnitude(vel, maxSpeed);
            rb.linearVelocity = vel;
            
            debugSpeed = rb.linearVelocity.magnitude;
        }
    }
}

