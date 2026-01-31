public class ItemsConfig : BaseConfig
{
    /// <summary>
    /// 名称
    /// </summary>
    public string name;
    /// <summary>
    /// 描述
    /// </summary>
    public string desc;
    /// <summary>
    /// 图标资源路径（Resources.Load<Sprite>）
    /// </summary>
    public string iconPath;

    /// <summary>
    /// 音频资源路径（Resources.Load<AudioClip>）。为空表示该物品无音频。
    /// </summary>
    public string audioPath;
}
