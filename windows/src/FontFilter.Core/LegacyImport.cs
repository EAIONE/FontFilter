using System.Text.Json;
namespace FontFilter;
public sealed record LegacyImportReport(int Linked,int Unresolved,string BackupPath,string UnresolvedPath,UserSettings? Settings);
public static class LegacyImport
{
 public static LegacyImportReport Import(string directory,FontStore target,string historyDirectory)
 {
  var source=Path.Combine(directory,"fonts.db");if(!File.Exists(source))throw new InvalidOperationException("所选目录中没有 fonts.db，请选择旧版的 Data 文件夹。");
  Directory.CreateDirectory(historyDirectory);
  var token=DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..8];
  var snapshot=Path.Combine(historyDirectory,"source-"+token+".db");
  NativeSqlite.Snapshot(source,snapshot);
  List<FontRecord> old;using(var store=new FontStore(snapshot))old=store.ExportAll();
  UserSettings? settings=null;var settingsPath=Path.Combine(directory,"settings.json");
  if(File.Exists(settingsPath)){if(new FileInfo(settingsPath).Length>2_000_000)throw new InvalidOperationException("旧设置文件过大。");try{settings=JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(settingsPath));if(settings!=null)settings=SettingsStore.Sanitize(settings);}catch(JsonException){throw new InvalidOperationException("旧设置文件格式错误，当前数据未修改。");}}
  var current=target.All();string Key(FontRecord f)=>OfficialCatalogue.Normalize(f.Family)+"\n"+OfficialCatalogue.Normalize(f.Face);
  var byName=current.GroupBy(Key).ToDictionary(g=>g.Key,g=>g.ToList());var oldNames=old.GroupBy(Key).ToDictionary(g=>g.Key,g=>g.Count());
  var linked=new List<(FontRecord,FontRecord)>();var unresolved=new List<FontRecord>();
  foreach(var item in old.Where(f=>f.ManualTags.Length>0||f.Summary.Length>0||f.IsFavorite)){
   var exact=current.Where(f=>f.Id==item.Id&&f.Source==item.Source).ToList();
   FontRecord? match=exact.Count==1?exact[0]:byName.TryGetValue(Key(item),out var list)&&list.Count==1&&oldNames[Key(item)]==1?list[0]:null;
   if(match==null)unresolved.Add(item);else linked.Add((match,item));
  }
  var unresolvedPath=Path.Combine(historyDirectory,"unresolved-"+token+".json");
  File.WriteAllText(unresolvedPath,JsonSerializer.Serialize(unresolved,new JsonSerializerOptions{WriteIndented=true}));
  var backup=Path.Combine(historyDirectory,"before-import-"+token+".db");target.Backup(backup);
  target.MergeAnnotations(linked);
  return new(linked.Count,unresolved.Count,backup,unresolvedPath,settings);
 }
}
