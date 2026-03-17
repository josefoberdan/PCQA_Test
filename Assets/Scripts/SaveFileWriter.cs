using System.IO;
using System.Text;

public static class SafeFileWriter
{
    public static void WriteAllTextAtomic(string path, string content)
    {
        string tempPath = path + ".tmp";

        File.WriteAllText(tempPath, content, Encoding.UTF8);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(tempPath, path);
    }
}

