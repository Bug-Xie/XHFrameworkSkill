using UnityEngine;
using SkillEditor.Data;
using Spine;
using Spine.Unity;

namespace SkillEditor.Runtime
{
    /// <summary>
    /// 控制效果动画处理器 - 监听标签事件，控制动画播放
    /// 独立于 Buff，通过标签系统驱动
    ///
    /// 使用方式：挂载到角色上，配置 ASC 引用
    /// </summary>
    public class AnimationComponent : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private AbilitySystemComponent _asc;
         private SkeletonAnimation _animation;

        // 缓存的标签（通过 GameplayTagLibrary 引用，tag 重命名/删除时编译期即可发现）
        private GameplayTag _cachedStunTag = GameplayTagLibrary.Buff_DeBuff_Stun;

        // 当前状态
        [HideInInspector]
        public bool _isStunned;

        
        private void Awake()
        {
            // 自动获取组件
            if (_asc == null)
                _asc = GetComponent<Unit>().ownerASC;
            if (_animation == null)
                _animation = GetComponent<SkeletonAnimation>();
        }

        private void OnEnable()
        {
            if (_asc?.OwnedTags == null) return;

            // 注册标签事件监听
            _asc.OwnedTags.OnTagAdded += OnTagAdded;
            _asc.OwnedTags.OnTagRemoved += OnTagRemoved;
        }

        private void OnDisable()
        {
            if (_asc?.OwnedTags == null) return;

            // 取消注册
            _asc.OwnedTags.OnTagAdded -= OnTagAdded;
            _asc.OwnedTags.OnTagRemoved -= OnTagRemoved;
        }

        /// <summary>
        /// 标签添加时的回调
        /// </summary>
        private void OnTagAdded(GameplayTag tag)
        {
            if (!_isStunned&&tag == _cachedStunTag)
            {
                _isStunned = true;
                PlayAnimation("Stun",true);  
            }
        }

        /// <summary>
        /// 标签移除时的回调
        /// </summary>
        private void OnTagRemoved(GameplayTag tag)
        {
            if (_isStunned&& tag == _cachedStunTag&&!_asc.OwnedTags.HasTag(_cachedStunTag))
            {
                _isStunned = false;
                PlayAnimation("Stand",true);    
            }
        }

        public void PlayAnimation(string name,bool loop)
        {
            // 检查当前是否已经在播放这个动画
            var current = _animation.AnimationState.GetCurrent(0);
            if (current != null && current.Animation.Name == name)
                return;

            _animation.AnimationState.SetAnimation(0,name,loop);
        }


    }
}
