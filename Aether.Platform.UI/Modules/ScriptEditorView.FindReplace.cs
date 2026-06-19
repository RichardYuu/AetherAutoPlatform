using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Scripting;

namespace Aether.Platform.UI.Modules
{
    public partial class ScriptEditorView : UserControl, IModuleView
    {
        // ============================================================
        //  查找/替换
        // ============================================================

        private void ShowFindPanel(bool replace)
        {
            _findPanel.Visible = true;
            _replaceBox.Visible = replace;
            _btnReplace.Visible = replace;
            _btnReplaceAll.Visible = replace;

            // 预填选中文字
            if (_codeEditor.SelectedText.Length > 0 && _codeEditor.SelectedText.Length < 100)
                _findBox.Text = _codeEditor.SelectedText;

            _findBox.Focus();
            _findBox.SelectAll();
        }

        private void HideFindPanel()
        {
            _findPanel.Visible = false;
            _lblFindCount.Text = "";
        }

        private void FindNext(bool showCount = false)
        {
            var text = _findBox.Text;
            if (string.IsNullOrEmpty(text)) return;

            var source = _codeEditor.Text;
            int startIndex = _codeEditor.SelectionStart + _codeEditor.SelectionLength;
            if (startIndex >= source.Length) startIndex = 0;

            int idx = source.IndexOf(text, startIndex, StringComparison.Ordinal);
            if (idx < 0 && startIndex > 0)
            {
                // 从开头重新搜索
                idx = source.IndexOf(text, 0, StringComparison.Ordinal);
                if (idx >= 0)
                    AppendOutput("--- 已从开头重新搜索 ---");
            }

            if (idx >= 0)
            {
                _codeEditor.Select(idx, text.Length);
                _codeEditor.ScrollToCaret();
                _codeEditor.Focus();
            }

            // 统计匹配数
            int count = 0;
            int searchFrom = 0;
            while ((searchFrom = source.IndexOf(text, searchFrom, StringComparison.Ordinal)) >= 0)
            {
                count++;
                searchFrom += text.Length;
            }
            _lblFindCount.Text = idx >= 0 ? $"{count}处" : "0处";
        }

        private void ReplaceOne()
        {
            if (_codeEditor.SelectedText == _findBox.Text)
            {
                _codeEditor.SelectedText = _replaceBox.Text;
            }
            FindNext();
        }

        private void ReplaceAll()
        {
            var find = _findBox.Text;
            var replace = _replaceBox.Text;
            if (string.IsNullOrEmpty(find)) return;

            int count = 0;
            var source = _codeEditor.Text;
            var sb = new StringBuilder(source);

            // 倒序替换（避免索引偏移）
            var matches = new List<int>();
            int idx = 0;
            while ((idx = source.IndexOf(find, idx, StringComparison.Ordinal)) >= 0)
            {
                matches.Add(idx);
                idx += find.Length;
            }

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                sb.Remove(matches[i], find.Length);
                sb.Insert(matches[i], replace);
                count++;
            }

            _codeEditor.Text = sb.ToString();
            AppendOutput($"--- 已替换 {count} 处 ---");
            _lblFindCount.Text = $"{count}处已替换";
        }
    }
}
