// Copyright (c) 2025 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;
using UnityEngine;

using PhantomBrigade.Data;

namespace EchKode.PBMods.HiResDrag
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CIViewCombatTimeline), "OnActionDrag")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civct_OnActionDragTranspiler1(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Translate drag offset to UI coordinate system.
			var floatFieldInfo = AccessTools.DeclaredField(typeof(UICallback), nameof(UICallback.argumentFloat));
			var floatMatch = new CodeMatch(OpCodes.Ldfld, floatFieldInfo);
			var call = CodeInstruction.Call(typeof(Patch), nameof(Patch.GetOffsetInUI));

			var cm = new CodeMatcher(instructions, generator);
			cm.Start();
			cm.MatchStartForward(floatMatch)
				.Advance(1)
				.Insert(call);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewCombatTimeline), "OnActionDrag")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civct_OnActionDragTranspiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Clamp action start time to turn end.
			var maxMatch = new CodeMatch(CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Max), new System.Type[]
			{
				typeof(float),
				typeof(float),
			}));
			var call = CodeInstruction.Call(typeof(Patch), nameof(Patch.ClampStartTime));

			var cm = new CodeMatcher(instructions, generator);
			cm.End();
			cm.MatchStartBackwards(maxMatch)
				.Advance(-1);
			var loadTimeTurnStart = cm.Instruction.Clone();
			cm.Advance(2)
				.InsertAndAdvance(loadTimeTurnStart)
				.InsertAndAdvance(call);

			return cm.InstructionEnumeration();
		}

		public static float GetOffsetInUI(float offset)
		{
			var cam = UILink.GetCamera();
			var root = CIViewLoader.ins.root;
			// (0,0) in UI space is center of screen. We want the offset in UI space which is an offset from the midpoint of the screen.
			var v = new Vector3(offset + cam.pixelWidth / 2f, 0f, 0f);
			v = UIHelper.GetScreenToUISpace(v, root.transform, cam);
			return v.x;
		}

		public static float ClampStartTime(float timeActionStart, float timeTurnStart)
		{
			var timeEnd = timeTurnStart + DataShortcuts.sim.maxActionTimePlacement;
			return Mathf.Min(timeActionStart, timeEnd);
		}
	}
}
