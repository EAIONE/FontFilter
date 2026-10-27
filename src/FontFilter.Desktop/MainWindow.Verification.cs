using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 public async Task VerifyInteractions()
 {
  void Check(bool condition,string message){if(!condition)throw new Exception(message);}
  Check(State.Selected!=null,"selection missing after scan");var first=State.Selected!;manual.Text="迁移验证标签";
  query.Text="不会匹配任何字体_7b4e";await Task.Delay(100);Check(Store.All().Single(f=>f.Id==first.Id).ManualTags=="迁移验证标签","UI lost pending manual edits");Check(State.Selected==null,"empty result retained old selection");
  query.Text="迁移验证标签";await Task.Delay(100);Check(matches.Count==1&&State.Selected?.Id==first.Id,"AND search / selection refresh failed");
  query.Text="";await Task.Delay(100);groupBox.IsChecked=false;Check(fontList.ItemCount==matches.Count,"ungrouped list omitted styles");groupBox.IsChecked=true;
  favoriteButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(State.Selected?.IsFavorite==true,"favorite UI did not persist");favoriteFilter.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(matches.All(f=>f.IsFavorite)&&matches.Count>0,"favorite filter failed");favoriteFilter.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
  var firstTask=PickTags(true);await Task.Delay(100);var picker=OwnedWindows.Last();Check(picker.GetVisualDescendants().OfType<CheckBox>().Count()>100,"tag picker failed to render catalogue");picker.Close();await firstTask;
  var catalogueTask=OpenCatalogue();await Task.Delay(100);var cat=OwnedWindows.Last();var list=cat.GetVisualDescendants().OfType<ListBox>().Single();Check(list.ItemCount>=4900,"offline catalogue truncated");list.SelectedIndex=0;cat.Close();await catalogueTask;
  var themeTask=Dialogs.Theme(this,Settings,SaveSettings);await Task.Delay(100);var theme=OwnedWindows.Last();Check(theme.GetVisualDescendants().OfType<Slider>().Count()==3,"theme HSB sliders missing");theme.Close();await themeTask;
  var apiTask=Dialogs.ApiSettings(this,Settings,SaveSettings);await Task.Delay(100);var api=OwnedWindows.Last();Check(api.GetVisualDescendants().OfType<TextBox>().Any(t=>t.PasswordChar!='\0'),"API key not masked");api.Close();await apiTask;
  Store.SetManual(new[]{first.Id},"");RefreshRows();
  var currentPreview=previewPanel.GetVisualDescendants().OfType<GlyphPreview>().First();
  int Ink(){using var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(700,250),new Avalonia.Vector(96,96));bitmap.Render(currentPreview);using var stream=new MemoryStream();bitmap.Save(stream);stream.Position=0;using var decoded=SkiaSharp.SKBitmap.Decode(stream);return decoded.Pixels.Count(p=>p.Alpha>20);}
  await Task.Delay(100);Check(Ink()>50,"initial glyph preview empty");tabs.SelectedIndex=1;await Task.Delay(100);tabs.SelectedIndex=0;await Task.Delay(100);Check(Ink()>50,"tab switching blanked glyph preview");
  var editorExpander=previewPanel.Children.OfType<Expander>().Single();editorExpander.IsExpanded=true;await Task.Delay(100);
  var editors=editorExpander.GetVisualDescendants().OfType<TextBox>().ToList();editors.First(t=>t.AcceptsReturn).Text="尚未应用的样文草稿";editors.First(t=>!t.AcceptsReturn).Text="尚未保存的名称";await Task.Delay(100);sizeInput.Value+=1;await Task.Delay(100);
  var replacement=previewPanel.Children.OfType<Expander>().Single();replacement.IsExpanded=true;await Task.Delay(100);var replacements=replacement.GetVisualDescendants().OfType<TextBox>().ToList();Check(replacements.First(t=>t.AcceptsReturn).Text=="尚未应用的样文草稿"&&replacements.First(t=>!t.AcceptsReturn).Text=="尚未保存的名称","sample refresh discarded drafts");
  var previousSize=Settings.Size;var previousMode=Settings.SampleMode;var previousText=Settings.Sample;
  Settings.Size=120;Settings.SampleMode=FontSamples.FreeKey;Settings.Sample=new string('字',4000);RenderPreviews();await Task.Delay(100);UpdateLayout();
  using(var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize(1160,740),new Avalonia.Vector(96,96)))bitmap.Render(this);
  Settings.Size=previousSize;Settings.SampleMode=previousMode;Settings.Sample=previousText;RenderPreviews();
 }
 public void SelectForProof(){sampleExpander.IsExpanded=false;query.Text="";var preferred=matches.FirstOrDefault(f=>f.Family.Contains("宋"))??matches.First();var row=fontList.Items.OfType<FontRow>().First(r=>r.Fonts.Any(f=>f.Id==preferred.Id));fontList.SelectedItem=row;}
 public void ShowComparisonForProof()=>tabs.SelectedIndex=1;
}
