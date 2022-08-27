namespace Game.Utility
{
    using UnityEngine;

    /// <summary>
    /// Base class that ensures only a single instance exists.
    /// </summary>
    /// <typeparam name="T">Class type.</typeparam>
    public abstract class Singleton<T> : MonoBehaviour
        where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var objs = FindObjectsOfType<T>();
                    switch (objs.Length)
                    {
                        case 0:
                            GameObject obj = new GameObject();
                            obj.name = string.Format("_{0}", typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                            break;
                        case 1:
                            _instance = objs[0];
                            break;
                        default:
                            _instance = objs[0];
                            Debug.LogError("There is more than one " + typeof(T).Name + " in the scene.");
                            break;
                    }
                }
                return _instance;
            }
        }
    }
}
