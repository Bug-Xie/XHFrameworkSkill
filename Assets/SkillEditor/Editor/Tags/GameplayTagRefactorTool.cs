using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using SkillEditor.Data;

namespace SkillEditor.Editor
{
    /// <summary>
    /// Tag 重构工具 - 当 Tag 重命名或删除时，自动更新所有引用该 Tag 的技能资产
    /// 支持文本预过滤优化，适用于上千文件规模
    /// </summary>
    public static class GameplayTagRefactorTool
    {
        /// <summary>
        /// 重命名 Tag 后，批量更新所有技能资产中的引用
        /// </summary>
        /// <param name="renamedPaths">旧路径 -> 新路径 的映射</param>
        /// <returns>被修改的资产数量</returns>
        public static int ApplyRename(Dictionary<string, string> renamedPaths)
        {
            if (renamedPaths == null || renamedPaths.Count == 0) return 0;

            var oldNames = renamedPaths.Keys.ToArray();
            var assetPaths = FindSkillAssets();
            int modifiedCount = 0;
            int totalAssets = assetPaths.Length;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < totalAssets; i++)
                {
                    var assetPath = assetPaths[i];

                    if (totalAssets > 50)
                    {
                        EditorUtility.DisplayProgressBar(
                            "更新 Tag 引用",
                            $"扫描: {Path.GetFileNameWithoutExtension(assetPath)} ({i + 1}/{totalAssets})",
                            (float)i / totalAssets);
                    }

                    // 文本预过滤：快速跳过不包含目标 tag 的文件
                    if (!TextPreFilter(assetPath, oldNames))
                        continue;

                    // 精确修改序列化属性
                    if (RenameTagsInAsset(assetPath, renamedPaths))
                        modifiedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[TagRefactor] 重命名完成，共更新 {modifiedCount} 个技能资产");
            }

            return modifiedCount;
        }

        /// <summary>
        /// 删除 Tag 前，扫描所有引用并返回引用信息
        /// </summary>
        /// <param name="removedPaths">被删除的 tag 路径列表</param>
        /// <returns>引用信息列表（资产路径 + 引用的 tag）</returns>
        public static List<TagReference> FindReferences(List<string> removedPaths)
        {
            if (removedPaths == null || removedPaths.Count == 0)
                return new List<TagReference>();

            var tagNames = removedPaths.ToArray();
            var assetPaths = FindSkillAssets();
            var references = new List<TagReference>();
            int totalAssets = assetPaths.Length;

            try
            {
                for (int i = 0; i < totalAssets; i++)
                {
                    var assetPath = assetPaths[i];

                    if (totalAssets > 50)
                    {
                        EditorUtility.DisplayProgressBar(
                            "扫描 Tag 引用",
                            $"扫描: {Path.GetFileNameWithoutExtension(assetPath)} ({i + 1}/{totalAssets})",
                            (float)i / totalAssets);
                    }

                    if (!TextPreFilter(assetPath, tagNames))
                        continue;

                    FindTagReferencesInAsset(assetPath, tagNames, references);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return references;
        }

        /// <summary>
        /// 从资产中移除指定的 tag 引用
        /// </summary>
        /// <param name="removedPaths">被删除的 tag 路径列表</param>
        /// <returns>被修改的资产数量</returns>
        public static int ApplyRemove(List<string> removedPaths)
        {
            if (removedPaths == null || removedPaths.Count == 0) return 0;

            var tagNames = removedPaths.ToArray();
            var assetPaths = FindSkillAssets();
            int modifiedCount = 0;
            int totalAssets = assetPaths.Length;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < totalAssets; i++)
                {
                    var assetPath = assetPaths[i];

                    if (totalAssets > 50)
                    {
                        EditorUtility.DisplayProgressBar(
                            "清理 Tag 引用",
                            $"处理: {Path.GetFileNameWithoutExtension(assetPath)} ({i + 1}/{totalAssets})",
                            (float)i / totalAssets);
                    }

                    if (!TextPreFilter(assetPath, tagNames))
                        continue;

                    if (RemoveTagsFromAsset(assetPath, tagNames))
                        modifiedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            if (modifiedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[TagRefactor] 清理完成，共更新 {modifiedCount} 个技能资产");
            }

            return modifiedCount;
        }

        #region 内部实现

        /// <summary>
        /// 查找所有 SkillGraphData 资产
        /// </summary>
        private static string[] FindSkillAssets()
        {
            var guids = AssetDatabase.FindAssets("t:SkillGraphData");
            var paths = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                paths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            }
            return paths;
        }

        /// <summary>
        /// 文本预过滤 - 快速判断文件是否可能包含目标 tag
        /// Unity YAML 会将非 ASCII 字符（中文等）转义为 \uXXXX 格式，
        /// 所以需要同时搜索原始字符串和 Unicode 转义形式
        /// </summary>
        private static bool TextPreFilter(string assetPath, string[] tagNames)
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath)) return false;

            var content = File.ReadAllText(fullPath);
            for (int i = 0; i < tagNames.Length; i++)
            {
                // 搜索原始字符串（适用于纯 ASCII tag 名）
                if (content.Contains(tagNames[i]))
                    return true;

                // 搜索 Unicode 转义形式（适用于包含中文等非 ASCII 字符的 tag 名）
                var escaped = ToUnicodeEscaped(tagNames[i]);
                if (escaped != tagNames[i] && content.Contains(escaped))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 将字符串中的非 ASCII 字符转换为 \uXXXX 格式（与 Unity YAML 序列化一致）
        /// </summary>
        private static string ToUnicodeEscaped(string input)
        {
            var sb = new StringBuilder(input.Length);
            bool hasNonAscii = false;
            foreach (var c in input)
            {
                if (c > 127)
                {
                    sb.Append($"\\u{(int)c:X4}");
                    hasNonAscii = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
            return hasNonAscii ? sb.ToString() : input;
        }

        /// <summary>
        /// 在单个资产中重命名 tag 引用（直接操作运行时对象，绕开 SerializedProperty 对 [SerializeReference] 的兼容问题）
        /// </summary>
        private static bool RenameTagsInAsset(string assetPath, Dictionary<string, string> renamedPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath);
            if (asset == null) return false;

            bool modified = false;

            foreach (var node in asset.nodes)
            {
                if (RenameTagsInNode(node, renamedPaths))
                    modified = true;
            }

            if (modified)
                EditorUtility.SetDirty(asset);

            return modified;
        }

        /// <summary>
        /// 通过反射遍历节点的所有 GameplayTagSet 字段，重命名匹配的 tag
        /// </summary>
        private static bool RenameTagsInNode(NodeData node, Dictionary<string, string> renamedPaths)
        {
            if (node == null) return false;

            bool modified = false;
            var type = node.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType != typeof(GameplayTagSet)) continue;

                var tagSet = (GameplayTagSet)field.GetValue(node);
                if (tagSet.IsEmpty) continue;

                var newTags = new List<GameplayTag>();
                bool setModified = false;

                foreach (var tag in tagSet.Tags)
                {
                    if (tag.IsValid && renamedPaths.TryGetValue(tag.Name, out var newName))
                    {
                        newTags.Add(new GameplayTag(newName));
                        setModified = true;
                    }
                    else
                    {
                        newTags.Add(tag);
                    }
                }

                if (setModified)
                {
                    field.SetValue(node, new GameplayTagSet(newTags));
                    modified = true;
                }
            }

            return modified;
        }

        /// <summary>
        /// 在单个资产中查找 tag 引用（直接操作运行时对象）
        /// </summary>
        private static void FindTagReferencesInAsset(string assetPath, string[] tagNames, List<TagReference> references)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath);
            if (asset == null) return;

            var tagNameSet = new HashSet<string>(tagNames);

            foreach (var node in asset.nodes)
            {
                if (node == null) continue;
                FindTagReferencesInNode(node, tagNameSet, assetPath, asset.name, references);
            }
        }

        /// <summary>
        /// 通过反射遍历节点的所有 GameplayTagSet 字段，查找匹配的 tag 引用
        /// </summary>
        private static void FindTagReferencesInNode(NodeData node, HashSet<string> tagNames,
            string assetPath, string assetName, List<TagReference> references)
        {
            var type = node.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType != typeof(GameplayTagSet)) continue;

                var tagSet = (GameplayTagSet)field.GetValue(node);
                if (tagSet.IsEmpty) continue;

                foreach (var tag in tagSet.Tags)
                {
                    if (tag.IsValid && tagNames.Contains(tag.Name))
                    {
                        references.Add(new TagReference
                        {
                            assetPath = assetPath,
                            assetName = assetName,
                            tagName = tag.Name,
                            propertyPath = $"{type.Name}.{field.Name}"
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 从单个资产中移除指定的 tag
        /// </summary>
        private static bool RemoveTagsFromAsset(string assetPath, string[] tagNames)
        {
            var asset = AssetDatabase.LoadAssetAtPath<SkillGraphData>(assetPath);
            if (asset == null) return false;

            var tagNameSet = new HashSet<string>(tagNames);
            bool modified = false;

            // 直接操作运行时对象，因为需要从数组中移除元素
            foreach (var node in asset.nodes)
            {
                if (RemoveTagsFromNode(node, tagNameSet))
                    modified = true;
            }

            if (modified)
                EditorUtility.SetDirty(asset);

            return modified;
        }

        /// <summary>
        /// 从节点数据中移除指定的 tag
        /// 通过反射遍历所有 GameplayTagSet 字段
        /// </summary>
        private static bool RemoveTagsFromNode(NodeData node, HashSet<string> tagNames)
        {
            if (node == null) return false;

            bool modified = false;
            var type = node.GetType();

            // 遍历所有字段，找到 GameplayTagSet 类型的字段
            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(GameplayTagSet)) continue;

                var tagSet = (GameplayTagSet)field.GetValue(node);
                if (tagSet.IsEmpty) continue;

                var newTags = new List<GameplayTag>();
                bool setModified = false;

                foreach (var tag in tagSet.Tags)
                {
                    if (tag.IsValid && tagNames.Contains(tag.Name))
                    {
                        setModified = true; // 跳过被删除的 tag
                    }
                    else
                    {
                        newTags.Add(tag);
                    }
                }

                if (setModified)
                {
                    field.SetValue(node, new GameplayTagSet(newTags));
                    modified = true;
                }
            }

            return modified;
        }

        #endregion
    }

    /// <summary>
    /// Tag 引用信息
    /// </summary>
    public struct TagReference
    {
        public string assetPath;
        public string assetName;
        public string tagName;
        public string propertyPath;
    }
}
