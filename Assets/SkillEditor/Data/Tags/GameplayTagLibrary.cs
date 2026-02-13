// =============================================================================
// 此文件由 GameplayTagCodeGenerator 自动生成
// 请勿手动修改此文件，修改将在下次生成时被覆盖
// =============================================================================

using System.Collections.Generic;

namespace SkillEditor.Data
{
    /// <summary>
    /// 游戏标签静态库 - 提供所有标签的静态引用
    /// </summary>
    public static class GameplayTagLibrary
    {
        #region 标签定义

        /// <summary>
        /// Buff
        /// </summary>
        public static GameplayTag Buff { get; } = new GameplayTag("Buff");

        /// <summary>
        /// Buff.DeBuff
        /// </summary>
        public static GameplayTag Buff_DeBuff { get; } = new GameplayTag("Buff.DeBuff");

        /// <summary>
        /// Buff.DeBuff.Dot
        /// </summary>
        public static GameplayTag Buff_DeBuff_Dot { get; } = new GameplayTag("Buff.DeBuff.Dot");

        /// <summary>
        /// Buff.DeBuff.Stun
        /// </summary>
        public static GameplayTag Buff_DeBuff_Stun { get; } = new GameplayTag("Buff.DeBuff.Stun");

        /// <summary>
        /// CD
        /// </summary>
        public static GameplayTag CD { get; } = new GameplayTag("CD");

        /// <summary>
        /// CD.万象天引
        /// </summary>
        public static GameplayTag CD_万象天引 { get; } = new GameplayTag("CD.万象天引");

        /// <summary>
        /// CD.三火球
        /// </summary>
        public static GameplayTag CD_三火球 { get; } = new GameplayTag("CD.三火球");

        /// <summary>
        /// CD.回血
        /// </summary>
        public static GameplayTag CD_回血 { get; } = new GameplayTag("CD.回血");

        /// <summary>
        /// CD.急速
        /// </summary>
        public static GameplayTag CD_急速 { get; } = new GameplayTag("CD.急速");

        /// <summary>
        /// CD.旋风斩
        /// </summary>
        public static GameplayTag CD_旋风斩 { get; } = new GameplayTag("CD.旋风斩");

        /// <summary>
        /// CD.流血
        /// </summary>
        public static GameplayTag CD_流血 { get; } = new GameplayTag("CD.流血");

        /// <summary>
        /// CD.火球术
        /// </summary>
        public static GameplayTag CD_火球术 { get; } = new GameplayTag("CD.火球术");

        /// <summary>
        /// CD.神罗天正
        /// </summary>
        public static GameplayTag CD_神罗天正 { get; } = new GameplayTag("CD.神罗天正");

        /// <summary>
        /// CD.被动回血
        /// </summary>
        public static GameplayTag CD_被动回血 { get; } = new GameplayTag("CD.被动回血");

        /// <summary>
        /// CD.践踏
        /// </summary>
        public static GameplayTag CD_践踏 { get; } = new GameplayTag("CD.践踏");

        /// <summary>
        /// unitType
        /// </summary>
        public static GameplayTag unitType { get; } = new GameplayTag("unitType");

        /// <summary>
        /// unitType.hero
        /// </summary>
        public static GameplayTag unitType_hero { get; } = new GameplayTag("unitType.hero");

        /// <summary>
        /// unitType.monster
        /// </summary>
        public static GameplayTag unitType_monster { get; } = new GameplayTag("unitType.monster");

        #endregion

        #region 标签映射

        /// <summary>
        /// 标签名称到标签实例的映射
        /// </summary>
        public static readonly Dictionary<string, GameplayTag> TagMap = new Dictionary<string, GameplayTag>
        {
            ["Buff"] = Buff,
            ["Buff.DeBuff"] = Buff_DeBuff,
            ["Buff.DeBuff.Dot"] = Buff_DeBuff_Dot,
            ["Buff.DeBuff.Stun"] = Buff_DeBuff_Stun,
            ["CD"] = CD,
            ["CD.万象天引"] = CD_万象天引,
            ["CD.三火球"] = CD_三火球,
            ["CD.回血"] = CD_回血,
            ["CD.急速"] = CD_急速,
            ["CD.旋风斩"] = CD_旋风斩,
            ["CD.流血"] = CD_流血,
            ["CD.火球术"] = CD_火球术,
            ["CD.神罗天正"] = CD_神罗天正,
            ["CD.被动回血"] = CD_被动回血,
            ["CD.践踏"] = CD_践踏,
            ["unitType"] = unitType,
            ["unitType.hero"] = unitType_hero,
            ["unitType.monster"] = unitType_monster,
        };

        #endregion

        #region 辅助方法

        /// <summary>
        /// 根据名称获取标签
        /// </summary>
        public static GameplayTag GetTag(string tagName)
        {
            if (TagMap.TryGetValue(tagName, out var tag))
                return tag;
            return GameplayTag.None;
        }

        /// <summary>
        /// 检查标签是否存在
        /// </summary>
        public static bool HasTag(string tagName)
        {
            return TagMap.ContainsKey(tagName);
        }

        /// <summary>
        /// 获取所有标签名称
        /// </summary>
        public static IEnumerable<string> GetAllTagNames()
        {
            return TagMap.Keys;
        }

        /// <summary>
        /// 获取所有标签
        /// </summary>
        public static IEnumerable<GameplayTag> GetAllTags()
        {
            return TagMap.Values;
        }

        #endregion
    }
}
