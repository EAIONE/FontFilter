using System.IO;
using System.Text.Json;
namespace FontFilter;
public sealed class UserSettings
{
 public Dictionary<string,int> TagUsage{get;set;}=new();
 public void RecordTags(IEnumerable<string> tags){TagUsage??=new();foreach(var tag in tags.Where(t=>!string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))TagUsage[tag]=Math.Min(TagUsage.GetValueOrDefault(tag),int.MaxValue-1)+1;}
 public IEnumerable<string> FrequentTags()=>(TagUsage??new()).OrderByDescending(p=>p.Value).ThenBy(p=>p.Key,StringComparer.OrdinalIgnoreCase).Take(12).Select(p=>p.Key);
 public string ThemeColor{get;set;}=ThemeService.DefaultColor;
 public string SampleMode{get;set;}=FontSamples.AutoKey;
 public string Sample{get;set;}="山川有风骨，文字见精神。\n在纸页之间，寻找恰当的声音。汉字的疏密、笔画与节奏，共同构成阅读的气质。";
 public ApiOptions Api{get;set;}=new();
 public double Size{get;set;}=32; public double LineHeight{get;set;}=1.6;
 public Dictionary<string,string> Samples{get;set;}=new(){["书名"]="山川有风骨\n中华文明的历史与回声",["正文"]="在纸页之间，寻找恰当的声音。汉字的疏密、笔画与节奏，共同构成阅读的气质。我们从字形中辨认时代，也在阅读中理解文字。",["诗词"]="明月松间照，清泉石上流。\n竹喧归浣女，莲动下渔舟。"};
}
public static class SettingsStore
{
 static string PathName=>Path.Combine(App.DataDirectory,"settings.json");
 public static UserSettings Load(){try{return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(PathName))??new();}catch{return new();}}
 public static void Save(UserSettings settings)=>AtomicWrite(PathName,JsonSerializer.Serialize(settings,new JsonSerializerOptions{WriteIndented=true}));
 public static void AtomicWrite(string path,string text){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var temp=path+".tmp";File.WriteAllText(temp,text);File.Move(temp,path,true);}
}
