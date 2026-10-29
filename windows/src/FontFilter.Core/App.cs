namespace FontFilter;
public static class App
{
 public static string DataDirectory => Environment.GetEnvironmentVariable("FONTFILTER_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontFilter");
}
