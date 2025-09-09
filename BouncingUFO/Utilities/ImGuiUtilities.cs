using Foster.Framework;
using ImGuiNET;

namespace BouncingUFO.Utilities
{
    public static class ImGuiUtilities
    {
        public static Color GetFosterColor(ImGuiCol idx, byte alpha)
        {
            var fosterColor = new Color();
            var imguiColor = ImGui.GetColorU32(idx);
            fosterColor.A = alpha;
            fosterColor.B = (byte)(imguiColor >> 16);
            fosterColor.G = (byte)(imguiColor >> 8);
            fosterColor.R = (byte)(imguiColor >> 0);
            return fosterColor;
        }

        public static bool ComboPoint2(string label, ref Point2 v, Point2[] v_valid, string format)
        {
            var v_idx = Math.Max(0, Array.IndexOf(v_valid, v));
            var v_labels = v_valid.Select(x => string.Format(format, x.X, x.Y)).ToArray();
            var changed = ImGui.Combo(label, ref v_idx, v_labels, v_labels.Length);
            if (changed) v = v_valid[v_idx];
            return changed;
        }

        public static void InputFileBrowser(string label, ref string input, FileSystem filesystem, FileSystem.DialogFilter[] filters, FileSystem.DialogCallback callback)
        {
            ImGui.InputText(label, ref input, 1024, ImGuiInputTextFlags.ReadOnly);
            ImGui.SameLine();
            if (ImGui.Button("Browse...")) filesystem.OpenFileDialog(callback, filters, input);
        }

        public static void InfoPopUp(string text, string label = "Info", string header = "Information")
        {
            ImGui.BeginDisabled();
            ImGui.Button(label);
            ImGui.EndDisabled();
            if (ImGui.BeginItemTooltip())
            {
                ImGui.SeparatorText(header);
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 75f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }
    }
}
