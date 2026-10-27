using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace FontFilter.Desktop;
public static class Ui
{
 public static TextBlock Text(string text,double size=13,IBrush? color=null){var block=new TextBlock{Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap};if(color!=null)block.Foreground=color;return block;}
 public static TextBlock Heading(string text,double size=19)=>new(){Text=text,FontSize=size,FontWeight=FontWeight.SemiBold,Margin=new Thickness(0,4,0,10)};
 public static Button Button(string text,Action action,string? style=null){var b=new Button{Content=text};if(style!=null)b.Classes.Add(style);b.Click+=(_,_)=>action();return b;}
 public static Button AsyncButton(string text,Func<Task> action,string? style=null){var b=new Button{Content=text};if(style!=null)b.Classes.Add(style);b.Click+=async(_,_)=>{b.IsEnabled=false;try{await action();}catch(Exception ex){if(TopLevel.GetTopLevel(b) is Window owner)await Dialogs.Message(owner,"操作失败",ex.Message);}finally{b.IsEnabled=true;}};return b;}
 public static StackPanel Stack(params Control[] children){var s=new StackPanel{Spacing=10};foreach(var c in children)s.Children.Add(c);return s;}
 public static StackPanel Row(params Control[] children){var s=new StackPanel{Orientation=Orientation.Horizontal,Spacing=8,VerticalAlignment=VerticalAlignment.Center};foreach(var c in children)s.Children.Add(c);return s;}
 public static Border Card(Control child,int padding=18)=>new(){Classes={"card"},Background=Brushes.White,BorderBrush=new SolidColorBrush(Color.Parse("#DCE5DD")),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(12),Padding=new Thickness(padding),Child=child};
 public static TextBox Input(string text="",string? hint=null,bool multi=false)=>new(){Text=text,Watermark=hint,AcceptsReturn=multi,TextWrapping=multi?TextWrapping.Wrap:TextWrapping.NoWrap,MinHeight=multi?100:34};
 public static Window Window(string title,Control content,double width=680,double height=600)=>new(){Title=title,Width=width,Height=height,MinWidth=450,MinHeight=300,Content=content,WindowStartupLocation=WindowStartupLocation.CenterOwner,FontFamily=new FontFamily(OperatingSystem.IsMacOS()?"PingFang SC":"Microsoft YaHei UI")};
 public static Grid Grid(string columns,string rows="*")=>new(){ColumnDefinitions=new ColumnDefinitions(columns),RowDefinitions=new RowDefinitions(rows)};
 public static void Put(Grid grid,Control control,int column=0,int row=0){Avalonia.Controls.Grid.SetColumn(control,column);Avalonia.Controls.Grid.SetRow(control,row);grid.Children.Add(control);}
}
