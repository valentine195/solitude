using System.IO;
using System.Text;
using SOLITUDE.Application.Persistence;
namespace SOLITUDE.SaveLoad
{
    public sealed class PhysicalSaveFiles : ISaveFiles
    {
        public bool Exists(string path)
        {
            try { File.GetAttributes(path); return true; }
            catch (FileNotFoundException) { return false; }
            catch (DirectoryNotFoundException) { return false; }
        }
        public string Read(string path) => File.ReadAllText(path);
        public void WriteDurable(string path, string text)
        {
            if (path.EndsWith(".tmp")) VerificationFaults.Hit("before-temp");
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            byte[] bytes = new UTF8Encoding(false, true).GetBytes(text);
            using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            file.Write(bytes, 0, bytes.Length); file.Flush(true);
            if (path.EndsWith(".tmp")) VerificationFaults.Hit("after-temp");
            if (path.EndsWith(".recovery")) VerificationFaults.Hit("after-receipt");
        }
        public void Replace(string source, string destination, string backup)
        { VerificationFaults.Hit("before-commit"); File.Replace(source, destination, backup); VerificationFaults.Hit("after-commit"); }
        public void Move(string source, string destination)
        { VerificationFaults.Hit("before-commit"); File.Move(source, destination); VerificationFaults.Hit("after-commit"); }
        public void Delete(string path) => File.Delete(path);
    }
    public static class SaveFileStore
    {
        public static void Write(string path, string json)
        {
            var files = new PhysicalSaveFiles(); files.WriteDurable(path + ".tmp", json);
            if (files.Exists(path)) files.Replace(path + ".tmp", path, path + ".bak");
            else files.Move(path + ".tmp", path);
        }
    }
}
