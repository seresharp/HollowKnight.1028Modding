using System;
using GlobalEnums;
using UnityEngine;

namespace Modding
{
    internal class ModVersionText : MonoBehaviour
    {
        private const string API_VERSION = "1028 Modding API: 0.0.2\n";

        private static string _versionText = API_VERSION;

        public static void PopulateVersionList()
        {
            _versionText = API_VERSION;

            foreach (Mod mod in Mod.ModList)
            {
                try
                {
                    _versionText += $"{mod.LogName}: {mod.Version}\n";
                }
                catch (Exception e)
                {
                    mod.Log("Error fetching version\n" + e);
                }
            }
        }

        private void OnGUI()
        {
            if (UIManager.instance?.uiState != UIState.MAIN_MENU_HOME
                && UIManager.instance?.uiState != UIState.PAUSED)
            {
                return;
            }

            Color backgroundColor = GUI.backgroundColor;
            Color contentColor = GUI.contentColor;
            Color color = GUI.color;
            Matrix4x4 matrix = GUI.matrix;
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1f));

            GUI.Label(new Rect(0f, 0f, 1920f, 1080f), _versionText, GUI.skin.label);

            GUI.backgroundColor = backgroundColor;
            GUI.contentColor = contentColor;
            GUI.color = color;
            GUI.matrix = matrix;
        }
    }
}
