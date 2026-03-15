using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AzWeaponLib.HediffTurret
{
    [HarmonyPatch]
    internal static class Patch_Command
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Command_Ability), nameof(Command_Ability.GizmoOnGUI))]
        private static void Postfix_GizmoOnGUI(Command_Ability __instance, ref GizmoResult __result, Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, __instance.GetWidth(maxWidth), 75f);
            PostfixCommon(__instance, ref __result, rect);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Command), nameof(Command.GizmoOnGUIShrunk))]
        private static void Postfix_GizmoOnGUIShrunk(Command __instance, ref GizmoResult __result, Vector2 topLeft, float size, GizmoRenderParms parms)
        {
            Command_Ability commandAbility = __instance as Command_Ability;
            if (commandAbility == null)
            {
                return;
            }

            Rect rect = new Rect(topLeft.x, topLeft.y, size, size);
            PostfixCommon(commandAbility, ref __result, rect);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Command_Ability), nameof(Command_Ability.Tooltip), MethodType.Getter)]
        private static void Postfix_Tooltip(Command_Ability __instance, ref string __result)
        {
            Ability ability = __instance.Ability;
            if (ability == null || ability.pawn == null || !ability.def.HasModExtension<TurretAbility>())
            {
                return;
            }

            Hediff_AbilityTurret turretHediff = TryGetTurretHediff(ability.pawn, ability);
            if (turretHediff == null)
            {
                return;
            }

            __result += "\n\n" + "AWL_AbilityToggle_Tips".Translate();
            __result += "\n" + GetCurrentStatusLabel(turretHediff, ability);
        }

        private static void PostfixCommon(Command_Ability command, ref GizmoResult result, Rect rect)
        {
            Ability ability = command.Ability;
            if (ability == null || ability.pawn == null || !ability.def.HasModExtension<TurretAbility>())
            {
                return;
            }

            Hediff_AbilityTurret turretHediff = TryGetTurretHediff(ability.pawn, ability);
            if (turretHediff != null)
            {
                DrawAutoToggleBadge(command, turretHediff, ability, rect);
            }

            if (result.State != GizmoState.OpenedFloatMenu)
            {
                return;
            }

            Event currentEvent = Event.current;
            if (currentEvent == null || turretHediff == null)
            {
                return;
            }

            List<FloatMenuOption> options = command.RightClickFloatMenuOptions.ToList();
            FloatMenuOption toggleOption = new FloatMenuOption(GetToggleLabel(turretHediff, ability), delegate
            {
                ToggleAuto(turretHediff, ability);
            });

            currentEvent.Use();
            result = new GizmoResult(GizmoState.Clear);

            if (options.Count == 0)
            {
                toggleOption.action();
                return;
            }

            options.Add(toggleOption);
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void DrawAutoToggleBadge(Command_Ability command, Hediff_AbilityTurret turretHediff, Ability ability, Rect rect)
        {
            if (command.Disabled)
            {
                return;
            }

            float badgeSize = Mathf.Min(24f, Mathf.Min(rect.width, rect.height));
            Rect position = new Rect(rect.x + rect.width - badgeSize, rect.y, badgeSize, badgeSize);
            Texture2D image = turretHediff.IsEnabled(ability) ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            GUI.DrawTexture(position, image);
        }

        private static Hediff_AbilityTurret TryGetTurretHediff(Pawn pawn, Ability ability)
        {
            TurretAbility turretAbility = ability.def.GetModExtension<TurretAbility>();
            HediffDef turretHediffDef = turretAbility?.ResolvedHediffDef ?? AWL_DefOf.AWL_AbilityTurret;
            return pawn.health.hediffSet.GetFirstHediffOfDef(turretHediffDef) as Hediff_AbilityTurret;
        }

        private static string GetCurrentStatusLabel(Hediff_AbilityTurret turretHediff, Ability ability)
        {
            return turretHediff.IsEnabled(ability) ? "Enabled".Translate() : "Disabled".Translate();
        }

        private static string GetToggleLabel(Hediff_AbilityTurret turretHediff, Ability ability)
        {
            string actionLabel = turretHediff.IsEnabled(ability) ? "AWL_DisableAutoCast".Translate() : "AWL_EnableAutoCast".Translate();
            return actionLabel;
        }

        private static void ToggleAuto(Hediff_AbilityTurret turretHediff, Ability ability)
        {
            bool enabledBefore = turretHediff.IsEnabled(ability);
            turretHediff.ToggleAuto(ability.def);
            (enabledBefore ? SoundDefOf.Checkbox_TurnedOff : SoundDefOf.Checkbox_TurnedOn).PlayOneShotOnCamera();
        }
    }
}