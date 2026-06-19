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
        //  语法高亮 & 行号 & 断点 & 当前行
        // ============================================================

        private void ApplySyntaxHighlighting()
        {
            if (_codeEditor == null || IsDisposed) return;

            var text = _codeEditor.Text;
            if (string.IsNullOrEmpty(text)) return;
            if (text == _lastHighlightText) return; // 避免重复高亮
            _lastHighlightText = text;

            int selStart = _codeEditor.SelectionStart;
            int selLen = _codeEditor.SelectionLength;

            _codeEditor.SuspendLayout();

            // Step 1: 全部恢复为前景色
            _codeEditor.SelectAll();
            _codeEditor.SelectionColor = EditorFg;
            _codeEditor.SelectionFont = _codeEditor.Font;

            // Step 2-5: 按优先级叠加
            HighlightPattern(LuaCommentRegex, CmtGreen);
            HighlightPattern(LuaString1Regex, StrOrange);
            HighlightPattern(LuaString2Regex, StrOrange);
            HighlightPattern(LuaNumberRegex, NumCyan);
            HighlightPattern(LuaKeywordRegex, KwBlue);

            // Step 6: 当前行高亮（黄色背景覆盖行）
            if (_currentHighlightLine >= 0 && _currentHighlightLine < _codeEditor.Lines.Length)
            {
                int lineStart = _codeEditor.GetFirstCharIndexFromLine(_currentHighlightLine);
                int lineLen = _codeEditor.Lines[_currentHighlightLine].Length;
                _codeEditor.Select(lineStart, lineLen);
                _codeEditor.SelectionBackColor = CurrentLineBg;
            }

            // 恢复选中和滚动
            _codeEditor.Select(selStart, selLen);
            _codeEditor.ScrollToCaret();
            _codeEditor.ResumeLayout();
        }

        private void HighlightPattern(Regex regex, Color color)
        {
            foreach (Match m in regex.Matches(_codeEditor.Text))
            {
                _codeEditor.Select(m.Index, m.Length);
                _codeEditor.SelectionColor = color;
            }
        }

        private void UpdateLineNumbers()
        {
            if (_lineNumPanel == null || IsDisposed) return;

            _lineNumPanel.SuspendLayout();
            _lineNumPanel.Controls.Clear();

            int lineCount = _codeEditor.Lines.Length;
            int firstVisibleLine = _codeEditor.GetLineFromCharIndex(
                _codeEditor.GetCharIndexFromPosition(new Point(0, 0)));
            int fontSize = _codeEditor.Font.Height;
            int visibleCount = _codeEditor.Height / (fontSize + 2) + 1;
            int lastLine = Math.Min(firstVisibleLine + visibleCount, lineCount - 1);

            for (int i = firstVisibleLine; i <= lastLine; i++)
            {
                int lineNum = i + 1;
                int y = 4 + (i - firstVisibleLine) * (fontSize + 2);
                bool isBreakpoint = _breakpoints.Contains(lineNum);
                bool isCurrentLine = (lineNum - 1) == _currentHighlightLine;

                var lbl = new Label
                {
                    Text = isBreakpoint ? "●" + lineNum : lineNum.ToString(),
                    Location = new Point(0, y),
                    Size = new Size(40, fontSize + 1),
                    Font = new Font("Consolas", isBreakpoint ? 10f : 9f,
                        isBreakpoint ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = isCurrentLine ? Color.FromArgb(255, 200, 50)
                                : isBreakpoint ? BreakpointRed : LineNumFg,
                    BackColor = isCurrentLine ? Color.FromArgb(55, 50, 25)
                                : isBreakpoint ? BreakpointBg : Color.Transparent,
                    TextAlign = ContentAlignment.MiddleRight,
                    Cursor = Cursors.Hand,
                    Tag = lineNum  // 存储行号
                };
                lbl.Click += (s, ev) =>
                {
                    int ln = (int)((Label)s).Tag;
                    ToggleBreakpoint(ln);
                    UpdateLineNumbers();
                };
                _lineNumPanel.Controls.Add(lbl);
            }

            _lineNumPanel.ResumeLayout();
        }

        private void ToggleBreakpoint(int line)
        {
            if (_breakpoints.Contains(line))
            {
                _breakpoints.Remove(line);
                AppendOutput($"--- 断点已移除: 行 {line} ---");
            }
            else
            {
                _breakpoints.Add(line);
                AppendOutput($"--- 断点已设置: 行 {line} ---");
            }
            // 运行时同步断点
            if (_engine.IsRunning || _engine.IsPaused)
                _engine.Breakpoints = new HashSet<int>(_breakpoints);
        }

        /// <summary>高亮当前执行行（line 为 MoonSharp SourceRef 1-based 行号）</summary>
        private void HighlightCurrentLine(int line)
        {
            if (line <= 0)
            {
                ClearLineHighlight();
                return;
            }

            // MoonSharp SourceRef.FromLine 是 1-based → 转为 0-based 内部存储
            int zeroBasedLine = line - 1;
            _currentHighlightLine = zeroBasedLine;

            // 滚动到可见区域
            if (zeroBasedLine >= 0 && zeroBasedLine < _codeEditor.Lines.Length)
            {
                int firstVisible = _codeEditor.GetLineFromCharIndex(
                    _codeEditor.GetCharIndexFromPosition(new Point(0, 0)));
                int visibleH = _codeEditor.Height / (_codeEditor.Font.Height + 2);

                if (zeroBasedLine < firstVisible || zeroBasedLine > firstVisible + visibleH)
                {
                    _codeEditor.SelectionStart = _codeEditor.GetFirstCharIndexFromLine(zeroBasedLine);
                    _codeEditor.ScrollToCaret();
                }
            }

            // 刷新行号和语法高亮（包含当前行背景）
            _lastHighlightText = null; // 强制重新高亮
            UpdateLineNumbers();
            ApplySyntaxHighlighting();
        }

        private void ClearLineHighlight()
        {
            _currentHighlightLine = -1;
            _lastHighlightText = null;
            UpdateLineNumbers();
            ApplySyntaxHighlighting();
            _watchPanel.Visible = false;
        }
    }
}
