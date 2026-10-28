using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
namespace FontFilter.Desktop;
public sealed partial class MainWindow
{
 async Task PickTags(bool search)
 {
  var filter=Ui.Input("","搜索标签名称、维度或说明");filter.Name="TagSearch";filter.Margin=new Thickness(0,16,0,20);filter.MinHeight=40;var chosen=new HashSet<string>();
  var panel=new StackPanel{Spacing=12};var count=Ui.Text("未选择标签",12);var apply=new Button{Content=search?"用所选标签筛选":"添加到人工标签"};apply.Name="ApplyTags";apply.Classes.Add("primary");
  var footer=Ui.Grid("*,Auto");footer.Margin=new Thickness(0,18,0,0);count.VerticalAlignment=VerticalAlignment.Center;Ui.Put(footer,count);Ui.Put(footer,apply,1);
  var grid=Ui.Grid("*","Auto,Auto,*,Auto");grid.Margin=new Thickness(24);Ui.Put(grid,Ui.Stack(Ui.Heading("按特征挑选字体"),Ui.Text("所选标签相加，匹配任一标签即显示；勾选后应用。",12)));Ui.Put(grid,filter,0,1);Ui.Put(grid,new ScrollViewer{Content=panel,HorizontalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled},0,2);Ui.Put(grid,footer,0,3);var w=Ui.Window("分类标签库",grid,850,720);
  var definitions=TagCatalog.OrderForPicker(Store.AvailableTagDefinitions()).ToList();var tagCounts=Store.TagCounts(definitions.Select(t=>t.Name));
  void Render(){panel.Children.Clear();foreach(var group in definitions.Where(t=>(t.Name+" "+t.Category+" "+t.Aliases).Contains(filter.Text??"",StringComparison.OrdinalIgnoreCase)).GroupBy(t=>t.Category)){
   var wrap=new WrapPanel();foreach(var tag in group){var box=new CheckBox{Content=$"{tag.Name}  {tagCounts.GetValueOrDefault(tag.Name)}",IsChecked=chosen.Contains(tag.Name),Margin=new Thickness(0,0,15,8),MinWidth=125};box.IsCheckedChanged+=(_,_)=>{if(box.IsChecked==true)chosen.Add(tag.Name);else chosen.Remove(tag.Name);count.Text=$"已选 {chosen.Count} 项";};wrap.Children.Add(box);}panel.Children.Add(Ui.Stack(Ui.Heading(group.Key,15),wrap));}}
  filter.TextChanged+=(_,_)=>Render();Render();apply.Click+=(_,_)=>{Settings.RecordTags(chosen);SaveSettings();if(search)ApplyTagSearch(chosen);else manual.Text=TagRules.SelectionText(manual.Text??"",chosen,false);w.Close();};await w.ShowDialog(this);
 }
 async Task EditMetadata()
 {
  if(State.Selected is not {} font){await Dialogs.Message(this,"先选择字体","从左侧选择一款字体后补充介绍。");return;}
  var url=Ui.Input(font.Url,"方正或汉仪官方 HTTPS 详情页");var text=Ui.Input(font.Summary,"粘贴你核对过的字体介绍",true);text.MaxLength=16000;text.MinHeight=240;
  var confirm=new CheckBox{Content="我已核对，此介绍属于当前字体",IsChecked=false};var info=Ui.Text("",12,Brushes.IndianRed);var apply=new Button{Content="保存介绍"};apply.Classes.Add("primary");
  var fetch=Ui.AsyncButton("从官方页面提取",async()=>{var result=await OfficialMetadataService.FetchAsync(new Uri(url.Text??""),CancellationToken.None);url.Text=result.Url;text.Text=result.Summary;confirm.IsChecked=false;info.Text="请删去无关内容，再核对并勾选确认。";});
  var root=Ui.Stack(Ui.Heading(font.Label,20),Ui.Text("仅从方正、汉仪官网提取；也可直接粘贴介绍摘要。",12),url,fetch,text,confirm,info,apply);root.Margin=new Thickness(24);var w=Ui.Window("补充官网介绍",new ScrollViewer{Content=root},750,700);
  apply.Click+=(_,_)=>{if(confirm.IsChecked!=true){info.Text="请先核对并勾选确认。";return;}SavePending();Store.SaveMetadata(font.Id,text.Text?.Trim()??"",url.Text?.Trim()??"");RefreshRows(font.Id);w.Close();};await w.ShowDialog(this);
 }
 async Task FindOfficial()
 {
  if(State.Selected is not {} f)return;await Launcher.LaunchUriAsync(new Uri("https://www.bing.com/search?q="+Uri.EscapeDataString(f.Family+" 字体 (site:foundertype.com OR site:hanyi.com.cn)")));
 }
 async Task OpenCatalogue()
 {
  if(catalogue==null){await Dialogs.Message(this,"无法打开资料库","离线资料文件缺失，请重新解压完整程序包。");return;}
  var search=Ui.Input("","搜索字体、品牌或介绍");var list=new ListBox();var description=Ui.Text("选择一项查看离线资料。",13);var selectedTitle=Ui.Heading("官网资料",20);OfficialFontInfo? selected=null;
  list.ItemTemplate=new FuncDataTemplate<OfficialFontInfo>((f,_)=>Ui.Stack(Ui.Text(f?.Family??"",14),Ui.Text((f?.Vendor??"")+" · "+(string.IsNullOrWhiteSpace(f?.Summary)?"暂无介绍":"含短摘录"),10)));
  var open=Ui.AsyncButton("打开官方来源",async()=>{if(selected!=null&&Uri.TryCreate(selected.SourceUrl,UriKind.Absolute,out var uri)&&OfficialMetadataService.IsAllowed(uri))await Launcher.LaunchUriAsync(uri);});
  var attach=Ui.AsyncButton("为本机匹配字体补充空白资料",async()=>{SavePending();var result=Store.ImportOfficial(OfficialCatalogue.Match(Store.All(),catalogue.Records));RefreshRows();await Dialogs.Message(this,"资料已关联",$"新增介绍 {result.Added}，保留已有介绍 {result.Existing}，没有介绍 {result.WithoutDescription}。\n仅唯一匹配条目自动关联，未修改人工标签。");},"primary");
  var grid=Ui.Grid("340,20,*","Auto,Auto,*");grid.Margin=new Thickness(24);var heading=Ui.Stack(Ui.Heading("方正与汉仪 · 离线官网资料库"),Ui.Text($"{catalogue.Records.Count} 条家族资料 · 快照 {catalogue.GeneratedAt}\n目录记录不代表已获授权；家族资料不等于每个字重的说明。",12));Ui.Put(grid,heading);Grid.SetColumnSpan(heading,3);Ui.Put(grid,search,0,1);Grid.SetColumnSpan(search,3);search.Margin=new Thickness(0,10,0,12);Ui.Put(grid,list,0,2);Ui.Put(grid,Ui.Stack(selectedTitle,new ScrollViewer{Content=description,MaxHeight=360},open,attach),2,2);
  list.SelectionChanged+=(_,_)=>{selected=list.SelectedItem as OfficialFontInfo;if(selected!=null){selectedTitle.Text=selected.Family;description.Text=$"{selected.Vendor}\n\n{(selected.Summary.Length>0?selected.Summary:"此条目暂无介绍短摘录。相关标签可能仍可用于搜索。 ")}\n\n官网标签：{string.Join("、",selected.Tags)}\n\n来源：{selected.SourceUrl}\n采集时间：{selected.FetchedAt}";}};
  void Filter(){var terms=TagRules.Terms(search.Text??"");list.ItemsSource=catalogue.Records.Where(f=>terms.All(t=>(f.Family+" "+f.Vendor+" "+f.Summary+" "+string.Join(" ",f.Tags)).Contains(t,StringComparison.OrdinalIgnoreCase))).ToList();}
  search.TextChanged+=(_,_)=>Filter();Filter();await Ui.Window("官网资料",grid,1040,740).ShowDialog(this);
 }
 async Task AnalyzeCurrent()
 {
  if(State.Selected is not {} font)return;
  var source=font with{Summary=font.Summary[..Math.Min(4000,font.Summary.Length)]};var info=Ui.Text($"服务：{Settings.Api.BaseUrl}\n模型：{Settings.Api.Model}\n\n只发送字体名称、字重和已保存的介绍（最多4000字符），不上传字体文件、路径或人工标签。调用可能产生服务商费用。",12);
  var sent=Ui.Input(FontAiService.Source(source),null,true);sent.IsReadOnly=true;sent.Height=150;var results=new StackPanel{Spacing=10};var note=Ui.Text("候选标签默认不勾选，需人工核对后保存。",12);var start=new Button{Content="开始分析"};start.Classes.Add("primary");var accept=new Button{Content="将勾选候选合并到人工标签",IsEnabled=false};var cancel=new Button{Content="关闭"};
  var root=Ui.Grid("*","Auto,Auto,Auto,*,Auto");root.Margin=new Thickness(24);Ui.Put(root,Ui.Stack(Ui.Heading("分析 · "+font.Family),info));Ui.Put(root,sent,0,1);Ui.Put(root,note,0,2);Ui.Put(root,new ScrollViewer{Content=results},0,3);Ui.Put(root,Ui.Row(start,accept,cancel),0,4);var w=Ui.Window("AI 辅助标注",root,800,770);using var cts=new CancellationTokenSource();var chosen=new List<(CheckBox Box,string Tag)>();w.Closed+=(_,_)=>cts.Cancel();
  start.Click+=async(_,_)=>{start.IsEnabled=false;results.Children.Clear();chosen.Clear();note.Text="正在分析…";try{
   var analysis=await new FontAiService().AnalyzeAsync(Settings.Api,ApiSession.Key,source,cts.Token);if(cts.IsCancellationRequested)return;
   foreach(var suggestion in analysis.Tags){var box=new CheckBox{Content=$"{suggestion.Name} · 置信度 {suggestion.Confidence:P0}",IsChecked=false};chosen.Add((box,suggestion.Name));results.Children.Add(Ui.Card(Ui.Stack(box,Ui.Text("依据："+suggestion.Evidence,12),Ui.Text(suggestion.Reason,11)),12));}
   note.Text=analysis.Tags.Count==0?"没有找到有资料依据的候选标签。 "+analysis.Note:analysis.Note;accept.IsEnabled=chosen.Count>0;
  }catch(OperationCanceledException){note.Text="分析已取消或超时。";}catch(Exception ex){note.Text=ex.Message;}finally{start.IsEnabled=true;}};
  accept.Click+=(_,_)=>{State.ManualText=manual.Text??"";State.MergeSuggestions(chosen.Where(c=>c.Box.IsChecked==true).Select(c=>c.Tag));manual.Text=State.ManualText;SavePending();RefreshRows(font.Id);w.Close();};cancel.Click+=(_,_)=>w.Close();await w.ShowDialog(this);
 }
 async Task ImportLegacy()
 {
  var path=Ui.Input("","选择 Windows 旧版的 Data 文件夹（包含 fonts.db）");var note=Ui.Text("请先扫描本机字体，再导入。只把名称与字重唯一对应的数据关联到本机字体；其他记录会保存到待关联文件。原字体文件不会导入。",12);var settings=new CheckBox{Content="同时导入旧版样文、主题及 API 地址设置（不含密钥）",IsChecked=true};var feedback=Ui.Text("",12);var run=new Button{Content="备份并导入"};run.Classes.Add("primary");
  var choose=Ui.AsyncButton("选择文件夹",async()=>{var picked=await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions{Title="选择旧版 Data 文件夹",AllowMultiple=false});if(picked.Count>0)path.Text=picked[0].TryGetLocalPath()??"";});
  var root=Ui.Stack(Ui.Heading("导入旧版数据"),note,path,choose,settings,feedback,run);root.Margin=new Thickness(24);var w=Ui.Window("导入旧版 Data",root,740,500);
  run.Click+=async(_,_)=>{run.IsEnabled=false;try{SavePending();var report=LegacyImport.Import(path.Text??"",Store,Path.Combine(FontFilter.App.DataDirectory,"ImportHistory"));
   if(settings.IsChecked==true&&report.Settings!=null){var settingsFile=Path.Combine(FontFilter.App.DataDirectory,"settings.json");if(File.Exists(settingsFile))File.Copy(settingsFile,report.BackupPath+".settings.json");Settings=report.Settings;SaveSettings();ThemeService.Apply(Settings);changing=true;sizeInput.Value=(decimal)Settings.Size;lineInput.Value=(decimal)Settings.LineHeight;RefreshSampleModes();changing=false;}
   RefreshRows();feedback.Text=$"已关联 {report.Linked} 个样式；待关联 {report.Unresolved} 项。\n备份：{report.BackupPath}\n待关联：{report.UnresolvedPath}\n待关联记录会保留在 ImportHistory 内，可在安装对应字体后重新导入旧 Data。";
  }catch(Exception ex){await Dialogs.Message(w,"导入失败",ex.Message);}finally{run.IsEnabled=true;}};await w.ShowDialog(this);
 }
}
