using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media;
namespace FontFilter;
public sealed record ScanProgress(int Done,int Total,string Name,int Errors,int Reused);
public sealed record ScanResult(int Faces,int Families,int Errors,int Reused,bool Complete);
public sealed class FontScanner:IDisposable
{
 readonly ManualResetEventSlim pause=new(true);public bool IsPaused{get;private set;}
 public void TogglePause(){IsPaused=!IsPaused;if(IsPaused)pause.Reset();else pause.Set();}
 public void Resume(){IsPaused=false;pause.Set();}
 public static string LocalName(IDictionary<CultureInfo,string> names)=>names.FirstOrDefault(x=>x.Key.Name.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)).Value??names.FirstOrDefault(x=>x.Key.TwoLetterISOLanguageName=="zh").Value??names.FirstOrDefault(x=>x.Key.TwoLetterISOLanguageName=="en").Value??names.Values.FirstOrDefault()??"未知";
 public static string LocalName(LanguageSpecificStringDictionary names)=>names.FirstOrDefault(x=>x.Key.IetfLanguageTag.Equals("zh-CN",StringComparison.OrdinalIgnoreCase)).Value??names.FirstOrDefault(x=>x.Key.IetfLanguageTag.StartsWith("zh",StringComparison.OrdinalIgnoreCase)).Value??names.FirstOrDefault(x=>x.Key.IetfLanguageTag.StartsWith("en",StringComparison.OrdinalIgnoreCase)).Value??names.Values.FirstOrDefault()??"未知";
 public static bool CanReuse(FontRecord current,FontRecord cached,string currentStamp,string cachedStamp)=>current.Id==cached.Id&&current.Source.Equals(cached.Source,StringComparison.OrdinalIgnoreCase)&&currentStamp.Length>0&&currentStamp==cachedStamp;
 public static string Stamp(string uri){try{var u=new Uri(uri);if(!u.IsFile)return "";var info=new FileInfo(u.LocalPath);return info.Exists?info.Length+":"+info.LastWriteTimeUtc.Ticks:"";}catch{return "";}}
 public async Task<ScanResult> ScanAsync(FontStore store,IProgress<ScanProgress> progress,CancellationToken ct)
 {
  return await Task.Run(()=>{
   var families=Fonts.SystemFontFamilies.ToArray();var old=store.All().ToDictionary(x=>x.Id);var seen=new HashSet<string>();int errors=0,reused=0,done=0;var batch=new List<FontRecord>();
   try{foreach(var family in families){ct.ThrowIfCancellationRequested();pause.Wait(ct);var name="不可读取家族";
    try{name=LocalName(family.FamilyNames);foreach(var typeface in family.GetTypefaces()){
      ct.ThrowIfCancellationRequested();pause.Wait(ct);try{
       var face=LocalName(typeface.FaceNames);var key=name+"\n"+face;
       if(!typeface.TryGetGlyphTypeface(out var glyph))continue;
       var source=glyph.FontUri.AbsoluteUri;var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name+"\n"+face+"\n"+source)));
       var stamp=Stamp(source);var supportsChinese=old.TryGetValue(id,out var cached)&&cached.SupportsChinese.HasValue&&stamp.Length>0&&ScanCache.GetStamp(id)==stamp?cached.SupportsChinese.Value:FontOrdering.HasChineseGlyphs(glyph.CharacterToGlyphMap);var actual=new FontRecord(id,name,face,source,""){SupportsChinese=supportsChinese};
       if(old.TryGetValue(id,out var previous)&&ScanCache.GetStamp(id) is var priorStamp&&CanReuse(actual,previous,stamp,priorStamp)&&previous.SupportsChinese==supportsChinese){
        if(seen.Add(id))reused++;continue;
       }
       var vendor=name.StartsWith("方正")||name.StartsWith("FZ",StringComparison.OrdinalIgnoreCase)?"方正":name.StartsWith("汉仪")||name.StartsWith("HY",StringComparison.OrdinalIgnoreCase)?"汉仪":"";
       if(seen.Add(id)){batch.Add(new FontRecord(id,name,face,source,vendor){SupportsChinese=supportsChinese});ScanCache.Remember(id,Stamp(source));}
       if(batch.Count>=40){store.UpsertBatch(batch);batch.Clear();}
      }catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;}
    }}catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;}
    progress.Report(new(++done,families.Length,name,errors,reused));
   }
   store.UpsertBatch(batch);if(errors==0)store.MarkAbsent(seen);ScanCache.Save();return new ScanResult(seen.Count,families.Length,errors,reused,true);
   }catch(OperationCanceledException){store.UpsertBatch(batch);ScanCache.Save();return new ScanResult(seen.Count,done,errors,reused,false);}
  },ct);
 }
 public void Dispose()=>pause.Dispose();
}
internal static class ScanCache
{
 static readonly Dictionary<string,string> Values=Load();
 static Dictionary<string,string> Load(){try{return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(App.DataDirectory,"scan-cache.json")))??new();}catch{return new();}}
 public static string GetStamp(string id)=>Values.TryGetValue(id,out var stamp)?stamp:"";
 public static void Remember(string id,string stamp)=>Values[id]=stamp;
 public static void Save()=>SettingsStore.AtomicWrite(Path.Combine(App.DataDirectory,"scan-cache.json"),System.Text.Json.JsonSerializer.Serialize(Values));
}
