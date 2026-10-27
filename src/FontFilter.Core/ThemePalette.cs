namespace FontFilter;
public sealed record ThemePalette(string Primary,string Secondary,string Accent)
{
 public const string DefaultPrimary="#16594D",DefaultSecondary="#E8F0EC",DefaultAccent="#C08A42",DefaultHeader="#DDEAE5";
 public string Header{get;init;}=DefaultHeader;
 public static bool IsValid(string? hex)=>hex!=null&&System.Text.RegularExpressions.Regex.IsMatch(hex,"^#[0-9a-fA-F]{6}$");
 public static ThemePalette From(UserSettings settings)=>new(Normalize(settings.ThemeColor,DefaultPrimary),Normalize(settings.SecondaryColor,DefaultSecondary),Normalize(settings.AccentColor,DefaultAccent)){Header=Normalize(settings.HeaderColor,DefaultHeader)};
 static string Normalize(string? color,string fallback)=>IsValid(color)?color!.ToUpperInvariant():fallback;
}
