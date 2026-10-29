using System.Globalization;
namespace FontFilter;
public static class FontOrdering
{
 static readonly StringComparer ChineseNames=StringComparer.Create(CultureInfo.GetCultureInfo("zh-CN"),true);
 public static bool NameLooksChinese(string name)=>name.Any(c=>c>='\u3400'&&c<='\u9fff')||new[]{"CJK","Source Han","Noto Sans SC","Noto Serif SC","Noto Sans TC","Noto Serif TC","SimSun","SimHei","KaiTi","FangSong","Microsoft YaHei"}.Any(s=>name.Contains(s,StringComparison.OrdinalIgnoreCase));
 public static bool HasChineseGlyphs(IDictionary<int,ushort> map)=>"中文天地人国".Count(c=>map.TryGetValue(c,out var index)&&index!=0)>=4;
 public static IEnumerable<FontRecord> Order(IEnumerable<FontRecord> fonts)=>fonts.OrderBy(f=>f.SupportsChinese==true||f.SupportsChinese==null&&NameLooksChinese(f.Family)?0:f.SupportsChinese==null?1:2).ThenBy(f=>f.Family.Any(c=>c>='\u3400'&&c<='\u9fff')?0:1).ThenBy(f=>f.Family,ChineseNames).ThenBy(f=>IsRegular(f.Face)?0:1).ThenBy(f=>f.Face,ChineseNames);
 static bool IsRegular(string face)=>new[]{"Regular","Normal","常规","标准","標準"}.Any(s=>face.Equals(s,StringComparison.OrdinalIgnoreCase));
}
