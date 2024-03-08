using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DroneLogic : MonoBehaviour
{
    [SerializeField] private TaskMan _taskMan;
    [SerializeField] private Transform[] _flyPoints;
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private PlayerController _player;
    [SerializeField] private float _smoothTime = 0.3f;
    [SerializeField] private float _rotationDamping = 0.4f;
    [SerializeField] private float _fireRate = 1.0f;
    [SerializeField] private float _moveRate = 4.0f;
    [SerializeField] private float _healthPoints = 100.0f;
    [SerializeField] private float _takeDamageTime = 0.2f;
    [SerializeField] private float _minDamageForce = 5.0f;
    [SerializeField] private float _minDamageWhenHit = 5.0f;
    [SerializeField] private float _forceToDamageScalar = 0.1f;

    private float _health;
    private Transform _currentTarget;
    private bool _isActive;
    private float _fireTimer;
    private float _moveTimer;
    private Vector3 _velocity;
    private Material _material;
    private float _takeDamagerTimer;
    private Rigidbody _physics;

    // Start is called before the first frame update
    void Start()
    {
        _health = _healthPoints;
        _healthText.text = _health.ToString();
        _physics = GetComponent<Rigidbody>();

        _isActive = true;
        MoveToRandomTarget();
        _moveTimer = _moveRate;

        _material = GetComponent<MeshRenderer>().material;
        _material.EnableKeyword("_EMISSION");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isActive)
        {
            if (_takeDamagerTimer <= 0.0f)
            {
                // Have we been hit by a throwable object?
                if (collision.collider.CompareTag("Throwable"))
                {
                    if (collision.relativeVelocity.magnitude > _minDamageForce)
                    {
                        // Calculate the force of collision
                        float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
                        if (collisionForce > 0)
                        {
                            float damage = collisionForce * _forceToDamageScalar;
                            if (damage < _minDamageWhenHit)
                                damage = _minDamageWhenHit;

                            TakeDamage(damage);
                        }
                    }
                }
            }
        }
    }

    void TakeDamage(float damage)
    {
        _takeDamagerTimer = _takeDamageTime;
        _health -= damage;
        _health = Mathf.Ceil(_health);

        if (_health <= 0.0f)
        {
            KillDrone();
            _health = 0.0f;
        }

        _healthText.text = _health.ToString();
    }

    void KillDrone()
    {
        _isActive = false;
        _physics.useGravity = true;
        _taskMan.CompleteFromScript("KilledBot");
    }

    // Update is called once per frame
    void Update()
    {
        if (_isActive)
        {
            // Move logic
            if (_moveTimer > 0.0f)
            {
                _moveTimer -= Time.deltaTime;
                if (_moveTimer <= 0.0f)
                {
                    // Move to the next target
                    MoveToRandomTarget();
                    _moveTimer = _moveRate;
                }
            }

            // Move towards the current target
            transform.position = Vector3.SmoothDamp(transform.position, _currentTarget.position, ref _velocity, _smoothTime);

            // Smoothly rotation towards the player
            Vector3 direction = _currentTarget.position - _player.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationDamping * Time.deltaTime);

            // Fire logic
            if (_fireTimer > 0.0f)
            {
                _fireTimer -= Time.deltaTime;
                if (_fireTimer <= 0.0f)
                {
                    // Shoot the player
                    _fireTimer = _fireRate;
                }
            }
        }

        if (_takeDamagerTimer > 0.0f)
        {
            _takeDamagerTimer -= Time.deltaTime;

            float channel = _takeDamagerTimer / _takeDamageTime;
            Color col = new Color(channel, channel, channel);

            _material.SetColor("_EmissionColor", col);

            if (_takeDamagerTimer <= 0.0f)
            {
                _takeDamagerTimer = 0.0f;
            }
        }
    }

    void MoveToRandomTarget()
    {
        _currentTarget = _flyPoints[Random.Range(0, _flyPoints.Length)];
    }
}
