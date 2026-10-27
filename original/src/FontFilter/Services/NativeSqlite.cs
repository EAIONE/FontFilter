using System.Runtime.InteropServices;
namespace FontFilter;
internal sealed class NativeSqlite:IDisposable
{
 const string Dll="winsqlite3.dll"; IntPtr db;
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string name,out IntPtr db,int flags,IntPtr vfs);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close_v2(IntPtr db);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_busy_timeout(IntPtr db,int ms);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_errmsg(IntPtr db);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr db,[MarshalAs(UnmanagedType.LPUTF8Str)] string sql,int len,out IntPtr stmt,IntPtr tail);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_bind_text(IntPtr stmt,int index,[MarshalAs(UnmanagedType.LPUTF8Str)] string value,int len,IntPtr destructor);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr stmt);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr stmt);
 [DllImport(Dll,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text(IntPtr stmt,int col);
 public NativeSqlite(string path){var rc=sqlite3_open_v2(path,out db,2|4|0x10000,IntPtr.Zero);if(rc!=0){var error=Error();Dispose();throw new InvalidOperationException(error);}sqlite3_busy_timeout(db,5000);}
 string Error()=>Marshal.PtrToStringUTF8(sqlite3_errmsg(db))??"SQLite错误";
 public List<string[]> Query(string sql,params string[] values)
 {
  if(db==IntPtr.Zero)throw new ObjectDisposedException(nameof(NativeSqlite));
  if(sqlite3_prepare_v2(db,sql,-1,out var stmt,IntPtr.Zero)!=0)throw new InvalidOperationException(Error());
  try{for(int i=0;i<values.Length;i++)if(sqlite3_bind_text(stmt,i+1,values[i],-1,new IntPtr(-1))!=0)throw new InvalidOperationException(Error());
   var rows=new List<string[]>();int rc;while((rc=sqlite3_step(stmt))==100){var row=new string[13];for(int i=0;i<13;i++)row[i]=Marshal.PtrToStringUTF8(sqlite3_column_text(stmt,i))??"";rows.Add(row);}if(rc!=101)throw new InvalidOperationException(Error());return rows;
  }finally{sqlite3_finalize(stmt);}
 }
 public void Dispose(){if(db!=IntPtr.Zero){sqlite3_close_v2(db);db=IntPtr.Zero;}}
}
