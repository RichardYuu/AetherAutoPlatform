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
        //  变量监视面板绘制
        // ============================================================

        private void OnDrawWatchItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _watchList.Items.Count) return;

            e.DrawBackground();
            var item = _watchList.Items[e.Index]?.ToString() ?? "";
            int eqPos = item.IndexOf('=');
            if (eqPos > 0)
            {
                // 变量名
                var nameRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y, eqPos * 7 - 4, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, item.Substring(0, eqPos).Trim(),
                    _watchList.Font, nameRect, KwBlue,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // 值
                var valRect = new Rectangle(e.Bounds.X + eqPos * 7, e.Bounds.Y,
                    e.Bounds.Width - eqPos * 7, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, item.Substring(eqPos + 1).Trim(),
                    _watchList.Font, valRect, StrOrange,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
            else
            {
                e.Graphics.DrawString(item, _watchList.Font,
                    new SolidBrush(Color.FromArgb(200, 200, 200)),
                    new PointF(e.Bounds.X + 4, e.Bounds.Y + 2));
            }
        }

        private void UpdateWatchPanel()
        {
            try
            {
                var watches = _engine.LocalWatches;
                _watchList.BeginUpdate();
                _watchList.Items.Clear();

                if (watches != null && watches.Count > 0)
                {
                    foreach (var w in watches)
                    {
                        string display = string.Format("{0} = {1}",
                            w.Name ?? "?", FormatWatchValue(w.Value));
                        _watchList.Items.Add(display);
                    }
                    _watchPanel.Visible = true;
                }
                else
                {
                    _watchList.Items.Add("(无局部变量)");
                    _watchPanel.Visible = true;
                }
                _watchList.EndUpdate();
            }
            catch
            {
                _watchPanel.Visible = false;
            }
        }

        private static string FormatWatchValue(DynValue v)
        {
            if (v == null) return "nil";
            switch (v.Type)
            {
                case DataType.String: return "\"" + v.String + "\"";
                case DataType.Number: return v.Number.ToString("F3");
                case DataType.Boolean: return v.Boolean ? "true" : "false";
                case DataType.Nil: return "nil";
                case DataType.Table: return "{...}";
                case DataType.Function: return "function";
                default: return v.ToPrintString();
            }
        }
    }
}
