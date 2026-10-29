using System.Globalization;
using System.Text;
namespace FontFilter;
// A tailored book-preview line breaker: CJK punctuation pairs, grapheme clusters,
// and whole Latin words. It does not claim full Unicode UAX #14 conformance.
public static class BookLineBreaker
{
 public const string NoStart="，。、；：？！…—）］｝】〕〉》」』”’％‰,.;:!?%)]}";
 public const string NoEnd="（［｛【〔〈《「『“‘([{“";
 public static IReadOnlyList<string> Lines(string text,double width,double em,Func<string,double> measure,int lineCount)
 {
  if(width<=0||em<=0||lineCount<=0)throw new ArgumentOutOfRangeException(nameof(width));
  var paragraphs=text.Replace("\r","").Split('\n').Select(p=>p.Trim()).Where(p=>p.Length>0).ToArray();if(paragraphs.Length==0)paragraphs=new[]{"书页之间，文字自有回声。"};
  var result=new List<string>();
  for(var paragraph=0;result.Count<lineCount;paragraph++){
   var groups=Groups(paragraphs[paragraph%paragraphs.Length]);var line=new StringBuilder("　　");double used=2*em;bool content=false;
   foreach(var group in groups){
    var space=string.IsNullOrWhiteSpace(group);var advance=measure(group);
    if(used+advance>width&&content){result.Add(line.ToString().TrimEnd());if(result.Count>=lineCount)return result;line.Clear();used=0;content=false;if(space)continue;}
    // A word longer than the entire measure stays intact and is clipped by the page.
    // It must never be broken into arbitrary letters just to fit the preview.
    if(!content&&space)continue;line.Append(group);used+=advance;if(!space)content=true;
   }
   if(content)result.Add(line.ToString().TrimEnd());
  }
  return result;
 }
 static List<string> Groups(string paragraph)
 {
  var tokens=new List<string>();var word=new StringBuilder();
  void Flush(){if(word.Length>0){tokens.Add(word.ToString());word.Clear();}}
  var iterator=StringInfo.GetTextElementEnumerator(paragraph);while(iterator.MoveNext()){
   var element=(string)iterator.Current;var rune=element.EnumerateRunes().First();
   var latinWord=Rune.IsLetterOrDigit(rune)&&!Cjk(rune.Value);
   if(word.Length>0&&char.IsDigit(word[^1])&&(element=="."||element==",")){word.Append(element);continue;}
   if(latinWord||word.Length>0&&(element=="'"||element=="’"||element=="-")){word.Append(element);continue;}Flush();tokens.Add(element);
  }Flush();
  var groups=new List<string>();var opening="";var straightQuote=false;
  foreach(var token in tokens){
   if(token=="\""){if(!straightQuote){opening+=token;straightQuote=true;continue;}straightQuote=false;if(groups.Count>0){groups[^1]+=token;continue;}}
   if(NoEnd.Contains(token,StringComparison.Ordinal)){opening+=token;continue;}
   if(opening.Length>0){if(string.IsNullOrWhiteSpace(token)){opening+=token;continue;}groups.Add(opening+token);opening="";continue;}
   if(NoStart.Contains(token,StringComparison.Ordinal)&&groups.Count>0){while(groups.Count>0&&string.IsNullOrWhiteSpace(groups[^1]))groups.RemoveAt(groups.Count-1);if(groups.Count>0){groups[^1]+=token;continue;}}
   groups.Add(token);
  }
  if(opening.Length>0){if(groups.Count>0)groups[^1]+=opening;else groups.Add(opening);}return groups;
 }
 static bool Cjk(int c)=>c is >=0x2E80 and <=0x9FFF or >=0xAC00 and <=0xD7AF or >=0xF900 and <=0xFAFF or >=0x20000 and <=0x323AF;
}
