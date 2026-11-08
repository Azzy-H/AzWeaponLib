using System;
using System.Collections.Generic;
using Verse;

namespace AzWeaponLib.Render
{
    /// <summary>
    /// 命名值注册表：只提供机制。具体有哪些键、取什么值，全部由实例层注册。
    /// </summary>
    public static class RenderValues
    {
        public delegate float Getter(Thing thing);

        private static readonly Dictionary<string, Getter> Getters =
            new Dictionary<string, Getter>(StringComparer.Ordinal);

        public static void Register(string key, Getter getter)
        {
            if (key.NullOrEmpty() || getter == null)
            {
                return;
            }
            Getters[key] = getter;
        }

        public static bool IsRegistered(string key)
        {
            return !key.NullOrEmpty() && Getters.ContainsKey(key);
        }

        public static float Get(Thing thing, string key)
        {
            if (thing == null || key.NullOrEmpty())
            {
                return 0f;
            }
            if (Getters.TryGetValue(key, out Getter? getter) && getter != null)
            {
                try
                {
                    return getter(thing);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce($"[AWL] value '{key}' threw: {ex}", key.GetHashCode());
                    return 0f;
                }
            }
            Log.ErrorOnce($"[AWL] unregistered value key '{key}'", key.GetHashCode());
            return 0f;
        }
    }

    /// <summary>
    /// 状态键贡献项：把运行期数据压成有限取值，供烘焙缓存做键。
    /// 抽象点：具体离散化规则由实例层提供。
    /// </summary>
    public abstract class StateContributor
    {
        public abstract int Get(Thing thing);
    }
}
