using TMPro;
using UnityEngine;

public class FloatingDamageNumber : MonoBehaviour
{
    [Header("Animation Settings")]
    public float riseSpeed = 1.5f;
    public float lifetime = 1f;
    public float scaleUp = 1.2f;
    public float fadeStart = 0.5f;

    private TextMeshPro textMesh;
    private Color originalColor;
    private Vector3 startScale;
    private float timer;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        originalColor = textMesh.color;
        startScale = transform.localScale;
    }

    public void Initialize(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
        originalColor = color;

        // Add a slight random offset for variation
        transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Make it face the camera
        if (Camera.main != null)
            transform.forward = Camera.main.transform.forward;

        // Move upward
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Scale up slightly for a pop effect
        float scaleFactor = Mathf.Lerp(scaleUp, 1f, timer / lifetime);
        transform.localScale = startScale * scaleFactor;

        // Fade out after fadeStart
        if (timer > lifetime * fadeStart)
        {
            float fadeProgress = (timer - lifetime * fadeStart) / (lifetime * (1 - fadeStart));
            Color c = textMesh.color;
            c.a = Mathf.Lerp(originalColor.a, 0, fadeProgress);
            textMesh.color = c;
        }

        // Destroy after lifetime
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
