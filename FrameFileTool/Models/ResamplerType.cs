namespace FrameFileTool.Models;

/// <summary>
/// 縮放重取樣演算法，對應 ImageMagick 的 FilterType。
/// 每個選項均有明確的適用場景，避免暴露不必要的技術細節給使用者。
/// </summary>
public enum ResamplerType
{
    /// <summary>一般用途（預設）。縮放倍率不大時效果穩定。</summary>
    Bicubic,

    /// <summary>高品質縮小。大幅縮小時保持文字與線條清晰。</summary>
    Lanczos3,

    /// <summary>高品質放大。放大時邊緣比一般用途更銳利。</summary>
    CatmullRom,

    /// <summary>像素精準。整數倍放大截圖時維持像素對齊，不插值。</summary>
    NearestNeighbor,

    /// <summary>銳利優先。線條圖與流程圖需要最銳利邊緣時使用。</summary>
    MitchellNetravali,
}
