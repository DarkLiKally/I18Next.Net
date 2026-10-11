using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using UnityEngine;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace I18Next.Net.Unity.Tests
{
    internal static class UnityTestEnvironment
    {
        public const string English = "{ \"title\": \"Menu\", \"greeting\": \"Hello {{name}}\", \"total\": \"Total: {{value}}\", " +
                                      "\"item_one\": \"{{count}} item\", \"item_other\": \"{{count}} items\" }";

        public const string German = "{ \"title\": \"Menü\", \"greeting\": \"Hallo {{name}}\", \"item_one\": \"{{count}} Artikel\", " +
                                     "\"item_other\": \"{{count}} Artikel\" }";

        public static void Reset(SynchronizationContext synchronizationContext = null)
        {
            Resources.Assets.Clear();
            Resources.UnloadedAssets.Clear();
            Debug.Messages.Clear();
            PackageInfo.Packages.Clear();
            Application.systemLanguage = SystemLanguage.English;
            Application.streamingAssetsPath = null;

            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);

            try
            {
                I18NextUnity.Initialize();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        public static void AddTranslations()
        {
            AddTextAsset("locales/en/translation", English);
            AddTextAsset("locales/de/translation", German);
            AddTextAsset("locales/en/common", "{ \"title\": \"Common menu\" }");
        }

        public static void AddTextAsset(string path, string text)
        {
            Resources.Assets[path] = new TextAsset(text);
        }

        public static T AddComponent<T>(GameObject gameObject) where T : MonoBehaviour
        {
            var component = gameObject.AddComponent<T>();

            SendMessage(component, "OnEnable");

            return component;
        }

        public static void SendMessage(MonoBehaviour component, string methodName)
        {
            var method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
                throw new MissingMethodException(component.GetType().Name, methodName);

            method.Invoke(component, null);
        }

        public static void RunOnOtherThread(Action action)
        {
            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.Start();
            thread.Join();

            if (exception != null)
                throw new InvalidOperationException("The action failed on the other thread.", exception);
        }
    }

    internal sealed class QueuedSynchronizationContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object State)> _queue = new Queue<(SendOrPostCallback Callback, object State)>();

        public int PendingCount
        {
            get
            {
                lock (_queue)
                    return _queue.Count;
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (_queue)
                _queue.Enqueue((d, state));
        }

        public void RunPending()
        {
            while (true)
            {
                (SendOrPostCallback Callback, object State) item;

                lock (_queue)
                {
                    if (_queue.Count == 0)
                        return;

                    item = _queue.Dequeue();
                }

                item.Callback(item.State);
            }
        }
    }
}
