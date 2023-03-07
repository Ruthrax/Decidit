using UnityEngine;

// Makes a transform oscillate relative to its start position
public class ProjectileOscillator : MonoBehaviour
{
    public float Amplitude = .3f;
    [SerializeField] float _frequency = 10f;
    Vector3 _actualDirection;
    float _t = 0f;
    Vector3 _lastFrameOffset;

    public void Setup(Vector3 direction, bool centered, float offset)
    {
        //_t = offset;

        _actualDirection = transform.right * direction.x + transform.up * direction.y + transform.forward * direction.z;
        transform.position += transform.forward * offset;
        if (centered)
            transform.position += _actualDirection * (Amplitude / 2);

        _lastFrameOffset = Vector3.zero;
    }

    void Update()
    {
        _t += Time.deltaTime * _frequency;
        transform.position -= _lastFrameOffset;
        Vector3 offset = _actualDirection * (Mathf.Sin(Mathf.PI * _t)) * Amplitude;
        transform.position += offset;
        _lastFrameOffset = offset;
    }
}
