namespace FontFilter;
public static class UpdateTransaction
{
 public static string Swap(string target,string candidate)
 {
  target=Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar);candidate=Path.GetFullPath(candidate).TrimEnd(Path.DirectorySeparatorChar);
  if(Path.GetDirectoryName(target)==null||target.Length<8||candidate.StartsWith(target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||target==candidate||!Directory.Exists(target)||!Directory.Exists(candidate)||(File.GetAttributes(target)&FileAttributes.ReparsePoint)!=0)throw new InvalidDataException("更新目标路径无效");
  // Preserve legacy portable annotations if any; the active data store lives outside the app.
  var oldData=Path.Combine(target,"Data");if(Directory.Exists(oldData))foreach(var name in new[]{"fonts.db","fonts.db-wal","fonts.db-shm","settings.json","scan-cache.json"}){var old=Path.Combine(oldData,name);if(File.Exists(old)){Directory.CreateDirectory(Path.Combine(candidate,"Data"));File.Copy(old,Path.Combine(candidate,"Data",name),true);}}
  var backup=target+".previous-"+DateTime.UtcNow.ToString("yyyyMMddHHmmss")+"-"+Guid.NewGuid().ToString("N")[..6];
  Directory.Move(target,backup);
  try{Directory.Move(candidate,target);}catch{Directory.Move(backup,target);throw;}
  return backup;
 }
 public static void Rollback(string target,string backup)
 {
  if(!Directory.Exists(backup))throw new DirectoryNotFoundException(backup);if(Directory.Exists(target))Directory.Move(target,target+".failed-"+Guid.NewGuid().ToString("N"));Directory.Move(backup,target);
 }
}
