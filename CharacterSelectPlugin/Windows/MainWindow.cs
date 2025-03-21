using System;
using System.Numerics;
using Dalamud.Interface.Utility;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using CharacterSelectPlugin;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using System.Net.NetworkInformation;

namespace CharacterSelectPlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private Plugin plugin;
        private int selectedCharacterIndex = -1;
        private string editedCharacterName = "";
        private string editedCharacterMacros = "";
        private string? editedCharacterImagePath = null;
        private List<CharacterDesign> editedCharacterDesigns = new();
        private bool isEditCharacterWindowOpen = false;
        private int activeDesignCharacterIndex = -1;
        private bool isDesignPanelOpen = false;
        private string? pendingImagePath = null; // ✅ Temporary storage for the selected image path
        private Vector3 editedCharacterColor = new Vector3(1.0f, 1.0f, 1.0f); // ✅ Default to white
        private string editedCharacterPenumbra = "";
        private string editedCharacterGlamourer = "";
        private string editedCharacterCustomize = "";
        private bool isAdvancedModeCharacter = false; // Separate Advanced Mode for Characters
        private bool isAdvancedModeDesign = false;    // Separate Advanced Mode for Designs
        private string advancedCharacterMacroText = ""; // Macro text for Character Advanced Mode
        private string advancedDesignMacroText = "";    // Macro text for Design Advanced Mode
        private bool isEditDesignWindowOpen = false;
        private string editedDesignName = "";
        private string editedDesignMacro = "";
        private string editedGlamourerDesign = "";
        private HashSet<string> knownHonorifics = new HashSet<string>();
        private string originalDesignName = ""; // Stores the original name before editing
        private bool isAdvancedModeWindowOpen = false; // Tracks if Advanced Mode window is open
                                                       // 🔹 Honorific Fields
        private string tempHonorificTitle = "";
        private string tempHonorificPrefix = "Prefix"; // Default to Prefix
        private string tempHonorificSuffix = "Suffix"; // Default to Suffix
        private Vector3 tempHonorificColor = new Vector3(1.0f, 1.0f, 1.0f); // Default to White
        private Vector3 tempHonorificGlow = new Vector3(1.0f, 1.0f, 1.0f); // Default to White

        // For Editing Characters
        private string editedCharacterHonorificTitle = "";
        private string editedCharacterHonorificPrefix = "Prefix";
        private string editedCharacterHonorificSuffix = "Suffix";
        private Vector3 editedCharacterHonorificColor = new Vector3(1.0f, 1.0f, 1.0f);
        private Vector3 editedCharacterHonorificGlow = new Vector3(1.0f, 1.0f, 1.0f);

        //MOODLES
        public string MoodlePreset { get; set; } = "";
        // Temporary storage for Moodle preset input in Add/Edit Character window
        private string tempMoodlePreset = "";

        // Stores the selected Moodle preset for an edited character
        private string editedCharacterMoodlePreset = "";




        // 🔹 Add Sorting Function
        private enum SortType { Favorites, Alphabetical, Recent, Oldest }
        private SortType currentSort;

        private enum DesignSortType { Favorites, Alphabetical, Recent, Oldest }
        private DesignSortType currentDesignSort = DesignSortType.Alphabetical;


        private void SortCharacters()
        {
            if (currentSort == SortType.Favorites)
            {
                plugin.Characters.Sort((a, b) =>
                {
                    int favCompare = b.IsFavorite.CompareTo(a.IsFavorite); // ⭐ Favorites first
                    if (favCompare != 0) return favCompare;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); // 🔠 Alphabetical within favorites
                });
            }
            else if (currentSort == SortType.Alphabetical)
            {
                plugin.Characters.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)); // 🔠 Alphabetical
            }
            else if (currentSort == SortType.Recent)
            {
                plugin.Characters.Sort((a, b) => b.DateAdded.CompareTo(a.DateAdded)); // 🔄 Most Recent First
            }
            else if (currentSort == SortType.Oldest)
            {
                plugin.Characters.Sort((a, b) => a.DateAdded.CompareTo(b.DateAdded)); // ⏳ Oldest First
            }
        }

        private void SortDesigns(Character character)
        {
            if (currentDesignSort == DesignSortType.Favorites)
            {
                character.Designs.Sort((a, b) =>
                {
                    int favCompare = b.IsFavorite.CompareTo(a.IsFavorite); // ⭐ Favorites first
                    if (favCompare != 0) return favCompare;
                    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                });
            }
            else if (currentDesignSort == DesignSortType.Alphabetical)
            {
                character.Designs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            }
            else if (currentDesignSort == DesignSortType.Recent)
            {
                character.Designs.Sort((a, b) => b.DateAdded.CompareTo(a.DateAdded));
            }
            else if (currentDesignSort == DesignSortType.Oldest)
            {
                character.Designs.Sort((a, b) => a.DateAdded.CompareTo(b.DateAdded));
            }
        }

        public MainWindow(Plugin plugin)
    : base("Character Select+", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(850, 700),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            this.plugin = plugin;

            // ✅ Load saved sorting preference
            currentSort = (SortType)plugin.Configuration.CurrentSortIndex;
            SortCharacters(); // ✅ Apply sorting on startup
                              // 🔹 Gather all existing honorifics at startup



        }


        public void Dispose() { }

        public override void Draw()
        {
            ImGui.Text("Choose your character");
            ImGui.Separator();

            if (ImGui.Button("Add Character"))
            {
                var tempSavedDesigns = new List<CharacterDesign>(plugin.NewCharacterDesigns); // ✅ Store existing designs
                ResetCharacterFields(); // ✅ Resets fields before opening window
                plugin.NewCharacterDesigns = tempSavedDesigns; // ✅ Restore designs after reset

                plugin.OpenAddCharacterWindow();
                isEditCharacterWindowOpen = false;
                isDesignPanelOpen = false;
                isAdvancedModeCharacter = false; // ✅ Force Advanced Mode to be off
            }

            if (plugin.IsAddCharacterWindowOpen || isEditCharacterWindowOpen)
            {
                DrawCharacterForm();
            }

            ImGui.BeginChild("CharacterGrid", new Vector2(isDesignPanelOpen ? -250 : 0, -30), true);
            DrawCharacterGrid();
            ImGui.EndChild(); // ✅ Close Character Grid Properly

            if (isDesignPanelOpen)
            {
                ImGui.SameLine();
                float characterGridHeight = ImGui.GetItemRectSize().Y; // Get height of the Character Grid
                ImGui.SetNextWindowSizeConstraints(new Vector2(250, characterGridHeight), new Vector2(250, characterGridHeight));
                ImGui.BeginChild("DesignPanel", new Vector2(250, characterGridHeight), true);
                DrawDesignPanel();
                ImGui.EndChild();
            }

            // 🔹 Ensure proper bottom-left alignment
            ImGui.SetCursorPos(new Vector2(10, ImGui.GetWindowHeight() - 30));

            // 🔹 Settings Button (⚙)
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button("\uf013")) // ⚙ Gear icon (Settings)
            {
                plugin.IsSettingsOpen = !plugin.IsSettingsOpen;
            }
            ImGui.PopFont();

            ImGui.SameLine(); // ✅ Forces the next item to be in the same line

            // 🔹 Quick Switch Button (🌀)
            if (ImGui.Button("Quick Switch"))
            {
                plugin.QuickSwitchWindow.IsOpen = !plugin.QuickSwitchWindow.IsOpen; // ✅ Toggle Quick Switch Window
            }

            if (plugin.IsSettingsOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(300, 180), ImGuiCond.FirstUseEver); // ✅ Adjusted for new setting

                bool isSettingsOpen = plugin.IsSettingsOpen;
                if (ImGui.Begin("Settings", ref isSettingsOpen, ImGuiWindowFlags.NoCollapse))
                {
                    if (!isSettingsOpen)
                        plugin.IsSettingsOpen = false;

                    ImGui.Text("Settings Panel");
                    ImGui.Separator();

                    // 🔹 Profile Image Scale
                    float tempScale = plugin.ProfileImageScale;
                    if (ImGui.SliderFloat("Profile Image Scale", ref tempScale, 0.5f, 2.0f, "%.1f"))
                    {
                        plugin.ProfileImageScale = tempScale;
                        plugin.SaveConfiguration();
                    }

                    // 🔹 Profile Columns
                    int tempColumns = plugin.ProfileColumns;
                    if (ImGui.InputInt("Profiles Per Row", ref tempColumns, 1, 1))
                    {
                        tempColumns = Math.Clamp(tempColumns, 1, 6);
                        plugin.ProfileColumns = tempColumns;
                        plugin.SaveConfiguration();
                    }

                    // 🔹 Profile Spacing - Match the layout of Profile Image Scale
                    float tempSpacing = plugin.ProfileSpacing;

                    // Slider first
                    ImGui.SetNextItemWidth(150);
                    if (ImGui.SliderFloat("##ProfileSpacing", ref tempSpacing, 0.0f, 50.0f, "%.1f"))
                    {
                        plugin.ProfileSpacing = tempSpacing;
                        plugin.SaveConfiguration();
                    }

                    // Align label to the right of the slider
                    ImGui.SameLine();
                    ImGui.Text("Profile Spacing");
                    // 🔹 Position "Sort By" Dropdown in the Bottom-Right
                    ImGui.SetCursorPos(new Vector2(ImGui.GetWindowWidth() - 150, ImGui.GetWindowHeight() - 35)); // ✅ Adjust position

                    ImGui.Text("Sort By:");
                    ImGui.SameLine();

                    // Create the dropdown menu
                    if (ImGui.BeginCombo("##SortDropdown", currentSort.ToString()))
                    {
                        if (ImGui.Selectable("Favorites", currentSort == SortType.Favorites))
                        {
                            currentSort = SortType.Favorites;
                            plugin.Configuration.CurrentSortIndex = (int)currentSort;
                            plugin.Configuration.Save();
                            SortCharacters();
                        }
                        if (ImGui.Selectable("Alphabetical", currentSort == SortType.Alphabetical))
                        {
                            currentSort = SortType.Alphabetical;
                            plugin.Configuration.CurrentSortIndex = (int)currentSort;
                            plugin.Configuration.Save();
                            SortCharacters();
                        }
                        if (ImGui.Selectable("Most Recent", currentSort == SortType.Recent))
                        {
                            currentSort = SortType.Recent;
                            plugin.Configuration.CurrentSortIndex = (int)currentSort;
                            plugin.Configuration.Save();
                            SortCharacters();
                        }
                        if (ImGui.Selectable("Oldest", currentSort == SortType.Oldest))
                        {
                            currentSort = SortType.Oldest;
                            plugin.Configuration.CurrentSortIndex = (int)currentSort;
                            plugin.Configuration.Save();
                            SortCharacters();
                        }
                        ImGui.EndCombo(); // ✅ Close dropdown properly
                    }
                    if (isAdvancedModeWindowOpen)
                    {
                        ImGui.SetNextWindowSize(new Vector2(500, 200), ImGuiCond.FirstUseEver);
                        if (ImGui.Begin("Advanced Macro Editor", ref isAdvancedModeWindowOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
                        {
                            ImGui.Text("Edit Design Macro Manually:");
                            ImGui.InputTextMultiline("##AdvancedDesignMacro", ref advancedDesignMacroText, 2000, new Vector2(-1, -1), ImGuiInputTextFlags.AllowTabInput);

                            // ✅ Auto-save on typing
                            if (isAdvancedModeDesign)
                            {
                                editedDesignMacro = advancedDesignMacroText;
                            }
                        }
                        ImGui.End();
                    }





                    ImGui.End();
                }
            }
            // 🔹 Position the Support Button near the bottom-right corner
            float buttonWidth = 110;
            float buttonHeight = 25;
            float padding = 10;

            // Set button position near the bottom-right
            ImGui.SetCursorPos(new Vector2(
                ImGui.GetWindowWidth() - buttonWidth - padding,  // Align to right
                ImGui.GetWindowHeight() - buttonHeight - padding // Align to bottom
            ));

            // 🔹 Create the Support Button
            if (ImGui.Button("💙 Support Dev", new Vector2(buttonWidth, buttonHeight)))
            {
                Dalamud.Utility.Util.OpenLink("https://ko-fi.com/icarusxiv");
            }

            // Tooltip on hover
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Enjoy Character Select+? Consider supporting development!");

        }



        // Resets input fields for a new character
        private void ResetCharacterFields()
        {
            plugin.NewCharacterName = "";
            plugin.NewCharacterColor = new Vector3(1.0f, 1.0f, 1.0f); // Reset to white
            plugin.NewPenumbraCollection = "";
            plugin.NewGlamourerDesign = "";
            plugin.NewCustomizeProfile = "";
            plugin.NewCharacterImagePath = null;
            plugin.NewCharacterDesigns.Clear();
            plugin.NewCharacterHonorificTitle = "";
            plugin.NewCharacterHonorificPrefix = "Prefix";
            plugin.NewCharacterHonorificSuffix = "Suffix";
            plugin.NewCharacterHonorificColor = new Vector3(1.0f, 1.0f, 1.0f); // Default White
            plugin.NewCharacterHonorificGlow = new Vector3(1.0f, 1.0f, 1.0f);  // Default White
            plugin.NewCharacterMoodlePreset = ""; // ✅ RESET Moodle Preset
            tempHonorificTitle = "";
            tempHonorificPrefix = "Prefix";
            tempHonorificSuffix = "Suffix";
            tempHonorificColor = new Vector3(1.0f, 1.0f, 1.0f);
            tempHonorificGlow = new Vector3(1.0f, 1.0f, 1.0f);
            tempMoodlePreset = ""; // ✅ RESET Temporary Moodle Preset

            // ✅ Fix: Preserve Advanced Mode Macro when Resetting Fields
            if (!isAdvancedModeCharacter)
            {
                plugin.NewCharacterMacros = GenerateMacro(); // ✅ Only reset macro in Normal Mode
            }
            // ✅ Do NOT touch plugin.NewCharacterMacros if Advanced Mode is active


        }



        private void DrawCharacterForm()
        {
            string tempName = isEditCharacterWindowOpen ? editedCharacterName : plugin.NewCharacterName;
            string tempMacros = isEditCharacterWindowOpen ? editedCharacterMacros : plugin.NewCharacterMacros;
            string? imagePath = isEditCharacterWindowOpen ? editedCharacterImagePath : plugin.NewCharacterImagePath;
            string tempPenumbra = isEditCharacterWindowOpen ? editedCharacterPenumbra : plugin.NewPenumbraCollection;
            string tempGlamourer = isEditCharacterWindowOpen ? editedCharacterGlamourer : plugin.NewGlamourerDesign;
            string tempCustomize = isEditCharacterWindowOpen ? editedCharacterCustomize : plugin.NewCustomizeProfile;
            Vector3 tempColor = isEditCharacterWindowOpen ? editedCharacterColor : plugin.NewCharacterColor;



            float labelWidth = 130; // Keep labels aligned
            float inputWidth = 250; // Longer input bars
            float inputOffset = 10; // Moves input fields slightly right

            // Character Name
            ImGui.SetCursorPosX(10);
            ImGui.Text("Character Name*");
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##CharacterName", ref tempName, 50);
            if (isEditCharacterWindowOpen) editedCharacterName = tempName;
            else plugin.NewCharacterName = tempName;

            // ℹ Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Enter your OC's name or nickname for profile here."); }

            ImGui.Separator();

            // Nameplate Color
            ImGui.SetCursorPosX(10);
            ImGui.Text("Nameplate Color");
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.ColorEdit3("##NameplateColor", ref tempColor);
            if (isEditCharacterWindowOpen) editedCharacterColor = tempColor;
            else plugin.NewCharacterColor = tempColor;

            // ℹ Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Affects your character's nameplate under their profile picture in Character Select+."); }

            ImGui.Separator();

            // Penumbra Collection
            ImGui.SetCursorPosX(10);
            ImGui.Text("Penumbra Collection*");
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##PenumbraCollection", ref tempPenumbra, 50);
            // ✅ Fix: Preserve Advanced Mode Edits While Allowing Normal Mode Updates
            if (isEditCharacterWindowOpen)
            {
                if (editedCharacterPenumbra != tempPenumbra)
                {
                    editedCharacterPenumbra = tempPenumbra;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        // ✅ Only update if Advanced Mode was already in use
                        advancedCharacterMacroText = GenerateMacro();
                    }
                }
            }
            else
            {
                if (plugin.NewPenumbraCollection != tempPenumbra)
                {
                    plugin.NewPenumbraCollection = tempPenumbra;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        // ✅ Preserve Advanced Mode macro when adding new characters
                        plugin.NewCharacterMacros = advancedCharacterMacroText;
                    }
                }
            }

            // Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("Enter the name of the Penumbra collection to apply to this character.");
                ImGui.TextUnformatted("Must be entered EXACTLY as it is named in Penumbra!");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.Separator();


            // Glamourer Design
            ImGui.SetCursorPosX(10);
            ImGui.Text("Glamourer Design*");
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##GlamourerDesign", ref tempGlamourer, 50);
            if (isEditCharacterWindowOpen)
            {
                if (editedCharacterGlamourer != tempGlamourer)
                {
                    editedCharacterGlamourer = tempGlamourer;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        advancedCharacterMacroText = GenerateMacro();
                    }
                }
            }
            else
            {
                if (plugin.NewGlamourerDesign != tempGlamourer)
                {
                    plugin.NewGlamourerDesign = tempGlamourer;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        plugin.NewCharacterMacros = advancedCharacterMacroText;
                    }
                }
            }

            // Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("Enter the name of the Glamourer design to apply to this character.");
                ImGui.TextUnformatted("Must be entered EXACTLY as it is named in Glamourer!");
                ImGui.TextUnformatted("Note: You can add additional designs later.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.Separator();


            // Customize+ Profile
            ImGui.SetCursorPosX(10);
            ImGui.Text("Customize+ Profile");
            ImGui.SameLine(labelWidth);
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##CustomizeProfile", ref tempCustomize, 50);
            if (isEditCharacterWindowOpen)
            {
                if (editedCharacterCustomize != tempCustomize)
                {
                    editedCharacterCustomize = tempCustomize;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        advancedCharacterMacroText = GenerateMacro();
                    }
                }
            }
            else
            {
                if (plugin.NewCustomizeProfile != tempCustomize)
                {
                    plugin.NewCustomizeProfile = tempCustomize;

                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        plugin.NewCharacterMacros = advancedCharacterMacroText;
                    }
                }
            }


            // Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("Enter the name of the Customize+ profile to apply to this character.");
                ImGui.TextUnformatted("Must be entered EXACTLY as it is named in Customize+!");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }



            ImGui.Separator();

            // 🔹 Honorific Title Section (Proper Alignment)
            ImGui.SetCursorPosX(10);
            ImGui.Text("Honorific Title");
            ImGui.SameLine();

            // Move cursor for input alignment
            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            // 🔹 Honorific Title Input (Fix)
            if (ImGui.InputText("##HonorificTitle", ref tempHonorificTitle, 50))
            {
                if (isEditCharacterWindowOpen)
                {
                    if (editedCharacterHonorificTitle != tempHonorificTitle)
                    {
                        editedCharacterHonorificTitle = tempHonorificTitle;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            advancedCharacterMacroText = GenerateMacro();
                        }
                    }
                }
                else
                {
                    if (plugin.NewCharacterHonorificTitle != tempHonorificTitle)
                    {
                        plugin.NewCharacterHonorificTitle = tempHonorificTitle;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            plugin.NewCharacterMacros = advancedCharacterMacroText;
                        }
                    }
                }
            }

            ImGui.SameLine();

            // 🔹 Honorific Placement Dropdown (Prefix/Suffix)
            ImGui.SetNextItemWidth(80);
            if (ImGui.BeginCombo("##HonorificPlacement", tempHonorificPrefix)) // ✅ Use correct prefix variable
            {
                string[] options = { "Prefix", "Suffix" };
                foreach (var option in options)
                {
                    bool isSelected = tempHonorificPrefix == option;
                    if (ImGui.Selectable(option, isSelected))
                    {
                        tempHonorificPrefix = option; // ✅ Set value properly
                        tempHonorificSuffix = option; // ✅ Ensure compatibility with macros

                        if (isEditCharacterWindowOpen)
                        {
                            if (editedCharacterHonorificPrefix != tempHonorificPrefix || editedCharacterHonorificSuffix != tempHonorificSuffix)
                            {
                                editedCharacterHonorificPrefix = tempHonorificPrefix;
                                editedCharacterHonorificSuffix = tempHonorificSuffix;

                                if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                                {
                                    advancedCharacterMacroText = GenerateMacro();
                                }
                            }
                        }
                        else
                        {
                            if (plugin.NewCharacterHonorificPrefix != tempHonorificPrefix || plugin.NewCharacterHonorificSuffix != tempHonorificSuffix)
                            {
                                plugin.NewCharacterHonorificPrefix = tempHonorificPrefix;
                                plugin.NewCharacterHonorificSuffix = tempHonorificSuffix;

                                if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                                {
                                    plugin.NewCharacterMacros = advancedCharacterMacroText;
                                }
                            }
                        }
                    }
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // 🔹 Honorific Color Picker (Fix)
            ImGui.SetNextItemWidth(40);
            if (ImGui.ColorEdit3("##HonorificColor", ref tempHonorificColor, ImGuiColorEditFlags.NoInputs))
            {
                if (isEditCharacterWindowOpen)
                {
                    if (editedCharacterHonorificColor != tempHonorificColor)
                    {
                        editedCharacterHonorificColor = tempHonorificColor;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            advancedCharacterMacroText = GenerateMacro();
                        }
                    }
                }
                else
                {
                    if (plugin.NewCharacterHonorificColor != tempHonorificColor)
                    {
                        plugin.NewCharacterHonorificColor = tempHonorificColor;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            plugin.NewCharacterMacros = advancedCharacterMacroText;
                        }
                    }
                }
            }

            ImGui.SameLine();

            // 🔹 Honorific Glow Picker (Fix)
            ImGui.SetNextItemWidth(40);
            if (ImGui.ColorEdit3("##HonorificGlow", ref tempHonorificGlow, ImGuiColorEditFlags.NoInputs))
            {
                if (isEditCharacterWindowOpen)
                {
                    if (editedCharacterHonorificGlow != tempHonorificGlow)
                    {
                        editedCharacterHonorificGlow = tempHonorificGlow;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            advancedCharacterMacroText = GenerateMacro();
                        }
                    }
                }
                else
                {
                    if (plugin.NewCharacterHonorificGlow != tempHonorificGlow)
                    {
                        plugin.NewCharacterHonorificGlow = tempHonorificGlow;

                        if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                        {
                            plugin.NewCharacterMacros = advancedCharacterMacroText;
                        }
                    }
                }
            }


            ImGui.SameLine();

            // ℹ Tooltip for Honorific Title (Correctly Positioned)
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("This will set a forced title when you switch to this character.");
                ImGui.TextUnformatted("The dropdown selects if the title appears above (prefix) or below (suffix) your name in-game.");
                ImGui.TextUnformatted("Use the Honorific plug-in’s 'Clear' button if you need to remove it.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.Separator();

            // 🔹 Moodle Preset Input
            ImGui.SetCursorPosX(10);
            ImGui.Text("Moodle Preset");
            ImGui.SameLine();

            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);
            ImGui.InputText("##MoodlePreset", ref tempMoodlePreset, 50);

            // ✅ Update stored preset value
            if (isEditCharacterWindowOpen)
            {
                if (editedCharacterMoodlePreset != tempMoodlePreset)
                {
                    editedCharacterMoodlePreset = tempMoodlePreset;
                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        advancedCharacterMacroText = GenerateMacro();
                    }
                }
            }
            else
            {
                if (plugin.NewCharacterMoodlePreset != tempMoodlePreset)
                {
                    plugin.NewCharacterMoodlePreset = tempMoodlePreset;
                    if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                    {
                        plugin.NewCharacterMacros = advancedCharacterMacroText;
                    }
                }
            }

            // ℹ Tooltip for Moodle Preset
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("Enter the Moodle preset name exactly as saved in the Moodle plugin.");
                ImGui.TextUnformatted("Example: 'HappyFawn' will apply the preset named 'HappyFawn'.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.Separator();

            // Idle Pose Dropdown (None + 0–6)
            ImGui.SetCursorPosX(10);
            ImGui.Text("Idle Pose");
            ImGui.SameLine();

            ImGui.SetCursorPosX(labelWidth + inputOffset);
            ImGui.SetNextItemWidth(inputWidth);

            // Poses start from index 0
            string[] poseOptions = { "None", "0", "1", "2", "3", "4", "5", "6" };
            int idlePoseIndex = isEditCharacterWindowOpen
                ? (plugin.Characters[selectedCharacterIndex].IdlePoseIndex == 0 ? 7 : plugin.Characters[selectedCharacterIndex].IdlePoseIndex)
                : (plugin.NewCharacterIdlePoseIndex == 0 ? 7 : plugin.NewCharacterIdlePoseIndex);

            if (ImGui.BeginCombo("##IdlePoseDropdown", idlePoseIndex == 7 ? "None" : idlePoseIndex.ToString()))
            {
                for (int i = 0; i < poseOptions.Length; i++)
                {
                    bool isSelected = i == (idlePoseIndex == 7 ? 0 : idlePoseIndex + 1); // Shifted by 1 for "None"
                    if (ImGui.Selectable(poseOptions[i], isSelected))
                    {
                        if (i == 0) // None selected
                            idlePoseIndex = 7;
                        else
                            idlePoseIndex = i - 1;

                        if (isEditCharacterWindowOpen)
                        {
                            if (plugin.Characters[selectedCharacterIndex].IdlePoseIndex != (byte)idlePoseIndex)
                            {
                                plugin.Characters[selectedCharacterIndex].IdlePoseIndex = (byte)idlePoseIndex;
                                if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                                {
                                    advancedCharacterMacroText = GenerateMacro();
                                }
                            }
                        }
                        else
                        {
                            if (plugin.NewCharacterIdlePoseIndex != (byte)idlePoseIndex)
                            {
                                plugin.NewCharacterIdlePoseIndex = (byte)idlePoseIndex;
                                if (isAdvancedModeCharacter && !string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                                {
                                    plugin.NewCharacterMacros = advancedCharacterMacroText;
                                }
                            }
                        }

                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }


            // ℹ Tooltip Icon
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.TextUnformatted("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted("Sets your character's idle pose (0–6).");
                ImGui.TextUnformatted("Choose 'None' if you don’t want Character Select+ to change your idle.");
                ImGui.EndTooltip();
            }

            ImGui.Separator();

            if (isEditCharacterWindowOpen)
                editedCharacterMacros = tempMacros;
            else
// ✅ Ensure Advanced Mode changes are actually applied to new characters
if (isAdvancedModeCharacter)
            {
                if (!string.IsNullOrWhiteSpace(advancedCharacterMacroText))
                {
                    plugin.NewCharacterMacros = advancedCharacterMacroText; // ✅ Save changes properly
                }
            }
            else
            {
                plugin.NewCharacterMacros = GenerateMacro(); // ✅ Generate normal macro if not in Advanced Mode
            }

            // ✅ Uses Advanced Mode if enabled
            if (isEditCharacterWindowOpen)
            {
                // Warning Icon
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.2f, 1.0f), "\uf071"); // ⚠️ Icon in bright orange
                ImGui.SameLine(0, 6);
                ImGui.PopFont();

                // Wrapped Warning Text
                ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + 600); // Adjust wrap width if needed
                ImGui.TextColored(
                    new Vector4(1.0f, 0.7f, 0.2f, 1.0f),
                    "WARNING: If you're using Advanced Mode, be aware that editing any of the above fields will result in your macros being reset. Be sure to copy the macros you need before making any changes so you can paste them back in!"
                );
                ImGui.PopTextWrapPos();
            }



            if (ImGui.Button("Choose Image"))
            {
                try
                {
                    Thread thread = new Thread(() =>
                    {
                        try
                        {
                            using (OpenFileDialog openFileDialog = new OpenFileDialog())
                            {
                                openFileDialog.Filter = "PNG files (*.png)|*.png";
                                openFileDialog.Title = "Select Character Image";

                                if (openFileDialog.ShowDialog() == DialogResult.OK)
                                {
                                    lock (this) // Prevent race conditions
                                    {
                                        pendingImagePath = openFileDialog.FileName;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.Error($"Error opening file picker: {ex.Message}");
                        }
                    });

                    thread.SetApartmentState(ApartmentState.STA); // Required for OpenFileDialog
                    thread.Start();
                }
                catch (Exception ex)
                {
                    Plugin.Log.Error($"Critical file picker error: {ex.Message}");
                }
            }



            // ✅ Apply the image path safely on the next frame
            if (pendingImagePath != null)
            {
                lock (this) // ✅ Prevent potential race conditions
                {
                    if (isEditCharacterWindowOpen)
                        editedCharacterImagePath = pendingImagePath;
                    else
                        plugin.NewCharacterImagePath = pendingImagePath;

                    pendingImagePath = null; // Reset after applying
                }
            }

            // ✅ Get Plugin Directory and Default Image Path
            string pluginDirectory = plugin.PluginDirectory;
            string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

            // ✅ Assign Default Image if None Selected
            // Ensure we get the correct plugin directory
            string pluginDir = plugin.PluginDirectory;
            string defaultImgPath = Path.Combine(pluginDirectory, "Assets", "Default.png");

            // Determine which image to display
            string finalImagePath = !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath)
            ? imagePath
             : defaultImagePath; // Always use Default.png if no other image is chosen


            if (!string.IsNullOrEmpty(finalImagePath) && File.Exists(finalImagePath))
            {
                var texture = Plugin.TextureProvider.GetFromFile(finalImagePath).GetWrapOrDefault();
                if (texture != null)
                {
                    float originalWidth = texture.Width;
                    float originalHeight = texture.Height;
                    float maxSize = 100f; // Maximum size for preview

                    float aspectRatio = originalWidth / originalHeight;
                    float displayWidth, displayHeight;

                    if (aspectRatio > 1) // Landscape (wider than tall)
                    {
                        displayWidth = maxSize;
                        displayHeight = maxSize / aspectRatio;
                    }
                    else // Portrait or Square (taller or equal)
                    {
                        displayHeight = maxSize;
                        displayWidth = maxSize * aspectRatio;
                    }

                    ImGui.Image(texture.ImGuiHandle, new Vector2(displayWidth, displayHeight));
                }
                else
                {
                    ImGui.Text($"Failed to load image: {Path.GetFileName(finalImagePath)}");
                }
            }
            else
            {
                ImGui.Text("No Image Available");
            }


            List<CharacterDesign> designsToDisplay = isEditCharacterWindowOpen ? editedCharacterDesigns : plugin.NewCharacterDesigns;

            for (int i = 0; i < designsToDisplay.Count; i++)
            {
                var design = designsToDisplay[i];
                string tempDesignName = design.Name;
                string tempDesignMacro = design.Macro;

                ImGui.InputText($"Design Name {i + 1}", ref tempDesignName, 100);
                ImGui.Text("Design Macros:");
                ImGui.BeginChild($"DesignMacroChild_{i}", new Vector2(300, 100), true);
                float minHeight = 110;
                float maxHeight = 300;
                float totalHeight = ImGui.GetContentRegionAvail().Y - 55;
                float inputHeight = Math.Clamp(totalHeight, minHeight, maxHeight);

                ImGui.BeginChild("AdvancedModeSection", new Vector2(0, inputHeight), true, ImGuiWindowFlags.NoScrollbar);
                ImGui.InputTextMultiline("##AdvancedDesignMacro", ref advancedDesignMacroText, 2000, new Vector2(-1, inputHeight - 10), ImGuiInputTextFlags.AllowTabInput);
                ImGui.EndChild();

                ImGui.EndChild();

                designsToDisplay[i] = new CharacterDesign(tempDesignName, tempDesignMacro);
            }

            // 🔹 Character Advanced Mode Toggle Button
            if (ImGui.Button(isAdvancedModeCharacter ? "Exit Advanced Mode" : "Advanced Mode"))
            {
                isAdvancedModeCharacter = !isAdvancedModeCharacter;
                if (isAdvancedModeCharacter)
                {
                    advancedCharacterMacroText = isEditCharacterWindowOpen
                        ? (!string.IsNullOrWhiteSpace(editedCharacterMacros) ? editedCharacterMacros : GenerateMacro())
                        : (!string.IsNullOrWhiteSpace(plugin.NewCharacterMacros) ? plugin.NewCharacterMacros : GenerateMacro());
                }
            }


            // ℹ Tooltip Icon (Info about Advanced Mode)
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 5); // Add slight spacing

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text("\uf05a");
            ImGui.PopFont();

            if (ImGui.IsItemHovered())
            {
                // ✅ Move tooltip to the **right side** of the button
                ImGui.SetNextWindowPos(ImGui.GetCursorScreenPos() + new Vector2(20, -5));

                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(300);
                ImGui.TextUnformatted("⚠️ Do not touch this unless you know what you're doing.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }



            // 🔹 Show Advanced Mode Editor When Enabled
            if (isAdvancedModeCharacter)
            {
                ImGui.Text("Edit Macro Manually:");
                ImGui.InputTextMultiline("##AdvancedCharacterMacro", ref advancedCharacterMacroText, 2000, new Vector2(500, 150), ImGuiInputTextFlags.AllowTabInput);
            }
            // Check if required fields are filled
            bool canSaveCharacter = !string.IsNullOrWhiteSpace(tempName) &&
                                    !string.IsNullOrWhiteSpace(tempPenumbra) &&
                                    !string.IsNullOrWhiteSpace(tempGlamourer);

            // Disable the button if any required field is empty
            if (!canSaveCharacter)
                ImGui.BeginDisabled();

            if (ImGui.Button(isEditCharacterWindowOpen ? "Save Changes" : "Save Character"))
            {
                if (isEditCharacterWindowOpen)
                {
                    SaveEditedCharacter();
                }
                else
                {
                    // ✅ Pass Advanced Macro when Saving a New Character
                    string finalMacro = isAdvancedModeCharacter ? advancedCharacterMacroText : plugin.NewCharacterMacros;
                    plugin.SaveNewCharacter(finalMacro);
                }

                isEditCharacterWindowOpen = false;
                plugin.CloseAddCharacterWindow();
            }



            if (!canSaveCharacter)
                ImGui.EndDisabled();

            ImGui.SameLine();

            if (ImGui.Button("Cancel"))
            {
                isEditCharacterWindowOpen = false;
                plugin.CloseAddCharacterWindow();
            }

        }

        private void DrawCharacterGrid()
        {
            // ✅ Get spacing & column settings
            float profileSpacing = plugin.ProfileSpacing;
            int columnCount = plugin.ProfileColumns;

            // ✅ Adjust column count if Design Panel is open
            if (isDesignPanelOpen)
            {
                columnCount = Math.Max(1, columnCount - 1);
            }

            // ✅ Calculate dynamic column width
            float columnWidth = (250 * plugin.ProfileImageScale) + profileSpacing;
            float availableWidth = ImGui.GetContentRegionAvail().X;

            // ✅ Ensure column count fits within available space
            columnCount = Math.Max(1, Math.Min(columnCount, (int)(availableWidth / columnWidth)));

            // ✅ Outer scrollable container (handles both horizontal & vertical scrolling)
            ImGui.BeginChild("CharacterGridContainer", new Vector2(0, 0), false,
                ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysVerticalScrollbar);

            // ✅ Begin column layout
            if (columnCount > 1)
            {
                ImGui.Columns(columnCount, "CharacterGrid", false);
            }

            for (int i = 0; i < plugin.Characters.Count; i++)
            {
                var character = plugin.Characters[i];

                // ✅ Ensure column width is properly set
                if (columnCount > 1)
                {
                    int colIndex = i % columnCount;
                    if (colIndex >= 0 && colIndex < ImGui.GetColumnsCount())
                    {
                        ImGui.SetColumnWidth(colIndex, columnWidth);
                    }
                }

                // ✅ Image Scaling
                float scale = plugin.ProfileImageScale;
                float maxSize = Math.Clamp(250 * scale, 64, 512); // ✅ Prevents excessive scaling
                float nameplateHeight = 30;

                float displayWidth, displayHeight;

                string pluginDirectory = plugin.PluginDirectory;
                string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

                string finalImagePath = !string.IsNullOrEmpty(character.ImagePath) && File.Exists(character.ImagePath)
                    ? character.ImagePath
                    : (File.Exists(defaultImagePath) ? defaultImagePath : "");

                if (!string.IsNullOrEmpty(finalImagePath) && File.Exists(finalImagePath))
                {
                    var texture = Plugin.TextureProvider.GetFromFile(finalImagePath).GetWrapOrDefault();

                    if (texture != null)
                    {
                        float originalWidth = texture.Width;
                        float originalHeight = texture.Height;
                        float aspectRatio = originalWidth / originalHeight;

                        if (aspectRatio > 1) // Landscape
                        {
                            displayWidth = maxSize;
                            displayHeight = maxSize / aspectRatio;
                        }
                        else // Portrait or Square
                        {
                            displayHeight = maxSize;
                            displayWidth = maxSize * aspectRatio;
                        }

                        float paddingX = (maxSize - displayWidth) / 2;
                        float paddingY = (maxSize - displayHeight) / 2;
                        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + paddingX);
                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + paddingY);

                        ImGui.Image(texture.ImGuiHandle, new Vector2(displayWidth, displayHeight));

                        // ✅ Click Image to Execute Macro
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                        {
                            if (activeDesignCharacterIndex != -1)
                            {
                                activeDesignCharacterIndex = -1;
                                isDesignPanelOpen = false;
                            }
                            plugin.ExecuteMacro(character.Macros);
                        }
                    }
                }

                // ✅ Nameplate Rendering (Keeps consistent alignment)
                DrawNameplate(character, maxSize, nameplateHeight);

                // 🔹 Buttons Section (Proper Spacing)
                float buttonWidth = maxSize / 3.1f;
                float btnWidth = maxSize / 3.2f;
                float btnHeight = 24;
                float btnSpacing = 4;

                float btnStartX = ImGui.GetCursorPosX() + (maxSize - (3 * btnWidth + 2 * btnSpacing)) / 2;
                ImGui.SetCursorPosX(btnStartX);

                // ✅ "Designs" Button
                if (ImGui.Button($"Designs##{i}", new Vector2(btnWidth, btnHeight)))
                {
                    if (activeDesignCharacterIndex == i && isDesignPanelOpen)
                    {
                        activeDesignCharacterIndex = -1;
                        isDesignPanelOpen = false;
                    }
                    else
                    {
                        activeDesignCharacterIndex = i;
                        isDesignPanelOpen = true;
                    }
                }

                ImGui.SameLine(0, btnSpacing);
                if (ImGui.Button($"Edit##{i}", new Vector2(btnWidth, btnHeight)))
                {
                    OpenEditCharacterWindow(i);
                    isDesignPanelOpen = false;
                }

                ImGui.SameLine(0, btnSpacing);
                bool isCtrlShiftPressed = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
                if (ImGui.Button($"Delete##{i}", new Vector2(btnWidth, btnHeight)))
                {
                    if (isCtrlShiftPressed)
                    {
                        plugin.Characters.RemoveAt(i);
                        plugin.Configuration.Save();
                    }
                }

                // ✅ Tooltip for Delete Button
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("Hold Ctrl + Shift and click to delete.");
                    ImGui.EndTooltip();
                }

                ImGui.NextColumn(); // ✅ Move to next column properly
            }

            if (columnCount > 1)
            {
                ImGui.Columns(1);
            }

            ImGui.EndChild(); // ✅ Close Outer Scrollable Container
        }


        private void DrawNameplate(Character character, float width, float height)
        {
            var cursorPos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            // 🔹 Nameplate Background
            drawList.AddRectFilled(
                new Vector2(cursorPos.X, cursorPos.Y),
                new Vector2(cursorPos.X + width, cursorPos.Y + height),
                ImGui.GetColorU32(new Vector4(0, 0, 0, 0.8f)) // ✅ Black background with slight transparency
            );

            // 🔹 Nameplate Color Strip
            drawList.AddRectFilled(
                new Vector2(cursorPos.X, cursorPos.Y + height - 4),
                new Vector2(cursorPos.X + width, cursorPos.Y + height),
                ImGui.GetColorU32(new Vector4(character.NameplateColor.X, character.NameplateColor.Y, character.NameplateColor.Z, 1.0f))
            );

            // 🔹 Character Name
            var textSize = ImGui.CalcTextSize(character.Name);
            var textPosX = cursorPos.X + (width - textSize.X) / 2;
            var textPosY = cursorPos.Y + (height - textSize.Y) / 2;

            drawList.AddText(new Vector2(textPosX, textPosY), ImGui.GetColorU32(ImGuiCol.Text), character.Name);

            // ⭐ Add Favorite Star in the Top-Left Corner
            string starSymbol = character.IsFavorite ? "★" : "☆";
            var starPos = new Vector2(cursorPos.X + 5, cursorPos.Y + 5); // Position near top-left
            drawList.AddText(starPos, ImGui.GetColorU32(ImGuiCol.Text), starSymbol);

            // 🔹 Clickable Area for Toggling Favorite
            if (ImGui.IsMouseHoveringRect(starPos, new Vector2(starPos.X + 20, starPos.Y + 20)) && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                character.IsFavorite = !character.IsFavorite; // Toggle favorite
                plugin.SaveConfiguration();
                SortCharacters(); // ✅ Resort after toggling
            }

            ImGui.Dummy(new Vector2(width, height)); // ✅ Maintain proper positioning
        }


        // Place GenerateMacro() here:
        private string GenerateMacro()
        {
            string penumbra = isEditCharacterWindowOpen ? editedCharacterPenumbra : plugin.NewPenumbraCollection;
            string glamourer = isEditCharacterWindowOpen ? editedCharacterGlamourer : plugin.NewGlamourerDesign;
            string customize = isEditCharacterWindowOpen ? editedCharacterCustomize : plugin.NewCustomizeProfile;
            string honorificTitle = isEditCharacterWindowOpen ? editedCharacterHonorificTitle : plugin.NewCharacterHonorificTitle;
            string honorificPrefix = isEditCharacterWindowOpen ? editedCharacterHonorificPrefix : plugin.NewCharacterHonorificPrefix;
            string honorificSuffix = isEditCharacterWindowOpen ? editedCharacterHonorificSuffix : plugin.NewCharacterHonorificSuffix;
            Vector3 honorificColor = isEditCharacterWindowOpen ? editedCharacterHonorificColor : plugin.NewCharacterHonorificColor;
            Vector3 honorificGlow = isEditCharacterWindowOpen ? editedCharacterHonorificGlow : plugin.NewCharacterHonorificGlow;

            if (string.IsNullOrWhiteSpace(penumbra) || string.IsNullOrWhiteSpace(glamourer))
                return "/penumbra redraw self";

            string macro = $"/penumbra collection individual | {penumbra} | self\n" +
                           $"/glamour apply {glamourer} | self\n" +
                           "/customize profile disable <me>\n";

            if (!string.IsNullOrWhiteSpace(customize))
                macro += $"/customize profile enable <me>, {customize}\n";

            // ✅ Ensure honorific is always cleared before setting a new one
            macro += "/honorific force clear\n";

            // ✅ Apply Honorifics (Only if a title is provided)
            if (!string.IsNullOrWhiteSpace(honorificTitle))
            {
                string colorHex = $"#{(int)(honorificColor.X * 255):X2}{(int)(honorificColor.Y * 255):X2}{(int)(honorificColor.Z * 255):X2}";
                string glowHex = $"#{(int)(honorificGlow.X * 255):X2}{(int)(honorificGlow.Y * 255):X2}{(int)(honorificGlow.Z * 255):X2}";

                macro += $"/honorific force set {honorificTitle} | {honorificPrefix} | {colorHex} | {glowHex}\n";
            }

            // ✅ Always remove ALL existing Moodles before applying a new one
            macro += "/moodle remove self preset all\n";

            // 🔹 Apply Moodle Preset if set
            string moodlePreset = isEditCharacterWindowOpen ? editedCharacterMoodlePreset : tempMoodlePreset;
            if (!string.IsNullOrWhiteSpace(moodlePreset))
            {
                macro += $"/moodle apply self preset \"{moodlePreset}\"\n";
            }
            // Set idle pose only if not set to "None" (7)
            int idlePose = isEditCharacterWindowOpen ? plugin.Characters[selectedCharacterIndex].IdlePoseIndex : plugin.NewCharacterIdlePoseIndex;

            if (idlePose != 7)
            {
                macro += $"/spose {idlePose}\n"; // ✅ Only apply idle command when an idle is chosen
            }


            macro += "/penumbra redraw self";

            return macro;
        }



        // 🔹 Add ExtractGlamourerDesignFromMacro BELOW GenerateMacro()
        private string ExtractGlamourerDesignFromMacro(string macro)// Store old honorific before updating

        {
            // 🔹 Find the Glamourer line in the macro
            string[] lines = macro.Split('\n');
            foreach (var line in lines)
            {
                if (line.StartsWith("/glamour apply ", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Replace("/glamour apply ", "").Replace(" | self", "").Trim();
                }
            }
            return ""; // Return empty if nothing was found
        }

        private void DrawDesignPanel()
        {
            if (activeDesignCharacterIndex < 0 || activeDesignCharacterIndex >= plugin.Characters.Count)
                return;

            var character = plugin.Characters[activeDesignCharacterIndex];

            // 🔹 ✅ Close Add Design when switching characters
            if (selectedCharacterIndex != activeDesignCharacterIndex)
            {
                isEditDesignWindowOpen = false;
                isAdvancedModeWindowOpen = false;
                editedDesignName = "";
                editedGlamourerDesign = "";
                editedDesignMacro = "";
                advancedDesignMacroText = "";
                selectedCharacterIndex = activeDesignCharacterIndex; // ✅ Update tracking
            }

            // 🔹 Header with Add Button
            ImGui.Text($"Designs for {character.Name}");
            ImGui.SameLine();

            // 🔹 Plus Button (Green)
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(new Vector4(0.27f, 1.07f, 0.27f, 1.0f)));
            if (ImGui.Button("+##AddDesign"))
            {
                isEditDesignWindowOpen = true;
                editedDesignName = "";
                editedGlamourerDesign = "";
                editedDesignMacro = "";
                isAdvancedModeDesign = false;
                isAdvancedModeWindowOpen = false;
                advancedDesignMacroText = "";
            }
            ImGui.PopStyleColor();

            // 🔹 Close Button (Red)
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowContentRegionMax().X - 20);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(new Vector4(1.0f, 0.27f, 0.27f, 1.0f)));
            if (ImGui.Button("x##CloseDesignPanel"))
            {
                activeDesignCharacterIndex = -1;
                isDesignPanelOpen = false;
                isEditDesignWindowOpen = false;
                isAdvancedModeWindowOpen = false; // ✅ Close pop-up window too
            }
            ImGui.PopStyleColor();

            ImGui.Separator();

            // 🔹 1️⃣ RENDER THE FORM **FIRST** BEFORE THE LIST
            if (isEditDesignWindowOpen)
            {
                ImGui.BeginChild("EditDesignForm", new Vector2(0, 320), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize);

                bool isNewDesign = string.IsNullOrEmpty(editedDesignName);
                ImGui.Text(isNewDesign ? "Add Design" : "Edit Design");

                float inputWidth = 200;
                ImGui.Text("Design Name*");
                ImGui.SetCursorPosX(10);
                ImGui.SetNextItemWidth(inputWidth);
                ImGui.InputText("##DesignName", ref editedDesignName, 100);

                ImGui.Separator();

                // 🔹 Glamourer Design Label
                ImGui.Text("Glamourer Design*");

                // ℹ Tooltip Icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf05a"); // Info icon
                ImGui.PopFont();

                if (ImGui.IsItemHovered()) // ✅ Show tooltip on hover
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(300);
                    ImGui.TextUnformatted("Enter the name of the Glamourer design to apply to this character.");
                    ImGui.TextUnformatted("Must be entered EXACTLY as it is named in Glamourer!");
                    ImGui.TextUnformatted("Note: You can add additional designs later.");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                // 🔹 Input Field
                ImGui.SetCursorPosX(10);
                ImGui.SetNextItemWidth(inputWidth);
                ImGui.InputText("##GlamourerDesign", ref editedGlamourerDesign, 100);


                ImGui.Separator();

                // 🔹 Advanced Mode Button
                if (ImGui.Button(isAdvancedModeDesign ? "Exit Advanced Mode" : "Advanced Mode"))
                {
                    isAdvancedModeDesign = !isAdvancedModeDesign;
                    isAdvancedModeWindowOpen = isAdvancedModeDesign;
                    if (isAdvancedModeDesign)
                    {
                        advancedDesignMacroText = !string.IsNullOrWhiteSpace(editedDesignMacro)
                            ? editedDesignMacro
                            : GenerateDesignMacro();
                    }
                }

                // ℹ Tooltip Icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text("\uf05a");
                ImGui.PopFont();
                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.PushTextWrapPos(300);
                    ImGui.TextUnformatted("⚠️ Do not touch this unless you know what you're doing.");
                    ImGui.PopTextWrapPos();
                    ImGui.EndTooltip();
                }

                // 🔹 Restore Advanced Mode Text Box INSIDE the Design Panel
                if (isAdvancedModeDesign)
                {
                    ImGui.Separator();
                    ImGui.Text("Edit Macro Manually:");

                    float totalHeight = ImGui.GetContentRegionAvail().Y - 55;
                    float minHeight = 110;
                    float maxHeight = 160;
                    float inputHeight = Math.Clamp(totalHeight, minHeight, maxHeight);

                    ImGui.BeginChild("AdvancedModeSection", new Vector2(0, inputHeight), true, ImGuiWindowFlags.NoScrollbar);
                    ImGui.InputTextMultiline("##AdvancedDesignMacro", ref advancedDesignMacroText, 2000, new Vector2(-1, inputHeight - 10), ImGuiInputTextFlags.AllowTabInput);
                    ImGui.EndChild();

                    // ✅ Auto-save Advanced Mode edits inside the panel
                    editedDesignMacro = advancedDesignMacroText;
                }

                ImGui.Separator();

                // 🔹 Align Buttons Properly
                float buttonWidth = 85;
                float buttonHeight = 20;
                float buttonSpacing = 8;
                float totalButtonWidth = (buttonWidth * 2 + buttonSpacing);
                float buttonPosX = (ImGui.GetContentRegionAvail().X - totalButtonWidth) / 2;
                ImGui.SetCursorPosX(buttonPosX);

                bool canSave = !string.IsNullOrWhiteSpace(editedDesignName) && !string.IsNullOrWhiteSpace(editedGlamourerDesign);

                if (!canSave)
                    ImGui.BeginDisabled();

                if (ImGui.Button("Save Design", new Vector2(buttonWidth, buttonHeight)))
                {
                    SaveDesign(plugin.Characters[activeDesignCharacterIndex]);
                    isEditDesignWindowOpen = false;
                    isAdvancedModeWindowOpen = false; // ✅ Close pop-up after saving
                }

                if (!canSave)
                    ImGui.EndDisabled();

                ImGui.SameLine();

                if (ImGui.Button("Cancel", new Vector2(buttonWidth, buttonHeight)))
                {
                    isEditDesignWindowOpen = false;
                    isAdvancedModeWindowOpen = false;
                }

                ImGui.EndChild(); // ✅ END FORM
            }

            ImGui.Separator(); // ✅ Visually separate the list
            ImGui.Text("Sort Designs By:");
            ImGui.SameLine();

            if (ImGui.BeginCombo("##DesignSortDropdown", currentDesignSort.ToString()))
            {
                if (ImGui.Selectable("Favorites", currentDesignSort == DesignSortType.Favorites))
                {
                    currentDesignSort = DesignSortType.Favorites;
                    SortDesigns(plugin.Characters[activeDesignCharacterIndex]);
                }
                if (ImGui.Selectable("Alphabetical", currentDesignSort == DesignSortType.Alphabetical))
                {
                    currentDesignSort = DesignSortType.Alphabetical;
                    SortDesigns(plugin.Characters[activeDesignCharacterIndex]);
                }
                if (ImGui.Selectable("Newest", currentDesignSort == DesignSortType.Recent))
                {
                    currentDesignSort = DesignSortType.Recent;
                    SortDesigns(plugin.Characters[activeDesignCharacterIndex]);
                }
                if (ImGui.Selectable("Oldest", currentDesignSort == DesignSortType.Oldest))
                {
                    currentDesignSort = DesignSortType.Oldest;
                    SortDesigns(plugin.Characters[activeDesignCharacterIndex]);
                }
                ImGui.EndCombo();
            }


            ImGui.Separator();


            // 🔹 NOW RENDER THE DESIGN LIST
            ImGui.BeginChild("DesignListBackground", new Vector2(0, ImGui.GetContentRegionAvail().Y), true, ImGuiWindowFlags.NoScrollbar);

            foreach (var design in character.Designs)
            {
                float rowWidth = ImGui.GetContentRegionAvail().X;

                // * Add Favorite Star before the design name (left side)
                string starSymbol = design.IsFavorite ? "★" : "☆"; // Solid star if favorited, empty star if not
                var starPos = new Vector2(ImGui.GetCursorPosX(), ImGui.GetCursorPosY()); // Align with design name

                ImGui.Text(starSymbol);
                ImGui.SameLine(); // Keep it next to the name

                // Clickable star toggle
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    design.IsFavorite = !design.IsFavorite;
                    plugin.SaveConfiguration();
                    SortDesigns(character);  // ✅ Resort after toggling favorite
                }

                // Tooltip for clarity
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(design.IsFavorite ? "Unfavorite this design" : "Mark as Favorite");
                }

                ImGui.SameLine();

                // 🔹 Dynamic Text Truncation
                float availableWidth = rowWidth - 130f; // Space for buttons (Apply, Edit, Delete)
                string displayName = design.Name;

                Vector2 textSize = ImGui.CalcTextSize(displayName);
                if (textSize.X > availableWidth)
                {
                    int maxChars = displayName.Length;
                    while (maxChars > 0 && ImGui.CalcTextSize(displayName.Substring(0, maxChars) + "...").X > availableWidth)
                    {
                        maxChars--;
                    }
                    displayName = displayName.Substring(0, maxChars) + "...";
                }

                // 🔹 Render the Truncated Design Name
                ImGui.Text(displayName);
                ImGui.SameLine();

                // * Show Full Name on Hover
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(design.Name);
                }

                // 🔹 Position the Apply Button Correctly
                ImGui.SetCursorPosX(rowWidth - 80);

                // 🔹 Apply Button ✅
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button("\uf00c" + $"##Apply{design.Name}"))
                {
                    plugin.ExecuteMacro(design.Macro);
                }
                ImGui.PopFont();

                ImGui.SameLine();

                // 🔹 Edit Icon ✏️
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button("\uf044" + $"##Edit{design.Name}"))
                {
                    OpenEditDesignWindow(character, design);
                }
                ImGui.PopFont();

                ImGui.SameLine();

                // 🔹 Delete Icon 🗑️
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button("\uf2ed" + $"##Delete{design.Name}"))
                {
                    bool isCtrlShiftPressed = ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift;
                    if (isCtrlShiftPressed)
                    {
                        character.Designs.Remove(design);
                        plugin.SaveConfiguration();
                    }
                }
                ImGui.PopFont();

                ImGui.Separator();
            }


            ImGui.EndChild(); // ✅ END DESIGN LIST

            // 🔹 ✅ RENDER THE ADVANCED MODE POP-UP WINDOW
            if (isAdvancedModeWindowOpen)
            {
                ImGui.SetNextWindowSize(new Vector2(500, 200), ImGuiCond.FirstUseEver);
                if (ImGui.Begin("Advanced Macro Editor", ref isAdvancedModeWindowOpen, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize))
                {
                    ImGui.Text("Edit Design Macro Manually:");
                    ImGui.InputTextMultiline("##AdvancedDesignMacroPopup", ref advancedDesignMacroText, 2000, new Vector2(-1, -1), ImGuiInputTextFlags.AllowTabInput);
                }
                ImGui.End();
            }
        }


        private void AddNewDesign()
        {
            isEditDesignWindowOpen = true; // ✅ Ensure the edit design form is opened properly
            editedDesignName = ""; // ✅ Reset for new design
            editedGlamourerDesign = ""; // ✅ Reset for new design
            editedDesignMacro = ""; // ✅ Clear macro for new design
            isAdvancedModeDesign = false; // ✅ Ensure Advanced Mode starts OFF
        }

        private void OpenEditDesignWindow(Character character, CharacterDesign design)
        {
            isEditDesignWindowOpen = true;
            originalDesignName = design.Name; // ✅ Save original name before editing
            editedDesignName = design.Name;
            editedDesignMacro = design.IsAdvancedMode ? design.AdvancedMacro : design.Macro;
            editedGlamourerDesign = ExtractGlamourerDesignFromMacro(design.Macro);
            isAdvancedModeDesign = design.IsAdvancedMode;
            advancedDesignMacroText = design.AdvancedMacro;
        }


        private void SaveDesign(Character character)
        {
            if (string.IsNullOrWhiteSpace(editedDesignName) || string.IsNullOrWhiteSpace(editedGlamourerDesign))
                return; // Prevent saving if fields are empty

            // 🔹 Find the existing design using the original name
            var existingDesign = character.Designs.FirstOrDefault(d => d.Name == originalDesignName);

            if (existingDesign != null)
            {
                // ✅ Update the existing design
                existingDesign.Name = editedDesignName;
                existingDesign.Macro = isAdvancedModeDesign ? advancedDesignMacroText : GenerateDesignMacro();
                existingDesign.AdvancedMacro = isAdvancedModeDesign ? advancedDesignMacroText : "";
                existingDesign.IsAdvancedMode = isAdvancedModeDesign;
            }
            else
            {
                // ✅ Fallback: If the design was deleted or not found, create a new one
                character.Designs.Add(new CharacterDesign(
                    editedDesignName,
                    isAdvancedModeDesign ? advancedDesignMacroText : GenerateDesignMacro(),
                    isAdvancedModeDesign,
                    isAdvancedModeDesign ? advancedDesignMacroText : ""
                )
                {
                    DateAdded = DateTime.UtcNow // ✅ Set DateAdded when creating a new design
                });
            }

            plugin.SaveConfiguration();
            isEditDesignWindowOpen = false;
        }


        private string GenerateDesignMacro()
        {
            if (string.IsNullOrWhiteSpace(editedGlamourerDesign))
                return "";

            return $"/glamour apply {editedGlamourerDesign} | self\n/penumbra redraw self";
        }


        private void OpenEditCharacterWindow(int index)
        {
            if (index < 0 || index >= plugin.Characters.Count)
                return;

            selectedCharacterIndex = index;
            var character = plugin.Characters[index];

            string pluginDirectory = plugin.PluginDirectory;
            string defaultImagePath = Path.Combine(pluginDirectory, "Assets", "Default.png");

            editedCharacterName = character.Name;
            editedCharacterPenumbra = character.PenumbraCollection;
            editedCharacterGlamourer = character.GlamourerDesign;
            editedCharacterCustomize = character.CustomizeProfile;
            editedCharacterColor = character.NameplateColor;
            editedCharacterMacros = character.Macros;
            editedCharacterImagePath = !string.IsNullOrEmpty(character.ImagePath) ? character.ImagePath : defaultImagePath;


            // ✅ Load Honorific Fields Properly
            editedCharacterHonorificTitle = character.HonorificTitle ?? "";
            editedCharacterHonorificPrefix = character.HonorificPrefix ?? "Prefix";
            editedCharacterHonorificSuffix = character.HonorificSuffix ?? "Suffix";
            editedCharacterHonorificColor = character.HonorificColor;
            editedCharacterHonorificGlow = character.HonorificGlow;
            editedCharacterMoodlePreset = character.MoodlePreset;

            // ✅ Check if MoodlePreset exists in older profiles
            editedCharacterMoodlePreset = character.MoodlePreset ?? ""; // Prevents null values

            character.IdlePoseIndex = plugin.Characters[selectedCharacterIndex].IdlePoseIndex;


            tempHonorificTitle = editedCharacterHonorificTitle;
            tempHonorificPrefix = editedCharacterHonorificPrefix;
            tempHonorificSuffix = editedCharacterHonorificSuffix;
            tempHonorificColor = editedCharacterHonorificColor;
            tempHonorificGlow = editedCharacterHonorificGlow;
            tempMoodlePreset = editedCharacterMoodlePreset;

            if (isAdvancedModeCharacter)
            {
                advancedCharacterMacroText = !string.IsNullOrWhiteSpace(character.Macros)
                    ? character.Macros
                    : GenerateMacro();
            }

            isEditCharacterWindowOpen = true;
        }


        private void SaveEditedCharacter()
        {
            if (selectedCharacterIndex < 0 || selectedCharacterIndex >= plugin.Characters.Count)
                return;

            var character = plugin.Characters[selectedCharacterIndex];

            character.Name = editedCharacterName;
            character.PenumbraCollection = editedCharacterPenumbra;
            character.GlamourerDesign = editedCharacterGlamourer;
            character.CustomizeProfile = editedCharacterCustomize;
            character.NameplateColor = editedCharacterColor;

            // ✅ Save Honorific Fields
            character.HonorificTitle = editedCharacterHonorificTitle;
            character.HonorificPrefix = editedCharacterHonorificPrefix;
            character.HonorificSuffix = editedCharacterHonorificSuffix;
            character.HonorificColor = editedCharacterHonorificColor;
            character.HonorificGlow = editedCharacterHonorificGlow;
            character.MoodlePreset = editedCharacterMoodlePreset;

            // ✅ Ensure MoodlePreset is saved even if previously missing
            character.MoodlePreset = string.IsNullOrWhiteSpace(editedCharacterMoodlePreset) ? "" : editedCharacterMoodlePreset;


            // ✅ Ensure Macro Updates Correctly
            character.Macros = isAdvancedModeCharacter ? advancedCharacterMacroText : GenerateMacro();

            if (!string.IsNullOrEmpty(editedCharacterImagePath))
            {
                character.ImagePath = editedCharacterImagePath;
            }

            plugin.SaveConfiguration();
            isEditCharacterWindowOpen = false;
        }


    }
}
