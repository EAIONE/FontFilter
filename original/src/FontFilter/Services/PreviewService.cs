using System.Windows;
using System.Windows.Media;
namespace FontFilter;
public static class PreviewService
{
 static readonly Dictionary<string,Typeface> Cache=new();static readonly Queue<string> Order=new();
 public static Typeface Resolve(FontRecord f){if(Cache.TryGetValue(f.Id,out var cached))return cached;var family=new FontFamily(f.Family);var typeface=family.GetTypefaces().FirstOrDefault(t=>FontScanner.LocalName(t.FaceNames)==f.Face&&t.TryGetGlyphTypeface(out var g)&&g.FontUri.AbsoluteUri.Equals(f.Source,StringComparison.OrdinalIgnoreCase));if(typeface==null)throw new InvalidOperationException("此字体已变动或不可读取，请重新扫描。");Cache[f.Id]=typeface;Order.Enqueue(f.Id);while(Order.Count>8)Cache.Remove(Order.Dequeue());return typeface;}
 public static string Missing(Typeface face,string text){if(!face.TryGetGlyphTypeface(out var glyph))return "无法检查字形";return string.Concat(text.EnumerateRunes().Where(r=>!System.Text.Rune.IsWhiteSpace(r)&&(!glyph.CharacterToGlyphMap.TryGetValue(r.Value,out var index)||index==0)).Distinct().Take(40).Select(r=>r.ToString()));}
}
