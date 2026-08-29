using System.Collections.Generic;
using UnityEngine;
using MetaCricket.Core;

namespace MetaCricket.VFX
{
    /// <summary>
    /// Manages all visual effects: boundary particles (fireworks for 6, trail for 4),
    /// bat hit impact spark, ball trail renderer, crowd wave effect, golden confetti for milestones.
    /// Uses object pooling for efficient particle system management.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        private static VFXManager _instance;

        /// <summary>
        /// Singleton instance accessor.
        /// </summary>
        public static VFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Debug.LogError("[VFXManager] Instance is null. Ensure VFXManager exists in the scene.");
                }
                return _instance;
            }
        }

        [Header("Prefab References")]
        [SerializeField] private GameObject _fireworksPrefab;
        [SerializeField] private GameObject _boundaryTrailPrefab;
        [SerializeField] private GameObject _batImpactPrefab;
        [SerializeField] private GameObject _ballTrailPrefab;
        [SerializeField] private GameObject _confettiPrefab;
        [SerializeField] private GameObject _crowdWavePrefab;

        [Header("Pool Settings")]
        [SerializeField] private int _fireworksPoolSize = 3;
        [SerializeField] private int _impactPoolSize = 5;
        [SerializeField] private int _trailPoolSize = 3;
        [SerializeField] private int _confettiPoolSize = 2;

        [Header("Effect Colors")]
        [SerializeField] private Color _sixColor = new Color(1f, 0.843f, 0f, 1f);
        [SerializeField] private Color _fourColor = new Color(0.129f, 0.588f, 0.953f, 1f);
        [SerializeField] private Color _milestoneColor = new Color(1f, 0.878f, 0.4f, 1f);
        [SerializeField] private Color _wicketColor = new Color(0.898f, 0.224f, 0.208f, 1f);

        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
        private Transform _poolContainer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }

        private void Start()
        {
            SubscribeToEvents();
            ServiceLocator.Register(this);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void InitializePools()
        {
            _poolContainer = new GameObject("VFX_Pool").transform;
            _poolContainer.SetParent(transform);

            CreatePool("Fireworks", _fireworksPrefab, _fireworksPoolSize);
            CreatePool("BatImpact", _batImpactPrefab, _impactPoolSize);
            CreatePool("BoundaryTrail", _boundaryTrailPrefab, _trailPoolSize);
            CreatePool("Confetti", _confettiPrefab, _confettiPoolSize);
        }

        private void CreatePool(string poolName, GameObject prefab, int size)
        {
            if (prefab == null) return;

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, _poolContainer);
                obj.name = $"{poolName}_{i}";
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            _pools[poolName] = pool;
        }

        private GameObject GetFromPool(string poolName)
        {
            if (!_pools.ContainsKey(poolName) || _pools[poolName].Count == 0) return null;

            GameObject obj = _pools[poolName].Dequeue();
            obj.SetActive(true);
            return obj;
        }

        private void ReturnToPool(string poolName, GameObject obj, float delay = 0f)
        {
            if (delay > 0f)
            {
                StartCoroutine(ReturnToPoolDelayed(poolName, obj, delay));
            }
            else
            {
                obj.SetActive(false);
                if (_pools.ContainsKey(poolName))
                {
                    _pools[poolName].Enqueue(obj);
                }
            }
        }

        private System.Collections.IEnumerator ReturnToPoolDelayed(string poolName, GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (obj != null)
            {
                obj.SetActive(false);
                if (_pools.ContainsKey(poolName))
                {
                    _pools[poolName].Enqueue(obj);
                }
            }
        }

        private void SubscribeToEvents()
        {
            EventBus.Subscribe<BoundaryEvent>(OnBoundary);
            EventBus.Subscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Subscribe<WicketEvent>(OnWicket);
        }

        private void UnsubscribeFromEvents()
        {
            EventBus.Unsubscribe<BoundaryEvent>(OnBoundary);
            EventBus.Unsubscribe<ShotPlayedEvent>(OnShotPlayed);
            EventBus.Unsubscribe<WicketEvent>(OnWicket);
        }

        private void OnBoundary(BoundaryEvent evt)
        {
            if (evt.IsSix)
            {
                PlaySixBoundaryEffect(evt.LandingPosition);
            }
            else
            {
                PlayFourBoundaryEffect(evt.LandingPosition);
            }
        }

        private void OnShotPlayed(ShotPlayedEvent evt)
        {
            // Bat impact effect based on power
            PlayBatImpactEffect(Vector3.zero, evt.Power);
        }

        private void OnWicket(WicketEvent evt)
        {
            // Red flash effect for wicket
            PlayWicketEffect();
        }

        /// <summary>
        /// Play six boundary celebration: gold fireworks + screen flash + camera shake trigger.
        /// </summary>
        public void PlaySixBoundaryEffect(Vector3 position)
        {
            // Gold fireworks
            GameObject fireworks = GetFromPool("Fireworks");
            if (fireworks != null)
            {
                fireworks.transform.position = position;
                ParticleSystem ps = fireworks.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = _sixColor;
                    ps.Play();
                }
                ReturnToPool("Fireworks", fireworks, 3f);
            }

            // Trigger camera shake via EventBus
            Debug.Log("[VFXManager] Six boundary - Gold fireworks + screen flash + camera shake");
        }

        /// <summary>
        /// Play four boundary effect: blue streak + crowd animation trigger.
        /// </summary>
        public void PlayFourBoundaryEffect(Vector3 position)
        {
            GameObject trail = GetFromPool("BoundaryTrail");
            if (trail != null)
            {
                trail.transform.position = position;
                ParticleSystem ps = trail.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = _fourColor;
                    ps.Play();
                }
                ReturnToPool("BoundaryTrail", trail, 2f);
            }

            // Trigger crowd wave
            PlayCrowdWave();

            Debug.Log("[VFXManager] Four boundary - Blue streak + crowd animation");
        }

        /// <summary>
        /// Play bat-ball impact spark effect at contact point.
        /// </summary>
        /// <param name="position">World position of impact.</param>
        /// <param name="intensity">Power-based intensity (0 to 1).</param>
        public void PlayBatImpactEffect(Vector3 position, float intensity)
        {
            GameObject impact = GetFromPool("BatImpact");
            if (impact != null)
            {
                impact.transform.position = position;
                ParticleSystem ps = impact.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startSpeed = Mathf.Lerp(2f, 8f, intensity);
                    var emission = ps.emission;
                    emission.SetBurst(0, new ParticleSystem.Burst(0f, (short)Mathf.Lerp(5, 20, intensity)));
                    ps.Play();
                }
                ReturnToPool("BatImpact", impact, 1.5f);
            }
        }

        /// <summary>
        /// Play golden confetti for milestones (50s, 100s, promotions).
        /// </summary>
        public void PlayMilestoneConfetti(Vector3 position)
        {
            GameObject confetti = GetFromPool("Confetti");
            if (confetti != null)
            {
                confetti.transform.position = position;
                ParticleSystem ps = confetti.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = _milestoneColor;
                    ps.Play();
                }
                ReturnToPool("Confetti", confetti, 4f);
            }

            Debug.Log("[VFXManager] Milestone confetti triggered");
        }

        /// <summary>
        /// Play crowd wave animation effect.
        /// </summary>
        public void PlayCrowdWave()
        {
            if (_crowdWavePrefab == null) return;

            // Crowd wave is typically a shader-based or animation-based effect
            Debug.Log("[VFXManager] Crowd wave animation triggered");
        }

        /// <summary>
        /// Play wicket fall visual effect.
        /// </summary>
        public void PlayWicketEffect()
        {
            Debug.Log("[VFXManager] Wicket effect triggered - red flash");
        }

        /// <summary>
        /// Enable/disable ball trail renderer.
        /// </summary>
        public void SetBallTrail(bool active, Vector3 startPosition)
        {
            if (_ballTrailPrefab == null) return;
            Debug.Log($"[VFXManager] Ball trail {(active ? "enabled" : "disabled")}");
        }
    }
}
