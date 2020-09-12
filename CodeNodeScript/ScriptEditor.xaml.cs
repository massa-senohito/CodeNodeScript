using ICSharpCode.AvalonEdit.CodeCompletion;
using ScrEngine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using static ScrEngine.FSTypeInfo;
using Microsoft.FSharp.Core;
using FSharp.Compiler.SourceCodeServices;
using System.Linq;
using System.Threading.Tasks;
using CodeNodeScript.UIUtil;
using System.Diagnostics;

namespace CodeNodeScript
{
        // CopyできるListbox 複数選択したいときには更に手を加える必要がある
        public static class ListBoxBehaviour
        {
            public static readonly DependencyProperty AutoCopyProperty = DependencyProperty.RegisterAttached("AutoCopy",
        typeof(bool), typeof(ListBoxBehaviour), new UIPropertyMetadata(AutoCopyChanged));

            public static bool GetAutoCopy( DependencyObject obj_ )
            {
                return ( bool )obj_.GetValue( AutoCopyProperty );
            }

            public static void SetAutoCopy( DependencyObject obj_ , bool value_ )
            {
                obj_.SetValue( AutoCopyProperty , value_ );
            }

            private static void AutoCopyChanged( DependencyObject obj_ , DependencyPropertyChangedEventArgs e_ )
            {
                var listBox = obj_ as ListBox;
                if ( listBox != null )
                {
                    if ( ( bool )e_.NewValue )
                    {
                        ExecutedRoutedEventHandler handler =
                        (sender_, arg_) =>
                        {
                            if (listBox.SelectedItem != null)
                            {
                                //Copy what ever your want here
                                Clipboard.SetDataObject(listBox.SelectedItem.ToString());
                            }
                        };

                        var command = new RoutedCommand("Copy", typeof (ListBox));
                        command.InputGestures.Add( new KeyGesture( Key.C , ModifierKeys.Control , "Copy" ) );
                        listBox.CommandBindings.Add( new CommandBinding( command , handler ) );
                    }
                }
            }
        }
    /// <summary>
    /// ScriptEditor.xaml の相互作用ロジック
    /// </summary>
    public partial class ScriptEditor : UserControl
    {

        private DispatcherTimer _timer;
        EvalResult Eval;
        private CompletionWindow completionWindow;

        public bool IsNeedUpdateUI { get; private set; }

        public ScriptEditor( )
        {
            InitializeComponent( );
            //Resultlistbox.Items
            CodeBox.TextChanged += CodeBox_TextChanged;
            CodeBox.TextArea.TextEntering += TextArea_TextEntering;
            CodeBox.TextArea.TextEntered += TextArea_TextEntered; ;
            _timer = new DispatcherTimer( );
            _timer.Interval = new TimeSpan( 0,0,1 );
            _timer.Tick += _timer_Tick;
            _timer.Start( );

            Eval = new EvalResult( );
            Eval.Eval( "" );
        }

        private void TextArea_TextEntered( object sender , TextCompositionEventArgs e )
        {
            if ( e.Text == "." )
            {
                AsyncCreateCompletion( );
            }

        }

        private void TextArea_TextEntering( object sender , TextCompositionEventArgs e )
        {
            //補完Windowが開いている間
            if ( e.Text.Length > 0 && completionWindow != null )
            {
                //英数字以外が入力された場合
                if ( !char.IsLetterOrDigit( e.Text[ 0 ] ) )
                {
                    //選択中のリストの項目をエディタに挿入する                
                    completionWindow.CompletionList.RequestInsertion( e );
                }
            }
        }

        FSharpDeclarationListInfo ComplList;
        private void _timer_Tick( object sender , EventArgs e )
        {
//Microsoft.FSharp.Collections.FSharpList
            var caret = CodeBox.TextArea.Caret;
            // フォーカスが移ったとかで見えていないときにShowするとエラー
            bool windowIsClosed = //completionWindow?.Visibility == Visibility.Collapsed;
              false;
            try
            {
                if ( CompleteFinished && !windowIsClosed )
                {
                    completionWindow.Show( );
                    completionWindow.Closed += OnCloseCompletion;
                    IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                    foreach ( var item in ComplList.Items )
                    {
                        data.Add( new CompletionData( item ) );
                    }
                    CompleteFinished = false;
                }

            }
            catch ( Exception ex )
            {
                Trace.WriteLine(ex);
                CompleteFinished = false;
            }

        }

        private void AsyncCreateCompletion( )
        {
            //入力補完Windowを生成
            completionWindow = new CompletionWindow( CodeBox.TextArea );
            //補完リストに表示するアイテムをコレクションに追加する
            //---> 
            //var s = new FSharp.Compiler.SourceCodeServices.AssemblySymbol("Microsoft.FSharp.Collections",)
            var inputLines = CodeBox.Text.Split('\n');
            var caret = CodeBox.TextArea.Caret;
            var compilation = FSTypeInfo.decls(CodeBox.Text , caret.Line , inputLines[caret.Line-1],caret.Column - 2 );
            var fetchCompileTask = AsyncUtil.defaultAsyncAsTask( compilation );
            var tas = fetchCompileTask.ContinueWith( t=> ShowCompletion ( t.Result ) );
        }
        bool CompleteFinished = false;

        private void ShowCompletion(FSharpDeclarationListInfo list)
        {
            ComplList = list;
            CompleteFinished = true;
        }

        void OnCloseCompletion(object sender , EventArgs e)
        {
            completionWindow.Closed -= OnCloseCompletion;
            completionWindow = null;
        }

        private void CodeBox_TextChanged( object sender , EventArgs e )
        {
            IsNeedUpdateUI = true;
            var tex = CodeBox.Text;
            Eval.Eval( tex );
            Resultlistbox.Text = "";
            var resbuilder = new StringBuilder();
            foreach ( var item in Eval.Result )
            {
                resbuilder.Append(item);
                resbuilder.Append( Environment.NewLine );
            }
            Resultlistbox.Text = resbuilder.ToString( );

            CompileErrorLabel.Content = "";

            var errbuilder = new StringBuilder();
            foreach ( var item in Eval.Error )
            {
                errbuilder.Append( item );
            }
            errbuilder.Append( Eval.Except );
            CompileErrorLabel.Content = errbuilder.ToString( );

            // デバッグ用に ResultList使いたいなら
            //Resultlistbox.Text = CodeBox.ToStringReflection( ).Aggregate( ( acc , i ) => acc + "\n" + i );
        }
    }
}
