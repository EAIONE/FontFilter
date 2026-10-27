using FontFilter;
public static class WorkspaceTests
{
 public static void Run(Action<string,Action> test,Action<bool,string> check,string root)
 {
  test("scan cancellation and errors preserve unseen records",()=>{
   using var s=new FontStore(Path.Combine(root,"scan.db"));s.Upsert(new("old","已知","Regular","x",""));
   ScanCommit.Finish(s,new HashSet<string>(),false,0);check(s.All().Count==1,"cancel removed fonts");
   ScanCommit.Finish(s,new HashSet<string>(),true,1);check(s.All().Count==1,"errors removed fonts");
   ScanCommit.Finish(s,new HashSet<string>(),true,0);check(s.All().Count==0,"complete scan retained absent font");
  });
  test("comparison deduplicates and limits six styles",()=>{var w=new WorkspaceState();for(int i=0;i<7;i++)w.AddCompare(new(i.ToString(),"家族"+i,"Regular","x",""));check(w.Comparison.Count==6,"cap ignored");w.AddCompare(w.Comparison[0]);check(w.Comparison.Count==6,"duplicate allowed");w.RemoveCompare("0");check(w.Comparison.Count==5,"remove failed");});
  test("AI tag merge preserves pending edits",()=>{var w=new WorkspaceState();w.Select(new("1","字体","Regular","x",""){ManualTags="原标签"});w.ManualText="尚未保存";w.MergeSuggestions(new[]{"古籍"});check(w.ManualText=="尚未保存 古籍","pending edits overwritten");});
  test("pending edits persist when selecting another font",()=>{using var s=new FontStore(Path.Combine(root,"edits.db"));var a=new FontRecord("1","字体1","Regular","x","");var b=a with{Id="2",Family="字体2"};s.Upsert(a);s.Upsert(b);var w=new WorkspaceState();w.Select(a);w.ManualText="手工";w.SavePending(s);w.Select(b);check(s.All().Single(f=>f.Id=="1").ManualTags=="手工","unsaved edits lost");});
  test("supplementary missing glyphs use Unicode scalar values",()=>{var missing=GlyphCoverage.Missing("中𠮷A𠮷",v=>v==0x4e2d||v==65);check(missing=="𠮷","surrogate split or duplicates");});
 }
}
