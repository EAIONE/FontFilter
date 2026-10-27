using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
namespace FontFilter;
public sealed class OfficialCatalogueRow
{
 public bool Selected{get;set;}public List<OfficialMatch> Matches{get;init;}=new();
 public OfficialFontInfo Source=>Matches[0].Source;public string Family=>Source.Family;public string Vendor=>Source.Vendor;
 public int Styles=>Matches.Count;public int Missing=>Matches.Count(m=>string.IsNullOrWhiteSpace(m.Local.Summary));public bool HasContent=>OfficialCatalogue.Format(Source).Length>0;
 public string Status=>!HasContent?"暂无介绍":Missing==0?"已有资料":"可补充 "+Missing+" 个样式";
 public string Tags=>string.Join("、",Source.Tags);public string LocalNames=>string.Join("、",Matches.Select(m=>m.Local.Family).Distinct().Take(4));
}
public sealed class OfficialCatalogueWindow:Window
{
 public OfficialImportResult? ImportResult{get;private set;}
 public OfficialCatalogueWindow(FontStore store){LightTheme.Apply(this);Title="方正 · 汉仪官方资料库";Width=1040;Height=740;MinWidth=800;MinHeight=550;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  var dock=new DockPanel{Margin=new Thickness(26)};Content=dock;var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);top.Children.Add(new TextBlock{Text="把官方介绍，补进你的字体档案。",FontSize=24,FontWeight=FontWeights.SemiBold});var info=new TextBlock{Text="正在读取离线目录与匹配本地字体…",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Gray,Margin=new Thickness(0,12,0,10)};top.Children.Add(info);top.Children.Add(new TextBlock{Text="仅唯一名称或文件名对应；介绍为家族资料，不代表具体字重、字符集或商用授权。",Foreground=Brushes.Gray,FontSize=11,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,14)});
  var actions=new WrapPanel{Margin=new Thickness(0,0,0,14)};top.Children.Add(actions);var select=new Button{Content="选中可补充项",Margin=new Thickness(0,0,8,0)};var clear=new Button{Content="取消选择"};actions.Children.Add(select);actions.Children.Add(clear);
  var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);dock.Children.Add(bottom);var excerpt=new TextBox{IsReadOnly=true,AcceptsReturn=true,Height=105,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,14,0,10)};bottom.Children.Add(excerpt);var footer=new DockPanel();bottom.Children.Add(footer);var apply=new Button{Content="补充选中的空白介绍",Style=(Style)FindResource("PrimaryButton"),IsEnabled=false,HorizontalAlignment=HorizontalAlignment.Right};DockPanel.SetDock(apply,Dock.Right);footer.Children.Add(apply);var source=new Button{Content="打开选中条目的官网 ↗",HorizontalAlignment=HorizontalAlignment.Left};footer.Children.Add(source);
  var grid=new DataGrid{AutoGenerateColumns=false,CanUserAddRows=false,CanUserDeleteRows=false,HeadersVisibility=DataGridHeadersVisibility.Column,GridLinesVisibility=DataGridGridLinesVisibility.Horizontal,RowHeaderWidth=0,Background=Brushes.White,BorderBrush=new SolidColorBrush(Color.FromRgb(231,233,227)),SelectionMode=DataGridSelectionMode.Single,EnableRowVirtualization=true,EnableColumnVirtualization=true,RowHeight=45};dock.Children.Add(grid);
  grid.Columns.Add(new DataGridCheckBoxColumn{Header="选中",Binding=new Binding("Selected"){Mode=BindingMode.TwoWay,UpdateSourceTrigger=UpdateSourceTrigger.PropertyChanged},Width=50});
  foreach(var column in new[]{("来源","Vendor",75d),("官网家族","Family",180d),("本地字体","LocalNames",190d),("样式","Styles",55d),("状态","Status",125d),("官网标签","Tags",220d)})grid.Columns.Add(new DataGridTextColumn{Header=column.Item1,Binding=new Binding(column.Item2),Width=column.Item3,IsReadOnly=true});
  var rows=new List<OfficialCatalogueRow>();grid.SelectionChanged+=(_,_)=>{if(grid.SelectedItem is OfficialCatalogueRow row)excerpt.Text=OfficialCatalogue.Format(row.Source)+"\n\n来源："+row.Source.SourceUrl+"\n采集时间："+row.Source.FetchedAt;};
  select.Click+=(_,_)=>{foreach(var r in rows)r.Selected=r.HasContent&&r.Missing>0;grid.Items.Refresh();};clear.Click+=(_,_)=>{foreach(var r in rows)r.Selected=false;grid.Items.Refresh();};source.Click+=(_,_)=>{if(grid.SelectedItem is not OfficialCatalogueRow row)return;try{Process.Start(new ProcessStartInfo(row.Source.SourceUrl){UseShellExecute=true});}catch(Exception ex){info.Text="无法打开官网："+ex.Message;}};
  apply.Click+=async(_,_)=>{grid.CommitEdit(DataGridEditingUnit.Row,true);var chosen=rows.Where(r=>r.Selected&&r.HasContent).SelectMany(r=>r.Matches).ToList();if(chosen.Count==0){info.Text="请先选择有资料、且需要补充的条目。";return;}apply.IsEnabled=false;try{ImportResult=await Task.Run(()=>store.ImportOfficial(chosen));DialogResult=true;}catch(Exception ex){info.Text="导入失败："+ex.Message;apply.IsEnabled=true;}};
  Loaded+=async(_,_)=>{try{var loaded=await Task.Run(()=>{var catalogue=OfficialCatalogue.Load(Path.Combine(App.DataDirectory,"official-fonts.json"));var fonts=store.All();return(catalogue,fonts,matches:OfficialCatalogue.Match(fonts,catalogue.Records));});rows=loaded.matches.GroupBy(m=>m.Source.Id).Select(g=>new OfficialCatalogueRow{Matches=g.ToList()}).OrderByDescending(r=>r.HasContent&&r.Missing>0).ThenBy(r=>r.Family).ToList();foreach(var r in rows)r.Selected=r.HasContent&&r.Missing>0;grid.ItemsSource=rows;var records=loaded.catalogue.Records;info.Text=$"目录 {records.Count} 条 · 有介绍 {records.Count(r=>!string.IsNullOrWhiteSpace(r.Summary))} 条 · 有官网标签 {records.Count(r=>r.Tags.Length>0)} 条\n唯一匹配 {rows.Count} 条家族资料 / {loaded.matches.Count} 个本地样式；可补充 {rows.Where(r=>r.HasContent).Sum(r=>r.Missing)} 个样式。导入不会覆盖已有介绍或人工标签。";apply.IsEnabled=rows.Any(r=>r.Selected);if(rows.Count>0)grid.SelectedIndex=0;}catch(Exception ex){info.Text="读取失败："+ex.Message;}};
 }
}
