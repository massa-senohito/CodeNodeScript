using FSharp.Compiler.SourceCodeServices;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace CodeNodeScript.UIUtil
{
    public class CompletionData : ICompletionData
    {
        //入力候補一覧(UI)に表示する内容
        public object Content { get; set; }
        //ツールチップに表示する説明文
        public object Description { get; set; }
        //入力候補の左側に表示するアイコン
        public ImageSource Image { get; set; }
        //表示順の優先度？
        public double Priority { get; set; }
        //アイテム選択時に挿入される文字列
        public string Text { get; set; }
        // 補完候補を追加
        public CompletionData( string entry )
        {
            Text = entry;
            Content = Text;
        }
        public CompletionData( FSharpDeclarationListItem item)
        {
            Text = item.Name;
            Content = item.Name;
        }
        //アイテム選択後の処理
        public void Complete( TextArea textArea , ISegment completionSegment , EventArgs insertionRequestEventArgs )
        {
            textArea.Document.Replace( completionSegment , Text );
        }
    }
}
