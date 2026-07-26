using UnityEngine;

public class CoverSeaMotion : MonoBehaviour
{
    [SerializeField] private float horizontalDistance = 0.08f;
    [SerializeField] private float horizontalSpeed = 0.25f;

    [SerializeField] private float verticalDistance = 0.025f;
    [SerializeField] private float verticalSpeed = 0.55f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        float x =
            Mathf.Sin(Time.time * horizontalSpeed)
            * horizontalDistance;

        float y =
            Mathf.Sin(Time.time * verticalSpeed + 1.2f)
            * verticalDistance;

        transform.localPosition =
            startPosition + new Vector3(x, y, 0f);
    }
}