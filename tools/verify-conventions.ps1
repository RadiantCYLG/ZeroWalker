# ZeroWalker 约定校验：分层边界与「数值不写进组件」。
# 用法： powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-conventions.ps1
# 通过标准：没有 FAIL 行，退出码 0。

$ErrorActionPreference = 'Stop'
$script:failCount = 0

function Pass($message) {
    Write-Host ("PASS  " + $message)
}

function Fail($message) {
    Write-Host ("FAIL  " + $message) -ForegroundColor Red
    $script:failCount++
}

$root = Split-Path -Parent $PSScriptRoot
$scriptsDir = Join-Path $root 'Assets\Scripts'
$allowListPath = Join-Path $PSScriptRoot 'serialized-number-allowlist.txt'

$allow = @{}
if (Test-Path -LiteralPath $allowListPath) {
    foreach ($line in Get-Content -LiteralPath $allowListPath -Encoding UTF8) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line.TrimStart().StartsWith('#')) { continue }
        $parts = $line -split '\|', 2
        $allow[$parts[0].Trim()] = $parts[1]
    }
}

function Relative($fullPath) {
    return $fullPath.Substring($root.Length + 1).Replace('\', '/')
}

# ---- 1. Core 层不得引用引擎 ----
$coreDir = Join-Path $scriptsDir 'Core'
$coreFiles = @(Get-ChildItem -LiteralPath $coreDir -Recurse -Filter '*.cs' -File)
if ($coreFiles.Count -eq 0) {
    Fail 'Core 目录下没有 .cs 文件'
}
foreach ($file in $coreFiles) {
    $rel = Relative $file.FullName
    $lineNo = 0
    $badLines = @()
    foreach ($line in [System.IO.File]::ReadAllLines($file.FullName, [System.Text.Encoding]::UTF8)) {
        $lineNo++
        $trimmed = $line.TrimStart()
        # 注释里提到 UnityEngine 不算违规（说明文字），只看真正的代码行
        if ($trimmed.StartsWith('//') -or $trimmed.StartsWith('/*') -or $trimmed.StartsWith('*')) { continue }
        if ($trimmed.Contains('UnityEngine')) { $badLines += $lineNo }
    }
    if ($badLines.Count -gt 0) {
        Fail ($rel + " 引用了 UnityEngine，行号 " + ($badLines -join ', '))
    }
}
Pass ("Core 层无引擎引用（" + $coreFiles.Count + " 个文件）")

$coreAsmdef = Join-Path $coreDir 'ZeroWalker.Core.asmdef'
if (-not (Test-Path -LiteralPath $coreAsmdef)) {
    Fail '缺少 Core 程序集定义 ZeroWalker.Core.asmdef'
}
else {
    $def = Get-Content -LiteralPath $coreAsmdef -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($def.noEngineReferences -eq $true) {
        Pass 'Core.asmdef 已开启 noEngineReferences'
    }
    else {
        Fail 'Core.asmdef 的 noEngineReferences 必须为 true'
    }
}

# ---- 2. Game 层不得把数值写成 SerializeField ----
$gameDir = Join-Path $scriptsDir 'Game'
$gameFiles = @(Get-ChildItem -LiteralPath $gameDir -Recurse -Filter '*.cs' -File)
$pattern = '\[SerializeField[^\]]*\]\s*private\s+(float|int|double|Vector2|Vector3|Color)\b'
$violations = 0
foreach ($file in $gameFiles) {
    $rel = Relative $file.FullName
    $hits = Select-String -LiteralPath $file.FullName -Pattern $pattern
    if (-not $hits) { continue }
    if ($allow.ContainsKey($rel)) { continue }

    foreach ($hit in $hits) {
        $violations++
        Fail ($rel + ":" + $hit.LineNumber + " 把数值写在组件上，应移入 Content/data/*.json：" + $hit.Line.Trim())
    }
}
if ($violations -eq 0) {
    Pass ("Game 层无数值 SerializeField（" + $gameFiles.Count + " 个文件，允许名单 " + $allow.Count + " 条）")
}

# ---- 3. 所有 .cs 必须是合法 UTF-8（GBK 文件会解码出替换字符）----
$allCs = @(Get-ChildItem -LiteralPath $scriptsDir -Recurse -Filter '*.cs' -File)
$badEncoding = 0
foreach ($file in $allCs) {
    $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    if ($text.Contains([string][char]0xFFFD)) {
        $badEncoding++
        Fail ((Relative $file.FullName) + " 不是 UTF-8 编码（出现替换字符）")
    }
}
if ($badEncoding -eq 0) {
    Pass ("全部 .cs 为合法 UTF-8（" + $allCs.Count + " 个文件）")
}

Write-Host ''
if ($script:failCount -gt 0) {
    Write-Host ("结果：FAIL（" + $script:failCount + " 项不通过）") -ForegroundColor Red
    exit 1
}

Write-Host '结果：PASS（分层与数值约定全部通过）' -ForegroundColor Green
exit 0
