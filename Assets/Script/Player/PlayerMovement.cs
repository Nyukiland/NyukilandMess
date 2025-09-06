using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
	[SerializeField, Min(0)]
	float _speed;

	[SerializeField, Min(0)]
	float _distFromGround;

	[SerializeField]
	LayerMask _layerToIgnore;

	Rigidbody _rb;

	InputMap _input;

	Vector2 _moveInput;

	private void Awake()
	{
		_input = new InputMap();
		_input.Enable();
		_input.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
		_input.Player.Move.canceled += ctx => _moveInput = ctx.ReadValue<Vector2>();
	}

	void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		Vector3 veloToApply = _rb.linearVelocity;

		Physics.Raycast(transform.position, -transform.up, out RaycastHit hit, Mathf.Infinity, ~_layerToIgnore);

		transform.rotation = Quaternion.LookRotation(transform.forward, hit.normal);

		float dist = Vector3.Distance(transform.position, hit.point);

		transform.position = new Vector3(transform.position.x, hit.point.y + _distFromGround, transform.position.z);

		_rb.linearVelocity = new Vector3(_moveInput.x * _speed, veloToApply.y, _moveInput.y * _speed);
    }
}
