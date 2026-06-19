using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Aether.Platform.App;
using Aether.Platform.Core.Interfaces;
using Aether.Platform.Core.Models;

namespace Aether.Platform.UI.Modules
{
    public class LoginView : UserControl, IModuleView
    {
        public string ModuleName => "Login";

        private ComboBox _cmbUsername;
        private TextBox _txtPassword;
        private Label _lblMessage;
        private Label _lblRole;
        private CheckBox _chkRemember;
        private Button _loginBtn;
        private Panel _loggedInPanel;
        private Panel _loginFormPanel;
        private readonly List<string> _recentUsers = new List<string> { "admin", "operator1", "engineer1" };

        public LoginView()
        {
            BackColor = Color.FromArgb(240, 242, 245);
            BuildLayout();
        }

        private void BuildLayout()
        {
            // 整个页面居中容器
            var outerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245)
            };
            outerPanel.Resize += (s, e) =>
            {
                if (outerPanel.Controls.Count == 0) return;
                var loginCard = outerPanel.Controls[0] as Panel;
                if (loginCard == null) return;
                loginCard.Left = (outerPanel.Width - loginCard.Width) / 2;
                loginCard.Top = Math.Max(40, (outerPanel.Height - loginCard.Height) / 3);
            };
            Controls.Add(outerPanel);

            // 登录卡片
            var cardWidth = 460;
            var cardHeight = 440;
            var card = new Panel
            {
                Size = new Size(cardWidth, cardHeight),
                Location = new Point(80, 40),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(20)
            };

            // === 头部 Logo 区 ===
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.Transparent
            };
            headerPanel.Paint += OnHeaderPaint;
            card.Controls.Add(headerPanel);

            // 标题
            var title = new Label
            {
                Text = "用户登录",
                Location = new Point(100, 48),
                Size = new Size(260, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei", 18f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 70, 140),
                BackColor = Color.Transparent
            };
            headerPanel.Controls.Add(title);

            // === 登录表单面板 ===
            _loginFormPanel = new Panel
            {
                Location = new Point(40, 110),
                Size = new Size(cardWidth - 80, 250),
                BackColor = Color.Transparent
            };
            card.Controls.Add(_loginFormPanel);

            int y = 6;

            // 用户名
            _loginFormPanel.Controls.Add(MakeLabel("用户名:", y));
            _cmbUsername = new ComboBox
            {
                Location = new Point(0, y + 22),
                Size = new Size(_loginFormPanel.Width, 30),
                Font = new Font("Microsoft YaHei", 10.5f),
                FlatStyle = FlatStyle.Flat,
                Text = ""
            };
            _cmbUsername.Items.AddRange(_recentUsers.ToArray());
            _cmbUsername.SelectedIndexChanged += OnUserSelected;
            _cmbUsername.KeyDown += OnEnterKeyLogin;
            _loginFormPanel.Controls.Add(_cmbUsername);
            y += 60;

            // 密码
            _loginFormPanel.Controls.Add(MakeLabel("密码:", y));
            _txtPassword = new TextBox
            {
                Location = new Point(0, y + 22),
                Size = new Size(_loginFormPanel.Width, 30),
                Font = new Font("Microsoft YaHei", 10.5f),
                UseSystemPasswordChar = true
            };
            _txtPassword.KeyDown += OnEnterKeyLogin;
            _loginFormPanel.Controls.Add(_txtPassword);
            y += 60;

            // 记住密码
            _chkRemember = new CheckBox
            {
                Text = "记住密码",
                Location = new Point(0, y),
                Size = new Size(100, 24),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(100, 100, 110),
                BackColor = Color.Transparent
            };
            _loginFormPanel.Controls.Add(_chkRemember);
            y += 36;

            // 登录按钮
            _loginBtn = new Button
            {
                Text = "登   录",
                Location = new Point(0, y),
                Size = new Size(_loginFormPanel.Width, 40),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 12f, FontStyle.Bold)
            };
            _loginBtn.FlatAppearance.BorderSize = 0;
            _loginBtn.Click += OnLoginClick;
            _loginFormPanel.Controls.Add(_loginBtn);

            y += 52;

            // 消息标签
            _lblMessage = new Label
            {
                Location = new Point(0, y),
                Size = new Size(_loginFormPanel.Width, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Red,
                Font = new Font("Microsoft YaHei", 9f),
                BackColor = Color.Transparent
            };
            _loginFormPanel.Controls.Add(_lblMessage);

            // === 已登录面板（初始隐藏）===
            _loggedInPanel = new Panel
            {
                Location = new Point(40, 110),
                Size = new Size(cardWidth - 80, 250),
                Visible = false,
                BackColor = Color.Transparent
            };

            var loggedIcon = new Label
            {
                Text = "✔",
                Size = new Size(60, 60),
                Location = new Point((_loggedInPanel.Width - 60) / 2, 10),
                Font = new Font("Microsoft YaHei", 36f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 180, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            _loggedInPanel.Controls.Add(loggedIcon);

            var loggedUser = new Label
            {
                Text = "",
                Size = new Size(_loggedInPanel.Width, 30),
                Location = new Point(0, 78),
                Font = new Font("Microsoft YaHei", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _loggedInPanel.Controls.Add(loggedUser);

            _lblRole = new Label
            {
                Text = "",
                Size = new Size(_loggedInPanel.Width, 24),
                Location = new Point(0, 110),
                Font = new Font("Microsoft YaHei", 10f),
                ForeColor = Color.FromArgb(120, 150, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _loggedInPanel.Controls.Add(_lblRole);

            var logoutBtn = new Button
            {
                Text = "退出登录",
                Size = new Size(200, 40),
                Location = new Point((_loggedInPanel.Width - 200) / 2, 150),
                BackColor = Color.FromArgb(200, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei", 10f, FontStyle.Bold)
            };
            logoutBtn.FlatAppearance.BorderSize = 0;
            logoutBtn.Click += (s, e) => Logout();
            _loggedInPanel.Controls.Add(logoutBtn);

            card.Controls.Add(_loggedInPanel);

            // 底部版权
            var footer = new Label
            {
                Text = "© 2025 Aether 版本 v2.1.0",
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(160, 160, 170),
                Font = new Font("Microsoft YaHei", 8f),
                BackColor = Color.Transparent
            };
            card.Controls.Add(footer);

            outerPanel.Controls.Add(card);
        }

        private void OnHeaderPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rect = ((Panel)sender).ClientRectangle;

            // 人头圆圈
            int cx = rect.Width / 2;
            int cy = 22;
            int r = 16;

            // 头
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = new Pen(Color.FromArgb(0, 100, 180), 2))
                g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            using (var brush = new SolidBrush(Color.FromArgb(220, 235, 250)))
                g.FillEllipse(brush, cx - r + 2, cy - r + 2, r * 2 - 4, r * 2 - 4);

            // 肩膀
            using (var pen = new Pen(Color.FromArgb(0, 100, 180), 2))
                g.DrawArc(pen, cx - 22, cy + r - 4, 44, 24, 190, 160);
        }

        private Label MakeLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(2, y),
                Size = new Size(80, 20),
                Font = new Font("Microsoft YaHei", 9f),
                ForeColor = Color.FromArgb(80, 80, 100),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent
            };
        }

        private void OnUserSelected(object sender, EventArgs e)
        {
            if (_cmbUsername.SelectedItem != null)
                _txtPassword.Focus();
        }

        private void OnEnterKeyLogin(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                PerformLogin();
        }

        private void OnLoginClick(object sender, EventArgs e)
        {
            PerformLogin();
        }

        private void PerformLogin()
        {
            var username = _cmbUsername.Text.Trim();
            var password = _txtPassword.Text;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("请输入用户名");
                _cmbUsername.Focus();
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowError("请输入密码");
                _txtPassword.Focus();
                return;
            }

            var boot = AppBootstrap.Instance;
            if (boot != null && boot.IsInitialized && boot.AuthService != null)
            {
                // 真实认证（异步后台执行）
                _loginBtn.Enabled = false;
                ShowMessage("正在认证...", Color.FromArgb(0, 120, 200));

                var user = username;
                var pwd = password;
                Task.Run(async () =>
                {
                    try
                    {
                        var result = await boot.AuthService.LoginAsync(user, pwd);
                        BeginInvoke((Action)(() =>
                        {
                            _loginBtn.Enabled = true;
                            if (result.Success)
                            {
                                LoginSuccess(result.UserName ?? user, MapRole(result.Role));
                            }
                            else
                            {
                                ShowError("认证失败: 用户名或密码错误");
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        BeginInvoke((Action)(() =>
                        {
                            _loginBtn.Enabled = true;
                            ShowError("认证异常: " + ex.Message);
                        }));
                    }
                });
            }
            else
            {
                // 未初始化，使用模拟认证
                if (SimulateAuth(username, password, out string role, out string error))
                {
                    LoginSuccess(username, role);
                }
                else
                {
                    ShowError(error);
                }
            }
        }

        private static string MapRole(UserRole role)
        {
            switch (role)
            {
                case UserRole.Administrator: return "管理员";
                case UserRole.Engineer: return "工艺工程师";
                case UserRole.Maintainer: return "维保工程师";
                case UserRole.Operator: return "操作员";
                default: return "用户";
            }
        }

        private bool SimulateAuth(string username, string password, out string role, out string error)
        {
            // 演示用认证逻辑
            var userRoles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin", "管理员" },
                { "operator1", "操作员" },
                { "engineer1", "工艺工程师" },
                { "maintainer", "维保工程师" }
            };
            var userPwds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin", "admin123" },
                { "operator1", "op123" },
                { "engineer1", "eng123" },
                { "maintainer", "mt123" }
            };

            if (userPwds.TryGetValue(username, out string expectedPwd))
            {
                if (expectedPwd == password)
                {
                    userRoles.TryGetValue(username, out string r);
                    role = r ?? "用户";
                    error = null;
                    return true;
                }
                role = null;
                error = "密码错误，请重试";
                return false;
            }

            role = null;
            error = "用户名不存在";
            return false;
        }

        private void LoginSuccess(string username, string role)
        {
            // 更新最近用户列表
            _recentUsers.Remove(username);
            _recentUsers.Insert(0, username);
            while (_recentUsers.Count > 5) _recentUsers.RemoveAt(_recentUsers.Count - 1);

            _cmbUsername.Items.Clear();
            _cmbUsername.Items.AddRange(_recentUsers.ToArray());

            _lblMessage.Text = "";
            _loginFormPanel.Visible = false;
            _loggedInPanel.Visible = true;

            var userLabel = _loggedInPanel.Controls[1] as Label;
            if (userLabel != null) userLabel.Text = username;

            _lblRole.Text = $"角色: {role}    登录时间: {DateTime.Now:HH:mm:ss}";
            _lblRole.ForeColor = Color.FromArgb(0, 140, 60);

            // 通知 AppBootstrap
            try
            {
                var boot = Control.FromHandle(Handle)?.FindForm()?.Tag;
                // 登录成功事件通过 AppBootstrap.OnSystemLog 回调
            }
            catch { }
        }

        private void Logout()
        {
            _loginFormPanel.Visible = true;
            _loggedInPanel.Visible = false;
            _txtPassword.Text = "";
            _lblMessage.Text = "";
            _cmbUsername.Focus();
        }

        private void ShowError(string msg)
        {
            _lblMessage.Text = msg;
            _lblMessage.ForeColor = Color.FromArgb(220, 50, 50);
        }

        private void ShowMessage(string msg, Color color)
        {
            _lblMessage.Text = msg;
            _lblMessage.ForeColor = color;
        }

        public void OnActivated()
        {
            _cmbUsername.Focus();
        }

        public void OnDeactivated() { }

        public void RefreshData()
        {
            _cmbUsername.Items.Clear();
            _cmbUsername.Items.AddRange(_recentUsers.ToArray());
        }
    }
}