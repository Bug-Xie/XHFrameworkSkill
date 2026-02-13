using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillEditor.Data
{
    /// <summary>
    /// 标签树节点 - 用于编辑器中的树形显示
    /// </summary>
    [Serializable]
    public class GameplayTagTreeNode
    {
        public string name;
        public int id;
        public int parentId;
        public List<int> childrenIds = new List<int>();

        public GameplayTagTreeNode() { }

        public GameplayTagTreeNode(string name, int id, int parentId = -1)
        {
            this.name = name;
            this.id = id;
            this.parentId = parentId;
        }
    }

    /// <summary>
    /// 游戏标签资源 - ScriptableObject
    /// 存储所有游戏标签的定义
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayTagsAsset", menuName = "SkillEditor/Gameplay Tags Asset")]
    public class GameplayTagsAsset : ScriptableObject
    {
        [SerializeField] private List<GameplayTagTreeNode> _treeNodes = new List<GameplayTagTreeNode>();
        [SerializeField] private List<GameplayTag> _cachedTags = new List<GameplayTag>();
        [SerializeField] private int _nextId = 1;

        /// <summary>
        /// 树节点列表
        /// </summary>
        public List<GameplayTagTreeNode> TreeNodes => _treeNodes;

        /// <summary>
        /// 缓存的标签列表（运行时使用）
        /// </summary>
        public List<GameplayTag> CachedTags => _cachedTags;

        /// <summary>
        /// 下一个可用ID
        /// </summary>
        public int NextId => _nextId;

        /// <summary>
        /// 初始化（确保有根节点）
        /// </summary>
        public void Initialize()
        {
            if (_treeNodes.Count == 0)
            {
                // 添加虚拟根节点
                _treeNodes.Add(new GameplayTagTreeNode("Root", 0, -1));
                _nextId = 1;
            }
        }

        /// <summary>
        /// 获取根节点
        /// </summary>
        public GameplayTagTreeNode GetRootNode()
        {
            Initialize();
            return _treeNodes[0];
        }

        /// <summary>
        /// 根据ID获取节点
        /// </summary>
        public GameplayTagTreeNode GetNodeById(int id)
        {
            for (int i = 0; i < _treeNodes.Count; i++)
            {
                if (_treeNodes[i].id == id)
                    return _treeNodes[i];
            }
            return null;
        }

        /// <summary>
        /// 添加标签节点
        /// </summary>
        /// <param name="name">节点名称（单级名称，不含点号）</param>
        /// <param name="parentId">父节点ID，-1或0表示添加到根节点</param>
        /// <returns>新节点的ID</returns>
        public int AddNode(string name, int parentId = 0)
        {
            Initialize();

            if (parentId < 0) parentId = 0;

            var parent = GetNodeById(parentId);
            if (parent == null)
            {
                Debug.LogError($"Parent node with id {parentId} not found");
                return -1;
            }

            // 检查同级是否已存在同名节点
            foreach (var childId in parent.childrenIds)
            {
                var child = GetNodeById(childId);
                if (child != null && child.name == name)
                {
                    Debug.LogWarning($"Node with name '{name}' already exists under parent");
                    return child.id;
                }
            }

            var newNode = new GameplayTagTreeNode(name, _nextId++, parentId);
            _treeNodes.Add(newNode);
            parent.childrenIds.Add(newNode.id);

            RebuildCache();
            return newNode.id;
        }

        /// <summary>
        /// 删除标签节点（及其所有子节点）
        /// </summary>
        public bool RemoveNode(int nodeId)
        {
            return RemoveNode(nodeId, out _);
        }

        /// <summary>
        /// 删除标签节点，并输出被删除的所有标签路径
        /// </summary>
        public bool RemoveNode(int nodeId, out List<string> removedPaths)
        {
            removedPaths = null;
            if (nodeId == 0) return false;

            var node = GetNodeById(nodeId);
            if (node == null) return false;

            // 删除前收集所有受影响的路径
            var affectedNodeIds = new List<int>();
            CollectDescendantIds(nodeId, affectedNodeIds);

            removedPaths = new List<string>();
            foreach (var id in affectedNodeIds)
            {
                var path = GetFullTagPath(id);
                if (!string.IsNullOrEmpty(path))
                    removedPaths.Add(path);
            }

            // 执行删除
            RemoveNodeInternal(nodeId);
            RebuildCache();
            return true;
        }

        /// <summary>
        /// 内部递归删除（不触发 RebuildCache）
        /// </summary>
        private void RemoveNodeInternal(int nodeId)
        {
            var node = GetNodeById(nodeId);
            if (node == null) return;

            var childrenToRemove = new List<int>(node.childrenIds);
            foreach (var childId in childrenToRemove)
            {
                RemoveNodeInternal(childId);
            }

            var parent = GetNodeById(node.parentId);
            if (parent != null)
            {
                parent.childrenIds.Remove(nodeId);
            }

            _treeNodes.Remove(node);
        }

        /// <summary>
        /// 收集节点自身及所有子孙节点的ID
        /// </summary>
        private void CollectDescendantIds(int nodeId, List<int> result)
        {
            result.Add(nodeId);
            var node = GetNodeById(nodeId);
            if (node == null) return;

            foreach (var childId in node.childrenIds)
            {
                CollectDescendantIds(childId, result);
            }
        }

        /// <summary>
        /// 重命名节点
        /// </summary>
        public bool RenameNode(int nodeId, string newName)
        {
            return RenameNode(nodeId, newName, out _);
        }

        /// <summary>
        /// 重命名节点，并输出受影响的标签路径映射（旧路径 -> 新路径）
        /// </summary>
        public bool RenameNode(int nodeId, string newName, out Dictionary<string, string> renamedPaths)
        {
            renamedPaths = null;
            if (nodeId == 0) return false;

            var node = GetNodeById(nodeId);
            if (node == null) return false;

            // 检查同级是否已存在同名节点
            var parent = GetNodeById(node.parentId);
            if (parent != null)
            {
                foreach (var childId in parent.childrenIds)
                {
                    if (childId == nodeId) continue;
                    var sibling = GetNodeById(childId);
                    if (sibling != null && sibling.name == newName)
                    {
                        Debug.LogWarning($"Node with name '{newName}' already exists");
                        return false;
                    }
                }
            }

            // 收集重命名前所有受影响节点的旧路径
            var affectedNodeIds = new List<int>();
            CollectDescendantIds(nodeId, affectedNodeIds);

            var oldPaths = new Dictionary<int, string>();
            foreach (var id in affectedNodeIds)
            {
                oldPaths[id] = GetFullTagPath(id);
            }

            // 执行重命名
            node.name = newName;
            RebuildCache();

            // 构建旧路径 -> 新路径映射
            renamedPaths = new Dictionary<string, string>();
            foreach (var id in affectedNodeIds)
            {
                var oldPath = oldPaths[id];
                var newPath = GetFullTagPath(id);
                if (oldPath != newPath)
                {
                    renamedPaths[oldPath] = newPath;
                }
            }

            return true;
        }

        /// <summary>
        /// 移动节点到新的父节点
        /// </summary>
        public bool MoveNode(int nodeId, int newParentId)
        {
            if (nodeId == 0) return false; // 不能移动根节点

            var node = GetNodeById(nodeId);
            if (node == null) return false;

            var newParent = GetNodeById(newParentId);
            if (newParent == null) return false;

            // 检查是否会造成循环引用
            if (IsDescendantOf(newParentId, nodeId))
            {
                Debug.LogWarning("Cannot move node to its own descendant");
                return false;
            }

            // 从旧父节点移除
            var oldParent = GetNodeById(node.parentId);
            if (oldParent != null)
            {
                oldParent.childrenIds.Remove(nodeId);
            }

            // 添加到新父节点
            node.parentId = newParentId;
            newParent.childrenIds.Add(nodeId);

            RebuildCache();
            return true;
        }

        /// <summary>
        /// 检查nodeId是否是potentialAncestorId的后代
        /// </summary>
        private bool IsDescendantOf(int nodeId, int potentialAncestorId)
        {
            var node = GetNodeById(nodeId);
            while (node != null && node.parentId >= 0)
            {
                if (node.parentId == potentialAncestorId)
                    return true;
                node = GetNodeById(node.parentId);
            }
            return false;
        }

        /// <summary>
        /// 获取节点的完整标签路径
        /// </summary>
        public string GetFullTagPath(int nodeId)
        {
            if (nodeId == 0) return ""; // 根节点没有路径

            var node = GetNodeById(nodeId);
            if (node == null) return "";

            var parts = new List<string>();
            while (node != null && node.id != 0)
            {
                parts.Insert(0, node.name);
                node = GetNodeById(node.parentId);
            }

            return string.Join(".", parts);
        }

        /// <summary>
        /// 重建缓存的标签列表
        /// </summary>
        public void RebuildCache()
        {
            _cachedTags.Clear();

            // 遍历所有非根节点，生成完整标签
            for (int i = 0; i < _treeNodes.Count; i++)
            {
                var node = _treeNodes[i];
                if (node.id == 0) continue; // 跳过根节点

                var fullPath = GetFullTagPath(node.id);
                if (!string.IsNullOrEmpty(fullPath))
                {
                    _cachedTags.Add(new GameplayTag(fullPath));
                }
            }

            // 按名称排序
            _cachedTags.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        /// <summary>
        /// 获取所有标签（用于下拉选择等）
        /// </summary>
        public List<string> GetAllTagNames()
        {
            var result = new List<string>();
            for (int i = 0; i < _cachedTags.Count; i++)
            {
                result.Add(_cachedTags[i].Name);
            }
            return result;
        }

        /// <summary>
        /// 检查标签是否存在
        /// </summary>
        public bool HasTag(string tagName)
        {
            for (int i = 0; i < _cachedTags.Count; i++)
            {
                if (_cachedTags[i].Name == tagName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 根据名称获取标签
        /// </summary>
        public GameplayTag GetTag(string tagName)
        {
            for (int i = 0; i < _cachedTags.Count; i++)
            {
                if (_cachedTags[i].Name == tagName)
                    return _cachedTags[i];
            }
            return GameplayTag.None;
        }

        /// <summary>
        /// 搜索标签（支持模糊匹配）
        /// </summary>
        public List<GameplayTag> SearchTags(string searchText)
        {
            var result = new List<GameplayTag>();
            if (string.IsNullOrEmpty(searchText))
            {
                result.AddRange(_cachedTags);
                return result;
            }

            var lowerSearch = searchText.ToLower();
            for (int i = 0; i < _cachedTags.Count; i++)
            {
                if (_cachedTags[i].Name.ToLower().Contains(lowerSearch))
                {
                    result.Add(_cachedTags[i]);
                }
            }
            return result;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Initialize();
            RebuildCache();
        }
#endif
    }
}
