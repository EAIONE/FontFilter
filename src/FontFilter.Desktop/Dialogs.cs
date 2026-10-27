using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace FontFilter.Desktop;
public static class Dialogs
{
 public static Task Message(Window owner,string title,string text)
 {
  var close=new Button{Content="知道了",HorizontalAlignment=HorizontalAlignment.Right};var root=Ui.Stack(Ui.Heading(title),Ui.Text(text),close);root.Margin=new Thickness(24);var w=Ui.Window(title,new ScrollViewer{Content=root},560,330);close.Click+=(_,_)=>w.Close();return w.ShowDialog(owner);
 }
 public static Task ApiSettings(Window owner,UserSettings settings,Action save)
 {
  var url=Ui.Input(settings.Api.BaseUrl,"https://…/v1");var model=Ui.Input(settings.Api.Model,"填写服务商支持的模型名称");var key=Ui.Input(ApiSession.Key,"仅保存在本次运行内存");key.PasswordChar='●';
  var json=new CheckBox{Content="请求 JSON 输出模式",IsChecked=settings.Api.JsonMode};
  var provider=new ComboBox{ItemsSource=new[]{"DeepSeek","OpenAI","自定义兼容服务"},SelectedIndex=settings.Api.BaseUrl.Contains("deepseek")?0:settings.Api.BaseUrl.Contains("openai")?1:2};
  provider.SelectionChanged+=(_,_)=>{if(provider.SelectedIndex==0)url.Text="https://api.deepseek.com";else if(provider.SelectedIndex==1)url.Text="https://api.openai.com/v1";};
  var error=Ui.Text("",12,Brushes.IndianRed);var apply=new Button{Content="保存设置",HorizontalAlignment=HorizontalAlignment.Right};apply.Classes.Add("primary");
  var content=Ui.Stack(Ui.Heading("API 连接"),Ui.Text("支持 Chat Completions 兼容接口。分析前会再次显示发送内容，点击开始后才调用。",12),provider,Ui.Text("服务地址"),url,Ui.Text("模型名称"),model,Ui.Text("API 密钥"),key,json,Ui.Text("地址和模型保存在本机；密钥关闭软件后清除。",11),error,apply);content.Margin=new Thickness(24);
  var w=Ui.Window("API 设置",new ScrollViewer{Content=content},620,660);
  apply.Click+=(_,_)=>{try{FontAiService.Endpoint(url.Text??"");settings.Api.BaseUrl=url.Text!.Trim();settings.Api.Model=model.Text?.Trim()??"";settings.Api.JsonMode=json.IsChecked==true;ApiSession.Key=key.Text?.Trim()??"";save();w.Close();}catch(Exception ex){error.Text=ex.Message;}};return w.ShowDialog(owner);
 }
 public static Task Theme(Window owner,UserSettings settings,Action save)
 {
  var original=settings.ThemeColor;var hex=Ui.Input(original,"#16594D");var preview=new Border{Height=70,CornerRadius=new CornerRadius(10),Background=new SolidColorBrush(Color.Parse(ThemeService.IsValid(original)?original:ThemeService.DefaultColor))};
  var hsb=HsbColor.FromColor(Color.Parse(ThemeService.IsValid(original)?original:ThemeService.DefaultColor));
  var hue=new Slider{Minimum=0,Maximum=360,Value=hsb.H};var saturation=new Slider{Minimum=0,Maximum=100,Value=hsb.S};var brightness=new Slider{Minimum=0,Maximum=100,Value=hsb.B};bool changing=false;
  var error=Ui.Text("",12,Brushes.IndianRed);var apply=new Button{Content="应用并保存"};apply.Classes.Add("primary");var cancel=new Button{Content="取消"};
  var content=Ui.Stack(Ui.Heading("主题颜色"),preview,Ui.Text("色相 H"),hue,Ui.Text("饱和度 S"),saturation,Ui.Text("明度 B"),brightness,Ui.Text("十六进制颜色"),hex,error,Ui.Row(apply,cancel));content.Margin=new Thickness(24);var w=Ui.Window("主题设置",content,550,600);
  void FromSlider(){if(changing)return;changing=true;var c=HsbColor.ToColor(hue.Value,saturation.Value,brightness.Value);hex.Text=$"#{c.R:X2}{c.G:X2}{c.B:X2}";preview.Background=new SolidColorBrush(c);changing=false;}
  hue.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};saturation.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};brightness.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};
  hex.TextChanged+=(_,_)=>{if(changing||!ThemeService.IsValid(hex.Text))return;changing=true;var c=Color.Parse(hex.Text!);var value=HsbColor.FromColor(c);hue.Value=value.H;saturation.Value=value.S;brightness.Value=value.B;preview.Background=new SolidColorBrush(c);changing=false;};
  apply.Click+=(_,_)=>{if(!ThemeService.IsValid(hex.Text)){error.Text="请输入 #RRGGBB 格式颜色。";return;}settings.ThemeColor=hex.Text!.ToUpperInvariant();ThemeService.Apply(settings.ThemeColor);save();w.Close();};cancel.Click+=(_,_)=>w.Close();return w.ShowDialog(owner);
 }
}
