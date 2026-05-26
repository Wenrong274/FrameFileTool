namespace FrameFileTool.Models;

/// <summary>縮放模式：以百分比計算，或直接指定目標像素尺寸。</summary>
public enum ResizeMode
{
    /// <summary>依百分比縮放，例如 50 代表縮小為原始尺寸的一半。</summary>
    Percentage,

    /// <summary>指定目標寬度與高度（像素），可選擇是否維持原始比例。</summary>
    Absolute,
}
