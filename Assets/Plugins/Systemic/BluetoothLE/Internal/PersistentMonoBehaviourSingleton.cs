using UnityEngine;

namespace Systemic.Unity.BluetoothLE.Internal
{
    /// <summary>
    /// Interface used by <see cref="PersistentMonoBehaviourSingleton{T}"/> to get the name of the game object to create.
    /// </summary>
    internal interface IPersistentMonoBehaviourSingleton
    {
        /// <summary>
        /// The name of the game object created to host the behaviour singleton.
        /// </summary>
        string GameObjectName { get; }
    }

    /// <summary>
    /// Unity helper class to maintain a MonoBehaviour singleton across scene loads and unloads.
    /// </summary>
    /// <typeparam name="T">The type of the singleton.</typeparam>
    internal abstract class PersistentMonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour, IPersistentMonoBehaviourSingleton
    {
        /// <summary>
        /// Gets the behaviour unique instance.
        /// </summary>
        public static T Instance { get; private set; }

        /// <summary>
        /// Indicates whether the behaviour will destroy itself on the next frame update.
        /// </summary>
        public static bool AutoDestroy { get; private set; }

        /// <summary>
        /// Instantiates a new game object with this behaviour on it.
        /// </summary>
        public static void Create()
        {
            AutoDestroy = false;
            if (!Instance)
            {
                var go = new GameObject();
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<T>();
                go.name = Instance.GameObjectName;
            }
        }

        /// <summary>
        /// Schedules the host game object to be destroyed on the next frame update
        /// </summary>
        public static void ScheduleDestroy()
        {
            AutoDestroy = true;
        }

        /// <summary>
        /// Throws an exception if the static instance is missing or not active
        /// or return true if it's not the case.
        /// </summary>
        /// <param name="noThrow">If true, return false instead of throwing an exception.</param>
        /// <returns>Whether the static instance is valid.</returns>
        public static bool CheckValid(bool noThrow = false)
        {
            if (Instance && Instance.gameObject.activeInHierarchy)
            {
                return true;
            }
            else if (noThrow)
            {
                return false;
            }
            else
            {
                throw new System.InvalidOperationException($"{nameof(T)} mono behaviour not instantiated");
            }
        }

        // Called when the instance becomes enabled and active
        void OnEnable()
        {
            // Safeguard
            if ((Instance != null) && (Instance != this))
            {
                Debug.LogError($"A second instance of {typeof(T)} got spawned, now destroying it");
                Destroy(this);
            }
        }

        // Called when the instance becomes disabled or inactive
        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (AutoDestroy)
            {
                Destroy(Instance);
                Instance = null;
            }
        }
    }
}
