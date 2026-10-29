namespace FontFilter;
public sealed class WorkspaceState
{
 readonly List<FontRecord> comparison=new();public IReadOnlyList<FontRecord> Comparison=>comparison;
 public FontRecord? Selected{get;private set;}public string ManualText{get;set;}="";
 public void Select(FontRecord? font){Selected=font;ManualText=font?.ManualTags??"";}
 public void SavePending(FontStore store){if(Selected is {} f&&ManualText.Trim()!=f.ManualTags){store.SetManual(new[]{f.Id},ManualText);f.ManualTags=ManualText.Trim();}}
 public void MergeSuggestions(IEnumerable<string> tags)=>ManualText=TagRules.SelectionText(ManualText,tags,false);
 public bool AddCompare(FontRecord font){if(comparison.Count>=6||comparison.Any(f=>f.Id==font.Id))return false;comparison.Add(font);return true;}
 public void RemoveCompare(string id)=>comparison.RemoveAll(f=>f.Id==id);
 public void ClearCompare()=>comparison.Clear();
}
public static class ScanCommit
{
 public static void Finish(FontStore store,ISet<string> seen,bool complete,int errors){if(complete&&errors==0)store.MarkAbsent(seen);}
}
public static class GlyphCoverage
{
 public static string Missing(string text,Func<int,bool> contains)=>string.Concat(text.EnumerateRunes().Where(r=>!System.Text.Rune.IsWhiteSpace(r)&&!contains(r.Value)).Distinct().Take(40).Select(r=>r.ToString()));
}
