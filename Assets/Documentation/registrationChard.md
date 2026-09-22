Is it a ScriptableObject?
  → RegisterInstance([SerializeField] reference)

Is it a MonoBehaviour?
  → In scene?           → RegisterComponentInHierarchy<T>()
  → New GO?             → RegisterComponentOnNewGameObject<T>(Lifetime.X)
  → Prefab?             → RegisterComponentInNewPrefab<T>(prefab, Lifetime.X)
  → Must survive scenes? → OnNewGameObject + .DontDestroyOnLoad()

Is it an entry point (IStartable, ITickable, ...)?
  → RegisterEntryPoint<T>()

Is it a MessagePipe handler?
  → RegisterAsyncRequestHandler<TReq, TRes, THandler>(options)  [Transient]

Is it a pure C# service?
  → Register<I, T>(Lifetime.X)
    - Global state?       → Singleton
    - Scene/feature state? → Scoped
    - Stateless/short?    → Transient