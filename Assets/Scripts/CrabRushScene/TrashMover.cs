using UnityEngine;

public class TrashMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float destroyX = -30f;

    private void Update()
    {
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // destroy when left screen
        if (transform.position.x <= destroyX)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("hit player");
            Destroy(gameObject);
        }
    }
}