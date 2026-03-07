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
        [SerializeField] private OceanBound oceanBound;

        [Header("Swim")] 
        [SerializeField] private float maxSpeed;
        [SerializeField] private float acceleration;

        public Rigidbody2D rb;
        private Collider2D bodyCollider;

        [Header("Debug")]
        [SerializeField] private float debugSpeed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            seahorseController = GetComponent<SeahorseController>();
            bodyCollider = GetComponent<Collider2D>();
            if (oceanBound == null) oceanBound = FindFirstObjectByType<OceanBound>();
            
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

            ClampInsideOceanBounds();
            
            debugSpeed = rb.linearVelocity.magnitude;
        }

        private void ClampInsideOceanBounds()
        {
            if (oceanBound == null) return;

            Bounds b = oceanBound.OceanBounds;

            float halfW = 0f;
            float halfH = 0f;
            if (bodyCollider != null)
            {
                Vector3 ext = bodyCollider.bounds.extents;
                halfW = ext.x;
                halfH = ext.y;
            }

            float minX = b.min.x + halfW;
            float maxX = b.max.x - halfW;
            float minY = b.min.y + halfH;
            float maxY = b.max.y - halfH;

            Vector2 pos = rb.position;
            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedY = Mathf.Clamp(pos.y, minY, maxY);

            bool hitLeft = clampedX <= minX && pos.x < minX;
            bool hitRight = clampedX >= maxX && pos.x > maxX;
            bool hitBottom = clampedY <= minY && pos.y < minY;
            bool hitTop = clampedY >= maxY && pos.y > maxY;

            if (!Mathf.Approximately(pos.x, clampedX) || !Mathf.Approximately(pos.y, clampedY))
            {
                rb.position = new Vector2(clampedX, clampedY);

                Vector2 v = rb.linearVelocity;
                if ((hitLeft && v.x < 0f) || (hitRight && v.x > 0f)) v.x = 0f;
                if ((hitBottom && v.y < 0f) || (hitTop && v.y > 0f)) v.y = 0f;
                rb.linearVelocity = v;
            }
        }
    }
}
