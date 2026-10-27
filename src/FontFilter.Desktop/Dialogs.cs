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
  var palette=ThemePalette.From(settings);var drafts=new[]{palette.Primary,palette.Secondary,palette.Accent,palette.Header};var labels=new[]{"主色","辅助色","强调色","顶栏底色"};
  var descriptions=new[]{"用于品牌色与滚动条。","用于标签与字体选中背景。","用于主要操作按钮和选中指示。","独立调整顶部背景，默认使用浅色。"};
  var roles=new ComboBox{Name="ThemeRole",ItemsSource=labels,SelectedIndex=0,HorizontalAlignment=HorizontalAlignment.Stretch};
  var hex=Ui.Input(drafts[0],"#RRGGBB");hex.Name="ThemeHex";var hint=Ui.Text(descriptions[0],12);var swatches=new Border[4];var swatchGrid=Ui.Grid("*,*,*,*");
  for(int i=0;i<4;i++){var c=Color.Parse(drafts[i]);swatches[i]=new Border{Height=74,CornerRadius=new CornerRadius(9),Background=new SolidColorBrush(c),Child=new TextBlock{Text=labels[i],HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=new SolidColorBrush(ThemeService.TextColor(c))},Margin=new Thickness(i==0?0:6,0,i==3?0:6,0)};Ui.Put(swatchGrid,swatches[i],i);}
  var hue=new Slider{Minimum=0,Maximum=360};var saturation=new Slider{Minimum=0,Maximum=100};var brightness=new Slider{Minimum=0,Maximum=100};bool changing=false;int role=0;
  var error=Ui.Text("",12,Brushes.IndianRed);var apply=new Button{Name="SaveTheme",Content="应用并保存"};apply.Classes.Add("primary");var cancel=new Button{Content="取消"};
  void UpdateSwatch(){if(!ThemeService.IsValid(drafts[role]))return;var c=Color.Parse(drafts[role]);swatches[role].Background=new SolidColorBrush(c);((TextBlock)swatches[role].Child!).Foreground=new SolidColorBrush(ThemeService.TextColor(c));}
  void LoadRole(){changing=true;hex.Text=drafts[role];var color=Color.Parse(ThemeService.IsValid(drafts[role])?drafts[role]:new[]{ThemePalette.DefaultPrimary,ThemePalette.DefaultSecondary,ThemePalette.DefaultAccent,ThemePalette.DefaultHeader}[role]);var hsb=HsbColor.FromColor(color);hue.Value=hsb.H;saturation.Value=hsb.S;brightness.Value=hsb.B;hint.Text=descriptions[role];changing=false;}
  void FromSlider(){if(changing)return;changing=true;var c=HsbColor.ToColor(hue.Value,saturation.Value,brightness.Value);drafts[role]=$"#{c.R:X2}{c.G:X2}{c.B:X2}";hex.Text=drafts[role];UpdateSwatch();error.Text="";changing=false;}
  hue.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};saturation.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};brightness.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};
  hex.PropertyChanged+=(_,e)=>{if(e.Property!=TextBox.TextProperty||changing)return;drafts[role]=hex.Text??"";if(!ThemeService.IsValid(drafts[role])){error.Text="请输入 #RRGGBB 格式颜色。";return;}changing=true;var value=HsbColor.FromColor(Color.Parse(drafts[role]));hue.Value=value.H;saturation.Value=value.S;brightness.Value=value.B;UpdateSwatch();error.Text="";changing=false;};
  roles.SelectionChanged+=(_,_)=>{if(roles.SelectedIndex>=0){role=roles.SelectedIndex;LoadRole();}};LoadRole();
  var footer=Ui.Grid("*,Auto");Ui.Put(footer,Ui.Button("恢复默认配色",()=>{drafts=new[]{ThemePalette.DefaultPrimary,ThemePalette.DefaultSecondary,ThemePalette.DefaultAccent,ThemePalette.DefaultHeader};for(int i=0;i<4;i++){role=i;UpdateSwatch();}role=roles.SelectedIndex;LoadRole();}));Ui.Put(footer,Ui.Row(cancel,apply),1);
  var content=Ui.Stack(Ui.Heading("主题配色"),Ui.Text("主色、辅助色、强调色和顶栏底色均可独立调整。",12),swatchGrid,roles,hint,Ui.Text("色相 H"),hue,Ui.Text("饱和度 S"),saturation,Ui.Text("明度 B"),brightness,Ui.Text("十六进制颜色"),hex,error,footer);content.Margin=new Thickness(24);var w=Ui.Window("主题设置",new ScrollViewer{Content=content},650,760);
  apply.Click+=(_,_)=>{if(drafts.Any(c=>!ThemeService.IsValid(c))){error.Text="请检查四个颜色，均需使用 #RRGGBB 格式。";return;}var previous=(settings.ThemeColor,settings.SecondaryColor,settings.AccentColor,settings.HeaderColor);try{settings.ThemeColor=drafts[0].ToUpperInvariant();settings.SecondaryColor=drafts[1].ToUpperInvariant();settings.AccentColor=drafts[2].ToUpperInvariant();settings.HeaderColor=drafts[3].ToUpperInvariant();save();ThemeService.Apply(settings);w.Close();}catch(Exception ex){(settings.ThemeColor,settings.SecondaryColor,settings.AccentColor,settings.HeaderColor)=previous;error.Text=ex.Message;}};cancel.Click+=(_,_)=>w.Close();return w.ShowDialog(owner);
 }
}
