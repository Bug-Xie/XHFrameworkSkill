using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using SkillEditor.Data;

namespace SkillEditor.Editor
{
    /// <summary>
    /// 游戏标签编辑器窗口
    /// 提供树形结构的标签管理界面
    /// </summary>
    public class GameplayTagsEditorWindow : EditorWindow
    {
        private GameplayTagsAsset _asset;
        private VisualElement _treeContainer;
        private TextField _searchField;
        private Label _statusLabel;
        private ScrollView _scrollView;

        private int _selectedNodeId = -1;
        private HashSet<int> _expandedNodes = new HashSet<int>();
        private string _searchText = "";
        private bool _clickHandledByItem = false;

        // 剪贴板数据
        private int _clipboardNodeId = -1;
        private bool _isCutOperation = false;

        [MenuItem("Tools/Gameplay Tags Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<GameplayTagsEditorWindow>();
            window.titleContent = new GUIContent("标签编辑器");
            window.minSize = new Vector2(400, 500);
        }

        public static void ShowWindow(GameplayTagsAsset asset)
        {
            var window = GetWindow<GameplayTagsEditorWindow>();
            window.titleContent = new GUIContent("标签编辑器");
            window.minSize = new Vector2(400, 500);
            window._asset = asset;
            window.RefreshTree();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 8;
            root.style.paddingRight = 8;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            // 标题
            var titleLabel = new Label("Gameplay Tags 编辑器")
            {
                style =
                {
                    fontSize = 18,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            root.Add(titleLabel);

            // 资源选择
            var assetField = new ObjectField("标签资源")
            {
                objectType = typeof(GameplayTagsAsset),
                value = _asset
            };
            assetField.RegisterValueChangedCallback(evt =>
            {
                _asset = evt.newValue as GameplayTagsAsset;
                _selectedNodeId = -1;
                _expandedNodes.Clear();
                if (_asset != null)
                {
                    _expandedNodes.Add(0);
                }
                RefreshTree();
            });
            root.Add(assetField);

            // 创建新资源按钮
            var createAssetButton = new Button(() => CreateNewAsset())
            {
                text = "创建新标签资源",
                style = { marginTop = 4, marginBottom = 4 }
            };
            root.Add(createAssetButton);

            // 生成标签代码按钮
            var generateCodeButton = new Button(() => GenerateTagCode())
            {
                text = "生成标签代码",
                style = { marginBottom = 8 }
            };
            root.Add(generateCodeButton);

            // 分隔线
            root.Add(CreateSeparator());

            // 搜索栏
            var searchContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginBottom = 8 }
            };
            _searchField = new TextField { style = { flexGrow = 1 } };
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RefreshTree();
            });
            var searchLabel = new Label("搜索:") { style = { alignSelf = Align.Center, marginRight = 4 } };
            searchContainer.Add(searchLabel);
            searchContainer.Add(_searchField);
            root.Add(searchContainer);

            // 提示标签
            var hintLabel = new Label("提示: 右键点击标签可进行更多操作")
            {
                style =
                {
                    fontSize = 10,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    marginBottom = 4
                }
            };
            root.Add(hintLabel);

            // 树形视图容器
            _scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1,
                    backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    paddingLeft = 4,
                    paddingRight = 4,
                    paddingTop = 4,
                    paddingBottom = 4
                }
            };
            _treeContainer = new VisualElement
            {
                style = { flexGrow = 1 }
            };
            _scrollView.Add(_treeContainer);
            root.Add(_scrollView);

            // 点击空白区域取消选中
            _scrollView.RegisterCallback<MouseDownEvent>(evt =>
            {
                _clickHandledByItem = false;
                _scrollView.schedule.Execute(() =>
                {
                    if (!_clickHandledByItem && _selectedNodeId != -1)
                    {
                        _selectedNodeId = -1;
                        RefreshTree();
                    }
                });
            }, TrickleDown.TrickleDown);

            // 空白区域右键菜单
            _scrollView.RegisterCallback<ContextClickEvent>(evt =>
            {
                if (!_clickHandledByItem)
                {
                    ShowBlankAreaContextMenu();
                    evt.StopPropagation();
                }
            });

            // 状态栏
            _statusLabel = new Label("")
            {
                style = { marginTop = 8, color = new Color(0.7f, 0.7f, 0.7f) }
            };
            root.Add(_statusLabel);

            // 底部按钮
            var bottomButtons = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, marginTop = 8 }
            };

            var expandAllButton = new Button(() => ExpandAll())
            {
                text = "全部展开",
                style = { flexGrow = 1, marginRight = 4 }
            };
            var collapseAllButton = new Button(() => CollapseAll())
            {
                text = "全部折叠",
                style = { flexGrow = 1 }
            };

            bottomButtons.Add(expandAllButton);
            bottomButtons.Add(collapseAllButton);
            root.Add(bottomButtons);

            // 初始化
            if (_asset != null)
            {
                _expandedNodes.Add(0);
                RefreshTree();
            }
        }

        private VisualElement CreateSeparator()
        {
            return new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.3f, 0.3f, 0.3f),
                    marginTop = 4,
                    marginBottom = 8
                }
            };
        }

        private void CreateNewAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "创建标签资源",
                "GameplayTagsAsset",
                "asset",
                "选择保存位置");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<GameplayTagsAsset>();
            asset.Initialize();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            _asset = asset;
            _expandedNodes.Clear();
            _expandedNodes.Add(0);
            RefreshTree();

            var assetField = rootVisualElement.Q<ObjectField>();
            if (assetField != null)
                assetField.value = asset;
        }

        private void RefreshTree()
        {
            _treeContainer.Clear();

            if (_asset == null)
            {
                _treeContainer.Add(new Label("请选择或创建一个标签资源")
                {
                    style = { color = new Color(0.6f, 0.6f, 0.6f) }
                });
                UpdateStatus();
                return;
            }

            _asset.Initialize();

            if (string.IsNullOrEmpty(_searchText))
            {
                var root = _asset.GetRootNode();
                BuildTreeUI(root, 0);
            }
            else
            {
                var results = _asset.SearchTags(_searchText);
                foreach (var tag in results)
                {
                    var item = CreateSearchResultItem(tag);
                    _treeContainer.Add(item);
                }

                if (results.Count == 0)
                {
                    _treeContainer.Add(new Label("没有找到匹配的标签")
                    {
                        style = { color = new Color(0.6f, 0.6f, 0.6f) }
                    });
                }
            }

            UpdateStatus();
        }

        private void BuildTreeUI(GameplayTagTreeNode node, int depth)
        {
            if (node.id != 0)
            {
                var item = CreateTreeItem(node, depth);
                _treeContainer.Add(item);
            }

            if (node.id == 0 || _expandedNodes.Contains(node.id))
            {
                foreach (var childId in node.childrenIds)
                {
                    var child = _asset.GetNodeById(childId);
                    if (child != null)
                    {
                        BuildTreeUI(child, node.id == 0 ? 0 : depth + 1);
                    }
                }
            }
        }

        private VisualElement CreateTreeItem(GameplayTagTreeNode node, int depth)
        {
            var container = new VisualElement
            {
                name = $"TagItem_{node.id}",
                userData = node.id,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = depth * 16,
                    paddingTop = 1,
                    paddingBottom = 1,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f)
                }
            };

            // 选中高亮
            if (_selectedNodeId == node.id)
            {
                container.style.backgroundColor = new Color(0.3f, 0.5f, 0.8f, 0.5f);
            }

            // 展开/折叠按钮
            var hasChildren = node.childrenIds.Count > 0;
            var foldButton = new Button(() =>
            {
                _clickHandledByItem = true;
                if (_expandedNodes.Contains(node.id))
                    _expandedNodes.Remove(node.id);
                else
                    _expandedNodes.Add(node.id);
                RefreshTree();
            })
            {
                text = hasChildren ? (_expandedNodes.Contains(node.id) ? "▼" : "▶") : "  ",
                style =
                {
                    width = 16,
                    height = 16,
                    paddingLeft = 0,
                    paddingRight = 0,
                    paddingTop = 0,
                    paddingBottom = 0,
                    marginRight = 2,
                    fontSize = 9,
                    backgroundColor = Color.clear,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    borderTopWidth = 0,
                    borderBottomWidth = 0
                }
            };
            if (!hasChildren)
                foldButton.SetEnabled(false);
            container.Add(foldButton);

            // 标签名称
            var nameLabel = new Label(node.name)
            {
                style =
                {
                    flexGrow = 1,
                    fontSize = 11,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            };
            container.Add(nameLabel);

            // 完整路径提示
            var fullPath = _asset.GetFullTagPath(node.id);
            container.tooltip = fullPath;

            // 左键点击选择
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    _clickHandledByItem = true;
                    _selectedNodeId = node.id;
                    RefreshTree();
                    evt.StopPropagation();
                }
            });

            // 双击重命名
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2 && evt.button == 0)
                {
                    _clickHandledByItem = true;
                    ShowRenameDialog(node.id);
                    evt.StopPropagation();
                }
            });

            // 右键菜单
            container.RegisterCallback<ContextClickEvent>(evt =>
            {
                _clickHandledByItem = true;
                _selectedNodeId = node.id;
                RefreshTree();
                ShowContextMenu(node);
                evt.StopPropagation();
            });

            return container;
        }

        #region 右键菜单

        /// <summary>
        /// 空白区域右键菜单
        /// </summary>
        private void ShowBlankAreaContextMenu()
        {
            if (_asset == null) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("添加根标签"), false, () => ShowAddTagDialog(0));

            if (_clipboardNodeId > 0)
            {
                menu.AddItem(new GUIContent("粘贴到根目录"), false, () => PasteTag(0));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴到根目录"));
            }

            menu.ShowAsContext();
        }

        private void ShowContextMenu(GameplayTagTreeNode node)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("添加子标签"), false, () => ShowAddTagDialog(node.id));
            menu.AddItem(new GUIContent("重命名"), false, () => ShowRenameDialog(node.id));
            menu.AddItem(new GUIContent("删除"), false, () => ShowDeleteConfirmDialog(node.id));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("复制"), false, () => CopyTag(node.id));
            menu.AddItem(new GUIContent("剪切"), false, () => CutTag(node.id));

            if (_clipboardNodeId > 0)
            {
                menu.AddItem(new GUIContent("粘贴"), false, () => PasteTag(node.id));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("粘贴"));
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("复制标签名"), false, () => CopyTagName(node.id));

            menu.ShowAsContext();
        }

        private void ShowAddTagDialog(int parentId)
        {
            var dialog = CreateInstance<TagInputDialog>();
            dialog.Initialize("添加子标签", "请输入新标签名称:", "", newName =>
            {
                if (!string.IsNullOrEmpty(newName))
                {
                    _asset.AddNode(newName, parentId);
                    _expandedNodes.Add(parentId);
                    EditorUtility.SetDirty(_asset);
                    AssetDatabase.SaveAssets();
                    RefreshTree();
                }
            });
            dialog.ShowModal();
        }

        private void ShowRenameDialog(int nodeId)
        {
            var node = _asset.GetNodeById(nodeId);
            if (node == null) return;

            var dialog = CreateInstance<TagInputDialog>();
            dialog.Initialize("重命名标签", "请输入新名称:", node.name, newName =>
            {
                if (!string.IsNullOrEmpty(newName) && newName != node.name)
                {
                    if (_asset.RenameNode(nodeId, newName, out var renamedPaths))
                    {
                        EditorUtility.SetDirty(_asset);
                        AssetDatabase.SaveAssets();

                        // 方案一：自动更新所有技能资产中的 tag 引用
                        if (renamedPaths != null && renamedPaths.Count > 0)
                        {
                            int count = GameplayTagRefactorTool.ApplyRename(renamedPaths);
                            if (count > 0)
                                Debug.Log($"[标签编辑器] 已自动更新 {count} 个技能资产的 Tag 引用");
                        }

                        // 方案三：自动重新生成 GameplayTagLibrary
                        GameplayTagCodeGenerator.AutoGenerate(_asset);

                        RefreshTree();
                    }
                }
            });
            dialog.ShowModal();
        }

        private void ShowDeleteConfirmDialog(int nodeId)
        {
            var node = _asset.GetNodeById(nodeId);
            if (node == null) return;

            var fullPath = _asset.GetFullTagPath(nodeId);
            var hasChildren = node.childrenIds.Count > 0;

            // 先收集将要删除的 tag 路径（删除前）
            var affectedIds = new System.Collections.Generic.List<int>();
            CollectDescendantIdsForDelete(nodeId, affectedIds);
            var removedPaths = new System.Collections.Generic.List<string>();
            foreach (var id in affectedIds)
            {
                var path = _asset.GetFullTagPath(id);
                if (!string.IsNullOrEmpty(path))
                    removedPaths.Add(path);
            }

            // 扫描引用
            var references = GameplayTagRefactorTool.FindReferences(removedPaths);

            // 构建确认消息
            var message = hasChildren
                ? $"确定要删除标签 \"{fullPath}\" 及其所有子标签吗？"
                : $"确定要删除标签 \"{fullPath}\" 吗？";

            if (references.Count > 0)
            {
                // 按资产分组显示引用
                var assetNames = new System.Collections.Generic.HashSet<string>();
                foreach (var r in references)
                    assetNames.Add(r.assetName);

                message += $"\n\n⚠ 以下 {assetNames.Count} 个技能资产仍在引用此标签:\n";
                int shown = 0;
                foreach (var name in assetNames)
                {
                    if (shown >= 10)
                    {
                        message += $"  ...等共 {assetNames.Count} 个\n";
                        break;
                    }
                    message += $"  • {name}\n";
                    shown++;
                }
                message += "\n删除后将自动从这些资产中移除对应的 Tag 引用。";
            }

            if (EditorUtility.DisplayDialog("删除标签", message, "删除", "取消"))
            {
                // 先从技能资产中移除引用
                if (references.Count > 0)
                {
                    int count = GameplayTagRefactorTool.ApplyRemove(removedPaths);
                    if (count > 0)
                        Debug.Log($"[标签编辑器] 已自动清理 {count} 个技能资产的 Tag 引用");
                }

                // 再删除 tag 定义
                _asset.RemoveNode(nodeId);
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();

                // 自动重新生成 GameplayTagLibrary
                GameplayTagCodeGenerator.AutoGenerate(_asset);

                if (_selectedNodeId == nodeId)
                    _selectedNodeId = -1;
                if (_clipboardNodeId == nodeId)
                    _clipboardNodeId = -1;

                RefreshTree();
            }
        }

        /// <summary>
        /// 收集节点及其所有子孙ID（用于删除前的引用扫描）
        /// </summary>
        private void CollectDescendantIdsForDelete(int nodeId, System.Collections.Generic.List<int> result)
        {
            result.Add(nodeId);
            var node = _asset.GetNodeById(nodeId);
            if (node == null) return;
            foreach (var childId in node.childrenIds)
                CollectDescendantIdsForDelete(childId, result);
        }

        private void CopyTag(int nodeId)
        {
            _clipboardNodeId = nodeId;
            _isCutOperation = false;
            Debug.Log($"已复制标签: {_asset.GetFullTagPath(nodeId)}");
        }

        private void CutTag(int nodeId)
        {
            _clipboardNodeId = nodeId;
            _isCutOperation = true;
            Debug.Log($"已剪切标签: {_asset.GetFullTagPath(nodeId)}");
        }

        private void PasteTag(int targetParentId)
        {
            if (_clipboardNodeId <= 0 || _asset == null) return;

            var sourceNode = _asset.GetNodeById(_clipboardNodeId);
            if (sourceNode == null)
            {
                _clipboardNodeId = -1;
                return;
            }

            // 检查是否粘贴到自己或自己的子节点下
            if (_clipboardNodeId == targetParentId || IsDescendantOf(targetParentId, _clipboardNodeId))
            {
                EditorUtility.DisplayDialog("错误", "不能将标签粘贴到自身或其子标签下", "确定");
                return;
            }

            if (_isCutOperation)
            {
                // 剪切 = 移动
                if (_asset.MoveNode(_clipboardNodeId, targetParentId))
                {
                    _expandedNodes.Add(targetParentId);
                    EditorUtility.SetDirty(_asset);
                    AssetDatabase.SaveAssets();
                    _clipboardNodeId = -1;
                    RefreshTree();
                    Debug.Log("标签已移动");
                }
            }
            else
            {
                // 复制 = 深度复制节点及其子节点
                CopyNodeRecursive(_clipboardNodeId, targetParentId);
                _expandedNodes.Add(targetParentId);
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
                RefreshTree();
                Debug.Log("标签已粘贴");
            }
        }

        private void CopyNodeRecursive(int sourceNodeId, int targetParentId)
        {
            var sourceNode = _asset.GetNodeById(sourceNodeId);
            if (sourceNode == null) return;

            // 创建新节点
            int newNodeId = _asset.AddNode(sourceNode.name, targetParentId);

            // 递归复制子节点
            foreach (var childId in sourceNode.childrenIds)
            {
                CopyNodeRecursive(childId, newNodeId);
            }
        }

        private bool IsDescendantOf(int nodeId, int potentialAncestorId)
        {
            var node = _asset.GetNodeById(nodeId);
            while (node != null && node.parentId >= 0)
            {
                if (node.parentId == potentialAncestorId)
                    return true;
                node = _asset.GetNodeById(node.parentId);
            }
            return false;
        }

        private void CopyTagName(int nodeId)
        {
            var fullPath = _asset.GetFullTagPath(nodeId);
            if (!string.IsNullOrEmpty(fullPath))
            {
                EditorGUIUtility.systemCopyBuffer = fullPath;
                Debug.Log($"已复制标签名: {fullPath}");
            }
        }

        #endregion

        private void GenerateTagCode()
        {
            if (_asset == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择或创建一个标签资源", "确定");
                return;
            }

            GameplayTagCodeGenerator.GenerateTagCodeFromEditor(_asset);
        }

        private VisualElement CreateSearchResultItem(GameplayTag tag)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = 2,
                    paddingBottom = 2,
                    paddingLeft = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f)
                }
            };

            var nameLabel = new Label(tag.Name)
            {
                style = { flexGrow = 1, fontSize = 11 }
            };
            container.Add(nameLabel);

            return container;
        }

        private void ExpandAll()
        {
            if (_asset == null) return;

            foreach (var node in _asset.TreeNodes)
            {
                if (node.childrenIds.Count > 0)
                    _expandedNodes.Add(node.id);
            }
            RefreshTree();
        }

        private void CollapseAll()
        {
            _expandedNodes.Clear();
            _expandedNodes.Add(0);
            RefreshTree();
        }

        private void UpdateStatus()
        {
            if (_asset == null)
            {
                _statusLabel.text = "";
                return;
            }

            var tagCount = _asset.CachedTags.Count;
            var selectedInfo = "";

            if (_selectedNodeId > 0)
            {
                var fullPath = _asset.GetFullTagPath(_selectedNodeId);
                selectedInfo = $" | 选中: {fullPath}";
            }

            _statusLabel.text = $"共 {tagCount} 个标签{selectedInfo}";
        }
    }

    /// <summary>
    /// 标签输入对话框 - 用于添加和重命名
    /// </summary>
    public class TagInputDialog : EditorWindow
    {
        private string _title;
        private string _message;
        private string _inputValue;
        private System.Action<string> _onConfirm;
        private TextField _inputField;

        public void Initialize(string title, string message, string defaultValue, System.Action<string> onConfirm)
        {
            _title = title;
            _message = message;
            _inputValue = defaultValue;
            _onConfirm = onConfirm;

            titleContent = new GUIContent(title);
            minSize = maxSize = new Vector2(300, 120);
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            // 提示信息
            root.Add(new Label(_message) { style = { marginBottom = 8 } });

            // 输入框
            _inputField = new TextField { value = _inputValue };
            _inputField.RegisterValueChangedCallback(evt => _inputValue = evt.newValue);
            _inputField.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return)
                    Confirm();
                else if (evt.keyCode == KeyCode.Escape)
                    Close();
            });
            root.Add(_inputField);

            // 按钮容器
            var buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 15
                }
            };

            var cancelButton = new Button(() => Close())
            {
                text = "取消",
                style = { width = 60, marginRight = 8 }
            };
            var confirmButton = new Button(() => Confirm())
            {
                text = "确定",
                style = { width = 60 }
            };

            buttonContainer.Add(cancelButton);
            buttonContainer.Add(confirmButton);
            root.Add(buttonContainer);

            // 聚焦并选中文本
            _inputField.schedule.Execute(() =>
            {
                _inputField.Focus();
                _inputField.SelectAll();
            });
        }

        private void Confirm()
        {
            _onConfirm?.Invoke(_inputValue?.Trim());
            Close();
        }
    }
}
