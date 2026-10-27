using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Avalonia.Media;
namespace FontFilter.Desktop;
public sealed record FontLocator(string Family,int Weight,int Style,int Stretch,string Fingerprint);
public sealed record ScanProgress(int Done,int Total,string Name,int Errors,int Reused);
public sealed record ScanResult(int Faces,int Families,int Errors,int Reused,bool Complete);
public sealed class FontScanner:IDisposable
{
 readonly ManualResetEventSlim pause=new(true);public bool IsPaused{get;private set;}
 public void TogglePause(){IsPaused=!IsPaused;if(IsPaused)pause.Reset();else pause.Set();}
 public void Resume(){IsPaused=false;pause.Set();}
 public static string Fingerprint(IGlyphTypeface glyph)
 {
  using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
  foreach(var tag in new uint[]{0x68656164,0x6e616d65,0x6d617870,0x636d6170})if(glyph.TryGetTable(tag,out var bytes))hash.AppendData(bytes);
  return Convert.ToHexString(hash.GetHashAndReset());
 }
 public static string LocalName(IGlyphTypeface glyph,int nameId,string fallback)
 {
  if(!glyph.TryGetTable(0x6e616d65,out var b)||b.Length<6)return fallback;
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
 public static string Source(Typeface typeface,IGlyphTypeface glyph)=>"fontfilter://system/"+Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new FontLocator(typeface.FontFamily.Name,(int)typeface.Weight,(int)typeface.Style,(int)typeface.Stretch,Fingerprint(glyph)))));
 public static IGlyphTypeface Resolve(FontRecord record)
 {
  const string prefix="fontfilter://system/";if(!record.Source.StartsWith(prefix))throw new InvalidOperationException("请先更新本机字体库。");
  var locator=JsonSerializer.Deserialize<FontLocator>(Encoding.UTF8.GetString(Convert.FromBase64String(record.Source[prefix.Length..])))!;
  var face=new Typeface(new FontFamily(locator.Family),(FontStyle)locator.Style,(FontWeight)locator.Weight,(FontStretch)locator.Stretch);
  if(!FontManager.Current.TryGetGlyphTypeface(face,out var glyph)||Fingerprint(glyph)!=locator.Fingerprint)throw new InvalidOperationException("字体已变化或已移除，请更新字体库。");return glyph;
 }
 public Task<ScanResult> ScanAsync(FontStore store,IProgress<ScanProgress> progress,CancellationToken ct)=>Task.Run(()=>{
  var families=FontManager.Current.SystemFonts.ToArray();var old=store.All().ToDictionary(f=>f.Id);var seen=new HashSet<string>();int errors=0,reused=0,done=0;var batch=new List<FontRecord>();
  try{
   foreach(var family in families){ct.ThrowIfCancellationRequested();pause.Wait(ct);
    try{var faces=family.FamilyTypefaces;if(faces.Count==0)errors++;
     foreach(var face in faces){ct.ThrowIfCancellationRequested();pause.Wait(ct);
      try{
       if(!FontManager.Current.TryGetGlyphTypeface(face,out var glyph)){errors++;continue;}
       var name=LocalName(glyph,16,LocalName(glyph,1,family.Name));var style=LocalName(glyph,17,LocalName(glyph,2,face.Weight+" "+face.Style));
       var source=Source(face,glyph);var id=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
       if(!seen.Add(id))continue;
       if(old.TryGetValue(id,out var prior)&&prior.SupportsChinese.HasValue){reused++;continue;}
       var map="中文天地人国".ToDictionary(c=>(int)c,c=>glyph.GetGlyph(c));
       var vendor=name.StartsWith("方正")||family.Name.StartsWith("FZ",StringComparison.OrdinalIgnoreCase)?"方正":name.StartsWith("汉仪")||family.Name.StartsWith("HY",StringComparison.OrdinalIgnoreCase)?"汉仪":"";
       batch.Add(new(id,name,style,source,vendor){SupportsChinese=FontOrdering.HasChineseGlyphs(map)});
       if(batch.Count>=40){store.UpsertBatch(batch);batch.Clear();}
      }catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;}
     }
    }catch(OperationCanceledException){throw;}catch(Exception ex) when(ex is not OutOfMemoryException){errors++;}
    progress.Report(new(++done,families.Length,family.Name,errors,reused));
   }
   store.UpsertBatch(batch);ScanCommit.Finish(store,seen,true,errors);return new ScanResult(seen.Count,families.Length,errors,reused,true);
  }catch(OperationCanceledException){store.UpsertBatch(batch);return new ScanResult(seen.Count,done,errors,reused,false);}
 },CancellationToken.None);
 public void Dispose()=>pause.Dispose();
}
