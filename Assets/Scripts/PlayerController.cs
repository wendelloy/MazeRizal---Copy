using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        // Get input from arrow keys or WASD
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Move the player
        Vector2 movement = new Vector2(moveX, moveY).normalized;
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Finish")
        {
            MazeGenerator generator = FindObjectOfType<MazeGenerator>();
            generator.PlayerReachedFinish(); // Trigger winning scenario
        }
        else
        {
            Debug.Log($"Collided with: {collision.gameObject.name}");
        }
    }
}
