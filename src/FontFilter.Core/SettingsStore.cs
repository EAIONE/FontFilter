using System.IO;
using System.Text.Json;
namespace FontFilter;
public sealed class UserSettings
{
 public Dictionary<string,int> TagUsage{get;set;}=new();
 public void RecordTags(IEnumerable<string> tags){TagUsage??=new();foreach(var tag in tags.Where(t=>!string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))TagUsage[tag]=Math.Min(TagUsage.GetValueOrDefault(tag),int.MaxValue-1)+1;}
 public IEnumerable<string> FrequentTags()=>(TagUsage??new()).OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.OrdinalIgnoreCase).Take(12).Select(p=>p.Key);
 public string ThemeColor{get;set;}=ThemePalette.DefaultPrimary;
 public string SecondaryColor{get;set;}=ThemePalette.DefaultSecondary;
 public string AccentColor{get;set;}=ThemePalette.DefaultAccent;
 public string HeaderColor{get;set;}=ThemePalette.DefaultHeader;
 public string BackgroundColor{get;set;}=ThemePalette.DefaultBackground;
 public string PageSampleMode{get;set;}=BookSamples.DefaultKey;public string PageSampleText{get;set;}="";
 public Dictionary<string,double> DisplayCalibrations{get;set;}=new();
 public double PageWidth{get;set;}=185;public double PageHeight{get;set;}=260;
 public double[] PageLayout{get;set;}=new double[]{20,20,22,18,12,1.6,0};
 public bool DetailsCollapsed{get;set;}
 public string SampleMode{get;set;}=FontSamples.AutoKey;
 public string Sample{get;set;}="山川有风骨，文字见精神。\n在纸页之间，寻找恰当的声音。汉字的疏密、笔画与节奏，共同构成阅读的气质。";
 public ApiOptions Api{get;set;}=new();
 public double Size{get;set;}=32; public double LineHeight{get;set;}=1.6;
 public Dictionary<string,string> Samples{get;set;}=new(){["书名"]="山川有风骨\n中华文明的历史与回声",["正文"]="在纸页之间，寻找恰当的声音。汉字的疏密、笔画与节奏，共同构成阅读的气质。我们从字形中辨认时代，也在阅读中理解文字。",["诗词"]="明月松间照，清泉石上流。\n竹喧归浣女，莲动下渔舟。"};
}
public static class SettingsStore
{
 static string PathName=>Path.Combine(App.DataDirectory,"settings.json");
 public static UserSettings Sanitize(UserSettings settings){settings.DisplayCalibrations??=new();settings.DisplayCalibrations=settings.DisplayCalibrations.Where(p=>double.IsFinite(p.Value)&&p.Value>=.25&&p.Value<=4).Take(30).ToDictionary(p=>p.Key,p=>p.Value);settings.PageSampleMode??=BookSamples.DefaultKey;settings.PageSampleText??="";settings.PageSampleText=settings.PageSampleText[..Math.Min(settings.PageSampleText.Length,4000)];settings.PageSampleMode=settings.PageSampleMode switch{"书籍内文（分段）" or "简体中文" or FontSamples.AutoKey=>BookSamples.DefaultKey,"繁體中文"=>BookSamples.TraditionalKey,"English"=>BookSamples.EnglishKey,_=>settings.PageSampleMode};settings.PageWidth=double.IsFinite(settings.PageWidth)?Math.Clamp(settings.PageWidth,50,1200):185;settings.PageHeight=double.IsFinite(settings.PageHeight)?Math.Clamp(settings.PageHeight,50,1200):260;settings.PageLayout=settings.PageLayout is {Length:7}&&settings.PageLayout.All(double.IsFinite)?settings.PageLayout:new double[]{20,20,22,18,12,1.6,0};settings.Size=double.IsFinite(settings.Size)?Math.Clamp(settings.Size,12,120):32;settings.LineHeight=double.IsFinite(settings.LineHeight)?Math.Clamp(settings.LineHeight,1,3):1.6;settings.Sample??=FontSamples.Simplified;settings.Sample=settings.Sample[..Math.Min(settings.Sample.Length,4000)];settings.Samples??=new();settings.Samples=settings.Samples.Where(p=>p.Key.Length>0&&p.Value!=null).Take(40).ToDictionary(p=>p.Key,p=>p.Value[..Math.Min(p.Value.Length,4000)]);settings.Api??=new();settings.TagUsage??=new();return settings;}
 public static UserSettings Load(){try{return Sanitize(JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(PathName))??new());}catch{return new();}}
 public static void Save(UserSettings settings)=>AtomicWrite(PathName,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
 public static void AtomicWrite(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";File.WriteAllText(temp,text);File.Move(temp,path,true);}
}
