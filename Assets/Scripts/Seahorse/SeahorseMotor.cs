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

        private const float EdgeEpsilon = 0.01f;

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

            if (TryGetPlayableBounds(out float minX, out float maxX, out float minY, out float maxY))
            {
                Vector2 pos = rb.position;

                if (pos.x <= minX + EdgeEpsilon && targetVel.x < 0f) targetVel.x = 0f;
                if (pos.x >= maxX - EdgeEpsilon && targetVel.x > 0f) targetVel.x = 0f;
                if (pos.y <= minY + EdgeEpsilon && targetVel.y < 0f) targetVel.y = 0f;
                if (pos.y >= maxY - EdgeEpsilon && targetVel.y > 0f) targetVel.y = 0f;
            }

            vel = Vector2.MoveTowards(vel, targetVel, acceleration * dt);
            vel = Vector2.ClampMagnitude(vel, maxSpeed);
            rb.linearVelocity = vel;

            ClampInsideOceanBounds();

            debugSpeed = rb.linearVelocity.magnitude;
        }

        private bool TryGetPlayableBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = maxX = minY = maxY = 0f;
            if (oceanBound == null) return false;

            Bounds b = oceanBound.OceanBounds;
            float halfW = 0f;
            float halfH = 0f;
            if (bodyCollider != null)
            {
                Vector3 ext = bodyCollider.bounds.extents;
                halfW = ext.x;
                halfH = ext.y;
            }

            minX = b.min.x + halfW;
            maxX = b.max.x - halfW;
            minY = b.min.y + halfH;
            maxY = b.max.y - halfH;
            return true;
        }

        private void ClampInsideOceanBounds()
        {
            if (!TryGetPlayableBounds(out float minX, out float maxX, out float minY, out float maxY)) return;

            Vector2 pos = rb.position;
            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedY = Mathf.Clamp(pos.y, minY, maxY);
            Vector2 clampedPos = new Vector2(clampedX, clampedY);

            if (clampedPos != pos)
            {
                rb.position = clampedPos;
            }

            Vector2 v = rb.linearVelocity;
            if (clampedPos.x <= minX + EdgeEpsilon && v.x < 0f) v.x = 0f;
            if (clampedPos.x >= maxX - EdgeEpsilon && v.x > 0f) v.x = 0f;
            if (clampedPos.y <= minY + EdgeEpsilon && v.y < 0f) v.y = 0f;
            if (clampedPos.y >= maxY - EdgeEpsilon && v.y > 0f) v.y = 0f;
            rb.linearVelocity = v;
        }
    }
}
