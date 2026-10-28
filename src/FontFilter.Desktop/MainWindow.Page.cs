using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 readonly ListBox pageCandidates=new(){Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0,0,24,0),ClipToBounds=true};
 readonly Viewbox pageFit=new(){Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch,Margin=new Thickness(14)};
 readonly TextBlock pageCaption=Ui.Text("",11);
 readonly NumericUpDown paperWidth=PageNumber(185,50,1200),paperHeight=PageNumber(260,50,1200);
 readonly ComboBox paperPreset=new(){HorizontalAlignment=HorizontalAlignment.Stretch};
 readonly ComboBox bookFace=new(){HorizontalAlignment=HorizontalAlignment.Stretch};
 readonly ComboBox bookSample=new(){HorizontalAlignment=HorizontalAlignment.Stretch};
 BookPageDocument? pageDocument;BookPagePreviewWindow? pageWindow;
 bool updatingBook;FontRecord? bookCandidate;
 static readonly (string Name,int Width,int Height)[] PaperPresets={
  ("大16开 · 210 × 285",210,285),("大32开 · 140 × 203",140,203),
  ("正度4开 · 370 × 520",370,520),("正度8开 · 260 × 370",260,370),("正度16开 · 185 × 260",185,260),("正度32开 · 130 × 185",130,185),("正度64开 · 92 × 185",92,185),
  ("另一组大16开 · 210 × 297",210,297),("另一组大32开 · 148 × 210",148,210),("另一组大64开 · 105 × 144",105,144),("异形 · 170 × 240",170,240),("异形 · 170 × 230",170,230)};
 readonly StackPanel bookPages=new(){Spacing=20,HorizontalAlignment=HorizontalAlignment.Center};
 readonly NumericUpDown pageTop=PageNumber(20,0,70),pageBottom=PageNumber(20,0,70),pageInner=PageNumber(22,0,60),pageOuter=PageNumber(18,0,60),pageSize=PageNumber(12,6,48),pageLeading=PageNumber(1.6m,1,3,.1m),pageTracking=PageNumber(0,-2,10,.1m);
 static NumericUpDown PageNumber(decimal value,decimal min,decimal max,decimal increment=1)=>new(){Value=value,Minimum=min,Maximum=max,Increment=increment,Width=126,FormatString="0.#"};
 TextBlock CopyableName(string label,double size,string? family=null){var block=Ui.Text(label,size);block.Cursor=new Cursor(StandardCursorType.Hand);ToolTip.SetTip(block,"点击复制字体名称");block.Tapped+=async(_,_)=>await CopyFontName(family??label);return block;}
 async Task CopyFontName(string name){try{if(Clipboard is {} clipboard){await clipboard.SetTextAsync(name);status.Text="已复制字体名称："+name;}}catch(Exception ex){status.Text="复制失败："+ex.Message;}}
 Control BuildPageTypesetting()
 {
  var inputs=new[]{pageTop,pageBottom,pageInner,pageOuter,pageSize,pageLeading,pageTracking};for(var i=0;i<inputs.Length;i++)inputs[i].Value=(decimal)Settings.PageLayout[i];
  paperWidth.Value=(decimal)Settings.PageWidth;paperHeight.Value=(decimal)Settings.PageHeight;
  paperPreset.ItemsSource=PaperPresets.Select(p=>p.Name).Append("自定义尺寸").ToArray();SyncPaperPreset();
  paperPreset.SelectionChanged+=(_,_)=>{if(updatingBook||!ready||paperPreset.SelectedIndex<0||paperPreset.SelectedIndex>=PaperPresets.Length)return;var p=PaperPresets[paperPreset.SelectedIndex];updatingBook=true;paperWidth.Value=p.Width;paperHeight.Value=p.Height;updatingBook=false;PaperChanged();};
  paperWidth.ValueChanged+=(_,_)=>PaperChanged();paperHeight.ValueChanged+=(_,_)=>PaperChanged();
  var controls=new WrapPanel{Orientation=Orientation.Horizontal};
  foreach(var pair in new[]{("天头 mm",pageTop),("地脚 mm",pageBottom),("订口 mm",pageInner),("切口 mm",pageOuter),("字号 pt",pageSize),("行距 倍",pageLeading),("字距 pt",pageTracking)}){var field=Ui.Stack(Ui.Text(pair.Item1,10),pair.Item2);field.Spacing=6;field.Margin=new Thickness(0,0,12,12);controls.Children.Add(field);pair.Item2.ValueChanged+=(_,_)=>{if(ready){Settings.PageLayout=inputs.Select(v=>(double)(v.Value??0)).ToArray();SaveSettings();RenderPageTypesetting();}};}
  pageCandidates.Classes.Add("bookCandidates");ScrollViewer.SetHorizontalScrollBarVisibility(pageCandidates,Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
  pageCandidates.ItemTemplate=new FuncDataTemplate<FontRow>((row,_)=>{var content=Ui.Stack(CopyableName(row?.Title??"",13),Ui.Text(row?.Subtitle??"",10));content.Spacing=6;try{if(row?.Fonts.FirstOrDefault() is {} font)content.Children.Add(new GlyphPreview{Glyph=FontScanner.Resolve(font),Text="山川有风骨，文字见精神。",Size=16,LineSpacing=1.3});}catch{}var card=Ui.Card(content,10);card.Classes.Add("bookCandidate");card.ClearValue(Border.BackgroundProperty);card.ClipToBounds=true;return card;});
  pageCandidates.SelectionChanged+=(_,_)=>{if(!updatingBook&&pageCandidates.SelectedItem is FontRow row){bookCandidate=row.Fonts[0];RenderPageTypesetting();}};
  bookFace.ItemTemplate=new FuncDataTemplate<FontRecord>((f,_)=>Ui.Text(f?.Face??"",12));bookFace.SelectionChanged+=(_,_)=>{if(ready&&!updatingBook&&bookFace.SelectedItem is FontRecord font){if(bookCandidate!=null){bookCandidate=font;RenderPageTypesetting();}else{changing=true;faceBox.SelectedItem=font;changing=false;SelectFont(font);}}};
  bookSample.SelectionChanged+=(_,_)=>{if(ready&&!updatingBook&&bookSample.SelectedItem is string mode){Settings.PageSampleMode=mode;SaveSettings();RenderPageTypesetting();}};
  var paperFields=Ui.Row(Ui.Stack(Ui.Text("宽 mm",10),paperWidth),Ui.Stack(Ui.Text("高 mm",10),paperHeight));
  var settings=Ui.Stack(Ui.Text("当前字体字重",10),bookFace,Ui.Heading("页面尺寸",13),paperPreset,paperFields,Ui.Heading("版心与文字",13),controls,Ui.Text("固定 1 P · 段首缩进两字 · 多余文字溢出裁切。",10),Ui.Text("试排样文",10),bookSample,Ui.AsyncButton("编辑样文",EditBookSample));settings.Spacing=12;
  var candidates=Ui.Grid("*","Auto,*");candidates.Name="BookCandidatesColumn";Ui.Put(candidates,Ui.Heading("字体候选",14));Ui.Put(candidates,pageCandidates,0,1);
  var settingsColumn=Ui.Grid("*","Auto,*");settingsColumn.Name="BookSettingsColumn";Ui.Put(settingsColumn,Ui.Heading("排版设置",14));Ui.Put(settingsColumn,new ScrollViewer{Content=settings,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled},0,1);
  var preview=Ui.Grid("*","*,Auto");pageFit.Child=bookPages;pageFit.Name="BookPageFit";Ui.Put(preview,pageFit);pageCaption.HorizontalAlignment=HorizontalAlignment.Center;pageCaption.VerticalAlignment=VerticalAlignment.Center;pageCaption.Margin=new Thickness(8,0);var previewFooter=Ui.Row(pageCaption,Ui.Button("独立预览 ↗",OpenPagePreview));previewFooter.HorizontalAlignment=HorizontalAlignment.Center;Ui.Put(preview,previewFooter,0,1);
  var layout=Ui.Grid("190,20,*,20,300");Ui.Put(layout,candidates);Ui.Put(layout,preview,2);Ui.Put(layout,settingsColumn,4);return layout;
 }
 void OpenPagePreview(){if(pageDocument==null)return;if(pageWindow!=null){if(pageWindow.WindowState==WindowState.Minimized)pageWindow.WindowState=WindowState.Normal;pageWindow.Activate();return;}pageWindow=new BookPagePreviewWindow(this,pageDocument,Settings,SaveSettings);pageWindow.Closed+=(_,_)=>pageWindow=null;pageWindow.Show(this);}

 void SyncPaperPreset(){updatingBook=true;var index=Array.FindIndex(PaperPresets,p=>Math.Abs(p.Width-(double)(paperWidth.Value??185))<.01&&Math.Abs(p.Height-(double)(paperHeight.Value??260))<.01);paperPreset.SelectedIndex=index<0?PaperPresets.Length:index;updatingBook=false;}
 void PaperChanged(){if(!ready||updatingBook)return;Settings.PageWidth=(double)(paperWidth.Value??185);Settings.PageHeight=(double)(paperHeight.Value??260);SyncPaperPreset();SaveSettings();RenderPageTypesetting();}
 async Task EditBookSample(){var editor=Ui.Input(CurrentBookText(),null,true);editor.MaxLength=4000;editor.Height=280;var layout=Ui.Stack(Ui.Heading("页面试排样文",18),editor);var window=Ui.Window("编辑样文",layout,600,430);layout.Margin=new Thickness(20);layout.Children.Add(Ui.Button("应用样文",()=>{Settings.PageSampleText=editor.Text??"";Settings.PageSampleMode=FontSamples.FreeKey;SaveSettings();RenderPageTypesetting();window.Close();},"primary"));await window.ShowDialog(this);}

 string CurrentBookText()=>Settings.PageSampleMode==BookSamples.DefaultKey?BookSamples.Text:Settings.PageSampleMode==BookSamples.TraditionalKey?BookSamples.Traditional:Settings.PageSampleMode==BookSamples.EnglishKey?BookSamples.English:Settings.PageSampleMode==BookSamples.MixedKey?BookSamples.Mixed:Settings.PageSampleMode==FontSamples.FreeKey?Settings.PageSampleText:Settings.Samples.GetValueOrDefault(Settings.PageSampleMode)??FontSamples.Defaults.GetValueOrDefault(Settings.PageSampleMode)??CurrentText();

 void RenderPageTypesetting()
 {
  updatingBook=true;
  if(bookCandidate!=null&&!State.Comparison.Any(f=>f.Id==bookCandidate.Id))bookCandidate=null;
  if(State.Comparison.Count>0)bookCandidate??=State.Comparison.FirstOrDefault(f=>f.Id==State.Selected?.Id)??State.Comparison[0];
  var candidateRows=State.Comparison.Select(f=>new FontRow(f.Family,f.Face,f.Tags,new(){f})).ToList();pageCandidates.ItemsSource=candidateRows;pageCandidates.SelectedItem=candidateRows.FirstOrDefault(r=>r.Fonts[0].Id==bookCandidate?.Id);
  var font=bookCandidate??State.Selected;bookFace.ItemsSource=bookCandidate!=null?new[]{bookCandidate}:matches.Where(f=>f.Family==font?.Family).ToArray();bookFace.SelectedItem=font;bookSample.ItemsSource=new[]{BookSamples.DefaultKey,BookSamples.TraditionalKey,BookSamples.EnglishKey,BookSamples.MixedKey,FontSamples.FreeKey}.Concat(Settings.Samples.Keys).Distinct().ToArray();bookSample.SelectedItem=Settings.PageSampleMode;updatingBook=false;
  pageDocument=null;pageWindow?.SetPage(null);bookPages.Children.Clear();if(font==null){pageCaption.Text="请选择字体";return;}
  try{
   var glyph=FontScanner.Resolve(font);const double mm=96/25.4,pt=96/72.0;var top=(double)(pageTop.Value??20)*mm;var bottom=(double)(pageBottom.Value??20)*mm;var inner=(double)(pageInner.Value??22)*mm;var outer=(double)(pageOuter.Value??18)*mm;var width=(double)(paperWidth.Value??185)*mm;var height=(double)(paperHeight.Value??260)*mm;var textWidth=width-inner-outer;var textHeight=height-top-bottom;var size=(double)(pageSize.Value??12)*pt;var leading=(double)(pageLeading.Value??1.6m);var tracking=(double)(pageTracking.Value??0)*pt;
   if(textWidth<3*size+2*Math.Max(0,tracking)||textHeight<size*leading){pageCaption.Text="边距过大，请缩小边距";bookPages.Children.Add(Ui.Text("请为版心留出至少一行文字的空间。",13));return;}
   var lines=new List<string>();using var paint=new SKPaint{Typeface=glyph,TextSize=(float)size};
   var perPage=Math.Max(1,(int)Math.Floor((textHeight-size-Math.Max(0,paint.FontMetrics.Descent))/(size*leading))+1);
   double Measure(string token){double advance=0;foreach(var rune in token.EnumerateRunes()){var amount=rune.Value==0x3000||glyph.GetGlyph(rune.Value)==0?size:Math.Max(size*.08,paint.MeasureText(rune.ToString()));advance+=Math.Max(1,amount+tracking);}return advance;}
   lines=BookLineBreaker.Lines(CurrentBookText(),textWidth,Math.Max(1,size+tracking),Measure,perPage+2).ToList();
   pageCaption.Text=$"1 P · {paperWidth.Value:0.#} × {paperHeight.Value:0.#} mm";
   {
    var left=inner;var right=outer;
    pageDocument=new BookPageDocument(glyph,string.Join("\n",lines.Take(perPage)),width,height,textWidth,textHeight,left,top,right,bottom,size,leading,tracking);
    var sheet=pageDocument.CreateSheet(true);var paper=new Border{Child=sheet,BorderBrush=new SolidColorBrush(Color.Parse("#DCE5DD")),BorderThickness=new Thickness(1),BoxShadow=new BoxShadows(new BoxShadow{Blur=10,OffsetY=3,Color=Color.Parse("#18000000")}),Cursor=new Cursor(StandardCursorType.Hand)};ToolTip.SetTip(paper,"点击打开独立等大预览");paper.Tapped+=(_,_)=>OpenPagePreview();bookPages.Children.Add(paper);pageWindow?.SetPage(pageDocument);
   }
  }catch(Exception ex){bookPages.Children.Add(Ui.Text("页面试排未能显示："+ex.Message,12));}
 }
}
