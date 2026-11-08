using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace AzWeaponLib.Render
{
    // ───────────────────────── 解析期收集 ─────────────────────────

    /// <summary>解析期收集绘制数据的出口。</summary>
    public interface IDrawSink
    {
        int Count { get; }
        void Add(in DrawData data);
    }

    internal sealed class CollectSink : IDrawSink
    {
        private readonly List<DrawData> list;

        public CollectSink(List<DrawData> list)
        {
            this.list = list;
        }

        public int Count => list.Count;

        public void Add(in DrawData data)
        {
            list.Add(data);
        }
    }

    internal static class DrawBufferPool
    {
        [ThreadStatic]
        private static List<DrawData>? buffer;

        public static List<DrawData> Rent()
        {
            List<DrawData> list = buffer ??= new List<DrawData>(64);
            list.Clear();
            return list;
        }

        public static void Return(List<DrawData> list)
        {
            list.Clear();
        }
    }

    // ───────────────────────── 提交 ─────────────────────────

    /// <summary>烘焙产物的提交出口，是宿主差异的唯一落点。</summary>
    public interface IRenderSink
    {
        void Submit(in DrawData data, in Matrix4x4 root);
    }

    /// <summary>实时绘制（物品、装备）。</summary>
    public readonly struct RealtimeSink : IRenderSink
    {
        public void Submit(in DrawData data, in Matrix4x4 root)
        {
            Graphics.DrawMesh(data.mesh, data.Matrix(in root), data.mat, 0);
        }
    }

    // ───────────────────────── 烘焙 ─────────────────────────

    public interface IBaker
    {
        BakedBatch Bake(LayerNode root, in FrameCtx ctx);
    }

    /// <summary>烘焙产物：已排序、只读、可跨帧共享。</summary>
    public sealed class BakedBatch
    {
        public static readonly BakedBatch Empty = new BakedBatch(Array.Empty<DrawData>());

        private readonly DrawData[] data;

        internal BakedBatch(DrawData[] data)
        {
            this.data = data;
        }

        public int Count => data.Length;

        /// <summary>排序后的首条（order 最小）材质，供外部单材质查询使用。</summary>
        public Material? BaseMaterial => data.Length > 0 ? data[0].mat : null;

        /// <summary>泛型提交，避免把 struct 宿主装箱成接口。</summary>
        public void Submit<TSink>(in TSink sink, in Matrix4x4 root) where TSink : IRenderSink
        {
            for (int i = 0; i < data.Length; i++)
            {
                sink.Submit(in data[i], in root);
            }
        }
    }

    /// <summary>数据烘焙：把节点树解析成绘制数据数组并排序。</summary>
    public sealed class DataBaker : IBaker
    {
        public static readonly DataBaker Instance = new DataBaker();

        private static readonly Comparison<DrawData> Order = CompareOrder;

        public BakedBatch Bake(LayerNode root, in FrameCtx ctx)
        {
            List<DrawData> list = DrawBufferPool.Rent();
            CollectSink sink = new CollectSink(list);
            root.Resolve(in ctx, Pose.Identity, sink);

            DrawData[] arr = list.ToArray();
            DrawBufferPool.Return(list);

            if (arr.Length == 0)
            {
                return BakedBatch.Empty;
            }
            if (arr.Length > 1)
            {
                Array.Sort(arr, Order);
            }
            return new BakedBatch(arr);
        }

        private static int CompareOrder(DrawData a, DrawData b)
        {
            int c = a.layer.CompareTo(b.layer);
            if (c != 0)
            {
                return c;
            }
            c = a.order.CompareTo(b.order);
            return c != 0 ? c : a.seq.CompareTo(b.seq);
        }
    }

    // ───────────────────────── 烘焙缓存 ─────────────────────────

    internal readonly struct BakeKey : IEquatable<BakeKey>
    {
        public readonly ThingDef def;
        public readonly RenderStage stage;
        public readonly int stateKey;
        public readonly Vector2 baseSize;
        public readonly bool baseFlip;

        /// <summary>层计划标识：不同计划（基座合并 / 仅额外层）不得共用同一批次。</summary>
        public readonly int planId;

        public BakeKey(in FrameCtx ctx, LayerNode plan)
        {
            def = ctx.thing.def;
            stage = ctx.stage;
            stateKey = ctx.stateKey;
            baseSize = ctx.baseSize;
            baseFlip = ctx.baseFlip;
            planId = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(plan);
        }

        public bool Equals(BakeKey other)
        {
            return planId == other.planId
                && def == other.def
                && stage == other.stage
                && stateKey == other.stateKey
                && baseFlip == other.baseFlip
                && baseSize.x == other.baseSize.x
                && baseSize.y == other.baseSize.y;
        }

        public override bool Equals(object? obj)
        {
            return obj is BakeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            int h = planId;
            h = Gen.HashCombineInt(h, def.shortHash);
            h = Gen.HashCombineInt(h, (int)stage);
            h = Gen.HashCombineInt(h, stateKey);
            h = Gen.HashCombineInt(h, baseFlip ? 1 : 0);
            h = Gen.HashCombineInt(h, baseSize.x.GetHashCode());
            h = Gen.HashCombineInt(h, baseSize.y.GetHashCode());
            return h;
        }
    }

    /// <summary>
    /// 烘焙缓存。容量固定，按最近最少使用淘汰；淘汰只影响性能，不影响正确性。
    /// </summary>
    public static class BakeCache
    {
        private const int Capacity = 512;

        private static readonly Dictionary<BakeKey, Entry> Cache = new Dictionary<BakeKey, Entry>();
        private static readonly LinkedList<BakeKey> Lru = new LinkedList<BakeKey>();

        private struct Entry
        {
            public BakedBatch batch;
            public LinkedListNode<BakeKey> node;
        }

        public static int Count => Cache.Count;

        public static BakedBatch Get(in FrameCtx ctx, LayerNode plan, IBaker baker)
        {
            BakeKey key = new BakeKey(in ctx, plan);

            if (Cache.TryGetValue(key, out Entry entry))
            {
                Lru.Remove(entry.node);
                Lru.AddFirst(entry.node);
                return entry.batch;
            }

            BakedBatch batch = baker.Bake(plan, in ctx);

            LinkedListNode<BakeKey> node = Lru.AddFirst(key);
            Cache[key] = new Entry { batch = batch, node = node };

            while (Cache.Count > Capacity)
            {
                LinkedListNode<BakeKey>? last = Lru.Last;
                if (last == null)
                {
                    break;
                }
                Lru.RemoveLast();
                Cache.Remove(last.Value);
            }

            return batch;
        }

        public static void Clear()
        {
            Cache.Clear();
            Lru.Clear();
        }
    }
}
