using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace CharacterSelectPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
        public List<Character> Characters { get; set; } = new List<Character>();
        public Vector3 NewCharacterColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        // 🔹 Existing Settings
        public bool IsConfigWindowMovable { get; set; } = true;
        public bool SomePropertyToBeSavedAndWithADefault { get; set; } = false;

        // ✅ Added Profile Settings (Only Adding, No Removal)
        public float ProfileImageScale { get; set; } = 1.0f; // Image scaling
        public int ProfileColumns { get; set; } = 3;        // Number of profiles per row
        public float ProfileSpacing { get; set; } = 10.0f;  // Default spacing between profiles ✅

        private IDalamudPluginInterface pluginInterface; // ✅ Fixed naming rule issue
        public int CurrentSortIndex { get; set; } = 0; // Default to Manual (SortType.Manual = 0)


        public Configuration(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public static Configuration Load(IDalamudPluginInterface pluginInterface)
        {
            var config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration(pluginInterface);
            config.pluginInterface = pluginInterface;

            // Ensure valid sorting index
            if (config.CurrentSortIndex < 0 || config.CurrentSortIndex > 4)
                config.CurrentSortIndex = 0;

            return config;
        }



        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
