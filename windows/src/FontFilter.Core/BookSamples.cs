namespace FontFilter;
public static class BookSamples
{
 public const string DefaultKey="简体内文（分段）",TraditionalKey="繁體內文（分段）",EnglishKey="English · paragraphs",MixedKey="中英混排（分段）";
 public const string Traditional="清晨的光從窗邊緩緩移過，落在剛剛翻開的書頁上。紙張微微泛著溫暖的顏色，字與字之間留著安靜的空隙。閱讀有時並不急於尋找答案，我們只是順著一行行文字向前走，讓遠處的風景與眼前的生活慢慢相遇。\n一本書的模樣，往往在閱讀開始之前就已留下印象。開本決定了握在手中的分量，版心安排著目光行進的方向，天頭與地腳為文字留下呼吸的餘地。那些看似細小的距離，並不是空白的浪費，而是讓內容變得清楚、讓長時間閱讀更加舒適的必要部分。\n我們辨認一款字體，不只看單個字的輪廓，也看它們排列在一起時形成的節奏。橫畫和豎畫的輕重，轉折處的收放，字面之間的疏密，都在整段文字中彼此回應。正文所需要的，常常是一種耐心而穩定的聲音。";
 public const string English="Morning light moves slowly across the desk and settles on the open book. Reading gives us time to notice what a hurried day can hide: a quiet street, a familiar voice, or a question that deserves another look. The page is a small space, yet it can hold an entire landscape of thoughts.\nTypography shapes that space. A comfortable line length helps the eye return to the beginning of the next line, while generous margins give the text room to breathe. Letters, words, and paragraphs should form a steady rhythm, allowing the reader to follow the meaning without unnecessary interruption.\nA typeface reveals its character through more than a single letter. Its proportions, spacing, and contrast become clearer in continuous reading. Comparing a full page shows how individual details work together, and how a change in size or leading can alter the texture of the whole.\nAt the end of the evening, we close the book but keep something of its atmosphere. An image or a sentence may return later, quietly changing the way we understand an ordinary moment. Good pages make room for that encounter between the words and the reader.";
 public const string Mixed="阅读一本书，也是在体会 Typography 如何组织信息。字形、段落与留白共同形成阅读节奏；在中英文混排中，完整的 English words 应保持连贯，不能为了凑满一行而随意截断。\n版心决定了文字的边界，Line spacing 则影响每一行之间的呼吸。我们可以比较不同字体的正文效果，观察中文标点、英文引号以及数字（185 × 260 mm）如何与周围的文字协调。\n“清晰而自然”，是排版希望达到的状态。Good typography supports reading, rather than distracting from it. 调整字号、字距和行距时，应同时关注一行文字的细节与整页的灰度，让内容在稳定的秩序中展开。";
 public static string Text{get;}=Build();
 static string Build()
 {
  string[] paragraphs={
   "清晨的光从窗边缓缓移过，落在刚刚翻开的书页上。纸张微微泛着温暖的颜色，字与字之间留着安静的空隙。阅读有时并不急于寻找答案，我们只是顺着一行行文字向前走，让远处的风景与眼前的生活慢慢相遇。窗外偶尔传来脚步声，屋里的时间却仿佛比平日从容了一些。",
   "一本书的模样，往往在阅读开始之前就已留下印象。开本决定了握在手中的分量，版心安排着目光行进的方向，天头与地脚为文字留下呼吸的余地。那些看似细小的距离，并不是空白的浪费，而是让内容变得清楚、让长时间阅读更加舒适的必要部分。",
   "我们辨认一款字体，不只看单个字的轮廓，也看它们排列在一起时形成的节奏。横画和竖画的轻重，转折处的收放，字面之间的疏密，都在整段文字中彼此回应。适合标题的鲜明个性，未必适合连续数页的内文；正文所需要的，常常是一种耐心而稳定的声音。",
   "午后沿着旧街散步，街角的小店还保留着手写的招牌。木门上的刻痕、墙面上褪色的字迹，以及来往行人不经意的交谈，让这条熟悉的路有了新的层次。记录这些细节，并非为了给日常寻找宏大的意义，而是提醒自己，在匆忙的日子里仍然可以停下来，认真看见身边的事物。",
   "回到桌前，再看刚才读过的几页，文字似乎也有了不同的温度。段落之间的停顿像道路上的转弯，让思绪从一个场景进入另一个场景。首行的退让、恰当的行距与均匀的字距，共同构成一种不必刻意察觉的秩序。排版的工作，正是在这样的细节里帮助作者与读者相遇。",
   "好的设计并不要求每一处都引人注目。有些部分需要清晰地站出来，有些部分则应安静地退到后面。对于书籍内文而言，稳定的灰度与连续的阅读节奏尤其重要。当目光能够顺畅地从这一行移到下一行，当翻页成为自然的动作，形式便真正参与了内容的表达。",
   "夜色渐深，桌上的台灯照亮了最后一段文字。合上书之后，留在记忆里的也许不是某一句完整的话，而是一幅景象、一种情绪，或一个尚未想明白的问题。阅读没有在合页的瞬间结束，它会随着之后的经历继续生长，在某个平常的时刻，再次为我们打开一扇窗。"
  };
  var result=new System.Text.StringBuilder();for(var i=0;;i++){var paragraph="　　"+paragraphs[i%paragraphs.Length];if(result.Length+paragraph.Length+1>4000)break;if(result.Length>0)result.Append('\n');result.Append(paragraph);}return result.ToString();
 }
}
