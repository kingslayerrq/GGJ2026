using System;
using Environments.Ocean;
using UnityEngine;

namespace Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class SeahorseCam : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("References")] 
        [SerializeField] private OceanBound oceanBound;

        [Header("Settings")] 
        [SerializeField] private float smoothTime = 0.12f;
        [SerializeField] private Vector2 followOffset = Vector2.zero;

        private UnityEngine.Camera cam;
        private Vector3 vel;

        private void Awake()
        {
            cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic = true;
            
            if (oceanBound == null) oceanBound = FindFirstObjectByType<OceanBound>();
        }

        private void LateUpdate()
        {
            if (target == null || oceanBound == null) return;
            
            Vector3 nextPos = new Vector3(target.position.x + followOffset.x, target.position.y + followOffset.y,
                transform.position.z);
            
            // smooth
            Vector3 smoothed = Vector3.SmoothDamp(transform.position, nextPos, ref vel, smoothTime);
            
            // Clamp to bounds
            transform.position = ClampToBounds(smoothed, oceanBound.OceanBounds);

        }
        
        private Vector3 ClampToBounds(Vector3 cameraPos, Bounds worldBounds)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            float minX = worldBounds.min.x + halfWidth;
            float maxX = worldBounds.max.x - halfWidth;
            float minY = worldBounds.min.y + halfHeight;
            float maxY = worldBounds.max.y - halfHeight;

            // If bounds are smaller than the camera view, lock to center on that axis.
            if (maxX < minX) cameraPos.x = worldBounds.center.x;
            else cameraPos.x = Mathf.Clamp(cameraPos.x, minX, maxX);

            if (maxY < minY) cameraPos.y = worldBounds.center.y;
            else cameraPos.y = Mathf.Clamp(cameraPos.y, minY, maxY);

            return cameraPos;
        }
    }

}