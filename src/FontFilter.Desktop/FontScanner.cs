using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SkiaSharp;
namespace FontFilter.Desktop;
public sealed record FontLocator(string Family,int Weight,int Style,int Stretch,string Fingerprint);
public sealed record ScanProgress(int Done,int Total,string Name,int Errors,int Reused);
public sealed record ScanResult(int Faces,int Families,int Errors,int Reused,bool Complete);
public sealed class FontScanner:IDisposable
{
 readonly ManualResetEventSlim pause=new(true);public bool IsPaused{get;private set;}
 public void TogglePause(){IsPaused=!IsPaused;if(IsPaused)pause.Reset();else pause.Set();}
 public void Resume(){IsPaused=false;pause.Set();}
 public static string Fingerprint(SKTypeface glyph)
 {
  using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
  foreach(var tag in new uint[]{0x68656164,0x6e616d65,0x6d617870,0x636d6170}){var bytes=glyph.GetTableData(tag);if(bytes!=null)hash.AppendData(bytes);}
  return Convert.ToHexString(hash.GetHashAndReset());
 }
 public static string LocalName(SKTypeface glyph,int nameId,string fallback)
 {
  var b=glyph.GetTableData(0x6e616d65);if(b==null||b.Length<6)return fallback;
  int U(int p)=>p+1<b.Length?(b[p]<<8)|b[p+1]:0;
  int count=U(2),storage=U(4);var names=new List<(int Rank,string Text)>();
  for(int i=0;i<count&&6+i*12+12<=b.Length;i++){
   int p=6+i*12,platform=U(p),language=U(p+4),id=U(p+6),len=U(p+8),offset=storage+U(p+10);
   if(id!=nameId||offset+len>b.Length||platform is not (0 or 3))continue;
   var text=Encoding.BigEndianUnicode.GetString(b,offset,len).Trim('\0',' ');if(text.Length==0)continue;
   int rank=(language&0x3ff)==4?0:language==0x409?1:2;names.Add((rank,text));
  }
  return names.OrderBy(n=>n.Rank).Select(n=>n.Text).FirstOrDefault()??fallback;
 }
 public static string Source(string family,SKTypeface glyph)=>"fontfilter://system/"+Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new FontLocator(family,glyph.FontStyle.Weight,(int)glyph.FontStyle.Slant,glyph.FontStyle.Width,Fingerprint(glyph)))));
 public static SKTypeface Resolve(FontRecord record)
 {
  const string prefix="fontfilter://system/";if(!record.Source.StartsWith(prefix))throw new InvalidOperationException("请先更新本机字体库。");
  var locator=JsonSerializer.Deserialize<FontLocator>(Encoding.UTF8.GetString(Convert.FromBase64String(record.Source[prefix.Length..])))!;
  using var styles=SKFontManager.Default.GetFontStyles(locator.Family);
  for(int i=0;i<styles.Count;i++){
   using var style=styles[i];if(style.Weight!=locator.Weight||(int)style.Slant!=locator.Style||style.Width!=locator.Stretch)continue;
   var glyph=styles.CreateTypeface(i);if(glyph!=null&&Fingerprint(glyph)==locator.Fingerprint)return glyph;
  }
  throw new InvalidOperationException("字体已变化或已移除，请更新字体库。");
 }
 public Task<ScanResult> ScanAsync(FontStore store,IProgress<ScanProgress> progress,CancellationToken ct)=>Task.Run(()=>{
  var families=SKFontManager.Default.FontFamilies.ToArray();var old=store.All().ToDictionary(f=>f.Id);var seen=new HashSet<string>();int errors=families.Length==0?1:0,reused=0,done=0;var batch=new List<FontRecord>();
  try{
   foreach(var family in families){ct.ThrowIfCancellationRequested();pause.Wait(ct);
    try{using var nativeStyles=SKFontManager.Default.GetFontStyles(family);
     if(nativeStyles.Count==0){errors++;File.AppendAllText(Path.Combine(FontFilter.App.DataDirectory,"scan-errors.log"),family+": no typefaces"+Environment.NewLine);}
     for(int i=0;i<nativeStyles.Count;i++){ct.ThrowIfCancellationRequested();pause.Wait(ct);
      try{
       var glyph=nativeStyles.CreateTypeface(i);if(glyph==null){errors++;continue;}
       var name=LocalName(glyph,16,LocalName(glyph,1,family));var legacyName=LocalName(glyph,1,family);if(!name.Any(c=>c>='\u3400'&&c<='\u9fff')&&legacyName.Any(c=>c>='\u3400'&&c<='\u9fff'))name=legacyName;var style=LocalName(glyph,17,LocalName(glyph,2,nativeStyles.GetStyleName(i)));
       var source=Source(family,glyph);var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
       if(!seen.Add(id))continue;
       if(old.TryGetValue(id,out var prior)&&prior.SupportsChinese.HasValue){reused++;continue;}

       var map="中文天地人国".ToDictionary(c=>(int)c,c=>glyph.GetGlyph(c));
       var vendor=name.StartsWith("方正")||family.StartsWith("FZ",StringComparison.OrdinalIgnoreCase)?"方正":name.StartsWith("汉仪")||family.StartsWith("HY",StringComparison.OrdinalIgnoreCase)?"汉仪":"";
       batch.Add(new(id,name,style,source,vendor){SupportsChinese=FontOrdering.HasChineseGlyphs(map)});
       if(batch.Count>=40){store.UpsertBatch(batch);batch.Clear();}
      }catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;File.AppendAllText(Path.Combine(FontFilter.App.DataDirectory,"scan-errors.log"),family+": "+ex.Message+Environment.NewLine);}
     }
    }catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;File.AppendAllText(Path.Combine(FontFilter.App.DataDirectory,"scan-errors.log"),family+": "+ex.Message+Environment.NewLine);}
    progress.Report(new(++done,families.Length,family,errors,reused));
   }
   store.UpsertBatch(batch);ScanCommit.Finish(store,seen,true,errors);return new ScanResult(seen.Count,families.Length,errors,reused,true);
  }catch(OperationCanceledException){store.UpsertBatch(batch);return new ScanResult(seen.Count,done,errors,reused,false);}
 },CancellationToken.None);
 public void Dispose()=>pause.Dispose();
}
