using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace FontFilter;
public sealed class ApiSettingsWindow:Window
{
 public ApiOptions Options{get;private set;}public string ApiKey{get;private set;}="";
 public ApiSettingsWindow(ApiOptions initial,string sessionKey){LightTheme.Apply(this);Title="API连接设置";Width=600;Height=660;MinHeight=560;MinWidth=520;WindowStartupLocation=WindowStartupLocation.CenterOwner;Options=initial;
  var panel=new StackPanel{Margin=new Thickness(30)};Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};panel.Children.Add(new TextBlock{Text="让资料成为可检索的特征。",FontSize=23,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,8)});panel.Children.Add(new TextBlock{Text="兼容OpenAI格式的Chat Completions接口。\n分析时发送当前字体名称和已有介绍，不上传字体文件。",TextWrapping=TextWrapping.Wrap,Foreground=Brushes.Gray,LineHeight=23,Margin=new Thickness(0,0,0,20)});
  void Label(string text)=>panel.Children.Add(new TextBlock{Text=text,Foreground=Brushes.Gray,Margin=new Thickness(0,0,0,8)});
  Label("服务预设");var presets=new ComboBox{ItemsSource=new[]{"DeepSeek","OpenAI","自定义兼容接口"},Margin=new Thickness(0,0,0,16)};panel.Children.Add(presets);Label("服务地址 / Base URL");var url=new TextBox{Text=initial.BaseUrl,Margin=new Thickness(0,0,0,16)};panel.Children.Add(url);Label("模型名称 · 按服务控制台填写");var model=new TextBox{Text=initial.Model,Margin=new Thickness(0,0,0,16)};panel.Children.Add(model);Label("API Key · 仅保留在本次运行中");var key=new PasswordBox{Password=sessionKey,Padding=new Thickness(10),BorderBrush=new SolidColorBrush(Color.FromRgb(231,233,227)),Margin=new Thickness(0,0,0,16)};panel.Children.Add(key);
  var json=new CheckBox{Content="使用JSON输出模式（接口不支持时可关闭）",IsChecked=initial.JsonMode,Margin=new Thickness(0,0,0,18)};panel.Children.Add(json);var state=new TextBlock{Foreground=Brushes.Firebrick,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)};panel.Children.Add(state);var save=new Button{Content="保存连接设置",Style=(Style)FindResource("PrimaryButton"),HorizontalAlignment=HorizontalAlignment.Right};panel.Children.Add(save);
  presets.SelectedIndex=initial.BaseUrl.Contains("api.deepseek.com",StringComparison.OrdinalIgnoreCase)?0:initial.BaseUrl.Contains("api.openai.com",StringComparison.OrdinalIgnoreCase)?1:2;
  url.TextChanged+=(_,_)=>key.Password="";presets.SelectionChanged+=(_,_)=>{url.Text=presets.SelectedIndex switch{0=>"https://api.deepseek.com",1=>"https://api.openai.com/v1",_=>""};model.Text="";key.Password="";};
  save.Click+=(_,_)=>{try{FontAiService.Endpoint(url.Text);if(model.Text.Trim().Length==0){state.Text="请填写服务支持的模型名称。";return;}Options=new(){BaseUrl=url.Text.Trim().TrimEnd('/'),Model=model.Text.Trim(),JsonMode=json.IsChecked==true};ApiKey=key.Password.Trim();DialogResult=true;}catch(ArgumentException ex){state.Text=ex.Message;}};
 }
}
