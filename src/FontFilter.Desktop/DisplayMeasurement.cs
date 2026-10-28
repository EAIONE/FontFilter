using System.Runtime.InteropServices;
using Avalonia.Controls;
namespace FontFilter.Desktop;
public sealed record DisplayMeasurement(string Key,string Name,double Scale,bool Estimated)
{
 public static DisplayMeasurement Read(Window window)
 {
  var screen=window.Screens.ScreenFromWindow(window)??window.Screens.Primary;
  if(screen==null)return new("unknown","当前屏幕",1,false);
  var key=$"{screen.DisplayName}:{screen.Bounds.Width}x{screen.Bounds.Height}:{screen.Scaling:0.###}";double width=0;
  try{
   var handle=screen.TryGetPlatformHandle();
   if(OperatingSystem.IsMacOS()&&handle?.HandleDescriptor=="CGDirectDisplayID"){
    var display=(uint)handle.Handle.ToInt64();var size=CGDisplayScreenSize(display);var rotation=CGDisplayRotation(display);width=Math.Abs(rotation%180)>45?size.Height:size.Width;key=$"mac:{display}:"+key;
   }else if(OperatingSystem.IsWindows()&&handle!=null){
    var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};if(GetMonitorInfo(handle.Handle,ref info)){
     var dc=CreateDC("DISPLAY",info.Device,null,IntPtr.Zero);if(dc!=IntPtr.Zero)try{width=GetDeviceCaps(dc,4);}finally{DeleteDC(dc);}key="win:"+info.Device+":"+key;
    }
   }
  }catch{width=0;}
  var scale=width>0?PhysicalPageScale.FromDisplay(screen.Bounds.Width,screen.Scaling,width):1;
  if(!double.IsFinite(scale)||scale<.25||scale>4)return new(key,screen.DisplayName??"当前屏幕",1,false);
  return new(key,screen.DisplayName??"当前屏幕",scale,width>0);
 }
 [StructLayout(LayoutKind.Sequential)]struct MillimeterSize{public double Width,Height;}
 [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]static extern MillimeterSize CGDisplayScreenSize(uint display);
 [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]static extern double CGDisplayRotation(uint display);
 [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct MonitorInfo{public int Size;public NativeRect Monitor,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
 [DllImport("user32.dll",EntryPoint="GetMonitorInfoW",CharSet=CharSet.Unicode)] [return:MarshalAs(UnmanagedType.Bool)]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 [DllImport("gdi32.dll",EntryPoint="CreateDCW",CharSet=CharSet.Unicode)]static extern IntPtr CreateDC(string driver,string device,string? output,IntPtr init);
 [DllImport("gdi32.dll")]static extern int GetDeviceCaps(IntPtr dc,int index);
 [DllImport("gdi32.dll")] [return:MarshalAs(UnmanagedType.Bool)]static extern bool DeleteDC(IntPtr dc);
}
