using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace AzWeaponLib
{
    public static class StatDispUtility
    {
        public static StringBuilder StringBuilderInit(string Text, int num)
        {
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + num.ToString());
            resultStringBuilder.AppendLine();
            return resultStringBuilder;
        }
        public static StringBuilder StringBuilderInit(string Text, float f)
        {
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + f.ToString());
            resultStringBuilder.AppendLine();
            return resultStringBuilder;
        }
        public static StringBuilder StringBuilderInit(string Text, bool b)
        {
            StringBuilder resultStringBuilder = new StringBuilder(Text);
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine();
            resultStringBuilder.AppendLine("StatsReport_BaseValue".Translate() + ": " + b.ToString());
            resultStringBuilder.AppendLine();
            return resultStringBuilder;
        }
    }
}
