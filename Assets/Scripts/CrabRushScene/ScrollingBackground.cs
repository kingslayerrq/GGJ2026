using UnityEngine;

public class ScrollingBackground : MonoBehaviour
{
    public float speed = 3f;

    float width;

    void Start()
    {
        width = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        if (transform.position.x < -width)
        {
            transform.position += new Vector3(width * 2f - 0.05f, 0, 0);
        }
    }
}