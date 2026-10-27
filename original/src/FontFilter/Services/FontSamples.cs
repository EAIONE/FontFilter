using System.Text.RegularExpressions;
namespace FontFilter;
public static class FontSamples
{
 public const string AutoKey="自动适配",FreeKey="自由试排";
 public const string Simplified="山川有风骨，文字见精神。\n在纸页之间，寻找恰当的声音。汉字的疏密、笔画与节奏，共同构成阅读的气质。0123456789";
 public const string Traditional="山川有風骨，文字見精神。\n在紙頁之間，尋找恰當的聲音。漢字的疏密、筆畫與節奏，共同構成閱讀的氣質。0123456789";
 public const string English="The quick brown fox jumps over the lazy dog.\nTypography gives words a voice.\nABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz 0123456789";
 public static IReadOnlyDictionary<string,string> Defaults{get;}=new Dictionary<string,string>{{"简体中文",Simplified},{"繁體中文",Traditional},{"English",English}};
 public static KeyValuePair<string,string> Select(FontRecord font,IDictionary<int,ushort> glyphs){
  var traditional=Regex.IsMatch(font.Family+" "+font.Face,@"繁|(?<![a-z])(?:TC|HK|Hant|Traditional)(?![a-z])",RegexOptions.IgnoreCase)||TagRules.Terms(font.AutoTags+" "+font.ManualTags).Contains("繁体");
  var preferred=traditional?"繁體中文":"简体中文";
  double Coverage(string text){var codes=text.EnumerateRunes().Where(r=>!System.Text.Rune.IsWhiteSpace(r)).Select(r=>r.Value).Distinct().ToArray();return codes.Count(c=>glyphs.TryGetValue(c,out var index)&&index!=0)/(double)codes.Length;}
  var best=Defaults.OrderByDescending(p=>Coverage(p.Value)).ThenBy(p=>p.Key==preferred?0:p.Key=="English"?2:1).First();return Coverage(best.Value)>0?best:new("English",English);
 }
}
