using System.Windows;
using System.Windows.Controls;
namespace FontFilter;
public sealed class MetadataWindow:Window
{
 readonly TextBox urlBox=new(){Padding=new Thickness(8),Margin=new Thickness(0,6,0,10)};
 readonly TextBox summaryBox=new(){AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxLength=4000,Padding=new Thickness(10)};
 readonly CheckBox confirm=new(){Content="已核对：这段介绍对应当前选定字体",Margin=new Thickness(0,14,0,14)};
 readonly TextBlock state=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,10)};readonly CancellationTokenSource cts=new();
 public string Summary=>summaryBox.Text.Trim();public string SourceUrl=>urlBox.Text.Trim();
 public Task SaveToAsync(FontStore store,string id){var summary=Summary;var url=SourceUrl;return Task.Run(()=>store.SaveMetadata(id,summary,url));}
 public MetadataWindow(FontRecord font){LightTheme.Apply(this);Title="补充介绍 · "+font.Family+" · "+font.Face;Width=760;Height=630;MinWidth=600;MinHeight=450;WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=new System.Windows.Media.FontFamily("Microsoft YaHei UI");FontSize=13;
  var dock=new DockPanel{Margin=new Thickness(24)};Content=dock;var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);dock.Children.Add(top);
  top.Children.Add(new TextBlock{Text="为「"+font.Label+"」补充资料",FontSize=20,FontWeight=FontWeights.Bold});top.Children.Add(new TextBlock{Text="粘贴方正 / 汉仪官方详情页地址，或直接录入介绍摘要。提取后请删除无关导航和广告。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,6)});urlBox.Text=font.Url;top.Children.Add(urlBox);
  var fetch=new Button{Content="从官方页面提取",Padding=new Thickness(12,8,12,8),HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,0,0,12)};top.Children.Add(fetch);top.Children.Add(state);
  var bottom=new StackPanel();DockPanel.SetDock(bottom,Dock.Bottom);dock.Children.Add(bottom);bottom.Children.Add(confirm);var save=new Button{Content="确认关联并保存摘要",Padding=new Thickness(12,8,12,8),HorizontalAlignment=HorizontalAlignment.Right,IsEnabled=false};bottom.Children.Add(save);confirm.Checked+=(_,_)=>save.IsEnabled=true;confirm.Unchecked+=(_,_)=>save.IsEnabled=false;summaryBox.Text=font.Summary;dock.Children.Add(summaryBox);
  urlBox.TextChanged+=(_,_)=>confirm.IsChecked=false;summaryBox.TextChanged+=(_,_)=>confirm.IsChecked=false;
  fetch.Click+=async(_,_)=>{if(!Uri.TryCreate(SourceUrl,UriKind.Absolute,out var uri)||!OfficialMetadataService.IsAllowed(uri)){state.Text="地址需为方正或汉仪官方HTTPS页面。";return;}fetch.IsEnabled=false;state.Text="正在获取；不会批量访问网站。";try{var data=await OfficialMetadataService.FetchAsync(uri,cts.Token);if(cts.IsCancellationRequested)return;urlBox.Text=data.Url;summaryBox.Text=data.Summary;state.Text="已提取候选摘要。请核对字体、删去无关文字，再勾选确认。";}catch(OperationCanceledException){if(!cts.IsCancellationRequested)state.Text="请求超时，可直接粘贴介绍。";}catch(Exception ex){if(!cts.IsCancellationRequested)state.Text="获取失败："+ex.Message+" 可直接粘贴介绍。";}finally{fetch.IsEnabled=true;}};
  save.Click+=(_,_)=>{if(Summary.Length<2){state.Text="请先填写字体介绍。";return;}if(SourceUrl.Length>0&&(!Uri.TryCreate(SourceUrl,UriKind.Absolute,out var u)||!OfficialMetadataService.IsAllowed(u))){state.Text="来源需为官方HTTPS地址；手动整理可留空。";return;}DialogResult=true;};Closed+=(_,_)=>cts.Cancel();
 }
}
