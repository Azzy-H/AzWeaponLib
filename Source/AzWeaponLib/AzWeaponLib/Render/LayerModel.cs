using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AzWeaponLib.Render
{
    /// <summary>
    /// 层树节点。解析由节点自身完成（多态），解析器不枚举节点类型。
    /// 字段均为可变值类型/引用类型，可直接由 XML 反序列化。
    /// 本类只描述结构（拓扑与变换），不含任何具体内容/判定实现。
    /// </summary>
    public abstract class LayerNode
    {
        public Vector3 offset = Vector3.zero;
        public Vector2 scale = Vector2.one;
        public float rotation;
        public float layer;
        public int order;
        public RenderStage stage = RenderStage.Both;
        public LayerCondition? condition;

        /// <summary>本节点的局部姿态（由 offset/rotation/scale 组合）。</summary>
        public Pose LocalPose => new Pose(offset, rotation, scale);

        public abstract void Resolve(in FrameCtx ctx, in Pose parent, IDrawSink sink);

        public bool Active(in FrameCtx ctx)
        {
            if ((stage & ctx.stage) == 0)
            {
                return false;
            }
            return condition == null || condition.Eval(in ctx);
        }
    }

    /// <summary>叶子：产出一条绘制数据。</summary>
    public sealed class LeafLayerNode : LayerNode
    {
        public LayerContent? content;

        public override void Resolve(in FrameCtx ctx, in Pose parent, IDrawSink sink)
        {
            if (content == null || !Active(in ctx))
            {
                return;
            }

            Pose p = Pose.Compose(parent, LocalPose);
            content.Emit(in ctx, in p, order, layer, sink);
        }
    }

    /// <summary>容器：只组合变换并转发子节点，自身不产出绘制。</summary>
    public sealed class GroupLayerNode : LayerNode
    {
        public List<LayerNode> children = new List<LayerNode>();

        public override void Resolve(in FrameCtx ctx, in Pose parent, IDrawSink sink)
        {
            if (!Active(in ctx))
            {
                return;
            }

            Pose p = Pose.Compose(parent, LocalPose);
            for (int i = 0; i < children.Count; i++)
            {
                children[i].Resolve(in ctx, in p, sink);
            }
        }
    }

    /// <summary>重复器（列表语义）：按命名值重复子节点，沿 step 等距排布。</summary>
    public sealed class RepeatLayerNode : LayerNode
    {
        public LayerNode? child;
        public string countKey = "";
        public float countScale = 1f;
        public int max = 32;
        public Vector3 step;

        public override void Resolve(in FrameCtx ctx, in Pose parent, IDrawSink sink)
        {
            if (child == null || !Active(in ctx))
            {
                return;
            }

            int n = Mathf.Clamp(
                (int)(RenderValues.Get(ctx.thing, countKey) * countScale),
                0,
                max);
            if (n <= 0)
            {
                return;
            }

            Pose p = Pose.Compose(parent, LocalPose);
            for (int i = 0; i < n; i++)
            {
                child.Resolve(in ctx, in p, sink);
                p = new Pose(p.pos + step, p.rot, p.scale);
            }
        }
    }

    /// <summary>节点可见性判定。抽象点：具体判定由实例层提供。</summary>
    public abstract class LayerCondition
    {
        public abstract bool Eval(in FrameCtx ctx);
    }

    /// <summary>叶子节点实际画什么。抽象点：具体内容由实例层提供。</summary>
    public abstract class LayerContent
    {
        public abstract void Emit(in FrameCtx ctx, in Pose pose, int order, float layer, IDrawSink sink);
    }
}
