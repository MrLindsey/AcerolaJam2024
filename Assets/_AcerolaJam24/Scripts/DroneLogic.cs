//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DroneLogic : MonoBehaviour
{
    [SerializeField] private TaskMan _taskMan;
    [SerializeField] CharacterMan _characterMan;
    [SerializeField] MusicMan _musicMan;
    [SerializeField] private Transform[] _flyPoints;
    [SerializeField] private Transform _splitPoint;
    [SerializeField] private Transform _homePoint;
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
    [SerializeField] private RectTransform _healthBar;
    [SerializeField] private Transform _healthBarCanvas;
    [SerializeField] private FaceController _faceController;
    [SerializeField] private Animation _sectionAnim;
    [SerializeField] private Transform _subDrones;

    [SerializeField] private float _stage1Health = 0.5f;       // Sub drones
    [SerializeField] private float _stage2Health = 0.3f;       // Sneezing
    [SerializeField] private float _stage3Health = 0.15f;      // Sneezing & sub drones

    [SerializeField] float _respawnTime = 3.0f;
    [SerializeField] Transform[] _boxSpawners;
    [SerializeField] Transform[] _boxes;
    [SerializeField] ActingClip _sneezeClip;
    [SerializeField] AudioClipDef[] _sneezeAudioDefs;
    [SerializeField] AudioClipDef _AberrationAudio;
    [SerializeField] AudioClipDef _AberrationDiseaseAudio;
    [SerializeField] AudioClipDef _2ndStageAudio;

    private float _health;
    private Transform _currentTarget;
    private bool _isActive;
    private float _fireTimer;
    private float _moveTimer;
    private Vector3 _velocity;
    private Material _material;
    private float _takeDamagerTimer;
    private Rigidbody _physics;
    private AudioSource _audio;
    private AberrationEffect _aberrationEffect;
    private float _healthBarWidth;
    private bool _isSneezing;
    private float _respawnTimer;
    private List<Transform> _randomisedSpawners = new List<Transform>();
    private Character _character;
    private int _droneStage;

    private enum SplitState
    {
        Invalid,
        MoveToSplitPoint,
        SplitAnimation,
        FlyToHome,
        InHome,
    }

    private SplitState _splitState;

    // Start is called before the first frame update
    void Start()
    {
        _health = _healthPoints;
        _healthText.text = _health.ToString();
        _physics = GetComponent<Rigidbody>();
        _audio = GetComponent<AudioSource>();
        _aberrationEffect = GetComponent<AberrationEffect>();

        _isActive = true;
        MoveToRandomTarget();
        _moveTimer = _moveRate;

        _material = GetComponent<MeshRenderer>().material;
        _material.EnableKeyword("_EMISSION");

        _healthBarWidth = _healthBar.sizeDelta.x;
        _subDrones.gameObject.SetActive(false);

        foreach (Transform spawner in _boxSpawners)
            _randomisedSpawners.Add(spawner);

        _character = _characterMan.GetCurrentCharacter();
    }

    void SpawnBoxes()
    {
        ShuffleList(_randomisedSpawners);
        for (int i = 0; i < _boxes.Length; ++i)
        {
            Transform box = _boxes[i];
            Transform spawner = _randomisedSpawners[i];

            Rigidbody physics = box.GetComponent<Rigidbody>();
            if (physics.freezeRotation == false && physics.isKinematic == false)
            {
                AberrationEffect aberration = box.GetComponent<AberrationEffect>();
                aberration.DoAberrationEffect();

                box.position = spawner.position;
                box.localRotation = Random.rotation;

                if (physics.isKinematic == false)
                    physics.velocity = Vector3.zero;
            }
        }
        _respawnTimer = _respawnTime;

        if (_character != null)
        {
            AudioClipDef sneeze = _sneezeAudioDefs[Random.Range(0, _sneezeAudioDefs.Length)];
            _character.PlayActingClip(_sneezeClip, sneeze);
        }
    }

    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isActive)
        {
            if (_takeDamagerTimer <= 0.0f)
            {
                // Don't allow to be hit if the player is still holding the grabbable
                if (collision.rigidbody)
                {
                    if (collision.rigidbody.freezeRotation == false)
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
        }
    }

    void TakeDamage(float damage)
    {
        // Don't take damage if you're in a split sequence
        if (_splitState != SplitState.Invalid)
            return;

        _takeDamagerTimer = _takeDamageTime;
        _health -= damage;
        _health = Mathf.Ceil(_health);

        if (_health <= 0.0f)
        {
            KillDrone();
            _health = 0.0f;
        }

        _audio.time = 0.0f;
        _audio.Play();

        float normalisedHealth = _health / _healthPoints;
        Vector2 size = _healthBar.sizeDelta;
        size.x = normalisedHealth * _healthBarWidth;
        _healthBar.sizeDelta = size;

        _aberrationEffect.DoAberrationEffect();
    }

    void KillDrone()
    {
        _healthBarCanvas.gameObject.SetActive(false);
        _isActive = false;
        _isSneezing = false;
        _physics.useGravity = true;
        _taskMan.CompleteFromScript("KilledBot");

        _faceController.KilledCharacter();
        _musicMan.StopMusic();
    }

    void StartSneezing()
    {
       _respawnTimer = _respawnTime;
        _isSneezing = true;

        _character = _characterMan.GetCurrentCharacter();
        _character.PlayActingClip(_sneezeClip, _AberrationDiseaseAudio);
    }

    void SplitDrone()
    {
        _splitState = SplitState.MoveToSplitPoint;
        _currentTarget = _splitPoint;
        _moveTimer = 0.0f;
        _isActive = false;
        _physics.isKinematic = true;
        _physics.freezeRotation = true;
        
    }

    void PlaySplitAnim()
    {
        _splitState = SplitState.SplitAnimation;
        _sectionAnim.Stop();
        _sectionAnim.Play();

        if (_droneStage == 1)
        {
            _character = _characterMan.GetCurrentCharacter();
            _character.PlayActingClip(_sneezeClip, _AberrationAudio);
        }
    }
    public void FlyToHome()
    {
        _splitState = SplitState.FlyToHome;
        _moveTimer = 0.0f;
        _currentTarget = _homePoint;
    }

    public void DroneSplitCompleted()
    {
        // Come back out of the home
        _isActive = true;
        _splitState = SplitState.Invalid;
        MoveToRandomTarget();
        _moveTimer = _moveRate;
        _physics.isKinematic = false;
        _physics.freezeRotation = false;

        if (_droneStage == 1)
        {
            _character.PlayActingClip(_sneezeClip, _2ndStageAudio);
        }
    }

    void UpdateStage()
    {
        if (_droneStage == 0)
        {
            // Wait until half health then kick in the sub-drones
            float stageHealth = _healthPoints * _stage1Health;
            if (_health <= stageHealth)
            {
                _health = stageHealth;
                SplitDrone();
                _droneStage++;

                _musicMan.StartActionMusic();
            }
        }
        else if (_droneStage == 1)
        {
            // Wait until half health then kick in the sneezing
            float stageHealth = _healthPoints * _stage2Health;
            if (_health <= stageHealth)
            {
                _health = stageHealth;
                StartSneezing();
                _droneStage++;
            }
        }
        else if (_droneStage == 2)
        {

            // Wait until half health then kick in the sneezing
            float stageHealth = _healthPoints * _stage3Health;
            if (_health <= stageHealth)
            {
                _health = stageHealth;
                SplitDrone();
                _droneStage++;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // For Debugging
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.B))
            SplitDrone();

        if (Input.GetKeyDown(KeyCode.G))
            StartSneezing();
#endif

        UpdateStage();

        _healthBarCanvas.LookAt(_player.transform);

        float dist = (_currentTarget.position - transform.position).magnitude;
        float targetRange = 0.1f;

        // Are we processing the split state?
        if (_splitState != SplitState.Invalid)
        {
            switch (_splitState)
            {
                case SplitState.MoveToSplitPoint:

                    // Move towards the current target
                    transform.position = Vector3.SmoothDamp(transform.position, _currentTarget.position, ref _velocity, _smoothTime);

                    Quaternion targetRotation = Quaternion.LookRotation(_splitPoint.forward);
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _rotationDamping * Time.deltaTime);

                    if (dist <= targetRange)
                        PlaySplitAnim();
                    break;

                case SplitState.FlyToHome:
                    // Move towards the current target
                    transform.position = Vector3.SmoothDamp(transform.position, _currentTarget.position, ref _velocity, _smoothTime);
                    if (dist <= targetRange)
                        _splitState = SplitState.InHome;
                    break;

                case SplitState.InHome:
                    if (_isSneezing)
                    {
                        if (_respawnTimer > 0.0f)
                            _respawnTimer -= Time.deltaTime;

                        if (_respawnTimer <= 0.0f)
                            SpawnBoxes();
                    }
                    break;
            }
        }
        else
        {
            if (_isSneezing)
            {
                if (_respawnTimer > 0.0f)
                    _respawnTimer -= Time.deltaTime;

                if (_respawnTimer <= 0.0f)
                    SpawnBoxes();
            }

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
        }
    }

    void MoveToRandomTarget()
    {
        _currentTarget = _flyPoints[Random.Range(0, _flyPoints.Length)];
    }
}
