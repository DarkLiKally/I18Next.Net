using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
    }

    public sealed class GameObject : Object
    {
        private readonly List<Component> _components = new List<Component>();

        public bool activeSelf { get; private set; } = true;

        public bool activeInHierarchy => activeSelf;

        public void SetActive(bool value)
        {
            activeSelf = value;
        }

        public T AddComponent<T>() where T : Component
        {
            var component = Activator.CreateInstance<T>();
            component.gameObject = this;
            _components.Add(component);

            return component;
        }

        public T GetComponent<T>()
        {
            foreach (var component in _components)
            {
                if (component is T match)
                    return match;
            }

            return default;
        }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; internal set; }

        public T GetComponent<T>()
        {
            return gameObject.GetComponent<T>();
        }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; } = true;

        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeInHierarchy;
    }

    public class MonoBehaviour : Behaviour
    {
    }

    public class TextAsset : Object
    {
        public TextAsset(string text)
        {
            this.text = text;
        }

        public string text { get; }
    }

    public static class Resources
    {
        internal static Dictionary<string, Object> Assets { get; } = new Dictionary<string, Object>();

        internal static List<Object> UnloadedAssets { get; } = new List<Object>();

        public static T Load<T>(string path) where T : Object
        {
            return Assets.TryGetValue(path, out var asset) ? asset as T : null;
        }

        public static void UnloadAsset(Object assetToUnload)
        {
            UnloadedAssets.Add(assetToUnload);
        }
    }

    public static class Application
    {
        public static string streamingAssetsPath { get; internal set; }

        public static SystemLanguage systemLanguage { get; internal set; } = SystemLanguage.English;
    }

    public enum SystemLanguage
    {
        Afrikaans = 0,
        Arabic = 1,
        Basque = 2,
        Belarusian = 3,
        Bulgarian = 4,
        Catalan = 5,
        Chinese = 6,
        Czech = 7,
        Danish = 8,
        Dutch = 9,
        English = 10,
        Estonian = 11,
        Faroese = 12,
        Finnish = 13,
        French = 14,
        German = 15,
        Greek = 16,
        Hebrew = 17,
        Hungarian = 18,
        Icelandic = 19,
        Indonesian = 20,
        Italian = 21,
        Japanese = 22,
        Korean = 23,
        Latvian = 24,
        Lithuanian = 25,
        Norwegian = 26,
        Polish = 27,
        Portuguese = 28,
        Romanian = 29,
        Russian = 30,
        SerboCroatian = 31,
        Slovak = 32,
        Slovenian = 33,
        Spanish = 34,
        Swedish = 35,
        Thai = 36,
        Turkish = 37,
        Ukrainian = 38,
        Vietnamese = 39,
        ChineseSimplified = 40,
        ChineseTraditional = 41,
        Hindi = 42,
        Unknown = 43
    }

    public static class Debug
    {
        internal static List<(string Level, object Message)> Messages { get; } = new List<(string Level, object Message)>();

        public static void Log(object message)
        {
            Messages.Add(("Log", message));
        }

        public static void LogWarning(object message)
        {
            Messages.Add(("Warning", message));
        }

        public static void LogError(object message)
        {
            Messages.Add(("Error", message));
        }

        public static void LogException(Exception exception)
        {
            Messages.Add(("Exception", exception));
        }
    }

    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad,
        AfterAssembliesLoaded,
        BeforeSplashScreen,
        SubsystemRegistration
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute()
        {
        }

        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType)
        {
            this.loadType = loadType;
        }

        public RuntimeInitializeLoadType loadType { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SerializeField : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AddComponentMenu : Attribute
    {
        public AddComponentMenu(string menuName)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DisallowMultipleComponent : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : Attribute
    {
        public RequireComponent(Type requiredComponent)
        {
        }
    }
}

namespace UnityEngine.UI
{
    public class Text : MonoBehaviour
    {
        public virtual string text { get; set; }
    }
}

namespace TMPro
{
    using UnityEngine;

    public abstract class TMP_Text : MonoBehaviour
    {
        public virtual string text { get; set; }
    }

    public class TextMeshProUGUI : TMP_Text
    {
    }
}
