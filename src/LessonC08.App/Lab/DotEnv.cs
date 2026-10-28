namespace LessonC08.App.Lab;

/// <summary>Minimal .env loader. Values already present in the process environment win.</summary>
public static class DotEnv
{
    /// <summary>The nearest .env walking up from the working directory, or ./.env when none exists yet.</summary>
    public static string Path { get; } = Find();

    public static void Load()
    {
        if (!File.Exists(Path)) return;
        foreach (var raw in File.ReadAllLines(Path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim().Trim('"', '\'');
            if (Environment.GetEnvironmentVariable(key) is null) Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>Sets or replaces one KEY=value line, keeping the rest of the file.</summary>
    public static void Save(string key, string value)
    {
        var lines = File.Exists(Path) ? File.ReadAllLines(Path).ToList() : [];
        var index = lines.FindIndex(l => l.TrimStart().StartsWith(key + "="));
        if (index >= 0) lines[index] = $"{key}={value}";
        else lines.Add($"{key}={value}");
        File.WriteAllLines(Path, lines);
        Environment.SetEnvironmentVariable(key, value);
    }

    private static string Find()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir is not null; dir = dir.Parent)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate)) return candidate;
        }
        return System.IO.Path.GetFullPath(".env");
    }
}
