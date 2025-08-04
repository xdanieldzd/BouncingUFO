using Foster.Framework;
using ImGuiNET;

namespace GameTest1.Utilities
{
    public static class ImGuiUtilities
    {
        readonly static ImGuiStylePtr style;

        static ImGuiUtilities()
        {
            style = ImGui.GetStyle();
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
    }
}
