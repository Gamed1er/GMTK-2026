using UnityEngine;

public class CoverBoatMotion : MonoBehaviour
{
    [Header("Vertical Bob")]
    [SerializeField] private float bobHeight = 0.08f;
    [SerializeField] private float bobSpeed = 0.65f;

    [Header("Rotation")]
    [SerializeField] private float rotationAngle = 0.4f;
    [SerializeField] private float rotationSpeed = 0.5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float verticalOffset =
            Mathf.Sin(Time.time * bobSpeed) * bobHeight;

        float angle =
            Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;

        transform.localPosition =
            startPosition + Vector3.up * verticalOffset;

        transform.localRotation =
            startRotation * Quaternion.Euler(0f, 0f, angle);
    }
}