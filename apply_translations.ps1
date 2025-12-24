# RimTalk Enhanced Prompt - 批量应用翻译Key脚本
# 此脚本会自动将硬编码的中文文本替换为翻译key

Write-Host "开始应用翻译key..." -ForegroundColor Green

# 定义替换映射（中文文本 -> 翻译Key）
$replacements = @{
    # 主窗口标签页
    '"当前状态"' = '"RTE_Tab_CurrentStatus".Translate()'
    '"历史快照"' = '"RTE_Tab_HistorySnapshots".Translate()'
    '"自定义区域"' = '"RTE_Tab_CustomAreas".Translate()'
    
    # 殖民地概况
    '"📝 殖民地概况"' = '"RTE_ColonyOverview_Title".Translate()'
    '"保存概况"' = '"RTE_ColonyOverview_Save".Translate()'
    '"AI 总结概况"' = '"RTE_ColonyOverview_AISummary".Translate()'
    '"⚙ 提示词"' = '"RTE_ColonyOverview_CustomPrompt".Translate()'
    '"提示：用自然语言描述殖民地状态，AI会读取这些信息。"' = '"RTE_ColonyOverview_Tip".Translate()'
    '"殖民地概况已更新"' = '"RTE_ColonyOverview_Updated".Translate()'
    '"概况为空，无法总结"' = '"RTE_ColonyOverview_EmptyError".Translate()'
    '"确定要让 AI 总结并精简当前的概况吗？\n这将替换当前的文本。"' = '"RTE_ColonyOverview_ConfirmSummary".Translate()'
    '"概况已由 AI 总结更新"' = '"RTE_ColonyOverview_SummarySuccess".Translate()'
    '"AI 总结失败"' = '"RTE_ColonyOverview_SummaryFailed".Translate()'
    
    # 历史快照
    '"暂无快照记录。每日 0 点将自动生成快照。"' = '"RTE_Snapshot_NoData".Translate()'
    '"← 前一天"' = '"RTE_Snapshot_PrevDay".Translate()'
    '"后一天 →"' = '"RTE_Snapshot_NextDay".Translate()'
    '"【AI 总结】"' = '"RTE_Snapshot_AISummary".Translate()'
    '"📊 详细变化"' = '"RTE_Snapshot_DetailedChanges".Translate()'
    '"【玩家操作】"' = '"RTE_Snapshot_PlayerActions".Translate()'
    '"【事件记录】"' = '"RTE_Snapshot_Events".Translate()'
    '"复制到概况"' = '"RTE_Snapshot_CopyToOverview".Translate()'
    '"重新生成"' = '"RTE_Snapshot_Regenerate".Translate()'
    '"已追加到概况（含日期）"' = '"RTE_Snapshot_CopiedWithDate".Translate()'
    '"确定要重新生成此快照的 AI 总结吗？"' = '"RTE_Snapshot_ConfirmRegenerate".Translate()'
    '"AI 总结已更新"' = '"RTE_Snapshot_RegenerateSuccess".Translate()'
    '"AI 总结生成失败"' = '"RTE_Snapshot_RegenerateFailed".Translate()'
    
    # 状况板
    '"+ 新建状况"' = '"RTE_Announcement_NewTask".Translate()'
    '"全部"' = '"RTE_Announcement_AllCategories".Translate()'
    '"工程"' = '"RTE_Announcement_Category_Project".Translate()'
    '"事件"' = '"RTE_Announcement_Category_Event".Translate()'
    '"任务"' = '"RTE_Announcement_Category_Quest".Translate()'
    '"资源"' = '"RTE_Announcement_Category_Resource".Translate()'
    '"人员"' = '"RTE_Announcement_Category_Personnel".Translate()'
    '"自定义"' = '"RTE_Announcement_Category_Custom".Translate()'
    '"完成"' = '"RTE_Announcement_Complete".Translate()'
    '"恢复"' = '"RTE_Announcement_Resume".Translate()'
    '"重开"' = '"RTE_Announcement_Reopen".Translate()'
    '"编辑"' = '"RTE_Announcement_Edit".Translate()'
    '"删除"' = '"RTE_Announcement_Delete".Translate()'
    
    # 自定义区域
    '"+ 新建区域"' = '"RTE_Area_NewArea".Translate()'
    '"暂无自定义区域。点击右上角\"新建区域\"按钮创建。"' = '"RTE_Area_NoData".Translate()'
    '"请先进入地图"' = '"RTE_Area_EnterMapFirst".Translate()'
    '"新区域"' = '"RTE_Area_NewAreaName".Translate()'
    '"绘制"' = '"RTE_Area_Draw".Translate()'
    '"移除格子"' = '"RTE_Area_RemoveCells".Translate()'
    
    # 任务编辑器
    '"新建状况"' = '"RTE_TaskEditor_NewTask".Translate()'
    '"编辑状况"' = '"RTE_TaskEditor_EditTask".Translate()'
    '"类别："' = '"RTE_TaskEditor_Category".Translate()'
    '"标题："' = '"RTE_TaskEditor_Title".Translate()'
    '"描述："' = '"RTE_TaskEditor_Description".Translate()'
    '"优先级："' = '"RTE_TaskEditor_Priority".Translate()'
    '"状态："' = '"RTE_TaskEditor_Status".Translate()'
    '"负责人："' = '"RTE_TaskEditor_AssignedPawn".Translate()'
    '"相关人员："' = '"RTE_TaskEditor_RelatedPerson".Translate()'
    '"无"' = '"RTE_TaskEditor_None".Translate()'
    '"保存 (Enter)"' = '"RTE_TaskEditor_Save".Translate()'
    '"取消 (Esc)"' = '"RTE_TaskEditor_Cancel".Translate()'
    '"框选施工区域"' = '"RTE_TaskEditor_SelectArea".Translate()'
    '"移除区域"' = '"RTE_TaskEditor_RemoveArea".Translate()'
    '"已移除施工区域"' = '"RTE_TaskEditor_AreaRemoved".Translate()'
    '"AI 总结工程"' = '"RTE_TaskEditor_AISummary".Translate()'
    '"生成中..."' = '"RTE_TaskEditor_Generating".Translate()'
    '"请先框选施工区域"' = '"RTE_TaskEditor_SelectAreaFirst".Translate()'
    '"施工区域不存在"' = '"RTE_TaskEditor_AreaNotExist".Translate()'
    '"施工区域内没有蓝图"' = '"RTE_TaskEditor_NoBlueprintsInArea".Translate()'
    '"AI调用失败，请检查API配置"' = '"RTE_TaskEditor_AICallFailed".Translate()'
    '"AI总结完成"' = '"RTE_TaskEditor_AISummaryComplete".Translate()'
    '"AI响应格式异常，已使用原始文本"' = '"RTE_TaskEditor_AIResponseError".Translate()'
    '"拖拽鼠标框选施工区域。完成后会自动扫描蓝图数量。"' = '"RTE_TaskEditor_SelectAreaTip".Translate()'
    '"未命名工程"' = '"RTE_TaskEditor_UnnamedProject".Translate()'
    
    # 区域编辑器
    '"区域名称:"' = '"RTE_AreaEditor_AreaName".Translate()'
    '"区域颜色:"' = '"RTE_AreaEditor_AreaColor".Translate()'
    '"选择颜色"' = '"RTE_AreaEditor_SelectColor".Translate()'
    '"启用此区域"' = '"RTE_AreaEditor_EnableArea".Translate()'
    '"保存并关闭"' = '"RTE_AreaEditor_SaveAndClose".Translate()'
    '"清空区域"' = '"RTE_AreaEditor_ClearArea".Translate()'
    
    # 颜色选择器
    '"选择预设颜色:"' = '"RTE_ColorPicker_Title".Translate()'
    
    # 设置界面标签页
    '"健康信息"' = '"RTE_Settings_Tab_Health".Translate()'
    '"物品描述"' = '"RTE_Settings_Tab_Items".Translate()'
    '"派系关系"' = '"RTE_Settings_Tab_Factions".Translate()'
    '"地图位置"' = '"RTE_Settings_Tab_Location".Translate()'
    '"通告系统"' = '"RTE_Settings_Tab_Announcement".Translate()'
    '"自动捕获"' = '"RTE_Settings_Tab_AutoCapture".Translate()'
    '"AI 史官"' = '"RTE_Settings_Tab_AIHistorian".Translate()'
}

# 需要处理的文件列表
$files = @(
    "Source/UI/MainTabWindow_Announcement.cs",
    "Source/UI/TaskEditorDialog.cs",
    "Source/UI/AreaEditorDialog.cs",
    "Source/Core/RimTalkHealthEnhanceMod.cs",
    "Source/Settings/HealthEnhanceSettings.cs"
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
            }
        }
        
        if ($fileReplacements -gt 0) {
            Set-Content $file -Value $content -Encoding UTF8 -NoNewline
            Write-Host "  ✓ 替换了 $fileReplacements 处文本" -ForegroundColor Green
        } else {
            Write-Host "  - 无需替换" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ✗ 文件不存在: $file" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "完成！总共替换了 $totalReplacements 处文本" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host "`n提示：请检查替换结果，确保没有误替换。" -ForegroundColor Yellow
Write-Host "建议使用 Git 查看更改：git diff" -ForegroundColor Yellow
