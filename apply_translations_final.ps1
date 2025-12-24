# 最终完整替换脚本 - 处理所有剩余的UI文本

Write-Host "开始最终替换..." -ForegroundColor Green

$files = @(
    "Source/UI/PromptEditorDialog.cs",
    "Source/UI/AreaDrawingDesignator.cs",
    "Source/UI/TaskEditorDialog.cs",
    "Source/UI/AreaEditorDialog.cs",
    "Source/UI/MainTabWindow_Announcement.cs"
)

$totalReplacements = 0

foreach ($file in $files) {
    if (Test-Path $file) {
        Write-Host "`n处理文件: $file" -ForegroundColor Cyan
        $content = Get-Content $file -Raw -Encoding UTF8
        $originalContent = $content
        
        # PromptEditorDialog.cs 的替换
        if ($file -like "*PromptEditorDialog.cs") {
            $content = $content -replace '"提示：留空将使用默认提示词。支持多行文本。\\n可用变量：\{overview\} \(概况\), \{diffReport\} \(变化\), \{actions\} \(操作\), \{events\} \(事件\)"', '"RTE_PromptEditor_Tip".Translate()'
            $content = $content -replace '"恢复默认"', '"RTE_PromptEditor_RestoreDefault".Translate()'
            $content = $content -replace '"查看默认"', '"RTE_PromptEditor_ViewDefault".Translate()'
            $content = $content -replace '"保存"', '"RTE_PromptEditor_Save".Translate()'
            $content = $content -replace '"取消"', '"RTE_PromptEditor_Cancel".Translate()'
            $content = $content -replace '"已清空自定义提示词，将使用默认提示词"', '"RTE_PromptEditor_Restored".Translate()'
            $content = $content -replace '\$"默认提示词：\\n\\n\{defaultPrompt\}"', '"RTE_PromptEditor_DefaultPrompt".Translate(defaultPrompt)'
            $content = $content -replace '"关闭"', '"RTE_PromptEditor_Close".Translate()'
            $content = $content -replace '"提示词已保存"', '"RTE_PromptEditor_Saved".Translate()'
            $content = $content -replace '"使用默认提示词"', '"RTE_PromptEditor_UseDefault".Translate()'
            $content = $content -replace '\$"字符数: \{promptText\.Length\}"', '"RTE_PromptEditor_CharCount".Translate(promptText.Length)'
        }
        
        # AreaDrawingDesignator.cs 的替换
        if ($file -like "*AreaDrawingDesignator.cs") {
            $content = $content -replace 'defaultLabel = "绘制区域";', 'defaultLabel = "RTE_AreaDrawing_Label".Translate();'
            $content = $content -replace 'defaultDesc = "拖拽鼠标绘制或移除区域格子";', 'defaultDesc = "RTE_AreaDrawing_Desc".Translate();'
            $content = $content -replace 'return "没有选择区域";', 'return "RTE_AreaDrawing_NoAreaSelected".Translate();'
            $content = $content -replace '"请先选择一个区域"', '"RTE_AreaDrawing_SelectAreaFirst".Translate()'
        }
        
        # TaskEditorDialog.cs 的替换
        if ($file -like "*TaskEditorDialog.cs") {
            $content = $content -replace '\? \$"进度: \{editProgress:P0\} \(自动计算\)"', '? "RTE_TaskEditor_Progress_AutoCalc".Translate(editProgress)'
            $content = $content -replace ': \$"进度: \{editProgress:P0\}";', ': "RTE_TaskEditor_Progress_Manual_Display".Translate(editProgress);'
            $content = $content -replace '\? \$"施工区域: \{area\.Label\} \(\{announcement\.InitialBlueprintCount\} 蓝图\)"', '? "RTE_TaskEditor_ConstructionArea_Display".Translate(area.Label, announcement.InitialBlueprintCount)'
            $content = $content -replace ': "施工区域: \(已删除\)";', ': "RTE_TaskEditor_ConstructionArea_Deleted".Translate();'
            $content = $content -replace '\$"\{announcement\.Title\} 施工区"', '"RTE_TaskEditor_ConstructionArea_Name".Translate(announcement.Title)'
            $content = $content -replace '\? "施工区域"', '? "RTE_TaskEditor_ConstructionArea_DefaultName".Translate()'
            $content = $content -replace ': \$"\{announcement\.Title\} 施工区";', ': "RTE_TaskEditor_ConstructionArea_Name".Translate(announcement.Title);'
            $content = $content -replace '"自定义提示词"', '"RTE_TaskEditor_CustomPromptButton".Translate()'
        }
        
        # AreaEditorDialog.cs 的替换
        if ($file -like "*AreaEditorDialog.cs") {
            $content = $content -replace 'Widgets\.Label\(listing\.GetRect\(22f\), \$"格子数量: \{area\.CellCount\}"\);', 'Widgets.Label(listing.GetRect(22f), "RTE_AreaEditor_CellCount_Display".Translate(area.CellCount));'
        }
        
        # MainTabWindow_Announcement.cs 的替换
        if ($file -like "*MainTabWindow_Announcement.cs") {
            $content = $content -replace 'string statusText = project\.Status == AnnouncementStatus\.Completed \? "\[已完成\]" :', 'string statusText = project.Status == AnnouncementStatus.Completed ? "RTE_Announcement_Status_Completed".Translate() :'
            $content = $content -replace 'project\.Status == AnnouncementStatus\.Paused \? "\[暂停\]" : "\[进行中\]";', 'project.Status == AnnouncementStatus.Paused ? "RTE_Announcement_Status_Paused".Translate() : "RTE_Announcement_Status_Active".Translate();'
            $content = $content -replace 'extra \+= \$" \[进度: \{item\.Progress:P0\}\]";', 'extra += "RTE_Announcement_Progress_Display".Translate(item.Progress);'
            $content = $content -replace 'extra \+= \$" \[负责人: \{item\.AssignedPawnName\}\]";', 'extra += "RTE_Announcement_AssignedTo_Display".Translate(item.AssignedPawnName);'
            $content = $content -replace 'Widgets\.Label\(infoRect\.BottomHalf\(\), \$"格子数: \{area\.CellCount\}  \|  状态: \{area\.IsActive \? "启用" : "禁用"\}"\);', 'Widgets.Label(infoRect.BottomHalf(), string.Format("{0}  |  {1}: {2}", "RTE_Area_CellCount_Display".Translate(area.CellCount), "RTE_Area_Status_Label".Translate(), area.IsActive ? "RTE_Area_Status_Active".Translate() : "RTE_Area_Status_Inactive".Translate()));'
        }
        
        if ($content -ne $originalContent) {
            Set-Content $file -Value $content -Encoding UTF8 -NoNewline
            $changes = ($content.Length - $originalContent.Length)
            Write-Host "  ✓ 文件已更新" -ForegroundColor Green
            $totalReplacements++
        } else {
            Write-Host "  - 无需更改" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ✗ 文件不存在: $file" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "完成！处理了 $totalReplacements 个文件" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`n建议：" -ForegroundColor Yellow
Write-Host "1. 使用 'git diff' 查看所有更改" -ForegroundColor Yellow
Write-Host "2. 编译项目测试：dotnet build -c Release" -ForegroundColor Yellow
Write-Host "3. 在游戏中测试UI显示和中英文切换" -ForegroundColor Yellow
