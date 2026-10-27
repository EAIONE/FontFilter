using FontFilter;
using System.Text.Json;
public static class ThemeTests
{
 public static void Run(Action<string,Action> test,Action<bool,string> check)
 {
  test("background color and details state persist independently",()=>{var settings=new UserSettings{ThemeColor="#E0A000",HeaderColor="#168468",BackgroundColor="#EEF2EE",DetailsCollapsed=true};var saved=JsonSerializer.Deserialize<UserSettings>(JsonSerializer.Serialize(settings))!;var palette=ThemePalette.From(saved);check(palette.Primary=="#E0A000"&&palette.Header=="#168468"&&palette.Background=="#EEF2EE"&&saved.DetailsCollapsed,"palette roles or panel state lost");});
  test("feature dimensions start with category temperament and usage",()=>{var categories=TagCatalog.OrderForPicker(TagCatalog.All).Select(t=>t.Category).Distinct().ToArray();check(categories.Take(3).SequenceEqual(new[]{"字体类别","风格气质","用途"}),string.Join(",",categories));check(TagCatalog.OrderForPicker(TagCatalog.All).Count()==TagCatalog.All.Count,"sorting lost tags");});
  test("other tag dimensions retain original order",()=>{var tags=new[]{new TagDefinition("笔画","甲",""),new TagDefinition("用途","乙",""),new TagDefinition("结构","丙",""),new TagDefinition("字体类别","丁",""),new TagDefinition("风格气质","戊",""),new TagDefinition("官网及自定义","己","")};check(TagCatalog.OrderForPicker(tags).Select(t=>t.Name).SequenceEqual(new[]{"丁","戊","乙","甲","丙","己"}),"other dimensions reordered");});
  test("header background is independent from primary and persists",()=>{var settings=new UserSettings{ThemeColor="#112233",HeaderColor="#EDF4F1"};var saved=JsonSerializer.Deserialize<UserSettings>(JsonSerializer.Serialize(settings))!;var palette=ThemePalette.From(saved);check(palette.Primary=="#112233"&&palette.Header=="#EDF4F1","header follows primary");});
  test("three theme colors persist independently",()=>{var settings=new UserSettings{ThemeColor="#123456",SecondaryColor="#EEDDBB",AccentColor="#DD5522"};var saved=JsonSerializer.Deserialize<UserSettings>(JsonSerializer.Serialize(settings))!;var palette=ThemePalette.From(saved);check(palette.Primary=="#123456"&&palette.Secondary=="#EEDDBB"&&palette.Accent=="#DD5522","roles overwritten");});
  test("old theme setting stays primary and invalid colors default independently",()=>{var old=JsonSerializer.Deserialize<UserSettings>("{\"ThemeColor\":\"#335577\"}")!;var palette=ThemePalette.From(old);check(palette.Primary=="#335577"&&palette.Secondary==ThemePalette.DefaultSecondary&&palette.Accent==ThemePalette.DefaultAccent,"old theme lost");old.SecondaryColor="bad";old.AccentColor="#778899";palette=ThemePalette.From(old);check(palette.Primary=="#335577"&&palette.Secondary==ThemePalette.DefaultSecondary&&palette.Accent=="#778899","invalid slot damaged other roles");});
 }
}
