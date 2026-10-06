#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    public class InjectionValidator : IStartable
    {
        // Static results readable by the EditorWindow
        public static List<InjectionFailure> Failures = new();
        public static bool HasRun;

        public struct InjectionFailure
        {
            public string ComponentName;
            public string FieldName;
            public string GameObjectName;
            public string SceneName;
            public string FieldType;
        }

        void IStartable.Start()
		{
			var go = new GameObject("[InjectionValidator]");
			Object.DontDestroyOnLoad(go);
			go.AddComponent<ValidatorRunner>();
		}   

        private class ValidatorRunner : MonoBehaviour
        {
            private void Start()
            {
                StartCoroutine(Validate());
            }

            private IEnumerator Validate()
			{
				yield return null;
				yield return null;

				Failures.Clear();
				var all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

				for (int i = 0; i < all.Length; i++)
				{
					var mb = all[i];
					if (mb == null) continue;

					var fields = mb.GetType().GetFields(
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

					for (int j = 0; j < fields.Length; j++)
					{
						var field = fields[j];
						if (field.GetCustomAttribute<InjectAttribute>() == null) continue;
						if (field.GetValue(mb) == null)
						{
							// ← ADD HERE
							Failures.Add(new InjectionFailure
							{
								ComponentName = mb.GetType().Name,
								FieldName = field.Name,
								GameObjectName = mb.gameObject.name,
								SceneName = mb.gameObject.scene.name,
								FieldType = field.FieldType.Name
							});
						}
					}
				}

				HasRun = true;
				Destroy(gameObject);
			}   
        }
    }
}
#endif   