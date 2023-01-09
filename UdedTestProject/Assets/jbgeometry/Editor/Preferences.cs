using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace jbgeo
{
    internal class UdedSettings : ScriptableObject
    {
        public const string k_MyCustomSettingsPath = "Assets/UdedSettings.asset";

        public Material floorMaterial;

        public Material ceilingMaterial;

        public Material wallMaterial;
        internal static UdedSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UdedSettings>(k_MyCustomSettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<UdedSettings>();
                AssetDatabase.CreateAsset(settings, k_MyCustomSettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
    static class Preferences
    {
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            // First parameter is the path in the Settings window.
            // Second parameter is the scope of this setting: it only appears in the Settings window for the Project scope.
            var provider = new SettingsProvider("Preferences/Uded", SettingsScope.Project)
            {
                label = "Custom UI Elements",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = UdedSettings.GetSerializedSettings();
                    var title = new Label()
                    {
                        text = "Uded Settings"
                    };
                    title.AddToClassList("title");
                    rootElement.Add(title);

                    var properties = new VisualElement()
                    {
                        style =
                        {
                            flexDirection = FlexDirection.Column
                        }
                    };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);

                    properties.Add(new PropertyField(settings.FindProperty("floorMaterial")));
                    properties.Add(new PropertyField(settings.FindProperty("ceilingMaterial")));
                    properties.Add(new PropertyField(settings.FindProperty("wallMaterial")));

                    rootElement.Bind(settings);
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] {"Number", "Some String"})
            };

            return provider;
        }
    }
}