using BouncingUFO.Game.Levels;
using BouncingUFO.Utilities;
using Foster.Framework;
using ImGuiNET;
using System.Numerics;
using System.Text.Json;

namespace BouncingUFO.Editors
{
    public class TilesetEditor(Manager manager) : EditorBase(manager), IEditor
    {
        public override string Name => "Tileset Editor";

        const float zoom = 3f;

        private Tileset? tileset;

        private string currentTilesetPath = string.Empty;
        private int hoveredCell = -1, selectedCell = 0;
        private Color gridColor, hoveredHighlightColor, selectedHighlightColor, hoveredBorderColor, selectedBorderColor;
        private bool drawCellGrid = true;

        public override void Setup()
        {
            if (gridColor.RGBA == 0) gridColor = new(0, 0, 0, 128);
            if (hoveredBorderColor.RGBA == 0) hoveredBorderColor = new(0, 255, 0, 128);
            if (selectedBorderColor.RGBA == 0) selectedBorderColor = new(255, 0, 0, 128);
            if (hoveredHighlightColor.RGBA == 0) hoveredHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.Border, 128);
            if (selectedHighlightColor.RGBA == 0) selectedHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.TextSelectedBg, 128);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                ImGui.BeginGroup();
                if (ImGui.Button("New Tileset"))
                {
                    if (tileset != null) ImGui.OpenPopup("New");
                    else tileset = new();
                }
                ImGui.SameLine();

                var center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f));
                if (ImGui.BeginPopupModal("New", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("A tileset is currently open; overwrite?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes")) { tileset = new(); currentTilesetPath = string.Empty; ImGui.CloseCurrentPopup(); }
                    ImGui.SameLine();
                    ImGui.SetItemDefaultFocus();
                    if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                }

                if (ImGui.Button("Load Tileset"))
                {
                    manager.FileSystem.OpenFileDialog(new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                        {
                            currentTilesetPath = s[0];
                            tileset = JsonSerializer.Deserialize<Tileset>(File.ReadAllText(currentTilesetPath), Manager.SerializerOptions);
                        }
                    }), [new("JSON files (*.json)", "json")], currentTilesetPath);
                }
                ImGui.SameLine();

                if (tileset == null) ImGui.BeginDisabled();
                if (ImGui.Button("Save Tileset") && tileset != null)
                {
                    manager.FileSystem.SaveFileDialog(new FileSystem.DialogCallbackSingleFile((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success)
                            File.WriteAllText(currentTilesetPath = s, JsonSerializer.Serialize(tileset, Manager.SerializerOptions));
                    }), [new("JSON files (*.json)", "json")], currentTilesetPath);
                }
                if (tileset == null) ImGui.EndDisabled();
                ImGui.SameLine();

                if (tileset == null) ImGui.BeginDisabled();
                if (ImGui.Button("Export as Asset") && tileset != null)
                {
                    var tilesheetFullPath = new string(tileset.TilesheetFile);
                    var jsonFilename = Path.ChangeExtension(Path.GetFileName(tileset.TilesheetFile), "json");
                    var relativeTilesheetFilePath = Path.Join(Assets.AssetsFolderName, Assets.TilesetFolderName, Path.GetFileName(tileset.TilesheetFile));
                    manager.FileSystem.OpenFolderDialog(new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                        {
                            tileset.TilesheetFile = relativeTilesheetFilePath;
                            File.WriteAllText(Path.Join(s[0], jsonFilename), JsonSerializer.Serialize(tileset, Manager.SerializerOptions));
                            File.Copy(tilesheetFullPath, Path.Join(s[0], Path.GetFileName(tilesheetFullPath)), true);
                            tileset.TilesheetFile = tilesheetFullPath;
                        }
                    }), null);
                }
                if (tileset == null) ImGui.EndDisabled();
                ImGui.EndGroup();

                if (tileset != null)
                {
                    ImGui.Separator();

                    ImGui.BeginGroup();
                    ImGui.Text($"Current tileset: {(string.IsNullOrWhiteSpace(currentTilesetPath) ? "unsaved" : currentTilesetPath)}");
                    var needSubtexAndFlags = false;
                    if (tileset.CellTextures == null && File.Exists(tileset.TilesheetFile))
                        needSubtexAndFlags = true;
                    if (ImGuiUtilities.ComboPoint2("Cell Size", ref tileset.CellSize, Tileset.ValidCellSizes, "{0}x{1}"))
                        needSubtexAndFlags = true;
                    ImGuiUtilities.InputFileBrowser("Tilesheet File", ref tileset.TilesheetFile, manager.FileSystem, [new("Image files (*.png;*.bmp;*.jpg)", "png;bmp;jpg")], new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success)
                        {
                            tileset.TilesheetFile = s.Length > 0 && s[0] != null ? s[0] : string.Empty;
                            tileset.CreateTextures(manager.GraphicsDevice);
                        }
                    }));
                    ImGui.EndGroup();

                    ImGui.BeginGroup();
                    ImGui.Checkbox("Draw cell grid", ref drawCellGrid);
                    ImGui.EndGroup();

                    if (tileset.TilesheetTexture != null)
                    {
                        ImGui.Separator();

                        ImGui.BeginGroup();
                        var imagePos = ImGui.GetCursorScreenPos();
                        if (manager.ImGuiRenderer.BeginBatch(new(tileset.TilesheetTexture.Width * zoom, tileset.TilesheetTexture.Height * zoom), out var batcher, out var bounds))
                        {
                            batcher.CheckeredPattern(bounds, 8, 8, Color.Gray, Color.LightGray);
                            batcher.Image(tileset.TilesheetTexture, Vector2.Zero, Vector2.Zero, Vector2.One * zoom, 0f, Color.White);

                            for (var x = 0; x < tileset.TilesheetSizeInCells.X; x++)
                            {
                                for (var y = 0; y < tileset.TilesheetSizeInCells.Y; y++)
                                {
                                    var cellPos = new Vector2(x, y) * zoom * tileset.CellSize;
                                    var cellRect = new Rect(cellPos, tileset.CellSize * zoom);
                                    if (drawCellGrid)
                                        batcher.RectLine(cellRect, 1f, gridColor);

                                    var cellIdx = y * tileset.TilesheetSizeInCells.X + x;
                                    if (selectedCell == cellIdx)
                                    {
                                        batcher.Rect(cellRect, selectedHighlightColor);
                                        batcher.RectLine(cellRect, 1f, selectedBorderColor);
                                    }

                                    if (hoveredCell != -1 && hoveredCell == cellIdx)
                                    {
                                        batcher.Rect(cellRect, hoveredHighlightColor);
                                        batcher.RectLine(cellRect, 1f, hoveredBorderColor);
                                    }
                                }
                            }
                        }
                        manager.ImGuiRenderer.EndBatch();
                        ImGui.SetCursorScreenPos(imagePos);
                        ImGui.InvisibleButton($"##dummy", new(tileset.TilesheetTexture.Width * zoom, tileset.TilesheetTexture.Height * zoom));

                        for (var x = 0; x < tileset.TilesheetSizeInCells.X; x++)
                        {
                            for (var y = 0; y < tileset.TilesheetSizeInCells.Y; y++)
                            {
                                var cellIdx = y * tileset.TilesheetSizeInCells.X + x;

                                var cellPos = imagePos + new Vector2(x, y) * zoom * tileset.CellSize;
                                var isHovering = ImGui.IsMouseHoveringRect(cellPos, cellPos + tileset.CellSize * zoom);
                                if (ImGui.IsWindowFocused() && isHovering)
                                {
                                    hoveredCell = cellIdx;
                                    if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                        selectedCell = hoveredCell;
                                }
                            }
                        }
                        ImGui.EndGroup();
                    }

                    ImGui.SameLine();

                    if (needSubtexAndFlags)
                    {
                        tileset.CreateTextures(manager.GraphicsDevice);
                        if ((tileset.CellFlags.Length == 0 || tileset.CellFlags.Length != tileset.TilesheetSizeInCells.Y * tileset.TilesheetSizeInCells.X) && tileset.CellTextures != null)
                            tileset.CellFlags = new CellFlag[tileset.CellTextures.Length];
                        selectedCell = 0;
                        hoveredCell = -1;
                        needSubtexAndFlags = false;
                    }

                    if (tileset.CellFlags.Length != 0)
                    {
                        ImGui.BeginGroup();
                        ImGui.Text($"Cell #{selectedCell + 1}/{tileset.CellFlags.Length}");
                        var cellValue = (uint)tileset.CellFlags[selectedCell];
                        foreach (var cellFlag in Enum.GetValues<CellFlag>())
                        {
                            if (cellFlag == CellFlag.Empty) continue;
                            if (ImGui.CheckboxFlags(cellFlag.ToString(), ref cellValue, (uint)cellFlag))
                                tileset.CellFlags[selectedCell] = (CellFlag)cellValue;
                        }
                        ImGui.EndGroup();
                    }
                }
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }
    }
}
