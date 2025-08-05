using Foster.Framework;
using GameTest1.Game.Levels;
using GameTest1.Utilities;
using ImGuiNET;
using System.Numerics;
using System.Text.Json;

namespace GameTest1.Editors
{
    public class MapEditor(Manager manager) : EditorBase(manager), IEditor
    {
        public override string Name => "Map Editor";

        const float mapZoom = 2f, tilesetZoom = 3f;

        private Map? map;
        private Tileset? tileset;

        private string currentMapPath = string.Empty;
        private int hoveredMapCell = -1, hoveredTilemapCell = -1, selectedTilemapCell = 0;
        private Color gridColor, inactiveLayerColor, hoveredHighlightColor, selectedHighlightColor, hoveredBorderColor, selectedBorderColor;
        private bool drawMapCellGrid = true, drawTilesetCellGrid = true, dimInactiveLayers = true;
        private int activeLayer = 0;

        public (Map? Map, Tileset? Tileset) CurrentMapAndTileset => (map, tileset);

        public override void Setup()
        {
            if (gridColor.RGBA == 0) gridColor = Color.FromHexStringRGBA("0x0000007F");
            if (inactiveLayerColor.RGBA == 0) inactiveLayerColor = Color.FromHexStringRGBA("0x5F5F5F5F");
            if (hoveredBorderColor.RGBA == 0) hoveredBorderColor = Color.FromHexStringRGBA("0x00FF007F");
            if (selectedBorderColor.RGBA == 0) selectedBorderColor = Color.FromHexStringRGBA("0xFF00007F");
            if (hoveredHighlightColor.RGBA == 0) hoveredHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.Border, 0x7F);
            if (selectedHighlightColor.RGBA == 0) selectedHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.TextSelectedBg, 0x7F);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                var style = ImGui.GetStyle();

                ImGui.BeginGroup();
                if (ImGui.Button("New Map"))
                {
                    if (map != null) ImGui.OpenPopup("New");
                    else map = new();
                }
                ImGui.SameLine();

                var center = ImGui.GetMainViewport().GetCenter();
                ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f));
                if (ImGui.BeginPopupModal("New", ImGuiWindowFlags.AlwaysAutoResize))
                {
                    ImGui.Text("A map is currently open; overwrite?");
                    ImGui.Separator();
                    if (ImGui.Button("Yes")) { map = new(); currentMapPath = string.Empty; ImGui.CloseCurrentPopup(); }
                    ImGui.SameLine();
                    ImGui.SetItemDefaultFocus();
                    if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                    ImGui.EndPopup();
                }

                if (ImGui.Button("Load Map"))
                {
                    manager.FileSystem.OpenFileDialog(new FileSystem.DialogCallback((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success && s.Length > 0 && s[0] != null)
                        {
                            currentMapPath = s[0];
                            map = JsonSerializer.Deserialize<Map>(File.ReadAllText(currentMapPath), Assets.SerializerOptions);
                        }
                    }), [new("JSON files (*.json)", "json")], currentMapPath);
                }
                ImGui.SameLine();

                if (map == null) ImGui.BeginDisabled();
                if (ImGui.Button("Save Map") && map != null)
                {
                    manager.FileSystem.SaveFileDialog(new FileSystem.DialogCallbackSingleFile((s, r) =>
                    {
                        if (r == FileSystem.DialogResult.Success)
                            File.WriteAllText(s, JsonSerializer.Serialize(map, Assets.SerializerOptions));
                    }), [new("JSON files (*.json)", "json")], currentMapPath);
                }
                if (map == null) ImGui.EndDisabled();
                ImGui.SameLine();

                if (map != null)
                {
                    ImGui.SameLine();
                    ImGui.Dummy(new(10f, 0f));
                    ImGui.SameLine();
                    ImGui.Text($"Current map: {(string.IsNullOrWhiteSpace(currentMapPath) ? "unsaved" : currentMapPath)}");
                }
                ImGui.EndGroup();

                ImGui.BeginGroup();
                if (map != null)
                {
                    ImGui.Separator();

                    var layersDirty = false;
                    var tilesetDirty = tileset == null;

                    ImGui.BeginGroup();
                    ImGui.InputText("Title", ref map.Title, 128);
                    if (ImGui.SliderInt2("Size", ref map.Size.X, 1, 40))
                        layersDirty = true;
                    if (ImGui.InputText("Tileset", ref map.Tileset, 128))
                        tilesetDirty = true;
                    ImGui.EndGroup();
                    ImGui.SameLine();

                    ImGui.Dummy(new(10f, 0f));
                    ImGui.SameLine();

                    ImGui.BeginGroup();
                    var currentLayerCount = map.Layers.Count;
                    if (currentLayerCount >= 8) ImGui.BeginDisabled();
                    if (ImGui.Button("Add new layer"))
                    {
                        map.Layers.Add(new(map.Size));
                        layersDirty = true;
                    }
                    if (currentLayerCount >= 8) ImGui.EndDisabled();
                    if (currentLayerCount <= 0) ImGui.BeginDisabled();
                    if (ImGui.Button("Remove active layer"))
                    {
                        map.Layers.RemoveAt(activeLayer);
                        layersDirty = true;
                    }
                    if (currentLayerCount <= 0) ImGui.EndDisabled();
                    ImGui.EndGroup();
                    ImGui.SameLine();

                    ImGui.Dummy(new(10f, 0f));
                    ImGui.SameLine();

                    ImGui.BeginGroup();
                    ImGui.Checkbox("Draw map grid", ref drawMapCellGrid);
                    ImGui.SameLine();
                    ImGui.Checkbox("Draw tileset grid", ref drawTilesetCellGrid);
                    if (map.Layers.Count == 0) ImGui.BeginDisabled();
                    ImGui.SliderInt("Active layer", ref activeLayer, 0, map.Layers.Count == 0 ? 0 : map.Layers.Count - 1);
                    if (map.Layers.Count == 0) ImGui.EndDisabled();
                    ImGui.Checkbox("Dim inactive layers", ref dimInactiveLayers);
                    ImGui.EndGroup();

                    if (layersDirty)
                    {
                        hoveredMapCell = -1;
                        map.ResizeLayers();
                        layersDirty = false;
                    }

                    if (tilesetDirty && manager.Assets.Tilesets.TryGetValue(map.Tileset, out Tileset? value))
                    {
                        tileset = value;
                        tilesetDirty = false;
                    }

                    if (tileset != null && tileset.CellTextures != null && map.Layers.Count != 0)
                    {
                        ImGui.Separator();

                        var mapScrollHeight = 0f;
                        var tileScrollWidth = tileset.CellSize.X * 2 * tilesetZoom + style.ScrollbarSize;

                        ImGui.BeginGroup();
                        if (ImGui.BeginChild("mapscroll", new(850f - tileScrollWidth, 0f), ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.AlwaysHorizontalScrollbar))
                        {
                            ImGui.BeginGroup();

                            var imagePos = ImGui.GetCursorScreenPos();
                            if (manager.ImGuiRenderer.BeginBatch(new(map.Size.X * tileset.CellSize.X * mapZoom, map.Size.Y * tileset.CellSize.Y * mapZoom), out var batcher, out var bounds))
                            {
                                batcher.CheckeredPattern(bounds, 8, 8, Color.DarkGray, Color.Gray);
                                for (var i = 0; i < map.Layers.Count; i++)
                                {
                                    for (var x = 0; x < map.Size.X; x++)
                                    {
                                        for (var y = 0; y < map.Size.Y; y++)
                                        {
                                            var cellOffset = y * map.Size.X + x;
                                            var cellValue = map.Layers[i].Tiles[cellOffset];
                                            var cellPos = new Vector2(x, y) * mapZoom * tileset.CellSize;
                                            batcher.Image(tileset.CellTextures[cellValue], cellPos, Vector2.Zero, new(mapZoom), 0f, i == activeLayer || !dimInactiveLayers ? Color.White : inactiveLayerColor);

                                            var cellRect = new Rect(cellPos, tileset.CellSize * mapZoom);
                                            if (drawMapCellGrid)
                                                batcher.RectLine(cellRect, 1f, gridColor);

                                            if (hoveredMapCell != -1 && hoveredMapCell == cellOffset)
                                            {
                                                batcher.Rect(cellRect, hoveredHighlightColor);
                                                batcher.RectLine(cellRect, 1f, hoveredBorderColor);
                                            }
                                        }
                                    }
                                }
                            }
                            manager.ImGuiRenderer.EndBatch();
                            ImGui.SetCursorScreenPos(imagePos);
                            ImGui.InvisibleButton($"##dummy1", new(map.Size.X * tileset.CellSize.X * mapZoom, map.Size.Y * tileset.CellSize.Y * mapZoom));

                            for (var x = 0; x < map.Size.X; x++)
                            {
                                for (var y = 0; y < map.Size.Y; y++)
                                {
                                    var cellOffset = y * map.Size.X + x;
                                    var cellIdx = map.Layers[activeLayer].Tiles[cellOffset];
                                    var cellPos = imagePos + new Vector2(x, y) * mapZoom * tileset.CellSize;

                                    var isHovering = ImGui.IsMouseHoveringRect(cellPos, cellPos + tileset.CellSize * mapZoom);
                                    if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && isHovering)
                                    {
                                        hoveredMapCell = cellOffset;
                                        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                        {
                                            map.Layers[activeLayer].Tiles[cellOffset] = selectedTilemapCell;
                                        }
                                        else if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
                                        {
                                            selectedTilemapCell = map.Layers[activeLayer].Tiles[cellOffset];
                                        }
                                    }
                                }
                            }
                            ImGui.EndGroup();

                            mapScrollHeight = Math.Max(ImGui.GetWindowHeight(), tileset.CellSize.Y * 4 * tilesetZoom + style.ScrollbarSize);
                        }
                        ImGui.EndChild();
                        ImGui.EndGroup();

                        ImGui.SameLine();

                        ImGui.BeginGroup();
                        if (ImGui.BeginChild("tilescroll", new(tileScrollWidth, mapScrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
                        {
                            ImGui.BeginGroup();

                            var tilesetViewSize = new Vector2(tileset.CellSize.X * 2, tileset.CellFlags.Length / 2 * tileset.CellSize.Y);

                            var imagePos = ImGui.GetCursorScreenPos();
                            if (manager.ImGuiRenderer.BeginBatch(tilesetViewSize * tilesetZoom, out var batcher, out var bounds))
                            {
                                batcher.CheckeredPattern(bounds, 8, 8, Color.DarkGray, Color.Gray);
                                for (var i = 0; i < tileset.TilesheetSizeInCells.X * tileset.TilesheetSizeInCells.Y; i++)
                                {
                                    var cellPos = new Vector2(i % 2, i / 2) * tilesetZoom * tileset.CellSize;
                                    batcher.Image(tileset.CellTextures[i], cellPos, Vector2.Zero, new(tilesetZoom), 0f, Color.White);

                                    var cellRect = new Rect(cellPos, tileset.CellSize * tilesetZoom);
                                    if (drawTilesetCellGrid)
                                        batcher.RectLine(cellRect, 1f, gridColor);

                                    if (selectedTilemapCell == i)
                                    {
                                        batcher.Rect(cellRect, selectedHighlightColor);
                                        batcher.RectLine(cellRect, 1f, selectedBorderColor);
                                    }

                                    if (hoveredTilemapCell != -1 && hoveredTilemapCell == i)
                                    {
                                        batcher.Rect(cellRect, hoveredHighlightColor);
                                        batcher.RectLine(cellRect, 1f, hoveredBorderColor);
                                    }
                                }
                            }
                            manager.ImGuiRenderer.EndBatch();
                            ImGui.SetCursorScreenPos(imagePos);
                            ImGui.InvisibleButton($"##dummy2", tilesetViewSize * tilesetZoom);

                            for (var i = 0; i < tileset.TilesheetSizeInCells.X * tileset.TilesheetSizeInCells.Y; i++)
                            {
                                var cellPos = imagePos + new Vector2(i % 2, i / 2) * tilesetZoom * tileset.CellSize;
                                var isHovering = ImGui.IsMouseHoveringRect(cellPos, cellPos + tileset.CellSize * tilesetZoom);
                                if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && isHovering)
                                {
                                    hoveredTilemapCell = i;
                                    if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                                        selectedTilemapCell = hoveredTilemapCell;
                                }
                            }
                            ImGui.EndGroup();
                        }
                        ImGui.EndChild();
                        ImGui.EndGroup();
                    }
                }
                ImGui.EndGroup();
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }
    }
}
