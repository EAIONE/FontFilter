namespace FontFilter;
public sealed record FontRecord(string Id,string Family,string Face,string Source,string Vendor)
{
 public string AutoTags {get;set;}=""; public string ManualTags {get;set;}=""; public string Summary {get;set;}=""; public string Url {get;set;}=""; public string Updated {get;set;}=""; public bool Active {get;set;}=true;
 public bool IsFavorite{get;set;}
 public bool? SupportsChinese {get;set;}
 public string Label=>Family+" · "+Face;
 public string Tags=>string.Join("  ",new[]{ManualTags.Length>0?"人工："+ManualTags:"",AutoTags.Length>0?"推断："+AutoTags:""}.Where(s=>s.Length>0));
 public string SearchText=>string.Join(" ",Family,Face,Vendor,AutoTags,ManualTags,Summary);
}
