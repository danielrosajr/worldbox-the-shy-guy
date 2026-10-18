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
                object humanAsset = Reflect.Call(_actorLibrary, get, "human");
                if (humanAsset == null)
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
                // v0.1.2 stability fix: keep the human texture subasset inherited by clone.
                // Replacing it with an incomplete ActorTextureSubAsset discarded baby/head
                // and shadow information and produced five ActorAssetLibrary warnings.
                // Custom sprites remain in the package but are NOT bound until the
                // exact format is confirmed against the player's DLLs.
                object inheritedTexture = Reflect.Member(asset, "texture_asset");
                if (inheritedTexture == null)
                {
                    object vanillaTexture = Reflect.Member(humanAsset, "texture_asset");
                    if (vanillaTexture == null || !Reflect.Set(asset, "texture_asset", vanillaTexture))
                    { reason = "The cloned actor has no usable inherited texture"; return false; }
                }
                // The loader may link the actor asset later; don't force a second
                // loadTexturesAndSprites() pass with incomplete custom resources.
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

        internal string TargetDistance(object a, object b)
        {
            if (!Position(a, out float ax, out float ay) || !Position(b, out float bx, out float by))
                return "indisponivel";
            double dx = ax - bx, dy = ay - by;
            return Math.Sqrt(dx * dx + dy * dy).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " tiles";
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
                _lastKillSignature = signature + " -> vitima saiu do registro vivo (confirmado)";
                return true;
            }
            _lastKillSignature = signature + " invocado; vitima ainda consta no registro vivo";
            return false;
        }

        internal string DiagnosticShadows()
        {
            if (_actorLibrary == null) return "Actor library unavailable";
            MethodInfo get = Reflect.Find(_actorLibrary, "get", p => p.Length == 1 && p[0].ParameterType == typeof(string));
            if (get == null) return "Actor get(string) unavailable";
            object human = Reflect.Call(_actorLibrary, get, "human");
            object scp = Reflect.Call(_actorLibrary, get, ScpId);
            return "SCP shadows: " + ShadowInfo(scp) + " | humano: " + ShadowInfo(human);
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
