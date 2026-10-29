using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
namespace FontFilter.Desktop;
public static class Verification
{
 public static async Task Run(MainWindow window)
 {
  var first=await window.ScanFonts();if(!first.Complete||first.Faces==0)throw new Exception("real font scan failed");
  var second=await window.ScanFonts();if(second.Faces!=first.Faces||second.Reused!=second.Faces)throw new Exception($"incremental scan mismatch: first={first}; second={second}");
  await window.VerifyInteractions();await window.VerifyLiveTheme();window.SelectForProof();window.State.ClearCompare();
  var fonts=window.Store.All();var chinese=FontOrdering.Order(fonts).First();var glyph=FontScanner.Resolve(chinese);var file=await FontFileLocator.FindAsync(chinese);if(file==null||!File.Exists(file))throw new Exception("cannot locate real system font file");var missing=GlyphCoverage.Missing("中文A𠮷",v=>glyph.GetGlyph(v)!=0);
  window.State.AddCompare(chinese);foreach(var f in FontOrdering.Order(fonts).Where(f=>f.Family!=chinese.Family).Take(2))window.State.AddCompare(f);window.RenderPreviews();
  await Task.Delay(700);window.UpdateLayout();
  using(var bitmap=new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width,(int)window.Bounds.Height),new Vector(96,96))){bitmap.Render(window);bitmap.Save(Path.Combine(FontFilter.App.DataDirectory,"workspace.png"));}
  window.ShowComparisonForProof();await Task.Delay(200);window.UpdateLayout();
  using(var bitmap=new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width,(int)window.Bounds.Height),new Vector(96,96))){bitmap.Render(window);bitmap.Save(Path.Combine(FontFilter.App.DataDirectory,"comparison.png"));}
  File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"verification.txt"),$"SCAN faces={first.Faces}, families={first.Families}, errors={first.Errors}, complete={first.Complete}\nINCREMENTAL reused={second.Reused}, faces={second.Faces}, errors={second.Errors}\nRESOLVE family={chinese.Family}, face={chinese.Face}, missing={missing}\nFILE located={Path.GetFileName(file)}\nCOMPARE count={window.State.Comparison.Count}\n");
 }
}
