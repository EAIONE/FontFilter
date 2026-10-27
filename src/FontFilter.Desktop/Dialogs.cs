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
  var palette=ThemePalette.From(settings);var drafts=new[]{palette.Header,palette.Secondary,palette.Accent,palette.Primary,palette.Background};var labels=new[]{"主题色","辅助色","强调色","滚动条色","底色"};
  var descriptions=new[]{"用于顶栏与品牌，列表标签使用偏暗的主题色。","用于标签与字体选中背景。","用于主要操作按钮和选中指示。","仅调整滚动条颜色，不再自动加黑。","用于整页背景，默认使用浅色。"};

  var hex=Ui.Input(drafts[0],"#RRGGBB");hex.Name="ThemeHex";var hint=Ui.Text(descriptions[0],12);var swatches=new Border[5];var swatchButtons=new Button[5];var swatchGrid=Ui.Grid("*,*,*,*,*");
  for(int i=0;i<5;i++){var c=Color.Parse(drafts[i]);swatches[i]=new Border{Height=74,CornerRadius=new CornerRadius(9),Background=new SolidColorBrush(c),Child=new TextBlock{Text=labels[i],HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,Foreground=new SolidColorBrush(ThemeService.TextColor(c))}};swatchButtons[i]=new Button{Name="ThemeRole"+i,Content=swatches[i],HorizontalAlignment=HorizontalAlignment.Stretch,VerticalAlignment=VerticalAlignment.Stretch,Padding=new Thickness(0),BorderThickness=new Thickness(0),CornerRadius=new CornerRadius(9),HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch,Margin=new Thickness(i==0?0:6,0,i==4?0:6,0)};Avalonia.Automation.AutomationProperties.SetName(swatchButtons[i],labels[i]);Ui.Put(swatchGrid,swatchButtons[i],i);}

  var hue=new Slider{Minimum=0,Maximum=360};var saturation=new Slider{Minimum=0,Maximum=100};var brightness=new Slider{Minimum=0,Maximum=100};bool changing=false,saved=false;int role=0;
  var hueLabel=Ui.Text("色相 H");var saturationLabel=Ui.Text("饱和度 S");var brightnessLabel=Ui.Text("明度 B");
  var hueBar=new Border{Name="HueColorBar",Height=18,CornerRadius=new CornerRadius(9),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0)};
  var saturationBar=new Border{Name="SaturationColorBar",Height=18,CornerRadius=new CornerRadius(9),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0)};
  var brightnessBar=new Border{Name="BrightnessColorBar",Height=18,CornerRadius=new CornerRadius(9),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(10,0)};
  Control Channel(Slider slider,Border bar){slider.Classes.Add("colorChannel");var grid=new Grid{Height=50};grid.Children.Add(bar);grid.Children.Add(slider);return grid;}
  var hueControl=Channel(hue,hueBar);var saturationControl=Channel(saturation,saturationBar);var brightnessControl=Channel(brightness,brightnessBar);
  var previewSettings=new UserSettings{ThemeColor=palette.Primary,SecondaryColor=palette.Secondary,AccentColor=palette.Accent,HeaderColor=palette.Header,BackgroundColor=palette.Background};
  LinearGradientBrush Gradient(params Color[] colors){var brush=new LinearGradientBrush{StartPoint=new RelativePoint(0,0.5,RelativeUnit.Relative),EndPoint=new RelativePoint(1,0.5,RelativeUnit.Relative)};for(int i=0;i<colors.Length;i++)brush.GradientStops.Add(new GradientStop(colors[i],i/(double)(colors.Length-1)));return brush;}
  hueBar.Background=Gradient(Enumerable.Range(0,7).Select(i=>HsbColor.ToColor(i*60,100,100)).ToArray());
  void UpdateBars(){saturationBar.Background=Gradient(HsbColor.ToColor(hue.Value,0,brightness.Value),HsbColor.ToColor(hue.Value,100,brightness.Value));brightnessBar.Background=Gradient(Colors.Black,HsbColor.ToColor(hue.Value,saturation.Value,100));hueLabel.Text=$"色相 H · {hue.Value:0}°";saturationLabel.Text=$"饱和度 S · {saturation.Value:0}%";brightnessLabel.Text=$"明度 B · {brightness.Value:0}%";}
  void Preview(){if(ThemeService.IsValid(drafts[0]))previewSettings.HeaderColor=drafts[0];if(ThemeService.IsValid(drafts[1]))previewSettings.SecondaryColor=drafts[1];if(ThemeService.IsValid(drafts[2]))previewSettings.AccentColor=drafts[2];if(ThemeService.IsValid(drafts[3]))previewSettings.ThemeColor=drafts[3];if(ThemeService.IsValid(drafts[4]))previewSettings.BackgroundColor=drafts[4];ThemeService.Apply(previewSettings);UpdateBars();}

  var error=Ui.Text("",12,Brushes.IndianRed);var apply=new Button{Name="SaveTheme",Content="应用并保存"};apply.Classes.Add("primary");var cancel=new Button{Content="取消"};
  void UpdateSwatch(){if(!ThemeService.IsValid(drafts[role]))return;var c=Color.Parse(drafts[role]);swatches[role].Background=new SolidColorBrush(c);swatches[role].BorderBrush=new SolidColorBrush(Color.FromArgb(100,255,255,255));((TextBlock)swatches[role].Child!).Foreground=new SolidColorBrush(ThemeService.TextColor(c));}
  void LoadRole(){changing=true;hex.Text=drafts[role];var color=Color.Parse(ThemeService.IsValid(drafts[role])?drafts[role]:new[]{ThemePalette.DefaultHeader,ThemePalette.DefaultSecondary,ThemePalette.DefaultAccent,ThemePalette.DefaultPrimary,ThemePalette.DefaultBackground}[role]);var hsb=HsbColor.FromColor(color);hue.Value=hsb.H;saturation.Value=hsb.S;brightness.Value=hsb.B;hint.Text="正在编辑："+labels[role]+" · "+descriptions[role];for(int i=0;i<5;i++){var c=Color.Parse(ThemeService.IsValid(drafts[i])?drafts[i]:"#FFFFFF");swatches[i].BorderThickness=new Thickness(i==role?2:0);swatches[i].BorderBrush=new SolidColorBrush(Color.FromArgb(100,255,255,255));((TextBlock)swatches[i].Child!).Text=(i==role?"✓ ":"")+labels[i];}UpdateBars();changing=false;}
  void FromSlider(){if(changing)return;changing=true;var c=HsbColor.ToColor(hue.Value,saturation.Value,brightness.Value);drafts[role]=$"#{c.R:X2}{c.G:X2}{c.B:X2}";hex.Text=drafts[role];UpdateSwatch();Preview();error.Text="";changing=false;}
  hue.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};saturation.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};brightness.PropertyChanged+=(_,e)=>{if(e.Property==Slider.ValueProperty)FromSlider();};
  hex.PropertyChanged+=(_,e)=>{if(e.Property!=TextBox.TextProperty||changing)return;drafts[role]=hex.Text??"";if(!ThemeService.IsValid(drafts[role])){error.Text="请输入 #RRGGBB 格式颜色。";return;}changing=true;var value=HsbColor.FromColor(Color.Parse(drafts[role]));hue.Value=value.H;saturation.Value=value.S;brightness.Value=value.B;UpdateSwatch();Preview();error.Text="";changing=false;};
  for(int i=0;i<5;i++){var selected=i;swatchButtons[i].Click+=(_,_)=>{role=selected;LoadRole();};}LoadRole();
  var footer=Ui.Grid("*,Auto");Ui.Put(footer,Ui.Button("恢复默认配色",()=>{drafts=new[]{ThemePalette.DefaultHeader,ThemePalette.DefaultSecondary,ThemePalette.DefaultAccent,ThemePalette.DefaultPrimary,ThemePalette.DefaultBackground};var selected=role;for(int i=0;i<5;i++){role=i;UpdateSwatch();}role=selected;LoadRole();Preview();}));Ui.Put(footer,Ui.Row(cancel,apply),1);
  var content=Ui.Stack(Ui.Heading("主题配色"),Ui.Text("点击上方色块编辑；拖动色条实时预览，保存才写入设置，取消恢复原配色。",12),swatchGrid,hint,hueLabel,hueControl,saturationLabel,saturationControl,brightnessLabel,brightnessControl,Ui.Text("十六进制颜色"),hex,error,footer);content.Margin=new Thickness(24);var w=Ui.Window("主题设置",new ScrollViewer{Content=content},740,720);
  apply.Click+=(_,_)=>{if(drafts.Any(c=>!ThemeService.IsValid(c))){error.Text="请检查五个颜色，均需使用 #RRGGBB 格式。";return;}var previous=(settings.ThemeColor,settings.SecondaryColor,settings.AccentColor,settings.HeaderColor,settings.BackgroundColor);try{settings.HeaderColor=drafts[0].ToUpperInvariant();settings.SecondaryColor=drafts[1].ToUpperInvariant();settings.AccentColor=drafts[2].ToUpperInvariant();settings.ThemeColor=drafts[3].ToUpperInvariant();settings.BackgroundColor=drafts[4].ToUpperInvariant();save();ThemeService.Apply(settings);saved=true;w.Close();}catch(Exception ex){(settings.ThemeColor,settings.SecondaryColor,settings.AccentColor,settings.HeaderColor,settings.BackgroundColor)=previous;error.Text=ex.Message;}};w.Closed+=(_,_)=>{if(!saved)ThemeService.Apply(new UserSettings{ThemeColor=palette.Primary,SecondaryColor=palette.Secondary,AccentColor=palette.Accent,HeaderColor=palette.Header,BackgroundColor=palette.Background});};cancel.Click+=(_,_)=>w.Close();return w.ShowDialog(owner);
 }
}
