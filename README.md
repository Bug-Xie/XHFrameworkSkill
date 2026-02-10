## 🎯 <span style="color: red; font-size: 20px; font-weight: bold;">大家感兴趣的话可以进入QQ群一起交流学习！</span>

## 🎯 <span style="color: red; font-size: 22px; font-weight: bold;">QQ交流群：621790749</span>

## 🎯 <span style="color: red; font-size: 26px; font-weight: bold;">Unity版本：2022.3.62f2c1  示例场景在SampleScene里面  分辨率1366*768 wasd控制主角，一个技能一个技能去演示</span>

# XHFrameworkSkill - Unity GAS技能框架与技能节点编辑器  
![alt text](./images/image36.png)  
### 编辑器概览    
![alt text](./images/image-2.png)

## 📖 目录
[项目简介](#项目简介) | [为什么做这个技能模块](#为什么做这个技能模块) | [框架概述](#框架概述) | [技能编辑器详解](#技能编辑器详解) | [标签编辑器](#标签编辑器) | [技能示例分析](#技能示例分析) | [数值系统](#数值系统) | [仓库内容](#仓库内容) | [交流学习](#交流学习)

---

## 🎯 <span style="color: red; font-size: 18px; font-weight: bold;">项目简介</span>

本组织还包含其他仓库：
- **技能框架**：基于GAS模式的技能系统
- **Unity客户端框架**：基础游戏框架
- **资源服务器**：资源管理和分发
- **工具集**：开发辅助工具
- **常用Package包**：Unity常用插件和库

---

## ❓ <span style="color: red; font-size: 18px; font-weight: bold;">为什么做这个技能模块</span>

![alt text](./images/image.png)

如上图，这是我在上家公司做的技能模块和树形技能编辑器。

### 传统方式的痛点
以往技能配置通常通过表格实现，存在以下问题：
- 各个表之间跳转复杂，不够直观
- 配置修改麻烦，难以维护

### 传统事件驱动的问题
在传统树形结构编辑器中：
- **技能/子弹/Buff/放置物**：都通过触发器驱动
- **核心思想**：在**何时**触发**什么效果**（Trigger + Effect）
- **事件系统**：在各个模块添加大量触发事件（技能内事件和全局事件）
- **局限性**：依赖于树形结构，一对多的思想做复杂技能很困难，需要添加很多特殊机制

### GAS模式的启发
后来了解了虚幻引擎的GAS模式：
- **标签系统**：避免了过多的事件情况
- **节点图**：天然的图形化结构
- **灵活性**：多对多的连接方式更加灵活

但是虚幻GAS也有不足：制作一个技能需要配置很多资产，在不同资产间跳转。

### 我们的解决方案
因此，我开发了这款技能编辑器，特点是：
- **一体化设计**：配置一个技能只有一个节点图
- **全面覆盖**：技能产生的所有效果、子弹、Buff等都可以在一个图中表现
- **直观可视**：所有逻辑关系一目了然

> 不了解虚幻GAS？可以参考[中文文档](https://github.com/BillEliot/GASDocumentation_Chinese?tab=readme-ov-file#concepts-gt)

---

## 🏗️ <span style="color: red; font-size: 18px; font-weight: bold;">框架概述</span>

框架模块遵循GAS的核心思想：
- **AbilitySystemComponent**：能力系统组件
- **Attribute**：属性系统
- **Ability**：技能能力
- **Effect**：效果系统
- **Cue**：表现系统

了解了技能编辑器，也就基本了解了整个技能框架。

---

## 🎨 <span style="color: red; font-size: 18px; font-weight: bold;">技能编辑器详解</span>

### 1. 技能资源栏
![alt text](./images/image-3.png)
- 左侧区域显示技能文件和文件夹
- 支持常规操作：新建、剪切、粘贴、重命名等

### 2. 技能编辑区域
![alt text](./images/image-4.png)
- 中间主编辑区
- 右键可唤出节点选择菜单

### 3. 节点类型
![alt text](./images/image-6.png)

目前有五种节点类型：

#### 技能节点
- 包含ID和标签配置
- 支持消耗、动画、CD等配置
- 提供事件激活输出点

#### 效果节点
![alt text](./images/image-7.png)
- 分为瞬时、持续、永久三种类型
- 主要用于属性和状态的修改

#### 任务节点
![alt text](./images/image21.png)
- 用于触发各种行为
- 动画节点是一个Timeline编辑器，方便策划和程序编辑

#### 条件节点
![alt text](./images/image22.png)
- 对情况进行分析
- 根据条件输出不同的结果

#### 表现节点
![alt text](./images/image-10.png)
- 处理美术表现：特效、音效、飘字等

### 4. 节点属性面板
![alt text](./images/image-5.png)
- 右侧显示选中节点的属性
- 可配置节点具体参数

### 设计理念
- **高度灵活**：所有节点都可以互相链接
- **复用性强**：所有节点设计为可复用组件

---

## 🏷️ <span style="color: red; font-size: 18px; font-weight: bold;">标签编辑器</span>

![alt text](./images/image-12.png)

这是一个树形结构的标签编辑器，特点包括：
- 直观的树形结构管理
- 支持常用功能：添加标签、剪切、复制、粘贴等

---

## 🎮 <span style="color: red; font-size: 18px; font-weight: bold;">技能示例分析</span>

### 5.1 横扫：对前方扇形区域造成伤害
![alt text](./images/image-11.png)

> 注意：由于资源来自上家公司（使用多皮肤换装系统），事件轴动画预览区域显示可能不正确。

#### 配置步骤：
1. **添加技能CD标签**

   ![alt text](./images/image-13.png)
   
2. **创建技能文件**

   ![alt text](./images/image23.png)

3. **创建技能节点**
 
     ![alt text](./images/image24.png)

   - 填写技能ID

   ![alt text](./images/image-14.png)

   - 将CD标签添加到"激活时阻止"

4. **创建消耗节点**

   ![alt text](./images/image25.png)
   
   - 连接技能消耗输出点
   - 可配置多个消耗项，释放技能前自动检测

5. **创建CD节点**

   ![alt text](./images/image26.png)

   - 连接技能冷却输出点
   - 配置"激活时授予标签"为CD标签
   - 支持普通CD和充能CD

6. **创建动画节点**
   ![alt text](./images/image-17.png)
   - 连接技能动画节点
   - 可添加轨道，轨道链接效果
   - 支持时间线拖拽，逐帧查看

7. **创建技能结束和特效节点**

   ![alt text](./images/image27.png)

   - 连接动画节点

8. **创建搜索目标节点 → 伤害 → 飘字**

    ![alt text](./images/image28.png)

   - 节点属性直观易懂

#### 技能流程： 释放技能-》技能tag检测-》技能cd检测-》消耗检测-》释放-》消耗-》cd-》动画-》范围检测-》伤害-》 飘字

### 5.2 流血：攻击到的敌人添加流血Buff

   ![alt text](./images/image29.png)

- **最大叠加层数**：3层
- **叠加规则**：刷新时间，周期不重置
- **过期处理**：清理全部堆叠
- **溢出处理**：拒绝新应用

#### 配置要点：
1. Buff节点提供多种输出点：
   - 初始效果
   - 每周期执行
   - 刷新时
   - 完成效果
   - 全部移除后
   - 溢出时

2. 可链接不同节点：特效、范围搜索、伤害等

3. Buff节点拥有五种不同标签（参考虚幻GAS Effect系统）

### 5.3 践踏：对区域内眩晕

   ![alt text](./images/image30.png)
.
1. 在标签编辑器添加眩晕标签

   ![alt text](./images/image-18.png)
。
2. 在Buff节点标签处添加眩晕标签


### 5.4 旋风斩：技能期间对周围伤害

   ![alt text](./images/image31.png)


- **实现方式1**：作为Buff实现
- **实现方式2**：作为通用效果实现（推荐）

#### 推荐配置：
- 创建通用效果，类型为永久
- 在节点中勾选"技能结束后取消"
- 技能结束，效果也随之结束

#### 替代方案：
- 使用标签系统
- 在"持续所需标签"内填写技能标签
- 标签移除时，效果自动取消

### 5.5 回血：回复血量

   ![alt text](./images/image32.png)

- 简单实现：链接恢复节点即可

### 5.6 火球术：投掷物技能

   ![alt text](./images/image33.png)

- **节点类型**：投掷物节点
- **发射来源**：可配置
- **目标来源**：可配置
- **飞行模式**：直线/曲线/偏移/飞越
- **碰撞设置**：穿透等
- **触发点**：碰撞时/完成时/到达目标时

### 5.7 三火球
![alt text](./images/image-19.png)

- 与火球术类似，可共用伤害节点
- 三个火球各自配置偏移属性

### 5.8 神罗天征/万象天引：位移Buff

   ![alt text](./images/image34.png)

- **核心机制**：带位移的Buff
- **内置配置**：提供多种位移配置选项
- **可扩展性**：支持自定义位移方式

### 5.9 急速：移速增加Buff

   ![alt text](./images/image35.png)

- **简单实现**：在Buff的属性管理器中添加移速属性即可

### 5.10 被动回血：三秒内没有受到伤害自动回血

   ![alt text](./images/image-20.png)

#### 配置步骤：
1. **创建被动回血的3秒CD标签**
2. **被动激活时添加永久回血Buff**
   - Buff的"打断持续标签"中增加CD标签
3. **技能添加受到伤害时事件**
   - 接收到事件后触发冷却节点
   - 冷却节点自动添加CD标签
   - Buff检测到CD标签，自动结束
   - 三秒后冷却节点结束，移除CD标签
   - 冷却节点结束时效果链接到Buff节点，重新添加Buff



#### 工作原理：

---

## 🔢 <span style="color: red; font-size: 18px; font-weight: bold;">数值系统</span>

节点中的数值配置支持四种方式：

1. **具体值**：直接填写固定数值
2. **公式**：使用公式计算数值，支持技能等级等变量
3. **MMC**（Modifier Magnitude Calculation）：自定义计算逻辑
4. **上下文**：从外部数据源（如配置表）获取数值

### 应用场景：
- **技能等级参数**：使用公式或MMC处理不同等级的数值差异
- **策划配表**：使用上下文方式从配置表读取数据
- **复杂计算**：使用MMC实现自定义计算逻辑

---

## 📁 <span style="color: red; font-size: 18px; font-weight: bold;">仓库内容</span>

本组织还包含以下内容：

1. **技能框架**：基于GAS模式的技能系统
2. **Unity客户端框架**：基础游戏框架
3. **资源服务器**：资源管理和分发
4. **工具集**：开发辅助工具
5. **常用Package包**：Unity常用插件和库

---

## 👥 <span style="color: red; font-size: 18px; font-weight: bold;">交流学习</span>

欢迎大家加入QQ群一起交流学习！

**QQ交流群：621790749**

**示例场景在SampleScene里面**

---
本组织还有 XHFramework - Unity 游戏客户端框架：https://github.com/XH-Unity/XHFrameworkClient

## 📄 许可证
（请根据实际情况添加许可证信息）

---

## 🚀 快速开始
（请根据实际情况添加使用说明）

---

**最后更新**：请根据实际情况更新日期  
**版本**：请根据实际情况填写版本号
