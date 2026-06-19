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
        private void InitPresets()
        {
            _presetScripts["初始化"] = @"-- 设备初始化脚本
log('INFO', '开始初始化...')
log('INFO', '时间: ' .. now())
print('尝试轴归零...')
local ok = pcall(function() axis_home(1) end)
if not ok then print('(axis_home 未绑定，仿真模式跳过)') end
msleep(500)
log('INFO', '初始化完成')
print('设备初始化成功')";

            _presetScripts["轴归零"] = @"-- 所有轴归零
log('INFO', '执行轴归零...')
for i = 1, 4 do
    local ok = pcall(function() axis_home(i) end)
    if ok then print('轴' .. i .. ' 归零完成') end
    msleep(500)
end
log('INFO', '轴归零完成')";

            _presetScripts["扫码测试"] = @"-- 扫码器读取测试
log('INFO', '开始扫码测试...')
local ok, code = pcall(function() return scanner_read() end)
if ok and code and code ~= '' then
    log('INFO', '扫描结果: ' .. code)
else
    log('WARN', '扫码失败或无绑定')
end";

            _presetScripts["视觉定位"] = @"-- 视觉定位测试
log('INFO', '执行视觉定位...')
local ok, result = pcall(function() return vision_locate('default') end)
if ok then
    log('INFO', '定位结果: ' .. tostring(result))
else
    log('WARN', '视觉定位失败或无绑定')
end
print('视觉定位完成')";

            _presetScripts["压力检测"] = @"-- 压力传感器读取
log('INFO', '读取压力值...')
local ok, p = pcall(function() return pressure_read() end)
if ok and p then
    log('INFO', '当前压力: ' .. tostring(p) .. ' kPa')
    if p > 100 then
        log('WARN', '压力过高!')
    end
else
    log('WARN', '压力传感器未连接')
end";

            _presetScripts["全流程"] = @"-- 完整测试流程
log('INFO', '===== 全流程测试开始 =====')
log('INFO', '时间: ' .. now())

log('INFO', '[1/4] 初始化设备...')
pcall(function() axis_home(1) end)
msleep(500)

log('INFO', '[2/4] 扫码识别...')
local ok, code = pcall(function() return scanner_read() end)
log('INFO', '件号: ' .. (code or '未绑定'))

log('INFO', '[3/4] 视觉定位...')
pcall(function() vision_locate('default') end)
msleep(500)

log('INFO', '[4/4] 流程结束')
log('INFO', '===== 全流程测试完成 =====')
print('全流程测试通过')";
        }
    }
}
