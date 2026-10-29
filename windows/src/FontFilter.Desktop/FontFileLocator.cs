using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using SkiaSharp;
namespace FontFilter.Desktop;
public static class FontFileLocator
{
 static readonly Dictionary<string,string> Cache=new();
 public static Task<string?> FindAsync(FontRecord record)=>Task.Run(()=>{
  lock(Cache)if(Cache.TryGetValue(record.Id,out var cached)&&File.Exists(cached))return cached;
  var glyph=FontScanner.Resolve(record);string? path=null;
  if(OperatingSystem.IsMacOS())path=FindMac(glyph);
  if(OperatingSystem.IsWindows())path=FindWindows(glyph);
  if(path!=null&&File.Exists(path)){lock(Cache)Cache[record.Id]=path;return path;}return null;
 });
 static string? FindMac(SKTypeface glyph)
 {
  var name=FontScanner.LocalName(glyph,6,"");if(name.Length==0)return null;
  IntPtr text=IntPtr.Zero,font=IntPtr.Zero,url=IntPtr.Zero,fsPath=IntPtr.Zero,actualName=IntPtr.Zero,library=IntPtr.Zero;
  try{
   text=CFStringCreateWithCString(IntPtr.Zero,name,0x08000100);font=CTFontCreateWithName(text,12,IntPtr.Zero);if(font==IntPtr.Zero)return null;
   actualName=CTFontCopyPostScriptName(font);if(ReadString(actualName)!=name)return null;
   library=NativeLibrary.Load(CoreText);var key=Marshal.ReadIntPtr(NativeLibrary.GetExport(library,"kCTFontURLAttribute"));
   url=CTFontCopyAttribute(font,key);if(url==IntPtr.Zero)return null;fsPath=CFURLCopyFileSystemPath(url,0);var path=ReadString(fsPath);return File.Exists(path)?path:null;
  }finally{foreach(var value in new[]{actualName,fsPath,url,font,text})if(value!=IntPtr.Zero)CFRelease(value);if(library!=IntPtr.Zero)NativeLibrary.Free(library);}
 }
 static string ReadString(IntPtr value){if(value==IntPtr.Zero)return "";var bytes=new byte[16384];return CFStringGetCString(value,bytes,bytes.Length,0x08000100)?Encoding.UTF8.GetString(bytes,0,Array.IndexOf(bytes,(byte)0)):"";}
 static string? FindWindows(SKTypeface glyph)
 {
  if(!OperatingSystem.IsWindows())return null;
  var directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"Fonts");var userDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Microsoft","Windows","Fonts");var candidates=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  foreach(var root in new[]{Registry.LocalMachine,Registry.CurrentUser}){
   using var key=root.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts");if(key==null)continue;
   foreach(var name in key.GetValueNames())if(key.GetValue(name) is string file){file=Environment.ExpandEnvironmentVariables(file);if(Path.IsPathRooted(file))candidates.Add(file);else{candidates.Add(Path.Combine(directory,file));candidates.Add(Path.Combine(userDirectory,file));}}
  }
  foreach(var folder in new[]{directory,userDirectory})if(Directory.Exists(folder))foreach(var path in Directory.EnumerateFiles(folder))if(new[]{".ttf",".otf",".ttc",".otc"}.Contains(Path.GetExtension(path),StringComparer.OrdinalIgnoreCase))candidates.Add(path);
  var postscript=FontScanner.LocalName(glyph,6,"");var fingerprint=FontScanner.Fingerprint(glyph);
  foreach(var path in candidates.Where(File.Exists)){
   try{int count=1;using(var stream=File.OpenRead(path)){var header=new byte[12];if(stream.Read(header)==12&&Encoding.ASCII.GetString(header.AsSpan(0,4))=="ttcf")count=(int)Math.Min(256,BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8)));}
    for(int i=0;i<count;i++){using var candidate=SKTypeface.FromFile(path,i);if(candidate!=null&&FontScanner.LocalName(candidate,6,"")==postscript&&FontScanner.Fingerprint(candidate)==fingerprint)return path;}
   }catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or ArgumentException){}
  }
  return null;
 }
 const string CoreText="/System/Library/Frameworks/CoreText.framework/CoreText",CoreFoundation="/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
 [DllImport(CoreFoundation)] static extern IntPtr CFStringCreateWithCString(IntPtr allocator,[MarshalAs(UnmanagedType.LPUTF8Str)] string text,uint encoding);
 [DllImport(CoreFoundation)] [return:MarshalAs(UnmanagedType.I1)] static extern bool CFStringGetCString(IntPtr text,byte[] buffer,nint size,uint encoding);
 [DllImport(CoreFoundation)] static extern IntPtr CFURLCopyFileSystemPath(IntPtr url,int style);
 [DllImport(CoreFoundation)] static extern void CFRelease(IntPtr value);
 [DllImport(CoreText)] static extern IntPtr CTFontCreateWithName(IntPtr name,double size,IntPtr matrix);
 [DllImport(CoreText)] static extern IntPtr CTFontCopyAttribute(IntPtr font,IntPtr attribute);
 [DllImport(CoreText)] static extern IntPtr CTFontCopyPostScriptName(IntPtr font);
}
