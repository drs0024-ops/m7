Is it a ScriptableObject?
  → RegisterInstance([SerializeField] reference)

Is it a MonoBehaviour?
  → Already in scene?    → RegisterComponent(existingComponent)
  → New GO (auto-named)? → RegisterComponentInHierarchy<T>()
  → New GO (named)?      → RegisterComponentOnNewGameObject<T>(Lifetime.X, "Name")
  → Prefab?              → RegisterComponentInNewPrefab(prefab, Lifetime.X)
  → Must survive scenes? → .DontDestroyOnLoad() on OnNewGameObject or InNewPrefab

Is it an entry point (IStartable, ITickable, ...)?
  → RegisterEntryPoint<T>()

Is it a MessagePipe handler?
  → RegisterAsyncRequestHandler<TReq, TRes, THandler>(options)  [Transient]

Is it a pure C# service?
  → Register<I, T>(Lifetime.X)
    - Global state?         → Singleton
    - Scene/feature state?  → Scoped
    - Stateless/short?      → Transient

Rule of thumb:
| Registration method                              | Injection style          |
|--------------------------------------------------|--------------------------|
| RegisterComponent(existingSceneObject)           | [Inject] field/method    |
| RegisterComponentInHierarchy<T>()                | [Inject] field/method    |
| RegisterComponentOnNewGameObject<T>(...)         | [Inject] field/method    |
| RegisterComponentInNewPrefab(prefab, ...)        | [Inject] field/method    |
| resolver.Inject(go) — manually instantiated      | [Inject] field/method    |
| Register<I, T>(...) — plain C# class             | Constructor ✅           |

**MonoBehaviour = always [Inject]. No exceptions.**
**Plain C# class = constructor injection.**

Why: Unity always calls the parameterless constructor via AddComponent/Instantiate.
VContainer never re-invokes it. It only calls [Inject] methods/fields on MonoBehaviours.
Constructor injection only works when VContainer itself creates the object (plain C# via Register<T>).   