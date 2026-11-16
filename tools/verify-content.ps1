# ZeroWalker 内容表校验：字段齐全、类型正确、数值在合理区间。
# 用法： powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\verify-content.ps1
# 通过标准：所有行都是 PASS，退出码 0。

$ErrorActionPreference = 'Stop'
$script:failCount = 0

function Pass($message) {
    Write-Host ("PASS  " + $message)
}

function Fail($message) {
    Write-Host ("FAIL  " + $message) -ForegroundColor Red
    $script:failCount++
}

function ToNumber($value) {
    if ($value -is [bool]) { return $null }
    if ($value -is [string]) { return $null }
    try { return [double]$value } catch { return $null }
}

function Check-Range($label, $value, $min, $max) {
    $number = ToNumber $value
    if ($null -eq $number) {
        Fail ($label + " 不是数字：" + $value)
        return
    }
    if ($number -lt $min -or $number -gt $max) {
        Fail ($label + " = " + $number + " 超出范围 [" + $min + ", " + $max + "]")
        return
    }
    Pass ($label + " = " + $number)
}

function Check-Text($label, $value) {
    if ([string]::IsNullOrWhiteSpace([string]$value)) {
        Fail ($label + " 不能为空")
        return
    }
    Pass ($label + " = " + $value)
}

function Check-Bool($label, $value) {
    if ($value -is [bool]) {
        Pass ($label + " = " + $value)
        return
    }
    Fail ($label + " 应为 true/false，实际：" + $value)
}

function Check-Color($label, $color) {
    if ($null -eq $color) {
        Fail ($label + " 缺失")
        return
    }
    foreach ($axis in @('r', 'g', 'b', 'a')) {
        if ($null -eq $color.PSObject.Properties[$axis]) {
            Fail ($label + "." + $axis + " 缺失")
            return
        }
        Check-Range ($label + "." + $axis) $color.$axis 0 1
    }
}

function Check-Flash($label, $flash) {
    if ($null -eq $flash) {
        Fail ($label + " 缺失")
        return
    }
    Check-Range ($label + ".duration") $flash.duration 0.001 5
    Check-Range ($label + ".startScale") $flash.startScale 0.01 20
    Check-Range ($label + ".endScale") $flash.endScale 0.01 20
}

function Check-FieldSet($label, $object, $fields) {
    if ($null -eq $object) {
        Fail ($label + " 缺失")
        return
    }
    $actual = @($object.PSObject.Properties.Name)
    $bad = @()
    foreach ($field in $fields) {
        if ($actual -notcontains $field) { $bad += ("缺少 " + $field) }
    }
    foreach ($field in $actual) {
        if ($fields -notcontains $field) { $bad += ("多出 " + $field) }
    }
    if ($bad.Count -eq 0) {
        Pass ($label + " 字段齐全")
    }
    else {
        Fail ($label + "：" + ($bad -join '，'))
    }
}

$root = Split-Path -Parent $PSScriptRoot
$dataDir = Join-Path $root 'Assets\Resources\Content\data'

$schema = @{
    'player.json'    = @('moveSpeed', 'moveSpeedY', 'dashDistance', 'dashDuration', 'dashCooldown', 'moveDeadzone')
    'input.json'     = @('moveLeft', 'moveRight', 'moveUp', 'moveDown', 'dash', 'melee', 'attack')
    'arena.json'     = @('minY', 'maxY')
'enemies.json'   = @('hitColor', 'hitFlashDuration', 'deathDelay', 'melee', 'ranged')
    'bullet.json'    = @('speed', 'lifeTime', 'damage', 'normalColor', 'specialColor', 'specialScale')
    'target.json'    = @('maxHealth', 'hitFlashDuration', 'hitColor', 'respawn', 'respawnDelay', 'deathScale')
    'camera.json'    = @('offset', 'smoothTime', 'lockY')
    'effects.json'   = @('muzzleFlash', 'hitFlash', 'hitEffectTint', 'hitEffectDestroyDelay')
    'animation.json' = @('defaultMix')
    'muzzle.json'    = @('fallbackOffset')
    'assets.json'    = @('spine')
}

Write-Host ("内容目录：" + $dataDir)
Write-Host ''

# ---- 1. 文件存在、能解析、字段集合与 schema 一致（缺字段/多字段都会失败）----
$content = @{}
foreach ($fileName in $schema.Keys) {
    $path = Join-Path $dataDir $fileName
    if (-not (Test-Path -LiteralPath $path)) {
        Fail ($fileName + " 不存在")
        continue
    }

    try {
        $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    }
    catch {
        Fail ($fileName + " 不是合法 JSON：" + $_.Exception.Message)
        continue
    }

    $content[$fileName] = $json
    $expected = $schema[$fileName]
    $actual = @($json.PSObject.Properties.Name)

    foreach ($key in $expected) {
        if ($actual -notcontains $key) {
            Fail ($fileName + " 缺少字段 " + $key)
        }
    }
    foreach ($key in $actual) {
        if ($expected -notcontains $key) {
            Fail ($fileName + " 有多余字段 " + $key + "（拼写错误？）")
        }
    }
    Pass ($fileName + " 字段集合正确")
}

Write-Host ''

# ---- 2. 逐项数值检查 ----
if ($content.ContainsKey('player.json')) {
    $p = $content['player.json']
    Check-Range 'player.moveSpeed' $p.moveSpeed 0.1 50
    Check-Range 'player.moveSpeedY' $p.moveSpeedY 0.1 50
    Check-Range 'player.dashDistance' $p.dashDistance 0.1 50
    Check-Range 'player.dashDuration' $p.dashDuration 0.01 2
    Check-Range 'player.dashCooldown' $p.dashCooldown 0 10
    Check-Range 'player.moveDeadzone' $p.moveDeadzone 0 0.99
}

if ($content.ContainsKey('input.json')) {
    $i = $content['input.json']
    Check-Text 'input.moveLeft' $i.moveLeft
    Check-Text 'input.moveRight' $i.moveRight
    Check-Text 'input.moveUp' $i.moveUp
    Check-Text 'input.moveDown' $i.moveDown
    Check-Text 'input.dash' $i.dash
    Check-Text 'input.melee' $i.melee
    Check-Text 'input.attack' $i.attack
}

if ($content.ContainsKey('arena.json')) {
    $a = $content['arena.json']
    Check-Range 'arena.minY' $a.minY 0 1
    Check-Range 'arena.maxY' $a.maxY 0 1
    if ([double]$a.minY -lt [double]$a.maxY) {
        Pass 'arena 纵向范围有效（minY < maxY）'
    }
    else {
        Fail 'arena 纵向范围无效：需要 minY < maxY'
    }
}

if ($content.ContainsKey('enemies.json')) {
    $e = $content['enemies.json']
    Check-Range 'enemies.deathDelay' $e.deathDelay 0 5
    Check-Range 'enemies.hitFlashDuration' $e.hitFlashDuration 0 5
    Check-Color 'enemies.hitColor' $e.hitColor

    $meleeFields = @('maxHealth', 'patrolSpeed', 'chaseSpeed', 'patrolRange', 'detectRadius', 'loseRadius', 'attackRange', 'hitRadius', 'windupTime', 'lungeSpeed', 'lungeTime', 'cooldown')
    Check-FieldSet 'enemies.melee' $e.melee $meleeFields
    $m = $e.melee
    Check-Range 'enemies.melee.maxHealth' $m.maxHealth 1 1000
    Check-Range 'enemies.melee.patrolSpeed' $m.patrolSpeed 0.1 50
    Check-Range 'enemies.melee.chaseSpeed' $m.chaseSpeed 0.1 50
    Check-Range 'enemies.melee.patrolRange' $m.patrolRange 0.1 50
    Check-Range 'enemies.melee.detectRadius' $m.detectRadius 0.1 50
    Check-Range 'enemies.melee.loseRadius' $m.loseRadius 0.1 50
    Check-Range 'enemies.melee.attackRange' $m.attackRange 0.1 50
    Check-Range 'enemies.melee.hitRadius' $m.hitRadius 0.1 50
    Check-Range 'enemies.melee.windupTime' $m.windupTime 0 5
    Check-Range 'enemies.melee.lungeSpeed' $m.lungeSpeed 0.1 50
    Check-Range 'enemies.melee.lungeTime' $m.lungeTime 0.01 5
    Check-Range 'enemies.melee.cooldown' $m.cooldown 0 10
    if ([double]$m.loseRadius -ge [double]$m.detectRadius) {
        Pass 'enemies.melee 追丢半径不小于发现半径'
    }
    else {
        Fail 'enemies.melee.loseRadius 不能小于 detectRadius'
    }
    if ([double]$m.hitRadius -ge [double]$m.attackRange) {
        Pass 'enemies.melee 命中半径不小于攻击距离'
    }
    else {
        Fail 'enemies.melee.hitRadius 不能小于 attackRange'
    }

    $rangedFields = @('maxHealth', 'detectRadius', 'aimTime', 'fireCooldown', 'bulletSpeed')
    Check-FieldSet 'enemies.ranged' $e.ranged $rangedFields
    $r = $e.ranged
    Check-Range 'enemies.ranged.maxHealth' $r.maxHealth 1 1000
    Check-Range 'enemies.ranged.detectRadius' $r.detectRadius 0.1 50
    Check-Range 'enemies.ranged.aimTime' $r.aimTime 0 5
    Check-Range 'enemies.ranged.fireCooldown' $r.fireCooldown 0 10
    Check-Range 'enemies.ranged.bulletSpeed' $r.bulletSpeed 0.1 100
}

if ($content.ContainsKey('bullet.json')) {
    $b = $content['bullet.json']
    Check-Range 'bullet.speed' $b.speed 0.1 100
    Check-Range 'bullet.lifeTime' $b.lifeTime 0.01 20
    Check-Range 'bullet.damage' $b.damage 1 100
    Check-Range 'bullet.specialScale' $b.specialScale 0.1 10
    Check-Color 'bullet.normalColor' $b.normalColor
    Check-Color 'bullet.specialColor' $b.specialColor
}

if ($content.ContainsKey('target.json')) {
    $t = $content['target.json']
    Check-Range 'target.maxHealth' $t.maxHealth 1 1000
    Check-Range 'target.hitFlashDuration' $t.hitFlashDuration 0 5
    Check-Range 'target.respawnDelay' $t.respawnDelay 0 60
    Check-Range 'target.deathScale' $t.deathScale 0.01 10
    Check-Bool 'target.respawn' $t.respawn
    Check-Color 'target.hitColor' $t.hitColor
}

if ($content.ContainsKey('camera.json')) {
    $c = $content['camera.json']
    Check-Range 'camera.smoothTime' $c.smoothTime 0 5
    Check-Bool 'camera.lockY' $c.lockY
    if ($null -ne $c.offset) {
        Check-Range 'camera.offset.x' $c.offset.x -100 100
        Check-Range 'camera.offset.y' $c.offset.y -100 100
        Check-Range 'camera.offset.z' $c.offset.z -100 -0.01
    }
    else {
        Fail 'camera.offset 缺失'
    }
}

if ($content.ContainsKey('effects.json')) {
    $e = $content['effects.json']
    Check-Flash 'effects.muzzleFlash' $e.muzzleFlash
    Check-Flash 'effects.hitFlash' $e.hitFlash
    Check-Range 'effects.hitEffectDestroyDelay' $e.hitEffectDestroyDelay 0 5
    Check-Color 'effects.hitEffectTint' $e.hitEffectTint
}

if ($content.ContainsKey('animation.json')) {
    $a = $content['animation.json']
    Check-Range 'animation.defaultMix' $a.defaultMix 0 2
}

if ($content.ContainsKey('muzzle.json')) {
    $m = $content['muzzle.json']
    if ($null -ne $m.fallbackOffset) {
        Check-Range 'muzzle.fallbackOffset.x' $m.fallbackOffset.x -10 10
        Check-Range 'muzzle.fallbackOffset.y' $m.fallbackOffset.y -10 10
        Check-Range 'muzzle.fallbackOffset.z' $m.fallbackOffset.z -10 10
    }
    else {
        Fail 'muzzle.fallbackOffset 缺失'
    }
}

# ---- 资源 key 登记表：动画名/骨骼名必须齐全且非空 ----
if ($content.ContainsKey('assets.json')) {
    $assetTuning = $content['assets.json']
    if ($null -eq $assetTuning.spine) {
        Fail 'assets.spine 缺失'
    }
    else {
        Check-Text 'assets.spine.muzzleBone' $assetTuning.spine.muzzleBone

        $animations = $assetTuning.spine.animations
        if ($null -eq $animations) {
            Fail 'assets.spine.animations 缺失'
        }
        else {
            $expectedAnimations = @('idle', 'move', 'attackBegin', 'attackLoop', 'attackEnd', 'attackEvent', 'die', 'start', 'skill01', 'skill02')
            $actualAnimations = @($animations.PSObject.Properties.Name)
            $animationSchemaOk = $true

            foreach ($key in $expectedAnimations) {
                if ($actualAnimations -notcontains $key) {
                    Fail ('assets.spine.animations 缺少字段 ' + $key)
                    $animationSchemaOk = $false
                }
            }
            foreach ($key in $actualAnimations) {
                if ($expectedAnimations -notcontains $key) {
                    Fail ('assets.spine.animations 有多余字段 ' + $key + '（拼写错误？）')
                    $animationSchemaOk = $false
                }
            }
            if ($animationSchemaOk) {
                Pass 'assets.spine.animations 字段集合正确'
            }

            foreach ($key in $expectedAnimations) {
                if ($actualAnimations -contains $key) {
                    Check-Text ('assets.spine.animations.' + $key) $animations.$key
                }
            }
        }
    }
}

# ---- 3. 文案表 ----
$csvPath = Join-Path $root 'Assets\Resources\Content\i18n\ui.csv'
if (-not (Test-Path -LiteralPath $csvPath)) {
    Fail 'i18n/ui.csv 不存在'
}
else {
    $header = (Get-Content -LiteralPath $csvPath -Encoding UTF8 -TotalCount 1)
    if ($header -ne 'key,zh,en') {
        Fail ('i18n/ui.csv 表头应为 key,zh,en，实际：' + $header)
    }
    else {
        Pass 'i18n/ui.csv 表头正确'
    }
}

Write-Host ''
if ($script:failCount -gt 0) {
    Write-Host ("结果：FAIL（" + $script:failCount + " 项不通过）") -ForegroundColor Red
    exit 1
}

Write-Host '结果：PASS（内容表全部通过）' -ForegroundColor Green
exit 0
