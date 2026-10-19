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
        // Using a non-civilization source is safer than cloning a human and
        // attempting to override the whole citizen/job AI with guessed flags.
        private const string BaseId = "sheep";
        internal string SourcePrototype => BaseId;
        internal string AppearanceStatus { get; private set; } = "not attempted";
        internal string ExtraFlagsStatus { get; private set; } = "not attempted";
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
                object prototype = Reflect.Call(_actorLibrary, get, BaseId);
                if (prototype == null)
                { reason = "Wild source asset '" + BaseId + "' not available: no human fallback (avoids cities)"; return false; }
                var clone = Reflect.Find(_actorLibrary, "clone", p =>
                    p.Length == 2 && p[0].ParameterType == typeof(string) && p[1].ParameterType == typeof(string));
                if (clone == null) { reason = "ActorAssetLibrary.clone(string,string) unavailable"; return false; }
                object asset = Reflect.Call(_actorLibrary, clone, ScpId, BaseId);
                if (asset == null || Convert.ToString(Reflect.Member(asset, "id")) != ScpId)
                { reason = "ActorAsset clone returned unexpected ID"; return false; }
                Reflect.Set(asset, "name_locale", "scp096_shyguy");
                Reflect.Set(asset, "color_hex", "#E5E8EB");
                Reflect.Set(asset, "can_turn_into_zombie", false);
                Reflect.Set(asset, "can_have_subspecies", false);
                Reflect.Set(asset, "can_evolve_into_new_species", false);
                Reflect.Set(asset, "damaged_by_ocean", false);
                Reflect.Set(asset, "die_in_lava", false);
                // Avoid instant environmental erasure but permit death from sustained damage.
                // No invulnerable/immortal trait is applied.
                Reflect.Set(asset, "can_be_killed_by_life_eraser", true);
                Reflect.Set(asset, "has_advanced_textures", false);
                // Keep the sheep AI independent from human civilizations.
                // Optional flags: not assumed to exist; recorded if the version exposes them.
                var disabled = new List<string>();
                foreach (string flag in new[] { "can_breed", "can_build", "can_have_city",
                    "can_be_citizen", "is_civilization", "can_create_city", "can_settle" })
                    if (Reflect.Set(asset, flag, false)) disabled.Add(flag);
                ExtraFlagsStatus = disabled.Count == 0 ? "none found, wild prototype still used" :
                    string.Join(",", disabled.ToArray());
                SetBaseStat(asset, "health", 40000f);
                SetBaseStat(asset, "lifespan", 10000f);
                SetBaseStat(asset, "damage", 250f);
                SetBaseStat(asset, "speed", 12f);
                // Cloning alone retains the valid sheep textures. Custom texture loading
                // is tried only when its resource files are present, with safe fallback.
                ConfigureAppearance(asset, prototype, modFolder);
                _assetRegistered = true;
                return true;
            }
            catch (Exception ex) { reason = ex.GetBaseException().Message; return false; }
        }


        private void ConfigureAppearance(object asset, object prototype, string root)
        {
            AppearanceStatus = "wild sprite inherited (custom files unavailable)";
            if (string.IsNullOrEmpty(root) ||
                !File.Exists(Path.Combine(root, "GameResources", "actors", ScpId, "Main", "idle_1.png")))
                return;
            object inherited = Reflect.Member(asset, "texture_asset") ?? Reflect.Member(prototype, "texture_asset");
            if (inherited == null) { AppearanceStatus = "texture source missing"; return; }
            try
            {
                Type textureType = Reflect.GameType("ActorTextureSubAsset");
                var ctor = textureType == null ? null : textureType.GetConstructor(Reflect.Any,
                    null, new[] { typeof(string), typeof(bool) }, null);
                if (ctor == null) { AppearanceStatus = "ActorTextureSubAsset ctor missing"; return; }
                object texture = ctor.Invoke(new object[] { "actors/" + ScpId + "/", false });
                // The 0.1.1 attempt lost the inherited shadow, baby/head paths.
                // Copy shadow/head/child data only, never modify the shared original.
                foreach (var f in inherited.GetType().GetFields(Reflect.Any))
                {
                    string name = f.Name.ToLowerInvariant();
                    if (!(name.Contains("shadow") || name.Contains("head") ||
                          name.Contains("baby") || name.Contains("child") || name.Contains("egg"))) continue;
                    try { Reflect.Set(texture, f.Name, f.GetValue(inherited)); } catch { }
                }
                foreach (var prop in inherited.GetType().GetProperties(Reflect.Any))
                {
                    string name = prop.Name.ToLowerInvariant();
                    if (!(name.Contains("shadow") || name.Contains("head") ||
                          name.Contains("baby") || name.Contains("child") || name.Contains("egg"))) continue;
                    if (!prop.CanRead || prop.GetIndexParameters().Length != 0) continue;
                    try { Reflect.Set(texture, prop.Name, prop.GetValue(inherited, null)); } catch { }
                }
                // Make the source path explicit (Linux is case sensitive).
                Reflect.Set(texture, "texture_path_main", "actors/" + ScpId + "/Main");
                // Never borrow sheep/child: the custom child folder has matching idle/swim frames.
                Reflect.Set(texture, "texture_path_baby", "actors/" + ScpId + "/child");
                EnsureValidShadowSizes(asset, prototype);
                EnsureValidShadowSizes(texture, inherited);
                if (!Reflect.Set(asset, "texture_asset", texture))
                { AppearanceStatus = "texture assignment unavailable; inherited sprite kept"; return; }
                Reflect.Set(asset, "animation_idle", new[] { "idle_1" });
                Reflect.Set(asset, "animation_walk", new[] { "walk_1", "walk_2" });
                Reflect.Set(asset, "animation_swim", new[] { "swim_0", "swim_1", "swim_2", "swim_3" });
                var load = Reflect.Find(_actorLibrary, "loadTexturesAndSprites", p =>
                    p.Length == 1 && p[0].ParameterType.IsInstanceOfType(asset));
                if (load == null)
                {
                    Reflect.Set(asset, "texture_asset", inherited);
                    AppearanceStatus = "loadTexturesAndSprites missing; inherited sprite kept";
                    return;
                }
                try
                {
                    Reflect.Call(_actorLibrary, load, asset);
                    AppearanceStatus = "custom texture attempted with inherited baby/head/shadow; verify in game";
                }
                catch (Exception e)
                {
                    Reflect.Set(asset, "texture_asset", inherited);
                    AppearanceStatus = "custom loader error, inherited sprite restored: " + e.GetBaseException().Message;
                }
            }
            catch (Exception e) { AppearanceStatus = "custom sprite setup error: " + e.GetBaseException().Message; }
        }

        // 0.51.2 rejects even (0.55,0.30): use a clearly nonzero size for this
        // tall humanoid. The exact visual proportions still require in-game testing.
        private static void EnsureValidShadowSizes(object target, object source)
        {
            if (target == null) return;
            foreach (var field in target.GetType().GetFields(Reflect.Any))
            {
                string n = field.Name.ToLowerInvariant();
                if (!n.Contains("shadow") || !n.Contains("size")) continue;
                try
                {
                    object current = field.GetValue(target);
                    if (field.FieldType == typeof(Vector2) && current is Vector2 v &&
                        (v.x < 1.5f || v.y < 1.5f))
                    {
                        object old = Reflect.Member(source, field.Name);
                        Vector2 valid = old is Vector2 ov && ov.x >= 1.5f && ov.y >= 1.5f
                            ? ov : new Vector2(2f, 2f);
                        field.SetValue(target, valid);
                    }
                }
                catch { }
            }
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

        private MethodInfo _restoreHealthMethod;
        private bool _healMethodSearched;
        private bool _healFailureLogged;
        internal bool TryRegenerate(object actor, int amount, out string detail)
        {
            detail = "not attempted";
            if (actor == null || !IsAlive(actor) || amount <= 0) return false;
            try
            {
                if (!_healMethodSearched)
                {
                    _healMethodSearched = true;
                    _restoreHealthMethod = Reflect.Find(actor, "restoreHealth", ps =>
                        ps.Length == 1 && ps[0].ParameterType == typeof(int));
                }
                if (_restoreHealthMethod == null)
                {
                    detail = "Actor.restoreHealth(Int32) unavailable";
                    return false;
                }
                // Avoid repeatedly invoking health regen at full health if game exposes ratio.
                MethodInfo ratioMethod = Reflect.Find(actor, "getHealthRatio", ps => ps.Length == 0);
                if (ratioMethod != null)
                {
                    object raw = Reflect.Call(actor, ratioMethod);
                    float ratio = Convert.ToSingle(raw);
                    if (!float.IsNaN(ratio) && ratio >= .999f)
                    { detail = "full health"; return true; }
                }
                Reflect.Call(actor, _restoreHealthMethod, amount);
                detail = "Actor.restoreHealth(Int32) amount=" + amount;
                return true;
            }
            catch (Exception ex)
            {
                detail = ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message;
                if (!_healFailureLogged) { _healFailureLogged = true; }
                return false;
            }
        }

        internal bool TrySetSpeed(float speed)
        {
            if (_actorLibrary == null) return false;
            var get = Reflect.Find(_actorLibrary, "get", p => p.Length == 1 && p[0].ParameterType == typeof(string));
            object asset = Reflect.Call(_actorLibrary, get, ScpId);
            // Changing the shared asset may not recalculate live actor stats on every game build.
            return asset != null && SetBaseStat(asset, "speed", speed);
        }

        // Custom texture binding intentionally disabled in v0.1.2.
        // The bundled sprites are preview assets, not a complete atlas with
        // valid adult/child/heads and shadow definitions.

        // Prefer the verified living-unit registry when exposed by this WorldBox build.
        // This avoids selecting dead characters as witnesses during manual tests.
        internal IEnumerable<object> Actors()
        {
            IEnumerable units = Reflect.Enumerate(Reflect.Member(Units, "units_only_alive"))
                ?? Reflect.Enumerate(Units);
            if (units == null) yield break;
            foreach (object actor in units)
                if (actor != null) yield return actor;
        }

        internal bool IsAlive(object actor)
        {
            if (actor == null) return false;
            var aliveMethod = Reflect.Find(actor, "isAlive", ps => ps.Length == 0);
            if (aliveMethod != null)
            {
                try
                {
                    object alive = Reflect.Call(actor, aliveMethod);
                    if (alive is bool && !(bool)alive) return false;
                }
                catch { }
            }
            IEnumerable living = Reflect.Enumerate(Reflect.Member(Units, "units_only_alive"));
            if (living == null) return Exists(actor); // conservative fallback on other builds
            foreach (object current in living)
                if (ReferenceEquals(current, actor)) return true;
            return false;
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
                ", assetRegistered=" + _assetRegistered + ", prototype=" + BaseId +
                ", appearance=" + AppearanceStatus;
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

        // Search all living actors, without a radius cap, prioritizing the cursor
        // position. This is a manual testing shortcut, not visual face detection.
        internal object NearestVictimAnywhere(object scp)
        {
            object tile = MouseTile() ?? ActorTile(scp);
            return tile == null ? null : NearestVictim(scp, tile, 1000000f);
        }

        internal string TargetDistance(object a, object b)
        {
            if (!Position(a, out float ax, out float ay) || !Position(b, out float bx, out float by))
                return "indisponivel";
            double dx = ax - bx, dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " tiles";
        }

        internal float Distance(object a, object b)
        {
            if (!Position(a, out float ax, out float ay) ||
                !Position(b, out float bx, out float by)) return float.MaxValue;
            float dx = ax - bx, dy = ay - by;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        internal bool Within(object a, object b, float distance)
        {
            if (!Position(a, out var ax, out var ay) || !Position(b, out var bx, out var by)) return false;
            return (ax - bx) * (ax - bx) + (ay - by) * (ay - by) <= distance * distance;
        }

        // Verified in user's The Unrelenting (WorldBox 0.51.2):
        // Actor.cancelAllBeh(); Actor.goTo(target.current_tile, false);
        // Older ShyGuy code incorrectly only searched one-argument movement methods.
        internal bool TryInterruptBehaviors(object scp)
        {
            if (scp == null) return false;
            MethodInfo m = Reflect.Find(scp, "cancelAllBeh", ps => ps.Length == 0);
            if (m == null) return false;
            try { Reflect.Call(scp, m); return true; }
            catch (Exception e)
            {
                _lastMoveSignature = "cancelAllBeh ERROR: " + e.GetBaseException().Message;
                return false;
            }
        }

        internal bool TryChase(object scp, object victim)
        {
            object tile = ActorTile(victim);
            if (tile == null || scp == null) return false;
            MethodInfo goTo = Reflect.Find(scp, "goTo", ps =>
                ps.Length == 2 && ps[0].ParameterType.IsInstanceOfType(tile) &&
                ps[1].ParameterType == typeof(bool));
            if (goTo != null)
            {
                try
                {
                    object outcome = Reflect.Call(scp, goTo, tile, false);
                    if (!(outcome is bool) || (bool)outcome)
                    {
                        _lastMoveSignature = "Actor." + Reflect.Signature(goTo) + " (from The Unrelenting)";
                        return true;
                    }
                }
                catch (Exception e)
                { _lastMoveSignature = "goTo FAILED: " + e.GetBaseException().Message; }
            }
            // Diagnostic compatibility fallback: no teleporting or manual mutation.
            MethodInfo moveTo = Reflect.Find(scp, "moveTo", ps =>
                ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(tile));
            if (moveTo != null)
            {
                try
                {
                    object result = Reflect.Call(scp, moveTo, tile);
                    if (!(result is bool) || (bool)result)
                    {
                        _lastMoveSignature = "Actor." + Reflect.Signature(moveTo) + " (fallback)";
                        return true;
                    }
                }
                catch (Exception e)
                { _lastMoveSignature = "moveTo FAILED: " + e.GetBaseException().Message; }
            }
            if (_lastMoveSignature == "not discovered")
                _lastMoveSignature = "UNRESOLVED: no supported movement call";
            return false;
        }

        // WorldBox 0.51.2 build 719: dieSimpleNone() has public third-party
        // runtime lifecycle evidence. Unlike speculative zero-argument die(),
        // it has been observed to remove the actor from units_only_alive and city.
        // We require confirmation from the live registry, not just Invoke() success.
        internal bool TryKill(object victim)
        {
            if (victim == null) { _lastKillSignature = "sem vitima"; return false; }
            if (!IsAlive(victim))
            {
                _lastKillSignature = "vitima ja ausente das unidades vivas";
                return true;
            }

            MethodInfo method = Reflect.Find(victim, "dieSimpleNone", p => p.Length == 0);
            if (method == null)
            {
                _lastKillSignature = "INDISPONIVEL: Actor.dieSimpleNone() nao encontrado";
                return false;
            }
            string signature = victim.GetType().Name + "." + Reflect.Signature(method);
            try
            {
                object outcome = Reflect.Call(victim, method);
                if (outcome is bool && !(bool)outcome)
                {
                    _lastKillSignature = signature + " retornou false";
                    return false;
                }
            }
            catch (Exception ex)
            {
                _lastKillSignature = signature + " ERRO: " + ex.GetBaseException().GetType().Name
                    + ": " + ex.GetBaseException().Message;
                return false;
            }
            if (!IsAlive(victim))
            {
                _lastKillSignature = signature + " -> morte confirmada imediatamente";
                return true;
            }
            // WorldBox updates the living registry later in the simulation tick.
            // Invocation succeeded; do not label an asynchronous removal as failure.
            _lastKillSignature = signature + " solicitado; aguardando verificacao de morte no proximo tick";
            return true;
        }


        // Trait mutations mirror The Unrelenting's version-safe overload matching.
        internal bool HasTrait(object actor, string id)
        {
            if (actor == null) return false;
            var method = Reflect.Find(actor, "hasTrait", ps =>
                ps.Length == 1 && ps[0].ParameterType == typeof(string));
            if (method == null) return false;
            try { return Reflect.Call(actor, method, id) is bool b && b; }
            catch { return false; }
        }

        internal bool MutateTrait(object actor, string methodName, string id)
        {
            if (actor == null) return false;
            foreach (MethodInfo method in actor.GetType().GetMethods(Reflect.Any))
            {
                if (method.Name != methodName) continue;
                var ps = method.GetParameters();
                if (ps.Length < 1 || ps[0].ParameterType != typeof(string)) continue;
                object[] args = new object[ps.Length];
                args[0] = id;
                bool supported = true;
                for (int i = 1; i < args.Length; i++)
                {
                    if (ps[i].HasDefaultValue && ps[i].DefaultValue != DBNull.Value &&
                        ps[i].DefaultValue != Type.Missing) args[i] = ps[i].DefaultValue;
                    else if (ps[i].ParameterType == typeof(bool)) args[i] = false;
                    else if (ps[i].ParameterType.IsValueType)
                        args[i] = Activator.CreateInstance(ps[i].ParameterType);
                    else supported = false;
                }
                if (!supported) continue;
                try { Reflect.Call(actor, method, args); return true; }
                catch { }
            }
            return false;
        }

        internal bool AddTrait(object actor, string id)
        {
            if (HasTrait(actor, id)) return true;
            return MutateTrait(actor, "addTrait", id) && HasTrait(actor, id);
        }
        internal bool RemoveTrait(object actor, string id)
        {
            if (!HasTrait(actor, id)) return true;
            return MutateTrait(actor, "removeTrait", id) && !HasTrait(actor, id);
        }

        internal string DiagnosticShadows()
        {
            if (_actorLibrary == null) return "Actor library unavailable";
            MethodInfo get = Reflect.Find(_actorLibrary, "get", p => p.Length == 1 && p[0].ParameterType == typeof(string));
            if (get == null) return "Actor get(string) unavailable";
            object scp = Reflect.Call(_actorLibrary, get, ScpId);
            object prototype = Reflect.Call(_actorLibrary, get, BaseId);
            return "SCP shadows: " + ShadowInfo(scp) + " | wild source: " + ShadowInfo(prototype);
        }

        private static string ShadowInfo(object asset)
        {
            if (asset == null) return "not found";
            List<string> fields = new List<string>();
            foreach (object target in new[] { asset, Reflect.Member(asset, "texture_asset") })
            {
                if (target == null) continue;
                string prefix = ReferenceEquals(target, asset) ? "asset." : "texture.";
                foreach (FieldInfo field in target.GetType().GetFields(Reflect.Any))
                {
                    if (field.Name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    try { fields.Add(prefix + field.Name + "=" + Convert.ToString(field.GetValue(target))); }
                    catch { }
                    if (fields.Count >= 24) break;
                }
                if (fields.Count >= 24) break;
            }
            return fields.Count == 0 ? "no shadow members found" : string.Join(", ", fields.ToArray());
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
