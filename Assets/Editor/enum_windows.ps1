Add-Type @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public class WindowEnum {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
"@

$list = New-Object System.Collections.Generic.List[string]
[WindowEnum]::EnumWindows({
    param($hWnd, $lParam)
    $sb = New-Object System.Text.StringBuilder 256
    [WindowEnum]::GetWindowText($hWnd, $sb, 256) | Out-Null
    $title = $sb.ToString()
    $pid = 0
    [WindowEnum]::GetWindowThreadProcessId($hWnd, [ref]$pid) | Out-Null
    $vis = [WindowEnum]::IsWindowVisible($hWnd)
    if ($title -match "Unity" -or $title -match "MiniShooterGame") {
        Write-Output "Found Window: HWnd=$hWnd, PID=$pid, Visible=$vis, Title='$title'"
        [WindowEnum]::ShowWindow($hWnd, 9)
        [WindowEnum]::SetForegroundWindow($hWnd)
    }
    return $true
}, [IntPtr]::Zero) | Out-Null
