using Microsoft.Win32;

namespace HRM.GUI.Helpers;

/// <summary>
/// Định danh máy ổn định trên Windows (MachineGuid trong registry).
/// Dùng khi nhân viên chấm công qua phần mềm.
/// </summary>
public static class MachineHwidHelper
{
    public static string? GetMachineHwid()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var v = key?.GetValue("MachineGuid") as string;
            return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
        }
        catch
        {
            return null;
        }
    }
}
