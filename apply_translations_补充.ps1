# 补充替换脚本 - 处理遗漏的翻译

Write-Host "开始补充替换遗漏的翻译key..." -ForegroundColor Green

# 补充的替换映射
$replacements = @{
    # 区域删除确认（需要特殊处理，因为包含变量）
    '确定要删除区域 \\"{0}\\" 吗？' = 'string.Format("RTE_Area_ConfirmDelete".Translate(), area.Label)'
    '$"确定要删除区域 \\"{area.Label}\\" 吗？"' = '"RTE_Area_ConfirmDelete".Translate(area.Label)'
    
    # 区域统计信息中的文本
    '$"格子数: {area.CellCount}  \|  状态: {(area.IsActive \? "启用" : "禁用")}"' = 'string.Format("{0}: {1}  |  {2}: {3}", "RTE_Area_CellCount".Translate(), area.CellCount, "状态", area.IsActive ? "RTE_Area_Status_Active".Translate() : "RTE_Area_Status_Inactive".Translate())'
    
    # PromptEditorDialog标题
    '"自定义概况总结提示词"' = '"RTE_PromptEditor_CustomPrompt".Translate("RTE_PromptEditor_OverviewSummary".Translate())'
    '"自定义每日快照提示词"' = '"RTE_PromptEditor_CustomPrompt".Translate("RTE_PromptEditor_DailySynthesis".Translate())'
    '"工程AI总结提示词"' = '"RTE_PromptEditor_CustomPrompt".Translate("RTE_PromptEditor_ProjectSummary".Translate())'
}

$files = @(
    "Source/UI/MainTabWindow_Announcement.cs",
    "Source/UI/TaskEditorDialog.cs",
    "Source/UI/AreaEditorDialog.cs"
)

$totalReplacements = 0

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "`n处理文件: $file" -ForegroundColor Cyan
        $content = Get-Content $file -Raw -Encoding UTF8
        $fileReplacements = 0
        
        foreach ($key in $replacements.Keys) {
            $value = $replacements[$key]
            if ($content -match [regex]::Escape($key)) {
                $content = $content -replace [regex]::Escape($key), $value
                $fileReplacements++
                $totalReplacements++
                Write-Host "  替换: $key" -ForegroundColor Yellow
            }
        }
        
        if ($fileReplacements -gt 0) {
            Set-Content $file -Value $content -Encoding UTF8 -NoNewline
            Write-Host "  ✓ 替换了 $fileReplacements 处文本" -ForegroundColor Green
        } else {
            Write-Host "  - 无需替换" -ForegroundColor Gray
        }
    }
}

Write-Host "`n完成！补充替换了 $totalReplacements 处文本" -ForegroundColor Green
