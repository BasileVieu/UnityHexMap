using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    private static HexMapCamera instance;

    private Transform swivel;
    private Transform stick;

    private float zoom = 1.0f;

    public float stickMinZoom;
    public float stickMaxZoom;

    public float swivelMinZoom;
    public float swivelMaxZoom;

    public float moveSpeedMinZoom;
    public float moveSpeedMaxZoom;

    public float rotationSpeed;

    public HexGrid grid;

    private float rotationAngle;

    public static bool Locked
    {
        set => instance.enabled = !value;
    }

    private void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    void OnEnable()
    {
        instance = this;

        ValidatePosition();
    }

    private void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");

        if (zoomDelta != 0.0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");

        if (rotationDelta != 0.0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");

        if (xDelta != 0.0f
            || zDelta != 0.0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    public static void ValidatePosition()
    {
        instance.AdjustPosition(0.0f, 0.0f);
    }

    private void AdjustZoom(float _delta)
    {
        zoom = Mathf.Clamp01(zoom + _delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);

        stick.localPosition = new Vector3(0.0f, 0.0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);

        swivel.localRotation = Quaternion.Euler(angle, 0.0f, 0.0f);
    }

    private void AdjustPosition(float _xDelta, float _zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(_xDelta, 0.0f, _zDelta).normalized;

        float damping = Mathf.Max(Mathf.Abs(_xDelta), Mathf.Abs(_zDelta));

        float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = grid.wrapping ? WrapPosition(position) : ClampPosition(position);
    }

    private Vector3 WrapPosition(Vector3 _position)
    {
        float width = grid.cellCountX * HexMetrics.innerDiameter;

        while (_position.x < 0.0f)
        {
            _position.x += width;
        }

        while (_position.x > width)
        {
            _position.x -= width;
        }

        float zMax = (grid.cellCountZ - 1.0f) * (1.5f * HexMetrics.outerRadius);
        _position.z = Mathf.Clamp(_position.z, 0.0f, zMax);

        grid.CenterMap(_position.x);

        return _position;
    }

    private Vector3 ClampPosition(Vector3 _position)
    {
        float xMax = (grid.cellCountX - 0.5f) * HexMetrics.innerDiameter;
        _position.x = Mathf.Clamp(_position.x, 0.0f, xMax);

        float zMax = (grid.cellCountZ - 1.0f) * (1.5f * HexMetrics.outerRadius);
        _position.z = Mathf.Clamp(_position.z, 0.0f, zMax);

        return _position;
    }

    private void AdjustRotation(float _delta)
    {
        rotationAngle += _delta * rotationSpeed * Time.deltaTime;

        if (rotationAngle < 0.0f)
        {
            rotationAngle = 360.0f;
        }
        else if (rotationAngle >= 360.0f)
        {
            rotationAngle = 0.0f;
        }

        transform.localRotation = Quaternion.Euler(0.0f, rotationAngle, 0.0f);
    }
}