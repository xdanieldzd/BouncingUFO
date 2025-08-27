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
        const int tileSelectorWidth = 3;

        private Map? map;
        private Tileset? tileset;

        private string currentMapPath = string.Empty;
        private int hoveredMapCell = -1, hoveredTilemapCell = -1, selectedTilemapCell = 0;
        private Color gridColor, inactiveLayerColor, hoveredHighlightColor, selectedHighlightColor, hoveredBorderColor, selectedBorderColor, activeSpawnColor, inactiveSpawnColor;
        private bool drawMapCellGrid = true, drawTilesetCellGrid = true, dimInactiveLayers = true;
        private int activeLayer = 0;
        private readonly string spawnEditorName = "Spawn Editor";
        private bool isSpawnEditorOpen = false, isSpawnEditorFocused = false;
        private int selectedSpawn = 0;

        public (Map? Map, Tileset? Tileset) CurrentMapAndTileset => (map, tileset);

        public override void Setup()
        {
            if (gridColor.RGBA == 0) gridColor = new(0, 0, 0, 128);
            if (inactiveLayerColor.RGBA == 0) inactiveLayerColor = new(255, 255, 255, 64);
            if (hoveredBorderColor.RGBA == 0) hoveredBorderColor = new(0, 255, 0, 128);
            if (selectedBorderColor.RGBA == 0) selectedBorderColor = new(255, 0, 0, 128);
            if (hoveredHighlightColor.RGBA == 0) hoveredHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.Border, 128);
            if (selectedHighlightColor.RGBA == 0) selectedHighlightColor = ImGuiUtilities.GetFosterColor(ImGuiCol.TextSelectedBg, 128);
            if (activeSpawnColor.RGBA == 0) activeSpawnColor = new(192, 192, 0, 192);
            if (inactiveSpawnColor.RGBA == 0) inactiveSpawnColor = new(32, 32, 0, 32);
        }

        public override void Run()
        {
            if (!isOpen) return;

            if (ImGui.Begin(Name, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) || isSpawnEditorFocused;

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
                            map = JsonSerializer.Deserialize<Map>(File.ReadAllText(currentMapPath), Manager.SerializerOptions);
                            tileset = null;
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
                            File.WriteAllText(currentMapPath = s, JsonSerializer.Serialize(map, Manager.SerializerOptions));
                    }), [new("JSON files (*.json)", "json")], currentMapPath);
                }
                if (map == null) ImGui.EndDisabled();
                ImGui.SameLine();

                if (map != null)
                {
                    ImGui.SameLine();
                    ImGuiUtilities.InfoPopUp(
                        "Recommended layer setup:\n" +
                        "- 0: Ground, water, etc.; cells must *not* have transparency\n" +
                        "- 1: Decorative cells with transparency, ex. grass, flowers, etc.\n" +
                        "- 2: Walls, fences, etc.; all actors (player, collectibles, etc.)\n" +
                        "- 3: Bridges, certain chunks of wall (again!), etc., so that the player appears behind them\n" +
                        "- 4: Further decorative cells, plus \"shadows\" cast by layer 3 terrain (from ex. bridges)\n" +
                        "\n" +
                        "Layer 4 can be considered optional, if no decorative elements are wanted for layer 3.\n" +
                        "Layer 3 *could* also be considered optional, *but* the player might appear in front of cells they shouldn't.\n" +
                        "\n" +
                        "If there are no walls or similar complex structures on the map, layers 3 and 4 are basically optional.",
                        "Editing Help",
                        "Tips and Tricks");

                    var currentMapLabel = $"Current map: {(string.IsNullOrWhiteSpace(currentMapPath) ? "unsaved" : currentMapPath)}";
                    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(currentMapLabel).X);
                    ImGui.Text(currentMapLabel);
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
                    {
                        layersDirty = true;
                        tilesetDirty = true;
                    }
                    ImGui.InputText("Intro ID", ref map.IntroID, 128);
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
                    ImGui.NewLine();
                    if (ImGui.Button("Spawn editor")) { isSpawnEditorOpen = true; ImGui.SetWindowFocus(spawnEditorName); }
                    ImGui.EndGroup();
                    ImGui.SameLine();

                    ImGui.Dummy(new(10f, 0f));
                    ImGui.SameLine();

                    ImGui.BeginGroup();
                    if (map.Layers.Count == 0) ImGui.BeginDisabled();
                    ImGui.SliderInt("Active layer", ref activeLayer, 0, map.Layers.Count == 0 ? 0 : map.Layers.Count - 1);
                    if (map.Layers.Count == 0) ImGui.EndDisabled();
                    ImGui.Checkbox("Dim inactive layers", ref dimInactiveLayers);
                    ImGui.NewLine();
                    ImGui.Checkbox("Draw map grid", ref drawMapCellGrid);
                    ImGui.SameLine();
                    ImGui.Checkbox("Draw tileset grid", ref drawTilesetCellGrid);
                    ImGui.EndGroup();

                    if (layersDirty)
                    {
                        hoveredMapCell = -1;
                        map.ResizeLayers();
                        activeLayer = Math.Min(activeLayer, map.Layers.Count - 1);
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

                        var mapScrollHeight = tileset.CellSize.Y * 14 * tilesetZoom;
                        var tileScrollWidth = tileset.CellSize.X * tileSelectorWidth * tilesetZoom + style.ScrollbarSize;

                        ImGui.BeginGroup();
                        if (ImGui.BeginChild("mapscroll", new(1250f - tileScrollWidth, mapScrollHeight + style.ScrollbarSize), ImGuiChildFlags.AlwaysAutoResize | ImGuiChildFlags.AutoResizeY, ImGuiWindowFlags.AlwaysHorizontalScrollbar))
                        {
                            ImGui.BeginGroup();

                            var imagePos = ImGui.GetCursorScreenPos();
                            if (manager.ImGuiRenderer.BeginBatch(new(map.Size.X * tileset.CellSize.X * mapZoom, map.Size.Y * tileset.CellSize.Y * mapZoom), out var batcher, out var bounds))
                            {
                                batcher.CheckeredPattern(bounds, 8, 8, Color.DarkGray, Color.Gray);
                                for (var i = 0; i < map.Layers.Count; i++)
                                {
                                    for (var y = 0; y < map.Size.Y; y++)
                                    {
                                        for (var x = 0; x < map.Size.X; x++)
                                        {
                                            var cellPos = new Vector2(x, y) * tileset.CellSize * mapZoom;
                                            var cellOffset = y * map.Size.X + x;
                                            var cellValue = map.Layers[i].Tiles[cellOffset];

                                            batcher.PushBlend(BlendMode.NonPremultiplied);
                                            batcher.Image(tileset.CellTextures[cellValue], cellPos, Vector2.Zero, new(mapZoom), 0f, i == activeLayer || !dimInactiveLayers ? Color.White : inactiveLayerColor);
                                            batcher.PopBlend();

                                            var cellRect = new Rect(cellPos, tileset.CellSize * mapZoom);
                                            if (drawMapCellGrid)
                                                batcher.RectLine(cellRect, 1f, gridColor);

                                            foreach (var spawn in map.Spawns)
                                            {
                                                if (spawn.Position == (x, y))
                                                {
                                                    if ((i == spawn.MapLayer && i == activeLayer) || !dimInactiveLayers)
                                                    {
                                                        batcher.Rect(cellRect, activeSpawnColor);
                                                        break;
                                                    }
                                                    else
                                                        batcher.Rect(cellRect, inactiveSpawnColor);
                                                }
                                            }

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
                        }
                        ImGui.EndChild();
                        ImGui.EndGroup();

                        ImGui.SameLine();

                        ImGui.BeginGroup();
                        if (ImGui.BeginChild("tilescroll", new(tileScrollWidth, mapScrollHeight), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar))
                        {
                            ImGui.BeginGroup();

                            var tilesetViewSize = new Vector2(tileset.CellSize.X * tileSelectorWidth, tileset.CellFlags.Length / tileSelectorWidth * tileset.CellSize.Y);

                            var imagePos = ImGui.GetCursorScreenPos();
                            if (manager.ImGuiRenderer.BeginBatch(tilesetViewSize * tilesetZoom, out var batcher, out var bounds))
                            {
                                batcher.CheckeredPattern(bounds, 8, 8, Color.DarkGray, Color.Gray);
                                for (var i = 0; i < tileset.TilesheetSizeInCells.X * tileset.TilesheetSizeInCells.Y; i++)
                                {
                                    var cellPos = new Vector2(i % tileSelectorWidth, i / tileSelectorWidth) * tilesetZoom * tileset.CellSize;
                                    var cellFlags = tileset.CellFlags[i];

                                    batcher.PushBlend(BlendMode.NonPremultiplied);
                                    batcher.Image(tileset.CellTextures[i], cellPos, Vector2.Zero, new(tilesetZoom), 0f, cellFlags.Has(CellFlag.Translucent) ? new(255, 255, 255, 160) : Color.White);
                                    batcher.PopBlend();

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
                                var cellPos = imagePos + new Vector2(i % tileSelectorWidth, i / tileSelectorWidth) * tilesetZoom * tileset.CellSize;
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

                        ImGui.BeginGroup();
                        if (ImGui.Button("Fill active layer")) ImGui.OpenPopup("Fill");

                        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new(0.5f));
                        if (map != null && ImGui.BeginPopupModal("Fill", ImGuiWindowFlags.AlwaysAutoResize))
                        {
                            ImGui.Text("Really fill layer? This is destructive!");
                            ImGui.Separator();
                            if (ImGui.Button("Yes")) { Array.Fill(map.Layers[activeLayer].Tiles, selectedTilemapCell); ImGui.CloseCurrentPopup(); }
                            ImGui.SameLine();
                            ImGui.SetItemDefaultFocus();
                            if (ImGui.Button("No")) ImGui.CloseCurrentPopup();
                            ImGui.EndPopup();
                        }
                        ImGui.EndGroup();
                    }

                    RunSpawnEditor();
                }
                ImGui.EndGroup();
            }
            isCollapsed = ImGui.IsWindowCollapsed();
            ImGui.End();
        }

        private void RunSpawnEditor()
        {
            if (!isSpawnEditorOpen || map == null) return;

            if (ImGui.Begin(spawnEditorName, ref isSpawnEditorOpen, ImGuiWindowFlags.AlwaysAutoResize))
            {
                isSpawnEditorFocused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);

                ImGui.BeginGroup();
                var currentSpawnCount = map.Spawns.Count;
                if (currentSpawnCount == 0) ImGui.BeginDisabled();
                ImGui.SliderInt("Selected spawn", ref selectedSpawn, 0, map.Spawns.Count == 0 ? 0 : map.Spawns.Count - 1);
                if (currentSpawnCount == 0) ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.Dummy(new(10f, 0f));
                ImGui.SameLine();
                if (ImGui.Button("Add new spawn")) map.Spawns.Add(new());
                if (currentSpawnCount <= 0) ImGui.BeginDisabled();
                ImGui.SameLine();
                if (ImGui.Button("Remove selected spawn"))
                {
                    map.Spawns.RemoveAt(selectedSpawn);
                    selectedSpawn = Math.Clamp(selectedSpawn - 1, 0, map.Spawns.Count);
                }
                if (currentSpawnCount <= 0) ImGui.EndDisabled();
                ImGui.EndGroup();

                if (map.Spawns.Count != 0)
                {
                    var spawn = map.Spawns[selectedSpawn];

                    ImGui.Separator();

                    ImGui.BeginGroup();
                    ImGui.InputText("Actor type", ref spawn.ActorType, 64);
                    ImGui.SameLine();
                    ImGui.SliderInt("Position X", ref spawn.Position.X, 0, map.Size.X - 1);
                    ImGui.SliderInt("Map layer", ref spawn.MapLayer, 0, map.Layers.Count - 1);
                    ImGui.SameLine();
                    ImGui.SliderInt("Position Y", ref spawn.Position.Y, 0, map.Size.Y - 1);
                    ImGui.EndGroup();
                }
            }
            ImGui.End();
        }
    }
}
