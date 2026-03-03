using UnityEngine;

namespace Environments.Ocean
{
    public class OceanResistance : MonoBehaviour
    {
        [Header("Resistance")]
        [Tooltip("Linear damping. Higher = thicker water.")]
        [SerializeField] private float linearResistance = 4f;

        [Tooltip("Extra damping that increases with speed (0 = off).")]
        [SerializeField] private float quadraticResistance = 0.8f;

        [Tooltip("Below this speed, snap to 0 to prevent endless drifting.")]
        [SerializeField] private float stopSpeedEpsilon = 0.03f;
        
        /// <summary>
        /// Applies ocean resistance to a velocity vector.
        /// Call this from motor before assigning rb.linearVelocity.
        /// </summary>
        public void Apply(ref Vector2 velocity, float dt)
        {
            float speed = velocity.magnitude;
            if (speed <= stopSpeedEpsilon)
            {
                velocity = Vector2.zero;
                return;
            }
            
            float linearFactor = Mathf.Exp(-linearResistance * dt);
            
            float quadFactor = 1f;
            if (quadraticResistance > 0f)
                quadFactor = 1f / (1f + quadraticResistance * speed * dt);

            velocity *= (linearFactor * quadFactor);
        }
    }
}

