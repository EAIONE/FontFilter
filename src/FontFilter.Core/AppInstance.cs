namespace FontFilter;
public static class AppInstance
{
 public static IDisposable Acquire(string directory){Directory.CreateDirectory(directory);return new FileStream(Path.Combine(directory,"app.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
}
