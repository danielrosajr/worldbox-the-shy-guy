using System;
using System.IO;

namespace SCP096TheShyGuy
{
    // WorldFall's pack is SEPARATE from the NeoModLoader Mods folder. In older
    // releases updating the NML folder did not replace the model that WorldFall
    // actually loads. This helper compares the files byte by byte and writes
    // only after an explicit click in F5; never alters other packs or game DLLs.
    internal sealed class WorldfallPackInstaller
    {
        private readonly string _sourceFolder;
        private readonly string _destFolder;
        private const string ModelName = "animal_scp096_shyguy.glb";
        internal string Status { get; private set; }
        internal string Destination => _destFolder;

        internal WorldfallPackInstaller(string modFolder)
        {
            _sourceFolder = Path.Combine(modFolder, "Worldfall");
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (string.IsNullOrEmpty(home)) home = Environment.GetEnvironmentVariable("HOME");
            if (string.IsNullOrEmpty(home)) home = ".";
            _destFolder = Path.Combine(home, ".config", "unity3d", "mkarpenko",
                "WorldBox", "FirstPerson", "packs", "arvak_scp096_worldfall");
            Status = "ainda nao verificado";
        }

        private static bool SameFile(string source, string installed)
        {
            if (!File.Exists(source) || !File.Exists(installed)) return false;
            FileInfo a = new FileInfo(source);
            FileInfo b = new FileInfo(installed);
            if (a.Length != b.Length) return false;
            // Small GLB (~260KB): exact-byte test is cheap when F5 diagnostics run.
            byte[] aa = File.ReadAllBytes(source);
            byte[] bb = File.ReadAllBytes(installed);
            for (int i = 0; i < aa.Length; ++i)
                if (aa[i] != bb[i]) return false;
            return true;
        }

        internal bool Check()
        {
            try
            {
                string source = Path.Combine(_sourceFolder, "models", ModelName);
                string target = Path.Combine(_destFolder, "models", ModelName);
                if (!File.Exists(source)) { Status = "GLB novo ausente no mod"; return false; }
                if (!File.Exists(target)) { Status = "PACK NAO INSTALADO (clique instalar)"; return false; }
                long original = new FileInfo(source).Length;
                long installed = new FileInfo(target).Length;
                if (!SameFile(source, target))
                {
                    Status = "GLB ANTIGO: " + installed + " -> " + original + " bytes. INSTALAR";
                    return false;
                }
                if (!SameFile(Path.Combine(_sourceFolder, "worldfall.json"),
                    Path.Combine(_destFolder, "worldfall.json")))
                {
                    Status = "GLB igual, mas manifest diferente (clique instalar)";
                    return false;
                }
                Status = "GLB NOVO OK (" + installed + " bytes); ative no F2";
                return true;
            }
            catch (Exception e)
            {
                Status = "verificacao falhou: " + e.GetType().Name + ": " + e.Message;
                return false;
            }
        }

        internal bool Install()
        {
            try
            {
                string modelSource = Path.Combine(_sourceFolder, "models", ModelName);
                string jsonSource = Path.Combine(_sourceFolder, "worldfall.json");
                if (!File.Exists(modelSource) || !File.Exists(jsonSource))
                {
                    Status = "mod incompleto: falta GLB/worldfall.json";
                    return false;
                }
                string modelTargetDir = Path.Combine(_destFolder, "models");
                Directory.CreateDirectory(modelTargetDir);
                File.Copy(modelSource, Path.Combine(modelTargetDir, ModelName), true);
                File.Copy(jsonSource, Path.Combine(_destFolder, "worldfall.json"), true);
                bool good = Check();
                if (good) Status += "; REINICIE o WorldBox";
                return good;
            }
            catch (Exception e)
            {
                Status = "instalacao falhou: " + e.GetType().Name + ": " + e.Message;
                return false;
            }
        }
    }
}
