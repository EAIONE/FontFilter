using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 public async Task VerifyTagPicker()
 {
  void Check(bool value,string why){if(!value)throw new Exception(why);}
  tagSearchMode=true;tagQueryText="宋体 仿宋";
  var timer=System.Diagnostics.Stopwatch.StartNew();var task=PickTags(true);await Task.Delay(100);
  var dialog=OwnedWindows.Last();
  var openMs=timer.ElapsedMilliseconds;
  var deadline=DateTime.UtcNow.AddSeconds(30);
  while(!dialog.GetVisualDescendants().OfType<CheckBox>().Any()&&DateTime.UtcNow<deadline)await Task.Delay(50);
  var boxes=dialog.GetVisualDescendants().OfType<CheckBox>().ToArray();
  Check(boxes.Length>100,"catalogue did not load");
  Check(boxes.All(b=>b.IsChecked!=true),"reopening restored previous selected tags");
  boxes[0].Focus();
  boxes[0].RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyDownEvent,Key=Avalonia.Input.Key.Space});
  boxes[0].RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyUpEvent,Key=Avalonia.Input.Key.Space});
  Check(boxes[0].IsChecked==true,"compact checkbox cannot toggle with keyboard");boxes[0].IsChecked=false;
  var selected=dialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="SelectedTags");
  boxes[0].IsChecked=true;boxes[1].IsChecked=true;
  Check(selected.Text!.Contains("黑体")&&selected.Text.Contains("宋体"),"selected tags missing above catalogue");
  var search=dialog.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="TagSearch");search.Text="手写";
  Check(boxes[0].IsChecked==true,"search discarded hidden selection");
  dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="ClearTags").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  Check(boxes.All(b=>b.IsChecked!=true)&&string.IsNullOrEmpty(selected.Text),"clear missed hidden selected tags");
  search.Text="";dialog.UpdateLayout();
  var first=boxes[0].TranslatePoint(new Point(),dialog)!.Value;var nextRow=boxes.First(b=>b.TranslatePoint(new Point(),dialog)!.Value.Y>first.Y+1).TranslatePoint(new Point(),dialog)!.Value;
  Check(nextRow.Y-first.Y<=23,"tag rows are not compact");
  boxes[0].IsChecked=true;await Task.Delay(50);dialog.UpdateLayout();
  while(!boxes[0].Content!.ToString()!.Contains("  ")&&DateTime.UtcNow<deadline)await Task.Delay(50);
  Check(boxes[0].Content!.ToString()!.Contains("  "),"background counts did not arrive");
  using(var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize((int)dialog.Bounds.Width,(int)dialog.Bounds.Height),new Vector(96,96))){bitmap.Render(dialog);bitmap.Save(Path.Combine(FontFilter.App.DataDirectory,"tag-picker-updated.png"));}
  dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="ApplyTags").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await task;
  Check(tagSearchMode&&tagQueryText=="黑体","selected tag did not apply to font results");
  task=PickTags(true);await Task.Delay(100);dialog=OwnedWindows.Last();
  Check(!dialog.GetVisualDescendants().OfType<CheckBox>().Any(b=>b.IsChecked==true),"new visit retained selection");dialog.Close();await task;
  File.WriteAllText(Path.Combine(FontFilter.App.DataDirectory,"tag-picker-pass.txt"),$"PASS: empty on open, selected display, filtered clear, compact rows, background counts, apply, reopen. Open observed at {openMs}ms; total {timer.ElapsedMilliseconds}ms");
 }
}
