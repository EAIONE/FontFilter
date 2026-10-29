using System.Diagnostics;
using System.Runtime.InteropServices;
namespace FontFilter.Desktop;

/// <summary>
/// 在系统文件管理器中定位文件。
///
/// C:\Windows\Fonts 里的字体文件是真实存在的普通文件，但这个路径在 shell 里是「字体库」
/// 虚拟文件夹：SHParseDisplayName 对它和它下面的任何路径都返回 0x80070057，explorer /select
/// 会弹「此电脑」；用目录联接换个路径也不行（explorer 会把联接解析回同一个字体库命名空间），
/// 硬链接又要 C:\Windows\Fonts 的写权限（普通权限被拒绝）。
/// 所以系统字体只能打开字体库本身，不再假装能选中单个文件。
/// %LOCALAPPDATA%\Microsoft\Windows\Fonts（用户字体）是普通目录，可以直接选中。
/// </summary>
public static class FileReveal
{
 public enum Outcome{Selected,OpenedFolder,OpenedFontLibrary,Failed}

 public static Outcome Reveal(string path,out string detail)
 {
  detail="";
  string full;
  try{full=Path.GetFullPath(path);}
  catch(Exception ex) when(ex is ArgumentException or NotSupportedException or PathTooLongException){detail="路径无效";return Outcome.Failed;}
  if(!File.Exists(full)){detail="文件已不存在";return Outcome.Failed;}
  if(OperatingSystem.IsWindows())return RevealWindows(full,out detail);
  if(OperatingSystem.IsMacOS()){if(TryStart("/usr/bin/open",new[]{"-R",full}))return Outcome.Selected;detail="系统拒绝了打开请求";return Outcome.Failed;}
  var directory=Path.GetDirectoryName(full);
  if(directory!=null&&TryStart("xdg-open",new[]{directory})){detail=directory;return Outcome.OpenedFolder;}
  detail="未找到可用的文件管理器";
  return Outcome.Failed;
 }

 static Outcome RevealWindows(string full,out string detail)
 {
  detail="";
  var directory=Path.GetDirectoryName(full);
  if(directory is {} fonts&&IsSystemFontsFolder(fonts))
  {
   // 字体文件确实躺在 C:\Windows\Fonts 里，但 shell 不允许按路径选中里面的文件，
   // 只能把字体库本身打开，让用户在列表里按字体名找到它。
   detail=fonts;
   return OpenFolder(fonts)?Outcome.OpenedFontLibrary:NoWay(out detail);
  }
  if(SelectInShell(full))return Outcome.Selected;
  if(directory!=null&&OpenFolder(directory)){detail=directory;return Outcome.OpenedFolder;}
  return NoWay(out detail);
 }

 static Outcome NoWay(out string detail){detail="系统拒绝了打开请求";return Outcome.Failed;}

 static bool IsSystemFontsFolder(string? directory)
 {
  if(string.IsNullOrEmpty(directory))return false;
  var fonts=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Fonts");
  return string.Equals(directory.TrimEnd(Path.DirectorySeparatorChar),fonts.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);
 }

 /// <summary>SHOpenFolderAndSelectItems：返回 HRESULT，0 才代表 shell 真的接受了这次选中请求。</summary>
 static bool SelectInShell(string path)
 {
  IntPtr pidl;
  try{pidl=ILCreateFromPath(path);}
  catch(Exception ex) when(ex is DllNotFoundException or EntryPointNotFoundException){return false;}
  if(pidl==IntPtr.Zero&&SHParseDisplayName(path,IntPtr.Zero,out pidl,0,out _)!=0)return false;
  if(pidl==IntPtr.Zero)return false;
  try{return SHOpenFolderAndSelectItems(pidl,0,null,0)==0;}
  finally{ILFree(pidl);}
 }

 static bool OpenFolder(string directory)
 {
  try{using var process=Process.Start(new ProcessStartInfo{FileName="explorer.exe",Arguments="\""+directory+"\"",UseShellExecute=true});return true;}
  catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException){return false;}
 }

 static bool TryStart(string fileName,string[] arguments)
 {
  try
  {
   var info=new ProcessStartInfo{FileName=fileName,UseShellExecute=false};
   foreach(var argument in arguments)info.ArgumentList.Add(argument);
   using var process=Process.Start(info);return true;
  }
  catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or PlatformNotSupportedException){return false;}
 }

 [DllImport("shell32.dll",CharSet=CharSet.Unicode,SetLastError=false)]
 static extern IntPtr ILCreateFromPath([MarshalAs(UnmanagedType.LPWStr)]string path);
 [DllImport("shell32.dll",CharSet=CharSet.Unicode)]
 static extern int SHParseDisplayName([MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr bindingContext,out IntPtr pidl,uint attributesIn,out uint attributesOut);
 [DllImport("shell32.dll")]
 static extern int SHOpenFolderAndSelectItems(IntPtr folder,uint count,IntPtr[]? items,uint flags);
 [DllImport("shell32.dll")]
 static extern void ILFree(IntPtr pidl);
}
