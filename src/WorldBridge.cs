using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SCP096TheShyGuy
{
    // Runtime feature detection: do not bind to an uninspected Assembly-CSharp at compile-time.
    // For each unsupported engine operation, fail closed and write a diagnostic.
    internal sealed class WorldBridge
    {
        internal const string ScpId = "scp096_shyguy";
        private object _actorLibrary;
        private bool _assetRegistered;
        private string _lastMoveSignature = "not discovered";
        private string _lastKillSignature = "not discovered";
        internal string LastMoveSignature => _lastMoveSignature;
        internal string LastKillSignature => _lastKillSignature;

        internal object Map => Reflect.Member(Reflect.GameType("MapBox"), "instance");
        internal object Units => Reflect.Member(Map, "units");
        internal bool HasMap => Map != null && Units != null && GetTiles() != null;
        internal bool AssetRegistered => _assetRegistered;
        internal string Readiness => Map == null ? "MapBox.instance indisponivel" :
            Units == null ? "MapBox.units indisponivel" :
            GetTiles() == null ? "MapBox.tiles_map indisponivel" : "Mapa pronto";

        private Array GetTiles() => Reflect.Member(Map, "tiles_map") as Array;

        internal object Tile(int x, int y)
        {
            Array tiles = GetTiles();
            if (tiles == null || tiles.Rank != 2 || x < tiles.GetLowerBound(0) ||
                y < tiles.GetLowerBound(1) || x > tiles.GetUpperBound(0) || y > tiles.GetUpperBound(1))
                return null;
            return tiles.GetValue(x, y);
        }

        internal bool TryRegisterAsset(string modFolder, out string reason)
        {
            reason = "";
            try
            {
                object managerType = Reflect.GameType("AssetManager");
                _actorLibrary = Reflect.Member(managerType, "actor_library");
                if (_actorLibrary == null) { reason = "AssetManager.actor_library unavailable"; return false; }
                var get = Reflect.Find(_actorLibrary, "get", p => p.Length == 1 && p[0].ParameterType == typeof(string));
                if (get == null) { reason = "actor_library.get(string) unavailable"; return false; }
                if (Reflect.Call(_actorLibrary, get, ScpId) != null) { _assetRegistered = true; return true; }
                if (Reflect.Call(_actorLibrary, get, "human") == null)
                { reason = "Base actor 'human' unavailable"; return false; }
                // clone(newId, sourceId) is present in public NML AssetLinker examples.
                var clone = Reflect.Find(_actorLibrary, "clone", p =>
                    p.Length == 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(string));
                if (clone == null) { reason = "ActorAssetLibrary.clone(string,string) unavailable"; return false; }
                object asset = Reflect.Call(_actorLibrary, clone, ScpId, "human");
                if (asset == null || Convert.ToString(Reflect.Member(asset, "id")) != ScpId)
                { reason = "ActorAsset clone returned unexpected ID; registration aborted"; return false; }
                // Deliberately conservative. Cloning copies the human texture until texture API is inspected.
                Reflect.Set(asset, "color_hex", "#EEEFF0");
                Reflect.Set(asset, "name_locale", "scp096_shyguy");
                Reflect.Set(asset, "can_turn_into_zombie", false);
                Reflect.Set(asset, "can_have_subspecies", false);
                Reflect.Set(asset, "can_evolve_into_new_species", false);
                Reflect.Set(asset, "damaged_by_ocean", false);
                Reflect.Set(asset, "die_in_lava", false);
                Reflect.Set(asset, "can_be_killed_by_life_eraser", true); // admin removal stays enabled
                Reflect.Set(asset, "has_advanced_textures", false);
                SetBaseStat(asset, "health", 10000f);
                SetBaseStat(asset, "lifespan", 10000f);
                SetBaseStat(asset, "damage", 250f);
                SetBaseStat(asset, "speed", 12f);
                SetupCustomTexture(asset, modFolder);
                _assetRegistered = true;
                return true;
            }
            catch (Exception ex) { reason = ex.GetBaseException().Message; return false; }
        }


        internal bool SetBaseStat(object actorAsset, string name, float value)
        {
            object stats = Reflect.Member(actorAsset, "base_stats");
            if (stats == null) return false;
            if (stats is IDictionary map)
            {
                try { map[name] = value; return true; } catch { }
            }
            var indexer = stats.GetType().GetProperties(Reflect.Any).FirstOrDefault(p =>
                p.Name == "Item" && p.CanWrite && p.GetIndexParameters().Length == 1 &&
                p.GetIndexParameters()[0].ParameterType == typeof(string));
            try
            {
                if (indexer != null) { indexer.SetValue(stats, value, new object[] { name }); return true; }
            }
            catch { }
            return false;
        }

        internal bool TrySetSpeed(float speed)
        {
            if (_actorLibrary == null) return false;
            var get = Reflect.Find(_actorLibrary, "get", p => p.Length == 1 && p[0].ParameterType == typeof(string));
            object asset = Reflect.Call(_actorLibrary, get, ScpId);
            // Changing the shared asset may not recalculate live actor stats on every game build.
            return asset != null && SetBaseStat(asset, "speed", speed);
        }

        private void SetupCustomTexture(object asset, string root)
        {
            if (string.IsNullOrEmpty(root)) return;
            string spriteFile = Path.Combine(root, "GameResources", "actors", ScpId, "Main", "idle_1.png");
            if (!File.Exists(spriteFile)) return;
            try
            {
                // A cached icon is helpful even if the build does not load the full custom atlas.
                Texture2D tex = new Texture2D(32, 48, TextureFormat.RGBA32, false);
                var type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                var loader = type == null ? null : type.GetMethods(Reflect.Any)
                    .FirstOrDefault(m => m.Name == "LoadImage" && m.GetParameters().Length == 3);
                if (loader != null)
                {
                    bool ok = Convert.ToBoolean(loader.Invoke(null, new object[] { tex, File.ReadAllBytes(spriteFile), false }));
                    if (ok)
                    {
                        tex.filterMode = FilterMode.Point;
                        var sprite = Sprite.Create(tex, new Rect(0, 0, 32, 48), new Vector2(0.5f, 0f), 32f);
                        Reflect.Set(asset, "_cached_sprite", sprite);
                    }
                }
                Type subType = Reflect.GameType("ActorTextureSubAsset");
                if (subType == null) return;
                var constructor = subType.GetConstructor(Reflect.Any, null, new[] { typeof(string), typeof(bool) }, null);
                if (constructor == null) return;
                object textures = constructor.Invoke(new object[] { "actors/" + ScpId + "/", false });
                if (!Reflect.Set(asset, "texture_asset", textures)) return;
                Reflect.Set(asset, "animation_idle", new[] { "idle_1" });
                Reflect.Set(asset, "animation_walk", new[] { "walk_1", "walk_2" });
                var loadSprites = Reflect.Find(_actorLibrary, "loadTexturesAndSprites", p =>
                    p.Length == 1 && p[0].ParameterType.IsInstanceOfType(asset));
                if (loadSprites != null) Reflect.Call(_actorLibrary, loadSprites, asset);
            }
            catch (Exception ex) { Debug.LogWarning("[SCP096] Custom texture fallback: " + ex.GetBaseException().Message); }
        }

        internal IEnumerable<object> Actors()
        {
            IEnumerable units = Reflect.Enumerate(Units);
            if (units == null) yield break;
            foreach (object actor in units)
                if (actor != null) yield return actor;
        }

        internal string AssetId(object actor)
        {
            var asset = Reflect.Member(actor, "asset");
            var id = Reflect.Member(asset, "id");
            if (id != null) return Convert.ToString(id);
            var data = Reflect.Member(actor, "data");
            return Convert.ToString(Reflect.Member(data, "asset_id") ?? "");
        }

        internal object FindExistingScp()
        {
            foreach (object actor in Actors())
                if (AssetId(actor) == ScpId) return actor;
            return null;
        }

        internal bool Exists(object actor)
        {
            if (actor == null) return false;
            foreach (object current in Actors())
                if (ReferenceEquals(current, actor)) return true;
            return false;
        }

        internal object ActorTile(object actor) => Reflect.Member(actor, "current_tile")
            ?? Reflect.Member(actor, "currentTile") ?? Reflect.Member(actor, "tile");

        internal bool Position(object actor, out float x, out float y)
        {
            object tile = ActorTile(actor);
            return PositionOfTile(tile, out x, out y);
        }

        private bool PositionOfTile(object tile, out float x, out float y)
        {
            x = y = 0;
            if (tile == null) return false;
            object xo = Reflect.Member(tile, "x");
            object yo = Reflect.Member(tile, "y");
            if (xo == null || yo == null) return false;
            try { x = Convert.ToSingle(xo); y = Convert.ToSingle(yo); return true; }
            catch { return false; }
        }

        internal object MouseTile()
        {
            return ScreenTile(Input.mousePosition);
        }

        internal object ScreenTile(Vector3 screenPosition)
        {
            if (Camera.main == null || GetTiles() == null) return null;
            Vector3 pos = Camera.main.ScreenToWorldPoint(screenPosition);
            return Tile(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        }

        // Test-friendly: predictable location at screen center rather than hidden in the world.
        internal object VisibleTile()
        {
            object centered = ScreenTile(new Vector3(Screen.width * .5f, Screen.height * .5f, 0));
            if (centered != null && !LooksLikeWater(centered)) return centered;
            // Nearby positions are more discoverable than a randomly chosen distant tile.
            if (centered != null && PositionOfTile(centered, out float x, out float y))
            {
                int cx = Mathf.RoundToInt(x), cy = Mathf.RoundToInt(y);
                for (int r = 1; r <= 15; r++)
                    for (int dx = -r; dx <= r; dx += 2)
                        for (int dy = -r; dy <= r; dy += 2)
                        {
                            object tile = Tile(cx + dx, cy + dy);
                            if (tile != null && !LooksLikeWater(tile)) return tile;
                        }
            }
            return null;
        }

        internal string TileCoordinates(object tile)
        {
            return PositionOfTile(tile, out float x, out float y)
                ? "(" + x.ToString("0") + ", " + y.ToString("0") + ")" : "desconhecidas";
        }

        internal string DiagnosticReadiness()
        {
            var m = Map;
            var u = Units;
            var tiles = GetTiles();
            object lib = Reflect.Member(Reflect.GameType("AssetManager"), "actor_library");
            var clone = Reflect.Find(lib, "clone", p => p.Length == 2);
            var spawn = Reflect.Find(u, "spawnNewUnit", p => p.Length >= 2);
            return "MapBox=" + (m != null) + ", units=" + (u != null) +
                ", tiles_map=" + (tiles == null ? "null" : tiles.GetLength(0) + "x" + tiles.GetLength(1)) +
                ", actor_library=" + (lib != null) +
                ", actor clone=" + (clone == null ? "missing" : Reflect.Signature(clone)) +
                ", spawnNewUnit=" + (spawn == null ? "missing" : Reflect.Signature(spawn)) +
                ", assetRegistered=" + _assetRegistered;
        }

        internal object IsolatedTile()
        {
            var tiles = GetTiles();
            if (tiles == null) return null;
            int w = tiles.GetLength(0), h = tiles.GetLength(1);
            if (w < 3 || h < 3) return null;
            // Deterministic bounded sampling. Start away from the map centre.
            for (int i = 0; i < 320; i++)
            {
                int x = UnityEngine.Random.Range(1, w - 1);
                int y = UnityEngine.Random.Range(1, h - 1);
                float dx = x - w * 0.5f, dy = y - h * 0.5f;
                if (dx * dx + dy * dy < (Mathf.Min(w, h) * 0.17f) * (Mathf.Min(w, h) * 0.17f))
                    continue;
                var tile = Tile(x, y);
                if (tile != null && !LooksLikeWater(tile)) return tile;
            }
            return null; // fail closed instead of spawning in an ocean
        }

        private bool LooksLikeWater(object tile)
        {
            object main = Reflect.Member(tile, "main_type") ?? Reflect.Member(tile, "tile_type");
            object id = Reflect.Member(main, "id");
            if (id == null) return false; // unknown; don't block all spawn attempts
            string value = Convert.ToString(id).ToLowerInvariant();
            return value.Contains("ocean") || value.Contains("water") || value.Contains("lava") || value.Contains("pit");
        }

        internal object SpawnAt(object tile, out string reason)
        {
            if (!_assetRegistered) { reason = "Actor asset not registered"; return null; }
            return SpawnIdAt(ScpId, tile, out reason);
        }

        // The vanilla-human test is opt-in and diagnostic only. It never pretends to be an SCP.
        internal object SpawnTestHuman(object tile, out string reason) => SpawnIdAt("human", tile, out reason);

        private object SpawnIdAt(string id, object tile, out string reason)
        {
            reason = "";
            if (tile == null || Units == null) { reason = "No valid tile or actor manager"; return null; }
            var spawn = Reflect.Find(Units, "spawnNewUnit", p => p.Length >= 2 &&
                p[0].ParameterType == typeof(string) && p[1].ParameterType.IsInstanceOfType(tile));
            if (spawn == null) { reason = "units.spawnNewUnit signature unavailable"; return null; }
            var parameters = spawn.GetParameters();
            object[] args = new object[parameters.Length];
            args[0] = id;
            args[1] = tile;
            for (int i = 2; i < args.Length; i++)
            {
                // WorldBox 0.51.2 defaults adult=false: force adult SCP instead of an infant.
                // Match by parameter name so other game builds can still use their defaults.
                if (parameters[i].Name == "adult" && parameters[i].ParameterType == typeof(bool))
                    args[i] = true;
                else if (parameters[i].HasDefaultValue && parameters[i].DefaultValue != DBNull.Value &&
                         parameters[i].DefaultValue != Type.Missing)
                    args[i] = parameters[i].DefaultValue;
                else if (parameters[i].ParameterType == typeof(float) && i == 4) args[i] = 6f;
                else if (parameters[i].ParameterType.IsValueType)
                    args[i] = Activator.CreateInstance(parameters[i].ParameterType);
                else args[i] = null;
            }
            try
            {
                object spawned = Reflect.Call(Units, spawn, args);
                if (spawned == null) reason = "spawnNewUnit retornou null para '" + id + "'";
                return spawned;
            }
            catch (Exception ex) { reason = ex.GetBaseException().Message; return null; }
        }

        internal object NearestVictim(object scp, object fromTile, float maxDistance)
        {
            if (!PositionOfTile(fromTile, out var cx, out var cy)) return null;
            object best = null;
            double score = maxDistance * maxDistance;
            foreach (object actor in Actors())
            {
                if (ReferenceEquals(actor, scp) || AssetId(actor) == ScpId) continue;
                if (!Position(actor, out var x, out var y)) continue;
                double distance = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                if (distance < score) { best = actor; score = distance; }
            }
            return best;
        }

        internal bool Within(object a, object b, float distance)
        {
            if (!Position(a, out var ax, out var ay) || !Position(b, out var bx, out var by)) return false;
            return (ax - bx) * (ax - bx) + (ay - by) * (ay - by) <= distance * distance;
        }

        // Uses only native movement methods discovered by signature; no Transform warp/desync.
        internal bool TryChase(object scp, object victim)
        {
            object tile = ActorTile(victim);
            if (tile == null || scp == null) return false;
            foreach (object receiver in new[] { scp, Reflect.Member(scp, "ai"), Reflect.Member(scp, "brain") })
            {
                if (receiver == null) continue;
                foreach (string name in new[] { "goTo", "goToTile", "tryGoTo", "moveTo", "setMoveTarget" })
                {
                    var method = Reflect.Find(receiver, name, p => p.Length == 1 && p[0].ParameterType.IsInstanceOfType(tile));
                    if (method == null) continue;
                    try
                    {
                        object outcome = Reflect.Call(receiver, method, tile);
                        if (outcome is bool && !(bool)outcome) continue;
                        _lastMoveSignature = receiver.GetType().Name + "." + Reflect.Signature(method);
                        return true;
                    }
                    catch { /* Try the next supported signature without spamming logs. */ }
                }
            }
            _lastMoveSignature = "UNRESOLVED: no confirmed native movement signature";
            return false;
        }

        internal bool TryKill(object victim)
        {
            if (victim == null) return false;
            // Death goes through Actor's own lifecycle only; never writes zero HP blindly.
            foreach (string name in new[] { "killHimself", "die" })
            {
                var method = Reflect.Find(victim, name, p => p.Length == 0);
                if (method == null) continue;
                try
                {
                    object result = Reflect.Call(victim, method);
                    if (result is bool && !(bool)result) continue;
                    _lastKillSignature = Reflect.Signature(method);
                    return true;
                }
                catch { }
            }
            _lastKillSignature = "UNRESOLVED: no safe zero-argument Actor death method";
            return false;
        }

        internal string DiagnosticMethods(object actor)
        {
            if (actor == null) return "No SCP actor available";
            object ai = Reflect.Member(actor, "ai");
            string[] tokens = { "move", "path", "goto", "target", "kill", "die", "health", "damage" };
            var methods = actor.GetType().GetMethods(Reflect.Any)
                .Where(m => tokens.Any(t => m.Name.ToLowerInvariant().Contains(t)))
                .Select(Reflect.Signature).Distinct().Take(36).ToArray();
            var aiMethods = ai == null ? new string[0] : ai.GetType().GetMethods(Reflect.Any)
                .Where(m => tokens.Any(t => m.Name.ToLowerInvariant().Contains(t)))
                .Select(Reflect.Signature).Distinct().Take(28).ToArray();
            return "Actor: " + string.Join(" | ", methods) + "\nAI: " + string.Join(" | ", aiMethods);
        }
    }
}
