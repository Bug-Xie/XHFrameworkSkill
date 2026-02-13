using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using SkillEditor.Data;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 标签代码生成器
    /// 将 GameplayTagsAsset 中的标签生成为静态 C# 代码
    /// </summary>
    public static class GameplayTagCodeGenerator
    {
        private const string DEFAULT_NAMESPACE = "SkillEditor.Data";
        private const string DEFAULT_CLASS_NAME = "GameplayTagLibrary";

        private const string LIBRARY_FILE_NAME = "GameplayTagLibrary.cs";

        /// <summary>
        /// 静默自动生成 - 自动查找已有的 GameplayTagLibrary.cs 路径，无需用户交互
        /// 用于 Tag 重命名/删除后自动重新生成
        /// </summary>
        /// <returns>是否成功生成</returns>
        public static bool AutoGenerate(GameplayTagsAsset asset)
        {
            if (asset == null || asset.CachedTags.Count == 0) return false;

            // 查找已有的 GameplayTagLibrary.cs
            var existingPath = FindExistingLibraryPath();
            if (string.IsNullOrEmpty(existingPath))
            {
                // 没有找到已有文件，放到 Tag 资源同目录下
                var assetPath = AssetDatabase.GetAssetPath(asset);
                var dir = Path.GetDirectoryName(assetPath);
                existingPath = Path.Combine(dir, LIBRARY_FILE_NAME);
            }

            var fullPath = Path.GetFullPath(existingPath);
            GenerateCode(asset, fullPath, DEFAULT_NAMESPACE, DEFAULT_CLASS_NAME);
            AssetDatabase.ImportAsset(existingPath);
            Debug.Log($"[TagCodeGen] GameplayTagLibrary 已自动更新: {existingPath}");
            return true;
        }

        /// <summary>
        /// 查找项目中已有的 GameplayTagLibrary.cs 文件路径
        /// </summary>
        private static string FindExistingLibraryPath()
        {
            var guids = AssetDatabase.FindAssets("GameplayTagLibrary t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(path) == LIBRARY_FILE_NAME)
                    return path;
            }
            return null;
        }

        /// <summary>
        /// 从编辑器窗口调用的生成方法
        /// </summary>
        public static void GenerateTagCodeFromEditor(GameplayTagsAsset asset)
        {
            if (asset == null || asset.CachedTags.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "标签资源为空或未包含任何标签。", "确定");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset);
            var defaultPath = Path.GetDirectoryName(assetPath);
            var savePath = EditorUtility.SaveFilePanel(
                "保存标签代码",
                defaultPath,
                $"{DEFAULT_CLASS_NAME}.cs",
                "cs");

            if (string.IsNullOrEmpty(savePath)) return;

            GenerateCode(asset, savePath, DEFAULT_NAMESPACE, DEFAULT_CLASS_NAME);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"标签代码已生成到:\n{savePath}", "确定");
        }
        
        public static void GenerateTagCode()
        {
            // 查找标签资源
            var guids = AssetDatabase.FindAssets("t:GameplayTagsAsset");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("错误", "未找到 GameplayTagsAsset 资源，请先创建一个。", "确定");
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<GameplayTagsAsset>(assetPath);

            if (asset == null || asset.CachedTags.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "标签资源为空或未包含任何标签。", "确定");
                return;
            }

            // 选择保存路径
            var defaultPath = Path.GetDirectoryName(assetPath);
            var savePath = EditorUtility.SaveFilePanel(
                "保存标签代码",
                defaultPath,
                $"{DEFAULT_CLASS_NAME}.gen.cs",
                "cs");

            if (string.IsNullOrEmpty(savePath)) return;

            GenerateCode(asset, savePath, DEFAULT_NAMESPACE, DEFAULT_CLASS_NAME);

            // 刷新资源
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"标签代码已生成到:\n{savePath}", "确定");
        }

        /// <summary>
        /// 生成标签代码
        /// </summary>
        public static void GenerateCode(GameplayTagsAsset asset, string filePath, string namespaceName, string className)
        {
            var sb = new StringBuilder();

            // 文件头
            sb.AppendLine("// =============================================================================");
            sb.AppendLine("// 此文件由 GameplayTagCodeGenerator 自动生成");
            sb.AppendLine("// 请勿手动修改此文件，修改将在下次生成时被覆盖");
            sb.AppendLine("// =============================================================================");
            sb.AppendLine();
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// 游戏标签静态库 - 提供所有标签的静态引用");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    public static class {className}");
            sb.AppendLine("    {");

            // 生成标签属性
            sb.AppendLine("        #region 标签定义");
            sb.AppendLine();

            foreach (var tag in asset.CachedTags)
            {
                var propertyName = MakeValidIdentifier(tag.Name);
                var comment = tag.Name;

                sb.AppendLine($"        /// <summary>");
                sb.AppendLine($"        /// {comment}");
                sb.AppendLine($"        /// </summary>");
                sb.AppendLine($"        public static GameplayTag {propertyName} {{ get; }} = new GameplayTag(\"{tag.Name}\");");
                sb.AppendLine();
            }

            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成标签映射字典
            sb.AppendLine("        #region 标签映射");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 标签名称到标签实例的映射");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static readonly Dictionary<string, GameplayTag> TagMap = new Dictionary<string, GameplayTag>");
            sb.AppendLine("        {");

            foreach (var tag in asset.CachedTags)
            {
                var propertyName = MakeValidIdentifier(tag.Name);
                sb.AppendLine($"            [\"{tag.Name}\"] = {propertyName},");
            }

            sb.AppendLine("        };");
            sb.AppendLine();
            sb.AppendLine("        #endregion");
            sb.AppendLine();

            // 生成辅助方法
            sb.AppendLine("        #region 辅助方法");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 根据名称获取标签");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static GameplayTag GetTag(string tagName)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (TagMap.TryGetValue(tagName, out var tag))");
            sb.AppendLine("                return tag;");
            sb.AppendLine("            return GameplayTag.None;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 检查标签是否存在");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static bool HasTag(string tagName)");
            sb.AppendLine("        {");
            sb.AppendLine("            return TagMap.ContainsKey(tagName);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 获取所有标签名称");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static IEnumerable<string> GetAllTagNames()");
            sb.AppendLine("        {");
            sb.AppendLine("            return TagMap.Keys;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 获取所有标签");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static IEnumerable<GameplayTag> GetAllTags()");
            sb.AppendLine("        {");
            sb.AppendLine("            return TagMap.Values;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        #endregion");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // 写入文件
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// 将标签名称转换为有效的 C# 标识符
        /// </summary>
        private static string MakeValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_";

            var sb = new StringBuilder();

            // 将点号替换为下划线
            name = name.Replace('.', '_');

            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            var result = sb.ToString();

            // 如果以数字开头，添加下划线前缀
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            // 检查是否为 C# 关键字
            if (IsCSharpKeyword(result))
            {
                result = "@" + result;
            }

            return result;
        }

        /// <summary>
        /// 检查是否为 C# 关键字
        /// </summary>
        private static bool IsCSharpKeyword(string word)
        {
            var keywords = new HashSet<string>
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
                "char", "checked", "class", "const", "continue", "decimal", "default",
                "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
                "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
                "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
                "namespace", "new", "null", "object", "operator", "out", "override",
                "params", "private", "protected", "public", "readonly", "ref", "return",
                "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
                "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
                "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
                "volatile", "while"
            };

            return keywords.Contains(word);
        }
    }
}
