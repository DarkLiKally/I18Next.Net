using System.Collections.Generic;

using UnityEngine;

namespace I18Next.Net.Unity.Samples
{
    public class BasicSample : MonoBehaviour
    {
        private int _count = 1;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 400, 300));

            GUILayout.Label(I18NextUnity.T("title"));
            GUILayout.Label(I18NextUnity.T("greeting", new Dictionary<string, object> { ["name"] = "Unity" }));
            GUILayout.Label(I18NextUnity.T("apples", new Dictionary<string, object> { ["count"] = _count }));

            if (GUILayout.Button(I18NextUnity.T("addApple")))
                _count++;

            GUILayout.Label(I18NextUnity.T("language"));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("English"))
                I18NextUnity.Language = "en";

            if (GUILayout.Button("Deutsch"))
                I18NextUnity.Language = "de";

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
