using System;
using System.Collections.Generic;
using Seahorse;
using UnityEngine;

namespace Environments.Ocean
{
    [DefaultExecutionOrder(100)]            // run after default scripts
    [RequireComponent(typeof(Collider2D))]
    public class OceanBound : MonoBehaviour
    {
        public Bounds OceanBounds {get; private set;}
        
        [Header("References")]
        [SerializeField] private OceanResistance oceanResistance;

        private Collider2D oceanCollider;
        
        private readonly HashSet<Rigidbody2D> rbInOceanBounds = new HashSet<Rigidbody2D>();

        private void Awake()
        {
            oceanCollider = GetComponent<Collider2D>();
            oceanCollider.isTrigger = true;
            
            if (oceanResistance == null) oceanResistance = GetComponent<OceanResistance>();
            
            OceanBounds = oceanCollider.bounds;
        }

        private void FixedUpdate()
        {
            if (oceanResistance == null) return;
            
            float dt = Time.fixedDeltaTime;
            
            // Apply drag
            foreach (Rigidbody2D rb in rbInOceanBounds)
            {
                if (rb == null) continue;
                Vector2 v = rb.linearVelocity;
                oceanResistance.Apply(ref v, dt);
                rb.linearVelocity = v;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            Rigidbody2D rb = collision.attachedRigidbody;            // child rb included
            if (rb == null) return;
            rbInOceanBounds.Add(rb);
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            Rigidbody2D rb = collision.attachedRigidbody;
            if (rb == null) return;
            rbInOceanBounds.Add(rb);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            Rigidbody2D rb = collision.attachedRigidbody;
            if (rb == null) return;
            rbInOceanBounds.Remove(rb);
        }
    }

}