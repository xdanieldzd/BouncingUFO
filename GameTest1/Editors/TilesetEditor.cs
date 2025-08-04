using Foster.Framework;
using GameTest1.Levels;
using GameTest1.Utilities;
using ImGuiNET;
using System.Numerics;
using System.Text.Json;

namespace GameTest1.Editors
{
    public class TilesetEditor(Manager manager) : EditorBase(manager), IEditor
    {
        // TODO: rewrite grid & selector to use foster batcher like mapeditor!

        public override string Name => "Tileset Editor";

        const float zoom = 3f;

        private Tileset? tileset;

        private string currentTilesetPath = string.Empty;
        private int hoveredCell = -1, selectedCell = 0;
        private uint hoveredHighlightColor, selectedHighlightColor;
        private readonly uint hoveredBorderColor = 0x7F00FF00, selectedBorderColor = 0x7F0000FF;
        private bool drawCellGrid = true;

        public override void Setup()
        {
            //if (hoveredHighlightColor == 0) hoveredHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.Border) & 0x00FFFFFF);
            //if (selectedHighlightColor == 0) selectedHighlightColor = 0x7F000000 | (ImGui.GetColorU32(ImGuiCol.TextSelectedBg) & 0x00FFFFFF);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                var drawList = ImGui.GetWindowDrawList();

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
                            tileset = JsonSerializer.Deserialize<Tileset>(File.ReadAllText(currentTilesetPath), Assets.SerializerOptions);
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
                            File.WriteAllText(s, JsonSerializer.Serialize(tileset, Assets.SerializerOptions));
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
                            File.WriteAllText(Path.Join(s[0], jsonFilename), JsonSerializer.Serialize(tileset, Assets.SerializerOptions));
                            File.Copy(tilesheetFullPath, Path.Join(s[0], Path.GetFileName(tilesheetFullPath)));
                            tileset.TilesheetFile = tilesheetFullPath;
                        }
                    }), null);
                }
                if (tileset == null) ImGui.EndDisabled();
                ImGui.EndGroup();

                if (tileset != null)
                {
                    ImGui.NewLine();

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
                            tileset.TilesheetFile = s.Length > 0 && s[0] != null ? s[0] : string.Empty;
                    }));
                    ImGui.EndGroup();

                    ImGui.NewLine();

                    ImGui.BeginGroup();
                    ImGui.Checkbox("Draw cell grid", ref drawCellGrid);
                    ImGui.EndGroup();

                    if (tileset.TilesheetTexture != null)
                    {
                        ImGui.BeginGroup();
                        var imagePos = ImGui.GetCursorScreenPos();
                        if (manager.ImGuiRenderer.BeginBatch(new(tileset.TilesheetTexture.Width * zoom, tileset.TilesheetTexture.Height * zoom), out var batcher, out var bounds))
                        {
                            batcher.CheckeredPattern(bounds, 8, 8, Color.Gray, Color.LightGray);
                            batcher.Image(tileset.TilesheetTexture, Vector2.Zero, Vector2.Zero, Vector2.One * zoom, 0f, Color.White);
                        }
                        manager.ImGuiRenderer.EndBatch();
                        ImGui.SetCursorScreenPos(imagePos);
                        ImGui.InvisibleButton($"##dummy", new(tileset.TilesheetTexture.Width * zoom, tileset.TilesheetTexture.Height * zoom));

                        if (drawCellGrid)
                        {
                            for (var x = 0; x < tileset.TilesheetSizeInCells.X; x++)
                            {
                                for (var y = 0; y < tileset.TilesheetSizeInCells.Y; y++)
                                {
                                    var cellPos = imagePos + new Vector2(x, y) * zoom * tileset.CellSize;
                                    drawList.AddRect(cellPos, cellPos + tileset.CellSize * zoom, 0x7F000000);
                                }
                            }
                        }

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

                                if (selectedCell == cellIdx)
                                {
                                    drawList.AddRectFilled(cellPos, cellPos + tileset.CellSize * zoom, selectedHighlightColor);
                                    drawList.AddRect(cellPos, cellPos + tileset.CellSize * zoom, selectedBorderColor, 0f, ImDrawFlags.None, 4f);
                                }

                                if (hoveredCell != -1 && hoveredCell == cellIdx)
                                {
                                    drawList.AddRectFilled(cellPos, cellPos + tileset.CellSize * zoom, hoveredHighlightColor);
                                    drawList.AddRect(cellPos, cellPos + tileset.CellSize * zoom, hoveredBorderColor, 0f, ImDrawFlags.None, 4f);
                                }
                            }
                        }
                        ImGui.EndGroup();
                    }

                    ImGui.SameLine();

                    if (needSubtexAndFlags)
                    {
                        tileset.GenerateSubtextures(manager.GraphicsDevice);
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
            ImGui.End();
        }
    }
}
