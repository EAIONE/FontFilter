using FontFilter;
using System.Text;
using System.Text.RegularExpressions;
public static class BookLineBreakerTests
{
 public static void Run(Action<string,Action> test,Action<bool,string> check)
 {
  test("physical preview accounts for pixel density and OS scaling",()=>{var ratio=PhysicalPageScale.FromDisplay(5120,2,598);var pageDip=185*PhysicalPageScale.DipPerMillimeter*ratio;check(Math.Abs(pageDip/(2560.0/598)-185)<.00001,"physical185mm changed by OS scaling");});
  test("ruler calibration corrects measured screen length",()=>{check(Math.Abs(PhysicalPageScale.Correct(1,80)-1.25)<.0001,"short ruler must enlarge page");check(Math.Abs(PhysicalPageScale.Correct(1.25,125)-1)<.0001,"long ruler correction wrong");});
  test("physical preview rejects invalid screen measurements",()=>{try{PhysicalPageScale.FromDisplay(5120,0,598);throw new Exception("invalid scaling accepted");}catch(ArgumentOutOfRangeException){} });
  double Measure(string s)=>s.EnumerateRunes().Count();
  test("book Chinese lines honor closing and opening punctuation",()=>{
   var lines=BookLineBreaker.Lines("甲乙（丙丁）戊己，庚辛。壬癸《书页之间》：安静阅读！",8,1,Measure,30);
   check(lines.Count>=30,"insufficient overflow");foreach(var line in lines){var clean=line.Trim();check(!BookLineBreaker.NoStart.Contains(clean[..1]),"closing punctuation at line head: "+line);check(!BookLineBreaker.NoEnd.Contains(clean[^1..]),"opening punctuation at line tail: "+line);}
  });
  test("book English wrapping preserves entire words and indentation",()=>{
   var known=new HashSet<string>{"Reading","typography","preserves","complete","words"};var lines=BookLineBreaker.Lines("Reading typography preserves complete words.",15,1,Measure,12);
   check(lines[0].StartsWith("　　"),"first line lost two em indent");foreach(var line in lines)foreach(Match word in Regex.Matches(line,"[A-Za-z]+"))check(known.Contains(word.Value),"split word "+word.Value);
  });
  test("book long words stay intact rather than arbitrary letter breaks",()=>{var lines=BookLineBreaker.Lines("extraordinary words",8,1,Measure,3);check(lines[0].Trim()=="extraordinary","overlong word split");});
  test("book mixed punctuation and grapheme text stays present",()=>{var lines=BookLineBreaker.Lines("中文（English words），阅读《Typography》。cafe\u0301",18,1,Measure,8);check(string.Join("",lines).Contains("cafe\u0301"),"combining grapheme lost");check(lines.All(l=>!BookLineBreaker.NoStart.Contains(l.TrimStart()[..1])),"mixed closing punctuation orphaned");});
  test("book scripts have distinct long form specimens and legacy migration",()=>{check(BookSamples.Traditional.Contains("閱讀")&&BookSamples.English.Contains("Typography")&&BookSamples.Mixed.Contains("English words"),"missing language specimen");var settings=SettingsStore.Sanitize(new(){PageSampleMode="书籍内文（分段）"});check(settings.PageSampleMode==BookSamples.DefaultKey,"old sample key not migrated");});
 }
}
