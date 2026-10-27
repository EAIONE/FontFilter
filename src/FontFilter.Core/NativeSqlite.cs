using System.Runtime.InteropServices;
namespace FontFilter;
internal sealed class NativeSqlite:IDisposable
{
 const string Dll="e_sqlite3"; IntPtr db;
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string name,out IntPtr db,int flags,IntPtr vfs);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close_v2(IntPtr db);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_busy_timeout(IntPtr db,int ms);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_errmsg(IntPtr db);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr db,[MarshalAs(UnmanagedType.LPUTF8Str)] string sql,int len,out IntPtr stmt,IntPtr tail);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_bind_text(IntPtr stmt,int index,[MarshalAs(UnmanagedType.LPUTF8Str)] string value,int len,IntPtr destructor);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr stmt);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr stmt);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text(IntPtr stmt,int col);
 public NativeSqlite(string path,bool readOnly=false){var rc=sqlite3_open_v2(path,out db,readOnly?1:2|4|0x10000,IntPtr.Zero);if(rc!=0){var error=Error();Dispose();throw new InvalidOperationException(error);}sqlite3_busy_timeout(db,5000);}
 string Error()=>Marshal.PtrToStringUTF8(sqlite3_errmsg(db))??"SQLite错误";
 public List<string[]> Query(string sql,params string[] values)
 {
  if(db==IntPtr.Zero)throw new ObjectDisposedException(nameof(NativeSqlite));
  if(sqlite3_prepare_v2(db,sql,-1,out var stmt,IntPtr.Zero)!=0)throw new InvalidOperationException(Error());
  try{for(int i=0;i<values.Length;i++)if(sqlite3_bind_text(stmt,i+1,values[i],-1,new IntPtr(-1))!=0)throw new InvalidOperationException(Error());
   var rows=new List<string[]>();int rc;while((rc=sqlite3_step(stmt))==100){var row=new string[13];for(int i=0;i<13;i++)row[i]=Marshal.PtrToStringUTF8(sqlite3_column_text(stmt,i))??"";rows.Add(row);}if(rc!=101)throw new InvalidOperationException(Error());return rows;
  }finally{sqlite3_finalize(stmt);}
 }
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_backup_init(IntPtr dest,[MarshalAs(UnmanagedType.LPUTF8Str)] string destName,IntPtr source,[MarshalAs(UnmanagedType.LPUTF8Str)] string sourceName);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_step(IntPtr backup,int pages);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_backup_finish(IntPtr backup);
 public static void Snapshot(string source,string destination)
 {
  using var input=new NativeSqlite(source,true);using var output=new NativeSqlite(destination);
  var backup=sqlite3_backup_init(output.db,"main",input.db,"main");if(backup==IntPtr.Zero)throw new InvalidOperationException(output.Error());
  int rc;try{rc=sqlite3_backup_step(backup,-1);}finally{sqlite3_backup_finish(backup);}
  if(rc!=101)throw new InvalidOperationException($"数据库无法备份（SQLite {rc}）：{input.Error()} / {output.Error()}。请关闭原程序后重试。");
 }
 public void Dispose(){if(db!=IntPtr.Zero){sqlite3_close_v2(db);db=IntPtr.Zero;}}
}
