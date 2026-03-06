using UnityEngine;
using UnityEngine.InputSystem;

namespace Seahorse
{
    public class SeahorseController : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        
        [Tooltip("If true, normalizes diagonal input so speed is consistent.")]
        [SerializeField] private bool normalize = true;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                Move = Vector2.zero;
                return;
            }

            float x = 0f;
            float y = 0f;

            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.sKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed) y += 1f;

            
            var v =  new Vector2(x, y);
            v = Vector2.ClampMagnitude(v, 1f);
            if (normalize) v.Normalize();
            
            Move = v;
        }
    }
}

