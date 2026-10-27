using System.Text.RegularExpressions;
namespace FontFilter;
public static class TagRules
{
 static readonly (string Name,string[] Aliases)[] Rules=TagCatalog.All.Select(t=>(t.Name,t.Aliases.Split('|'))).ToArray();
 public static string SelectionText(string existing,IEnumerable<string> selected,bool forSearch)=>string.Join(" ",(forSearch?Array.Empty<string>():Terms(existing)).Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase));
 public static string Extract(string text){var normalized=Regex.Replace(text,@"sans[\s-]*serif","sans",RegexOptions.IgnoreCase);return string.Join(" ",Rules.Where(r=>r.Aliases.Any(a=>normalized.Contains(a,StringComparison.OrdinalIgnoreCase))).Select(r=>r.Name).Distinct());}
 public static string ForFont(FontRecord f)=>string.Join(" ",new[]{Extract(f.Family+" "+f.Face+" "+f.Summary),f.SupportsChinese==true?"中文":f.SupportsChinese==false?"西文":""}.Where(s=>s.Length>0));
 public static string[] Terms(string q)=>Regex.Split(q.Trim(),@"[\s,，+＋;；]+").Where(x=>x.Length>0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
 public static string MatchReason(FontRecord f,string query)=>string.Join("；",Terms(query).Select(t=>f.ManualTags.Contains(t,StringComparison.OrdinalIgnoreCase)?t+"（人工标签）":f.AutoTags.Contains(t,StringComparison.OrdinalIgnoreCase)?t+"（规则推断）":f.Summary.Contains(t,StringComparison.OrdinalIgnoreCase)?t+"（介绍）":t+"（字体信息）"));
}
