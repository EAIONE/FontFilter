using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
namespace FontFilter.Desktop;
public sealed record FontRow(string Title,string Subtitle,string Tags,List<FontRecord> Fonts);
public sealed partial class MainWindow:Window
{
 public readonly FontStore Store;
 public UserSettings Settings;
 public readonly WorkspaceState State=new();
 readonly FontScanner scanner=new();CancellationTokenSource? scanCancellation;Task<ScanResult>? scanTask;
 readonly TextBox query=Ui.Input("","搜索字体、特征、用途，如：宋体 古籍");
 readonly ListBox fontList=new(){SelectionMode=SelectionMode.Multiple,Background=Brushes.Transparent,BorderThickness=new Thickness(0)};
 readonly ComboBox faceBox=new(){HorizontalAlignment=HorizontalAlignment.Stretch};
 readonly CheckBox groupBox=new(){Content="合并同一家族字重",IsChecked=true};
 readonly TextBlock counts=Ui.Text("尚未扫描字体",11),title=Ui.Heading("选择一款字体，开始试排",27),details=Ui.Text("在左侧选择字体",12),status=Ui.Text("准备就绪",11),missing=Ui.Text("",11,Brushes.IndianRed);
 readonly TextBox manual=Ui.Input("","如：风骨 古籍",true);
 readonly TextBlock summary=Ui.Text("暂无介绍，可补充官网资料。",12);
 readonly WrapPanel quickTags=new(){Orientation=Orientation.Horizontal};
 readonly Button favoriteButton=new(){Content="☆ 收藏当前家族"},favoriteFilter=new(){Content="★ 我的收藏"};
 readonly Button scanButton=new(){Content="↻ 更新字体库"},pauseButton=new(){Content="暂停",IsEnabled=false},cancelButton=new(){Content="取消",IsEnabled=false};
 readonly ComboBox sampleBox=new(){MinWidth=130};
 readonly NumericUpDown sizeInput=new(){Minimum=12,Maximum=120,Increment=1,Width=120,FormatString="0"};
 readonly NumericUpDown lineInput=new(){Minimum=1,Maximum=3,Increment=.1m,Width=120,FormatString="0.0"};
 readonly StackPanel previewPanel=new(){Spacing=16},comparePanel=new(){Spacing=16};
 readonly WrapPanel comparisonChips=new();readonly TabControl tabs=new();readonly List<Button> workspaceModeButtons=new();
 readonly TextBox sampleEditor=Ui.Input("",null,true),sampleName=Ui.Input("","保存为样文名称");
 readonly Expander sampleExpander=new(){Header="编辑试排文字",IsExpanded=false};
 string sampleSeed="";bool sampleDraftDirty;
 bool ready,changing,onlyFavorites,closingAllowed,closePending;OfficialCatalogue? catalogue;
 List<FontRecord> matches=new();
 bool tagSearchMode;string? tagQueryText;
 Control? filterControls;Border? selectedCard;readonly TextBlock favoriteNotice=Ui.Text("",10);
 Grid? bodyGrid;Border? detailsCard;Control? detailsBody;TextBlock? detailsHeading;Button? detailsToggle;
 public MainWindow()
 {
  Title="字里行间 · 字体筛选与试排";Width=1440;Height=920;MinWidth=1160;MinHeight=740;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  FontFamily=new FontFamily(OperatingSystem.IsMacOS()?"PingFang SC":"Microsoft YaHei UI");
  Settings=SettingsStore.Load();ThemeService.Apply(Settings);
  Store=new FontStore(Path.Combine(FontFilter.App.DataDirectory,"fonts.db"));Store.RefreshRuleTags();
  try{catalogue=OfficialCatalogue.Load(Path.Combine(AppContext.BaseDirectory,"Data","official-fonts.json"));}catch(Exception ex){status.Text="官网资料加载失败："+ex.Message;}
  using(var stream=AssetLoader.Open(new Uri("avares://FontFilter/Assets/FontFilter.ico")))Icon=new WindowIcon(stream);
  sampleExpander.Content=BuildSampleEditor();Build();Wire();ready=true;RefreshRows();
  Closing+=async(_,e)=>{if(closingAllowed)return;e.Cancel=true;if(closePending)return;closePending=true;try{if(scanTask is {IsCompleted:false}){scanCancellation?.Cancel();scanner.Resume();await scanTask;}SavePending();SaveSettings();ready=false;Store.Dispose();scanner.Dispose();closingAllowed=true;Close();}catch(Exception ex){closePending=false;await Dialogs.Message(this,"未能退出",ex.Message);}};

 }
 void Build()
 {
  var root=Ui.Grid("*","86,*,Auto,30");
  var header=Ui.Grid("210,*,Auto");header.Margin=new Thickness(24,0);
  using(var image=AssetLoader.Open(new Uri("avares://FontFilter/Assets/FontFilter.png"))){var logo=new Image{Name="BrandLogo",Source=new Bitmap(image),Width=44,Height=44,VerticalAlignment=VerticalAlignment.Center};var brand=Ui.Grid("44,12,*");brand.Name="BrandBlock";brand.VerticalAlignment=VerticalAlignment.Center;brand.Height=48;Ui.Put(brand,logo);var words=new StackPanel{Spacing=2,VerticalAlignment=VerticalAlignment.Center};words.Children.Add(new TextBlock{Name="BrandTitle",Text="字里行间",FontSize=23,LineHeight=28,FontWeight=FontWeight.SemiBold,Classes={"brand"}});words.Children.Add(new TextBlock{Name="BrandSubtitle",Text="FONT ATELIER",FontSize=9,LineHeight=12,Classes={"brand"},Opacity=.72});Ui.Put(brand,words,2);Ui.Put(header,brand);}

  query.Margin=new Thickness(20,0,24,0);query.VerticalAlignment=VerticalAlignment.Center;Ui.Put(header,query,1);
  scanButton.Classes.Add("primary");var actions=Ui.Row(Ui.AsyncButton("主题",()=>Dialogs.Theme(this,Settings,SaveSettings),"header"),Ui.AsyncButton("官网资料",OpenCatalogue,"header"),Ui.AsyncButton("API 设置",()=>Dialogs.ApiSettings(this,Settings,SaveSettings),"header"),scanButton);Ui.Put(header,actions,2);
  var headerBorder=new Border{Child=header};headerBorder.Classes.Add("appHeader");Ui.Put(root,headerBorder);
  var body=Ui.Grid("265,18,*,18,265");bodyGrid=body;body.Margin=new Thickness(24,20,24,16);
  favoriteFilter.HorizontalAlignment=HorizontalAlignment.Stretch;favoriteFilter.Classes.Add("tag");
  filterControls=Ui.Stack(Ui.Text("常用标签",10),new ScrollViewer{Content=quickTags,MaxHeight=110,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled},Ui.AsyncButton("按特征挑选字体  →",()=>PickTags(true),"primary"),groupBox);
  favoriteNotice.Foreground=new SolidColorBrush(Color.Parse("#C23E3E"));favoriteNotice.IsVisible=false;var leftTop=Ui.Stack(Ui.Heading("本地字体"),favoriteFilter,favoriteNotice,filterControls,counts);
  var left=Ui.Grid("*","Auto,*");leftTop.Margin=new Thickness(5,0,5,12);Ui.Put(left,leftTop);Ui.Put(left,fontList,0,1);
  fontList.ItemTemplate=new FuncDataTemplate<FontRow>((row,_)=>{var name=CopyableName(row?.Title??"",14);name.FontWeight=FontWeight.SemiBold;name.TextTrimming=TextTrimming.CharacterEllipsis;return new StackPanel{Spacing=3,Children={name,new TextBlock{Text=row?.Subtitle??"",FontSize=10,Classes={"muted"}},new TextBlock{Text=row?.Tags??"",FontSize=10,Classes={"fontTags"},TextTrimming=TextTrimming.CharacterEllipsis}}};});
  fontList.PointerEntered+=(_,_)=>{if(filterControls!=null)filterControls.IsVisible=false;};
  leftTop.PointerEntered+=(_,_)=>{if(filterControls!=null)filterControls.IsVisible=true;};

  Ui.Put(body,Ui.Card(left,12));
  var main=Ui.Grid("*","Auto,Auto,Auto,*");var workspaceHeader=Ui.Grid("Auto,*,Auto","Auto,Auto");workspaceHeader.Margin=new Thickness(0,0,0,16);Ui.Put(workspaceHeader,Ui.Heading("试排工作台",22));var reveal=Ui.AsyncButton("打开字体位置 ↗",RevealCurrentFont);reveal.VerticalAlignment=VerticalAlignment.Top;reveal.Margin=new Thickness(0,4,0,0);var modes=Ui.Row();modes.HorizontalAlignment=HorizontalAlignment.Left;modes.Margin=new Thickness(20,0,16,0);foreach(var label in new[]{"字体试排","字体对比","页面试排"}){var index=workspaceModeButtons.Count;var button=Ui.Button(label,()=>tabs.SelectedIndex=index);button.FontSize=15;button.Padding=new Thickness(12,10);button.MinHeight=42;workspaceModeButtons.Add(button);modes.Children.Add(button);}workspaceModeButtons[0].Classes.Add("primary");Ui.Put(workspaceHeader,modes,1);Ui.Put(workspaceHeader,reveal,2);Ui.Put(main,workspaceHeader);workspaceHeader.SizeChanged+=(_,_)=>{var narrow=workspaceHeader.Bounds.Width<620;workspaceHeader.ColumnDefinitions[2].Width=narrow?new GridLength(0):GridLength.Auto;Grid.SetColumn(reveal,narrow?0:2);Grid.SetColumnSpan(reveal,narrow?3:1);Grid.SetRow(reveal,narrow?1:0);reveal.HorizontalAlignment=HorizontalAlignment.Right;reveal.Margin=new Thickness(0,narrow?12:4,0,0);};
  var selected=Ui.Stack(Ui.Text("当前字体",10),title,faceBox,favoriteButton);selectedCard=Ui.Card(selected);Ui.Put(main,selectedCard,0,1);
  title.Tapped+=async(_,_)=>{if(State.Selected is {} f)await CopyFontName(f.Family);};ToolTip.SetTip(title,"点击复制字体名称");title.Cursor=new Cursor(StandardCursorType.Hand);
  var options=Ui.Row(sampleBox,Ui.Text("字号",11),sizeInput,Ui.Text("行距",11),lineInput);options.Margin=new Thickness(0,16,0,16);Ui.Put(main,options,0,2);
  sizeInput.Value=(decimal)Settings.Size;lineInput.Value=(decimal)Settings.LineHeight;
  RefreshSampleModes();
  tabs.Items.Add(new TabItem{Header="字体试排",Content=new ScrollViewer{Content=previewPanel,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled}});
  tabs.Items.Add(new TabItem{Header="字体对比",Content=new ScrollViewer{Content=comparePanel,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled}});
  tabs.Items.Add(new TabItem{Header="页面试排",Content=BuildPageTypesetting()});
  tabs.SelectionChanged+=(_,_)=>{if(selectedCard!=null)selectedCard.IsVisible=tabs.SelectedIndex==0;options.IsVisible=tabs.SelectedIndex!=2;options.Margin=new Thickness(0,tabs.SelectedIndex==0?16:0,0,16);for(var i=0;i<workspaceModeButtons.Count;i++){if(i==tabs.SelectedIndex)workspaceModeButtons[i].Classes.Add("primary");else workspaceModeButtons[i].Classes.Remove("primary");}ApplyDetailsVisibility();};
  tabs.Classes.Add("workspaceTabs");tabs.Padding=new Thickness(0);tabs.Margin=new Thickness(0);Ui.Put(main,tabs,0,3);Ui.Put(body,main,2);
  manual.MaxLength=4000;manual.Height=100;
  var right=Ui.Stack(details,Ui.Heading("人工标签",14),manual,Ui.Button("应用到左侧所选结果",ApplyManual,"primary"),Ui.AsyncButton("从标签库添加",()=>PickTags(false)),Ui.Text("切换字体时自动保存当前字重的编辑。",10,Brushes.SlateGray),Ui.Heading("介绍与来源",14),summary,Ui.AsyncButton("补充官网介绍",EditMetadata),Ui.AsyncButton("查找官方页面",FindOfficial),Ui.AsyncButton("分析当前字体",AnalyzeCurrent),Ui.Heading("工具",14),Ui.AsyncButton("导入旧版 Data",ImportLegacy),Ui.AsyncButton("打开数据文件夹",()=>OpenFolder(FontFilter.App.DataDirectory)),Ui.Text("数据保存在本机；字体文件不会被复制。",10,Brushes.SlateGray));
  detailsBody=new ScrollViewer{Content=right,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled};detailsHeading=Ui.Heading("字体资料",18);detailsToggle=Ui.Button("→",ToggleDetails);detailsToggle.Name="ToggleDetails";detailsToggle.Padding=new Thickness(4);var infoHeader=Ui.Grid("*,Auto");Ui.Put(infoHeader,detailsHeading);Ui.Put(infoHeader,detailsToggle,1);var infoLayout=Ui.Grid("*","Auto,*");Ui.Put(infoLayout,infoHeader);Ui.Put(infoLayout,detailsBody,0,1);detailsCard=Ui.Card(infoLayout);detailsCard.Name="DetailsCard";Ui.Put(body,detailsCard,4);ApplyDetailsVisibility();Ui.Put(root,body,0,1);
  var footer=Ui.Grid("Auto,*,Auto");footer.Margin=new Thickness(24,10);Ui.Put(footer,Ui.Button("＋ 加入对比",AddSelectedCompare,"primary"));comparisonChips.Margin=new Thickness(16,0);Ui.Put(footer,comparisonChips,1);Ui.Put(footer,Ui.Row(Ui.Button("加入左侧所选",AddAllSelected),Ui.Button("清空",()=>{State.ClearCompare();RenderPreviews();})),2);Ui.Put(root,Ui.Card(footer,0),0,2);
  var bottom=Ui.Grid("*,Auto");bottom.Margin=new Thickness(24,0);status.VerticalAlignment=VerticalAlignment.Center;Ui.Put(bottom,status);pauseButton.Padding=cancelButton.Padding=new Thickness(10,2);pauseButton.MinHeight=cancelButton.MinHeight=23;Ui.Put(bottom,Ui.Row(pauseButton,cancelButton),1);Ui.Put(root,bottom,0,3);
  Content=root;
 }
 void ToggleDetails(){Settings.DetailsCollapsed=!Settings.DetailsCollapsed;ApplyDetailsVisibility();SaveSettings();}
 void ApplyDetailsVisibility(){if(bodyGrid==null||detailsCard==null||detailsBody==null||detailsHeading==null||detailsToggle==null)return;var pageMode=tabs.SelectedIndex==2;detailsCard.IsVisible=!pageMode;if(pageMode){bodyGrid.ColumnDefinitions[4].Width=new GridLength(0);bodyGrid.ColumnDefinitions[3].Width=new GridLength(0);return;}var collapsed=Settings.DetailsCollapsed;bodyGrid.ColumnDefinitions[4].Width=new GridLength(collapsed?42:265);bodyGrid.ColumnDefinitions[3].Width=new GridLength(collapsed?10:18);detailsBody.IsVisible=detailsHeading.IsVisible=!collapsed;detailsCard.Padding=new Thickness(collapsed?0:18);detailsCard.VerticalAlignment=collapsed?VerticalAlignment.Top:VerticalAlignment.Stretch;detailsCard.Background=collapsed?Brushes.Transparent:Brushes.White;detailsCard.BorderThickness=new Thickness(collapsed?0:1);detailsToggle.Content=collapsed?Ui.Stack(new TextBlock{Text="字\n体\n资\n料",FontSize=12,LineHeight=17,TextAlignment=TextAlignment.Center,HorizontalAlignment=HorizontalAlignment.Center},new TextBlock{Text="←",FontSize=15,HorizontalAlignment=HorizontalAlignment.Center}):"→";detailsToggle.VerticalAlignment=VerticalAlignment.Top;detailsToggle.Padding=new Thickness(collapsed?7:4);detailsCard.Margin=new Thickness(0,collapsed?4:0,0,0);ToolTip.SetTip(detailsToggle,collapsed?"展开字体资料":"向右收起字体资料");Avalonia.Automation.AutomationProperties.SetName(detailsToggle,collapsed?"展开字体资料":"收起字体资料");}
 void Wire()
 {
  WireWheelAdjustments();
  query.TextChanged+=(_,_)=>{tagSearchMode=tagQueryText!=null&&query.Text==tagQueryText;if(ready)RefreshRows();};groupBox.IsCheckedChanged+=(_,_)=>{if(ready)RefreshRows();};
  fontList.SelectionChanged+=(_,_)=>{if(changing)return;var row=fontList.SelectedItems?.OfType<FontRow>().LastOrDefault();if(row!=null)SelectRow(row);};
  fontList.DoubleTapped+=(_,_)=>AddSelectedCompare();faceBox.SelectionChanged+=(_,_)=>{if(!changing&&faceBox.SelectedItem is FontRecord f)SelectFont(f);};
  manual.TextChanged+=(_,_)=>{if(!changing)State.ManualText=manual.Text??"";};
  favoriteFilter.Click+=(_,_)=>{onlyFavorites=!onlyFavorites;favoriteFilter.Content=onlyFavorites?"★ 正在看收藏 · 退出收藏":"★ 我的收藏";favoriteNotice.Text="仅看收藏中的筛选结果\n退出收藏，查看全部匹配字体";favoriteNotice.IsVisible=onlyFavorites;RefreshRows();};
  favoriteButton.Click+=(_,_)=>{if(State.Selected is {} f){SavePending();Store.SetFavorite(new[]{f.Family},!f.IsFavorite);RefreshRows(f.Id);}};
  scanButton.Click+=async(_,_)=>{try{await ScanFonts();}catch(Exception ex){await Dialogs.Message(this,"扫描失败",ex.Message);}};
  pauseButton.Click+=(_,_)=>{scanner.TogglePause();pauseButton.Content=scanner.IsPaused?"继续":"暂停";};cancelButton.Click+=(_,_)=>{scanCancellation?.Cancel();scanner.Resume();};
  sampleBox.SelectionChanged+=(_,_)=>{if(ready&&!changing&&sampleBox.SelectedItem is string key){Settings.SampleMode=key;SaveSettings();RenderPreviews();}};
  sizeInput.ValueChanged+=(_,_)=>{if(ready){Settings.Size=(double)(sizeInput.Value??32);SaveSettings();RenderPreviews();}};
  lineInput.ValueChanged+=(_,_)=>{if(ready){Settings.LineHeight=(double)(lineInput.Value??1.6m);SaveSettings();RenderPreviews();}};
 }
 void SaveSettings()=>SettingsStore.Save(Settings);
 void SavePending(){if(!ready)return;State.ManualText=manual.Text??"";State.SavePending(Store);}
 void RefreshSampleModes(){var keys=new[]{FontSamples.AutoKey,FontSamples.FreeKey}.Concat(Settings.Samples.Keys).Concat(FontSamples.Defaults.Keys).Distinct().ToList();sampleBox.ItemsSource=keys;sampleBox.SelectedItem=keys.Contains(Settings.SampleMode)?Settings.SampleMode:FontSamples.AutoKey;}
 public void RefreshRows(string? keep=null)
 {
  SavePending();keep??=State.Selected?.Id;matches=FontOrdering.Order(Store.Search(query.Text??"",onlyFavorites,tagSearchMode)).ToList();changing=true;
  var rows=groupBox.IsChecked==true?matches.GroupBy(f=>f.Family).Select(g=>new FontRow(g.Key,$"{g.Count()} 个字重 / 样式",g.First().Tags,g.ToList())).ToList():matches.Select(f=>new FontRow(f.Family,f.Face,f.Tags,new(){f})).ToList();
  fontList.ItemsSource=rows;counts.Text=$"{rows.Count} 项 · {matches.Count} 个样式 · 中文优先";
  var next=rows.FirstOrDefault(r=>r.Fonts.Any(f=>f.Id==keep))??rows.FirstOrDefault();fontList.SelectedItem=next;changing=false;
  if(next!=null)SelectRow(next,keep);else{State.Select(null);manual.Text="";title.Text="没有匹配字体";faceBox.ItemsSource=null;details.Text="试试其他关键词，或更新字体库。";summary.Text="";RenderPreviews();}
  RenderQuickTags();
 }
 void SelectRow(FontRow row,string? keep=null){SavePending();changing=true;faceBox.ItemTemplate=new FuncDataTemplate<FontRecord>((f,_)=>Ui.Text(f?.Face??""));faceBox.ItemsSource=row.Fonts;var face=row.Fonts.FirstOrDefault(f=>f.Id==keep)??row.Fonts[0];faceBox.SelectedItem=face;changing=false;SelectFont(face);}
 void SelectFont(FontRecord font){SavePending();changing=true;State.Select(font);manual.Text=font.ManualTags;title.Text=font.Family;details.Text=$"{font.Face}\n{(font.SupportsChinese==true?"含常用汉字":"汉字覆盖有限")}  {font.Vendor}";summary.Text=(font.Summary.Length==0?"暂无介绍，可补充官网资料。":font.Summary)+(font.Url.Length>0?"\n来源："+font.Url:"");favoriteButton.Content=font.IsFavorite?"★ 已收藏 · 点击取消":"☆ 收藏当前家族";changing=false;RenderPreviews();}
 void ApplyTagSearch(IEnumerable<string> tags){tagQueryText=string.Join(" ",tags.Distinct(StringComparer.OrdinalIgnoreCase));tagSearchMode=true;query.Text=tagQueryText;if(ready)RefreshRows();}
 void RenderQuickTags(){quickTags.Children.Clear();foreach(var tag in Settings.FrequentTags().Concat(new[]{"宋体","黑体","楷书","隶书","古籍","清雅"}).Distinct().Take(12)){var b=Ui.Button(tag,()=>ApplyTagSearch(TagRules.Terms(query.Text??"").Concat(new[]{tag})),"tag");b.Margin=new Thickness(0,0,5,5);quickTags.Children.Add(b);}}
 void ApplyManual(){var rows=fontList.SelectedItems?.OfType<FontRow>().ToList()??new();var ids=rows.SelectMany(r=>r.Fonts).Select(f=>f.Id).Distinct().ToArray();if(ids.Length==0)return;Store.SetManual(ids,manual.Text??"");if(State.Selected!=null)State.Selected.ManualTags=manual.Text?.Trim()??"";Settings.RecordTags(TagRules.Terms(manual.Text??""));SaveSettings();status.Text=$"已保存 {ids.Length} 个字重的人工标签";RefreshRows();}
 IEnumerable<FontRecord> SelectedFonts()=>fontList.SelectedItems?.OfType<FontRow>().Select(r=>State.Selected!=null&&r.Fonts.Any(f=>f.Id==State.Selected.Id)?State.Selected:r.Fonts[0])??Enumerable.Empty<FontRecord>();
 void AddSelectedCompare(){if(State.Selected is not {} f)return;if(!State.AddCompare(f))status.Text="对比最多六款，同一字重不重复加入。";tabs.SelectedIndex=1;RenderPreviews();}
 void AddAllSelected(){foreach(var f in SelectedFonts())State.AddCompare(f);tabs.SelectedIndex=1;RenderPreviews();status.Text=$"对比中有 {State.Comparison.Count} 款字体（最多六款）";}
 string CurrentText()
 {
  if(Settings.SampleMode==FontSamples.AutoKey&&State.Selected is {} f){try{var glyph=FontScanner.Resolve(f);var map=string.Concat(FontSamples.Defaults.Values).EnumerateRunes().Distinct().ToDictionary(r=>r.Value,r=>glyph.GetGlyph(r.Value));return FontSamples.Select(f,map).Value;}catch{return Settings.Sample;}}
  if(Settings.SampleMode==FontSamples.FreeKey)return Settings.Sample;
  return Settings.Samples.GetValueOrDefault(Settings.SampleMode)??FontSamples.Defaults.GetValueOrDefault(Settings.SampleMode)??Settings.Sample;
 }
 public void RenderPreviews()
 {
  RenderPageTypesetting();
  previewPanel.Children.Clear();comparePanel.Children.Clear();comparisonChips.Children.Clear();var text=CurrentText();
  if(State.Selected is {} font)previewPanel.Children.Add(PreviewCard(font,text));else previewPanel.Children.Add(Ui.Card(Ui.Text("点击“更新字体库”，扫描本机字体后开始试排。",17)));
  if(!sampleDraftDirty){sampleSeed=text;sampleEditor.Text=text;}previewPanel.Children.Add(sampleExpander);
  if(State.Comparison.Count==0)comparePanel.Children.Add(Ui.Card(Ui.Text("从左侧选字体，点击“加入对比”。最多六款使用同一段文字。",16)));
  foreach(var item in State.Comparison){comparePanel.Children.Add(PreviewCard(item,text,true));var chip=Ui.Button(item.Family+" ×",()=>{State.RemoveCompare(item.Id);RenderPreviews();},"tag");chip.Margin=new Thickness(0,2,5,2);comparisonChips.Children.Add(chip);}

 }
 Control BuildSampleEditor()
 {
  sampleEditor.MaxLength=4000;sampleEditor.Height=150;sampleEditor.TextChanged+=(_,_)=>sampleDraftDirty=(sampleEditor.Text??"")!=sampleSeed;
  void Apply(string mode){sampleSeed=sampleEditor.Text??"";sampleDraftDirty=false;Settings.Sample=sampleSeed;Settings.SampleMode=mode;SaveSettings();changing=true;RefreshSampleModes();changing=false;RenderPreviews();}
  return Ui.Stack(sampleEditor,Ui.Row(Ui.Button("应用自由试排",()=>Apply(FontSamples.FreeKey)),Ui.AsyncButton("保存样文",async()=>{if(string.IsNullOrWhiteSpace(sampleName.Text)){await Dialogs.Message(this,"请输入名称","填写样文名称后保存。");return;}Settings.Samples[sampleName.Text.Trim()]=sampleEditor.Text??"";Apply(sampleName.Text.Trim());})),sampleName);
 }
 Control PreviewCard(FontRecord font,string text,bool removable=false)
 {
  var stack=Ui.Stack();var header=Ui.Grid("*,Auto");Ui.Put(header,CopyableName(font.Label,16,font.Family));if(removable)Ui.Put(header,Ui.Button("移除",()=>{State.RemoveCompare(font.Id);RenderPreviews();}),1);stack.Children.Add(header);
  try{var preview=new GlyphPreview{Glyph=FontScanner.Resolve(font),Text=text,Size=Settings.Size,LineSpacing=Settings.LineHeight};stack.Children.Add(preview);if(preview.Missing.Length>0)stack.Children.Add(Ui.Text("缺字（方框显示）："+preview.Missing,11,Brushes.IndianRed));}
  catch(Exception ex){stack.Children.Add(Ui.Text(ex.Message,12,Brushes.IndianRed));}
  return Ui.Card(stack,24);
 }
 public Task<ScanResult> ScanFonts(){if(scanTask is {IsCompleted:false})return scanTask;scanTask=RunScanFonts();return scanTask;}
 async Task<ScanResult> RunScanFonts()
 {
  SavePending();scanCancellation=new();scanButton.IsEnabled=false;pauseButton.IsEnabled=cancelButton.IsEnabled=true;pauseButton.Content="暂停";
  try{
   var activeScan=scanner.ScanAsync(Store,new Progress<ScanProgress>(p=>status.Text=$"扫描 {p.Done}/{p.Total} · {p.Name} · 复用 {p.Reused} · 异常 {p.Errors}"),scanCancellation.Token);
   var result=await activeScan;
   if(catalogue!=null){var imported=Store.ImportOfficial(OfficialCatalogue.Match(Store.All(),catalogue.Records));status.Text=$"{(result.Complete?"扫描完成":"已取消并保留进度")}：{result.Faces} 个样式 · 复用 {result.Reused} · 异常 {result.Errors} · 补充资料 {imported.Added}";}
   RefreshRows();return result;
  }finally{scanButton.IsEnabled=true;pauseButton.IsEnabled=cancelButton.IsEnabled=false;scanner.Resume();scanCancellation.Dispose();scanCancellation=null;}
 }
 async Task OpenFolder(string path)=>await Launcher.LaunchUriAsync(new Uri(Path.GetFullPath(path)+Path.DirectorySeparatorChar));
}
