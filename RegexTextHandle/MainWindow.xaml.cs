using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace RegexTextHandle
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow /*:Window*/
    {
        AutoResetEvent resetEvent = new AutoResetEvent(false);
        string textInput;
        string textRegex1;
        string textRegex2;
        
    
        public MainWindow()
        {
            InitializeComponent();
            Task.Run(UpdateInput);
            //WindowChrome.SetIsHitTestVisibleInChrome(buttonText, true);
        }
        #region UI 事件与UI操作
        private void OnInputChanged(object sender, TextChangedEventArgs e)
        {
            textRegex1 = mTextRegex1.Text;
            textRegex2 = mTextRegex2.Text;
            textInput = GetInputText();
            resetEvent.Set();


        }


        string GetInputText()
        {
            TextRange textRange = new TextRange(mTextInput.Document.ContentStart, mTextInput.Document.ContentEnd);

            return textRange.Text;
        }

        void UpdateInput()
        {
            while (true)
            {
                resetEvent.WaitOne();
                if (string.IsNullOrEmpty(textRegex1) || string.IsNullOrEmpty(textInput))
                {
                    continue;
                }
                try
                {
                    //MatchCollection mc = Regex.Matches(textInput, textRegex1);
                    GetRegexTexts(textInput, textRegex1, textRegex2,out var list1,out var list2);
                    Dispatcher.Invoke(() => { UpdateExtract(list1, list2); });

                }
                catch (Exception)
                {

                    continue;
                }


            }
        }
        /// <summary>
        /// 多余空格移除
        /// https://bbs.csdn.net/topics/360062812
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveSpace(object sender, RoutedEventArgs e)
        {
            TextReplace(mTextInput, " ", "[ ]{2,}");

        }
        private void TextReplace(object sender, RoutedEventArgs e)
        {
            RichTextBoxCopy(mTextInput, mTextOutPut);
            TextReplace(mTextOutPut, mTextFormat.Text, textRegex1, textRegex2);
        }

        private void StringFormat(object sender, RoutedEventArgs e)
        {
            mTextOutPut.Document.Blocks.Clear();
            try
            {
                List<string> textsFound = new List<string>();
                
                RegexText(GetInputText(), textRegex1, textRegex2, (item,position) => {
                    textsFound.Add(item.Value);
                });
                foreach (var item in textsFound)
                {
                    var text = string.Format(mTextFormat.Text, item);
                    mTextOutPut.Document.Blocks.Add(new Paragraph(new Run(text) { Foreground = Brushes.Blue }));
                }
            }
            catch (Exception)
            {


            }
        }
        private void StringFormatAndReplace(object sender, RoutedEventArgs e)
        {
            /// 要求list1与list2的数据一一对应
            /// 
            RichTextBoxCopy(mTextInput, mTextOutPut);

            var start = mTextOutPut.Document.Blocks.FirstBlock;
            /// 从头开始进行字节匹配
            //textInput = GetInputText();
            while (start != null)
            {

                TextRange textRange = new TextRange(start.ContentStart, start.ContentEnd);
                RegexText(textRange.Text, textRegex1, "", (item, position) => {

                    string to = "";
                    /// 检测正则2是否为空
                    if (string.IsNullOrEmpty(textRegex2))
                    {
                        to = string.Format(mTextFormat.Text, item.Value);
                    }
                    else
                    {
                        /// 1. 检测是否存在正则2的match
                        /// 1.1 存在，只对第一个match进行替换，剩余的忽略
                        /// 1.2 不存在，跳过
                        /// 
                        MatchCollection mc = Regex.Matches(item.Value, textRegex2);
                        if (mc.Count>0)
                        {
                            foreach (Match i in mc)
                            {
                                to = string.Format(mTextFormat.Text, i.Value);
                            }
                        }
                        else
                        {
                             
                        }
                    }
                    /// 只在to存在的时候替换
                    if (string.IsNullOrEmpty(to) == false)
                    {
                        /// 选中文本，然后替换
                        mTextOutPut.Selection.Select(textRange.Start.GetPositionAtOffset(position),
                                     textRange.Start.GetPositionAtOffset(item.Value.Length + position));
                        mTextOutPut.Selection.Text = to;
                    }
                   
                });

                start = start.NextBlock;
            }
            

        }
        /// <summary>
        /// 刷新正则提取数据
        /// </summary>
        /// <param name="mc"></param>
        void UpdateExtract(List<string> ts, List<string> ts2)
        {

            Update(mTextExtract, ts);
            Update(mTextExtract2, ts2);
            ///异步执行，防止UI卡顿
            async void Update(RichTextBox r, List<string> l)
            {
                r.Document.Blocks.Clear();
                r.AppendText($"共找到 {l.Count} 处匹配：\r");
                string textShow = "";
                await Task.Run(()=> 
                 {
                     foreach (var item in l)
                     {
                         textShow += item + "\r";
                     }


                 });
                r.AppendText($"{textShow}");
            }
            //textsFound.Clear();

        }
        #endregion

        /// <summary>
        /// 对RichtextBOx文本进行字符替换
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="regex">正则变换规则</param>
        /// <param name="to">替换为</param>
        void TextReplace(RichTextBox textBox, string to,string  regex1,string regex2 = "")
        {
            var start = textBox.Document.Blocks.FirstBlock;

            /// 从头开始进行字节匹配
            //textInput = GetInputText();
            while (start != null)
            {

                TextRange textRange = new TextRange(start.ContentStart, start.ContentEnd);
                RegexText(textRange.Text, regex1, regex2, (item, position) => {
                    textBox.Selection.Select(textRange.Start.GetPositionAtOffset(position),
                                     textRange.Start.GetPositionAtOffset(item.Value.Length + position));
                    textBox.Selection.Text = to;
                });
               
                start = start.NextBlock;
            }
            
        }
        List<string> GetRegexedText(string orignalText, string regex1, string regex2 = "")
        {
           
               var result = new List<string>();
            RegexText(orignalText, regex1, regex2, (m,position) => { result.Add(m.Value); });
           
            return result;
        }
        void GetRegexTexts(string orignalText, string regex1, string regex2, out List<string> list1,out List<string> list2)
        {
            var l1 = new List<string>();
            var l2 = new List<string>();
            RegexText(orignalText, regex1, regex2, null, (item, isFirstRegex) => 
            {
                if (isFirstRegex)
                {
                    l1.Add(item.Value);
                }
                else
                {
                    l2.Add(item.Value);
                }
            });
            list1 = l1;
            list2 = l2;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="orignalText"></param>
        /// <param name="regex1"></param>
        /// <param name="regex2"></param>
        /// <param name="OnFinalMatchCallBack">最终显示的Match回调，只能返回其中一组正咋表达式返回 int：字符起始位置</param>
        /// <param name="OnAllMatchCallBack">对于所有的Match都会触发的回调，bool：是否为第一个正则表达式</param>
        void RegexText(string orignalText, string regex1, string regex2, Action<Match, int> OnFinalMatchCallBack = null, Action<Match, bool> OnAllMatchCallBack = null)
        {
            MatchCollection mc = Regex.Matches(orignalText, regex1);
            foreach (Match item in mc)
            {
                OnAllMatchCallBack?.Invoke(item, true);
                int positionStart = item.Index;
                if (string.IsNullOrEmpty(regex2) == false)
                {
                    MatchCollection mc2 = Regex.Matches(item.Value, regex2);
                    foreach (Match mc2Item in mc2)
                    {
                        OnFinalMatchCallBack?.Invoke(mc2Item, positionStart + mc2Item.Index);
                        OnAllMatchCallBack?.Invoke(mc2Item, false);
                    }
                }
                else
                {
                    OnFinalMatchCallBack?.Invoke(item, positionStart );
                }

            }
        }
        
        void RichTextBoxCopy(RichTextBox from,RichTextBox to)
        {
            to.Document.Blocks.Clear();
            foreach (var item in from.Document.Blocks)
            {
                TextRange textRange = new TextRange(item.ContentStart, item.ContentEnd);
                to.Document.Blocks.Add(new Paragraph(new Run(textRange.Text) { Foreground = Brushes.Blue}));
            }
        }


    }
}
