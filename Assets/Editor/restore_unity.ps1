Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinUtil {
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@

$procs = Get-Process Unity
foreach ($p in $procs) {
    Write-Output "PID: $($p.Id), MainWindowHandle: $($p.MainWindowHandle)"
    if ($p.MainWindowHandle -ne 0) {
        [WinUtil]::ShowWindow($p.MainWindowHandle, 9) # 9 = SW_RESTORE
        [WinUtil]::SetForegroundWindow($p.MainWindowHandle)
        Write-Output "Restored and brought to foreground: $($p.Id)"
    }
}
