using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using HarmonyLib;
using LudeonTK;
using Microsoft.CodeAnalysis;
using RimWorld;
using UnityEngine;
using VFM_VanillaFireModes.Comps;
using VFM_VanillaFireModes.ModSettingUI;
using VFM_VanillaFireModes.ModSettingUI.WeaponProfileSetting;
using VFM_VanillaFireModes.Settings;
using VFM_VanillaFireModes.Settings.CustomWeaponProfile;
using VFM_VanillaFireModes.Stat;
using VFM_VanillaFireModes.Utilities;
using Verse;
using Verse.Sound;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETFramework,Version=v4.7.2", FrameworkDisplayName = ".NET Framework 4.7.2")]
[assembly: AssemblyCompany("VFM_VanillaFireModes")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+5bcab05758778252e58dad00fa815fbd11481ae7")]
[assembly: AssemblyProduct("VFM_VanillaFireModes")]
[assembly: AssemblyTitle("VFM_VanillaFireModes")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableAttribute : Attribute
	{
		public readonly byte[] NullableFlags;

		public NullableAttribute(byte P_0)
		{
			NullableFlags = new byte[1] { P_0 };
		}

		public NullableAttribute(byte[] P_0)
		{
			NullableFlags = P_0;
		}
	}
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;

		public NullableContextAttribute(byte P_0)
		{
			Flag = P_0;
		}
	}
}
[StaticConstructorOnStartup]
public static class VFM_StatPatcher
{
	static VFM_StatPatcher()
	{
		InjectPart(StatDefOf.ShootingAccuracyPawn, new VFM_FireMode_ShootingAccuracyPawnPart());
		InjectPart(StatDefOf.RangedCooldownFactor, new VFM_FireMode_RangedCooldownFactorPart());
		InjectPart(StatDefOf.AimingDelayFactor, new VFM_FireMode_AimingDelayFactorPart());
	}

	private static void InjectPart<T>(StatDef stat, T part) where T : VFM_FireMode_StatPart, new()
	{
		if (stat.parts == null)
		{
			stat.parts = new List<StatPart>();
		}
		if (!GenCollection.Any<StatPart>(stat.parts, (Predicate<StatPart>)((StatPart p) => p is T)))
		{
			stat.parts.Add((StatPart)(object)part);
		}
	}
}
namespace VFM_VanillaFireModes
{
	public class VanillaFireModes : Mod
	{
		public static VanillaFireModesModSetting settings;

		public VanillaFireModes(ModContentPack contentPack)
			: base(contentPack)
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			settings = ((Mod)this).GetSettings<VanillaFireModesModSetting>();
			Log.Message("<color=cyan>[VanillaFireModes]</color> is loaded!");
			new Harmony("Aliza.VanillaFireModes").PatchAll();
		}

		public override string SettingsCategory()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			return TaggedString.op_Implicit(Translator.Translate("VFM_ModTitle"));
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			VFM_SettingsWindowContents.SettingsWindowContents(inRect, ref settings);
		}
	}
	public static class VFM_DevMenu
	{
		private const string Category = "VFM";

		private static bool IsValidTarget(Pawn p)
		{
			if (p != null)
			{
				if (!p.RaceProps.Humanlike)
				{
					return p.RaceProps.ToolUser;
				}
				return true;
			}
			return false;
		}

		[DebugAction(/*Could not decode attribute arguments.*/)]
		public static void ResetAllVFMComps()
		{
			IEnumerable<Pawn> enumerable = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive.Where(IsValidTarget);
			int num = 0;
			foreach (Pawn item in enumerable)
			{
				VFM_PawnCompFireMode comp = ((ThingWithComps)item).GetComp<VFM_PawnCompFireMode>();
				if (comp != null)
				{
					comp.curMode = VFM_FireMode.Default;
					comp.curEnableAutoSelection = false;
					num++;
				}
			}
			Log.Message($"<color=cyan>[VanillaFireModes]</color>Reset VFM Comps to default from {num} Pawns");
		}
	}
}
namespace VFM_VanillaFireModes.Utilities
{
	public static class FireModeDB
	{
		public static VanillaFireModesModSetting Settings => VanillaFireModes.settings;

		public static float GetWarmup(VFM_FireMode mode, string? weaponDefName)
		{
			if (weaponDefName != null && Settings.CustomWeaponProfiles.TryGetValue(weaponDefName, out VFM_WeaponProfile value))
			{
				return mode switch
				{
					VFM_FireMode.Precision => value.Precision.warmupMultiplier, 
					VFM_FireMode.Burst => value.Burst.warmupMultiplier, 
					VFM_FireMode.Suppression => value.Suppression.warmupMultiplier, 
					VFM_FireMode.Default => value.Default.warmupMultiplier, 
					_ => 1f, 
				};
			}
			return mode switch
			{
				VFM_FireMode.Precision => Settings.precisionWarmup, 
				VFM_FireMode.Burst => Settings.burstWarmup, 
				VFM_FireMode.Suppression => Settings.suppressionWarmup, 
				VFM_FireMode.Default => Settings.defaultWarmup, 
				_ => 1f, 
			};
		}

		public static float GetCooldown(VFM_FireMode mode, string? weaponDefName)
		{
			if (weaponDefName != null && Settings.CustomWeaponProfiles.TryGetValue(weaponDefName, out VFM_WeaponProfile value))
			{
				return mode switch
				{
					VFM_FireMode.Precision => value.Precision.cooldownMultiplier, 
					VFM_FireMode.Burst => value.Burst.cooldownMultiplier, 
					VFM_FireMode.Suppression => value.Suppression.cooldownMultiplier, 
					VFM_FireMode.Default => value.Default.cooldownMultiplier, 
					_ => 1f, 
				};
			}
			return mode switch
			{
				VFM_FireMode.Precision => Settings.precisionCooldown, 
				VFM_FireMode.Burst => Settings.burstCooldown, 
				VFM_FireMode.Suppression => Settings.suppressionCooldown, 
				VFM_FireMode.Default => Settings.defaultCooldown, 
				_ => 1f, 
			};
		}

		public static float GetAccuracy(VFM_FireMode mode, string? weaponDefName)
		{
			if (weaponDefName != null && Settings.CustomWeaponProfiles.TryGetValue(weaponDefName, out VFM_WeaponProfile value))
			{
				return mode switch
				{
					VFM_FireMode.Precision => value.Precision.accuracyMultiplier, 
					VFM_FireMode.Burst => value.Burst.accuracyMultiplier, 
					VFM_FireMode.Suppression => value.Suppression.accuracyMultiplier, 
					VFM_FireMode.Default => value.Default.accuracyMultiplier, 
					_ => 1f, 
				};
			}
			return mode switch
			{
				VFM_FireMode.Precision => Settings.precisionAccuracy, 
				VFM_FireMode.Burst => Settings.burstAccuracy, 
				VFM_FireMode.Suppression => Settings.suppressionAccuracy, 
				VFM_FireMode.Default => Settings.defaultAccuracy, 
				_ => 1f, 
			};
		}

		public static int GetBurstCount(VFM_FireMode mode, int baseBurstCount, string? weaponDefName)
		{
			if (weaponDefName != null && Settings.CustomWeaponProfiles.TryGetValue(weaponDefName, out VFM_WeaponProfile value))
			{
				return mode switch
				{
					VFM_FireMode.Precision => value.Precision.burstShotCount, 
					VFM_FireMode.Burst => value.Burst.burstShotCount, 
					VFM_FireMode.Suppression => value.Suppression.burstShotCount, 
					VFM_FireMode.Default => value.Default.burstShotCount, 
					_ => baseBurstCount, 
				};
			}
			return mode switch
			{
				VFM_FireMode.Precision => GetBurstCount_Precision(baseBurstCount), 
				VFM_FireMode.Burst => GetBurstCount_Burst(baseBurstCount), 
				VFM_FireMode.Suppression => GetBurstCount_Suppression(baseBurstCount), 
				VFM_FireMode.Default => GetBurstCount_Default(baseBurstCount), 
				_ => baseBurstCount, 
			};
		}

		public static int GetBurstCount_Precision(int baseBurstCount)
		{
			return GetBurstCountByOption(baseBurstCount, Settings.precisionBurstOption, Settings.precisionBurstLinearMultiplier, Settings.precisionBurstAdditiveBonus, Settings.precisionBurstTentMaxMultiplier, Settings.precisionBurstTentSlopeK, Settings.precisionBurstTentPeakOffset, Settings.precisionBurstAdaptiveBonus, Settings.precisionBurstAdaptivePeakOffset);
		}

		public static int GetBurstCount_Burst(int baseBurstCount)
		{
			return GetBurstCountByOption(baseBurstCount, Settings.burstBurstOption, Settings.burstBurstLinearMultiplier, Settings.burstBurstAdditiveBonus, Settings.burstBurstTentMaxMultiplier, Settings.burstBurstTentSlopeK, Settings.burstBurstTentPeakOffset, Settings.burstBurstAdaptiveBonus, Settings.burstBurstAdaptivePeakOffset);
		}

		public static int GetBurstCount_Suppression(int baseBurstCount)
		{
			return GetBurstCountByOption(baseBurstCount, Settings.suppressionBurstOption, Settings.suppressionBurstLinearMultiplier, Settings.suppressionBurstAdditiveBonus, Settings.suppressionBurstTentMaxMultiplier, Settings.suppressionBurstTentSlopeK, Settings.suppressionBurstTentPeakOffset, Settings.suppressionBurstAdaptiveBonus, Settings.suppressionBurstAdaptivePeakOffset);
		}

		public static int GetBurstCount_Default(int baseBurstCount)
		{
			return GetBurstCountByOption(baseBurstCount, Settings.defaultBurstOption, Settings.defaultBurstLinearMultiplier, Settings.defaultBurstAdditiveBonus, Settings.defaultBurstTentMaxMultiplier, Settings.defaultBurstTentSlopeK, Settings.defaultBurstTentPeakOffset, Settings.defaultBurstAdaptiveBonus, Settings.defaultBurstAdaptivePeakOffset);
		}

		private static int GetBurstCountByOption(int baseBurstCount, BurstShotOption burstOption, float linearMult, int addBonus, float tentMaxMult, float tentSlopeK, int tentPeak, int adaptBonus, int adaptPeak)
		{
			return burstOption switch
			{
				BurstShotOption.Linear => Mathf.Max(1, handleLinear(baseBurstCount, linearMult)), 
				BurstShotOption.Additive => Mathf.Max(1, handleAdditive(baseBurstCount, addBonus)), 
				BurstShotOption.Tent => Mathf.Max(1, handleTentFunc(baseBurstCount, tentMaxMult, tentSlopeK, tentPeak)), 
				BurstShotOption.Adaptive => Mathf.Max(1, handleAdaptFunc(baseBurstCount, adaptBonus, adaptPeak)), 
				_ => baseBurstCount, 
			};
		}

		private static int handleLinear(int baseBurstCount, float linearMult)
		{
			return Utils.GetBurstShotCountByMultiplier(baseBurstCount, linearMult);
		}

		private static int handleAdditive(int baseBurstCount, int addBonus)
		{
			return Utils.GetBurstShotCountByBonus(baseBurstCount, addBonus);
		}

		private static int handleTentFunc(int baseBurstCount, float tentMaxMult, float tentSlopeK, int tentPeak)
		{
			return Utils.GetBurstShotCountByTentFunction(baseBurstCount, tentMaxMult, tentSlopeK, tentPeak);
		}

		private static int handleAdaptFunc(int baseBurstCount, int adaptBonus, int adaptPeak)
		{
			return Utils.GetBurstShotCountByMod(baseBurstCount, adaptBonus, adaptPeak);
		}
	}
	public static class PawnFireModeExtension
	{
		public static VFM_FireMode VFM_GetFireMode(this Pawn pawn)
		{
			if (pawn == null)
			{
				return VFM_FireMode.Default;
			}
			return ThingCompUtility.TryGetComp<VFM_PawnCompFireMode>((Thing)(object)pawn)?.curMode ?? VFM_FireMode.Default;
		}

		public static void VFM_SetFireMode(this Pawn pawn, VFM_FireMode fireMode)
		{
			if (pawn != null)
			{
				VFM_PawnCompFireMode vFM_PawnCompFireMode = ThingCompUtility.TryGetComp<VFM_PawnCompFireMode>((Thing)(object)pawn);
				if (vFM_PawnCompFireMode != null)
				{
					VFM_PawnCompFireMode vFM_PawnCompFireMode2 = vFM_PawnCompFireMode;
					vFM_PawnCompFireMode2.curMode = fireMode;
				}
			}
		}

		public static bool VFM_enableAutoSelection(this Pawn pawn)
		{
			if (pawn == null)
			{
				return false;
			}
			return ThingCompUtility.TryGetComp<VFM_PawnCompFireMode>((Thing)(object)pawn)?.curEnableAutoSelection ?? false;
		}
	}
	internal static class Utils
	{
		private static readonly float MAX_MULTIPLIER = 100f;

		private static readonly float MAX_EXTRASHOT = 100f;

		private static readonly float MIN_EXTRASHOT = 0f;

		public static int GetBurstShotCountByMultiplier(int burstShotCount, float multiplier)
		{
			if (burstShotCount <= 1)
			{
				return burstShotCount;
			}
			float num = Math.Min(multiplier, MAX_MULTIPLIER);
			return Mathf.Max(1, (int)Math.Round((float)burstShotCount * num));
		}

		public static int GetBurstShotCountByBonus(int burstShotCount, float bonus)
		{
			if (burstShotCount <= 1)
			{
				return burstShotCount;
			}
			float num = Mathf.Min(bonus, MAX_EXTRASHOT);
			return Mathf.Max(1, burstShotCount + (int)Math.Round(num));
		}

		public static int GetBurstShotCountByMod(int burstShotCount, float extra, float peakOffSet)
		{
			if (burstShotCount <= 1)
			{
				return burstShotCount;
			}
			float num = Mathf.Max(peakOffSet, 2f);
			float num2 = Mathf.Min(MAX_EXTRASHOT, Mathf.Max(extra, MIN_EXTRASHOT));
			float num3 = ((float)burstShotCount - 1f) / (num - 1f);
			float num4 = num3 * Mathf.Exp(1f - num3);
			int num5 = Mathf.Max(1, (int)Mathf.Round(num2 * num4));
			return Mathf.Max(1, burstShotCount + num5);
		}

		public static int GetBurstShotCountByTentFunction(int burstShotCount, float maxMultiplier, float slopeK, float peakOffSet)
		{
			if (burstShotCount <= 1)
			{
				return burstShotCount;
			}
			float num = Mathf.Max(slopeK, 0f);
			float num2 = Mathf.Max(peakOffSet, 2f);
			float num3 = Mathf.Min(maxMultiplier, MAX_MULTIPLIER);
			float num4 = Mathf.Abs((float)burstShotCount - num2);
			float num5 = num3 - num * num4;
			num5 = Mathf.Max(0.5f, num5);
			return Mathf.Max(1, (int)Math.Round((float)burstShotCount * num5));
		}

		public static string GetFireModeLabelFor(VFM_FireMode mode)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			return mode switch
			{
				VFM_FireMode.Precision => TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode")), 
				VFM_FireMode.Burst => TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode")), 
				VFM_FireMode.Suppression => TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode")), 
				_ => TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode")), 
			};
		}

		public static string ToPercentString(float value)
		{
			return $"{value * 100f}%";
		}

		public static VFM_FireMode EvaluateByDistance(float distance, VanillaFireModesModSetting settings)
		{
			if (distance >= settings.precisionMinDistance)
			{
				return VFM_FireMode.Precision;
			}
			if (distance >= settings.burstMinDistance)
			{
				return VFM_FireMode.Burst;
			}
			return VFM_FireMode.Suppression;
		}
	}
	[StaticConstructorOnStartup]
	public static class VFM_IconTexture
	{
		public static readonly Texture2D VFM_Default_Icon = ContentFinder<Texture2D>.Get("Icon/VFM_Default_icon", true);

		public static readonly Texture2D VFM_Precision_Icon = ContentFinder<Texture2D>.Get("Icon/VFM_Precision_icon", true);

		public static readonly Texture2D VFM_Burst_Icon = ContentFinder<Texture2D>.Get("Icon/VFM_Burst_icon", true);

		public static readonly Texture2D VFM_Suppression_Icon = ContentFinder<Texture2D>.Get("Icon/VFM_Suppression_icon", true);

		public static readonly Texture2D VFM_Auto_Icon = ContentFinder<Texture2D>.Get("Icon/VFM_Auto_icon", true);
	}
	internal static class WeaponProfileUtils
	{
		private static HashSet<ThingDef> _turretWeaponCache;

		internal static HashSet<ThingDef> TurretWeaponSet
		{
			get
			{
				if (_turretWeaponCache == null)
				{
					_turretWeaponCache = new HashSet<ThingDef>();
					foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
					{
						if (allDef != null && !GenText.NullOrEmpty(((Def)allDef).defName) && IsTurret(allDef) && allDef.building?.turretGunDef != null)
						{
							_turretWeaponCache.Add(allDef.building.turretGunDef);
						}
					}
				}
				return _turretWeaponCache;
			}
		}

		public static VerbProperties? GetPrimaryVerb(ThingDef def)
		{
			List<VerbProperties> verbs = def.Verbs;
			if (verbs == null)
			{
				return null;
			}
			return GenCollection.FirstOrDefault<VerbProperties>(verbs, (Predicate<VerbProperties>)((VerbProperties v) => v.defaultProjectile != null));
		}

		public static void AddSingleWeapon(string defName, int baseBurstShotCount)
		{
			VFM_WeaponProfile value = new VFM_WeaponProfile(defName, VFM_FireModeProfile.CreateDefault(baseBurstShotCount), VFM_FireModeProfile.CreatePrecision(baseBurstShotCount), VFM_FireModeProfile.CreateBurst(baseBurstShotCount), VFM_FireModeProfile.CreateSuppression(baseBurstShotCount));
			if (VanillaFireModes.settings?.CustomWeaponProfiles != null)
			{
				VanillaFireModes.settings.CustomWeaponProfiles.Add(defName, value);
			}
		}

		public static IEnumerable<ThingDef> GetAllRangedWeaponsWithSearch(string leftSearch)
		{
			return from d in GetAllRangedWeapons()
				where GenText.NullOrEmpty(leftSearch) || ((Def)d).label.ToLower().Contains(leftSearch.ToLower())
				select d;
		}

		public static IEnumerable<ThingDef> GetAllRangedWeapons()
		{
			return DefDatabase<ThingDef>.AllDefs.Where((ThingDef d) => d != null && !GenText.NullOrEmpty(((Def)d).defName) && d.IsRangedWeapon && GetPrimaryVerb(d) != null && IsActualPawnRangedWeapon(d));
		}

		public static bool IsActualPawnRangedWeapon(ThingDef def)
		{
			if (def == null || !def.IsRangedWeapon)
			{
				return false;
			}
			if (TurretWeaponSet.Contains(def))
			{
				return false;
			}
			return true;
		}

		public static bool IsTurret(ThingDef? t)
		{
			if (t == null)
			{
				return false;
			}
			if (!(t.thingClass != null) || !(t.thingClass == typeof(Building_TurretGun)))
			{
				if (t.building != null)
				{
					return t.building.turretGunDef != null;
				}
				return false;
			}
			return true;
		}
	}
}
namespace VFM_VanillaFireModes.Stat
{
	public abstract class VFM_FireMode_StatPart : StatPart
	{
		protected abstract float GetFactor(VFM_FireMode mode, string? weaponDefName);

		public override void TransformValue(StatRequest req, ref float val)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			VFM_PawnCompFireMode vFM_PawnCompFireMode = TryGetComp(req);
			if (vFM_PawnCompFireMode != null)
			{
				string primaryWeaponDefName = GetPrimaryWeaponDefName(req);
				val *= GetFactor(vFM_PawnCompFireMode.curMode, primaryWeaponDefName);
			}
		}

		public override string? ExplanationPart(StatRequest req)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			VFM_PawnCompFireMode vFM_PawnCompFireMode = TryGetComp(req);
			if (vFM_PawnCompFireMode == null)
			{
				return null;
			}
			string primaryWeaponDefName = GetPrimaryWeaponDefName(req);
			float factor = GetFactor(vFM_PawnCompFireMode.curMode, primaryWeaponDefName);
			return TaggedString.op_Implicit(TranslatorFormattedStringExtensions.Translate("VFM_StatPart_Label", NamedArgument.op_Implicit(Utils.GetFireModeLabelFor(vFM_PawnCompFireMode.curMode)), NamedArgument.op_Implicit(Utils.ToPercentString(factor))));
		}

		private static string? GetPrimaryWeaponDefName(StatRequest req)
		{
			Thing thing = ((StatRequest)(ref req)).Thing;
			Pawn val = (Pawn)(object)((thing is Pawn) ? thing : null);
			if (val != null)
			{
				Pawn_EquipmentTracker equipment = val.equipment;
				if (equipment == null)
				{
					return null;
				}
				return ((Def)(((Thing)(equipment.Primary?)).def?)).defName;
			}
			return null;
		}

		private static VFM_PawnCompFireMode? TryGetComp(StatRequest req)
		{
			if (!((StatRequest)(ref req)).HasThing)
			{
				return null;
			}
			Thing thing = ((StatRequest)(ref req)).Thing;
			Pawn val = (Pawn)(object)((thing is Pawn) ? thing : null);
			if (val == null)
			{
				return null;
			}
			return ThingCompUtility.TryGetComp<VFM_PawnCompFireMode>((Thing)(object)val);
		}
	}
	public class VFM_FireMode_AimingDelayFactorPart : VFM_FireMode_StatPart
	{
		protected override float GetFactor(VFM_FireMode mode, string? weaponDefName)
		{
			return FireModeDB.GetWarmup(mode, weaponDefName);
		}
	}
	public class VFM_FireMode_RangedCooldownFactorPart : VFM_FireMode_StatPart
	{
		protected override float GetFactor(VFM_FireMode mode, string? weaponDefName)
		{
			return FireModeDB.GetCooldown(mode, weaponDefName);
		}
	}
	public class VFM_FireMode_ShootingAccuracyPawnPart : VFM_FireMode_StatPart
	{
		protected override float GetFactor(VFM_FireMode mode, string? weaponDefName)
		{
			return FireModeDB.GetAccuracy(mode, weaponDefName);
		}
	}
}
namespace VFM_VanillaFireModes.Settings
{
	public enum BurstShotOption
	{
		Linear,
		Additive,
		Tent,
		Adaptive
	}
	public class VanillaFireModesModSetting : ModSettings
	{
		public float defaultAccuracy = 1f;

		public float defaultWarmup = 1f;

		public float defaultCooldown = 1f;

		public BurstShotOption defaultBurstOption;

		public float defaultBurstLinearMultiplier = 1f;

		public int defaultBurstAdditiveBonus = 1;

		public float defaultBurstTentMaxMultiplier = 1f;

		public float defaultBurstTentSlopeK;

		public int defaultBurstTentPeakOffset = 3;

		public int defaultBurstAdaptiveBonus = 1;

		public int defaultBurstAdaptivePeakOffset = 2;

		public float precisionAccuracy = 1.5f;

		public float precisionWarmup = 1.2f;

		public float precisionCooldown = 0.5f;

		public BurstShotOption precisionBurstOption;

		public float precisionBurstLinearMultiplier = 0.8f;

		public int precisionBurstAdditiveBonus = 1;

		public float precisionBurstTentMaxMultiplier = 1f;

		public float precisionBurstTentSlopeK = 0.05f;

		public int precisionBurstTentPeakOffset = 3;

		public int precisionBurstAdaptiveBonus = 1;

		public int precisionBurstAdaptivePeakOffset = 2;

		public float burstAccuracy = 0.8f;

		public float burstWarmup = 0.8f;

		public float burstCooldown = 0.8f;

		public BurstShotOption burstBurstOption = BurstShotOption.Tent;

		public float burstBurstLinearMultiplier = 1f;

		public int burstBurstAdditiveBonus = 3;

		public float burstBurstTentMaxMultiplier = 1.75f;

		public float burstBurstTentSlopeK = 0.1f;

		public int burstBurstTentPeakOffset = 4;

		public int burstBurstAdaptiveBonus = 5;

		public int burstBurstAdaptivePeakOffset = 4;

		public float suppressionAccuracy = 0.5f;

		public float suppressionWarmup = 0.5f;

		public float suppressionCooldown = 1.2f;

		public BurstShotOption suppressionBurstOption = BurstShotOption.Adaptive;

		public float suppressionBurstLinearMultiplier = 1f;

		public int suppressionBurstAdditiveBonus = 10;

		public float suppressionBurstTentMaxMultiplier = 2f;

		public float suppressionBurstTentSlopeK = 0.05f;

		public int suppressionBurstTentPeakOffset = 5;

		public int suppressionBurstAdaptiveBonus = 10;

		public int suppressionBurstAdaptivePeakOffset = 5;

		public bool alwaysDisplayGizmo;

		public bool enableAutoSelectionForPlayer = true;

		public float burstMinDistance = 12f;

		public float precisionMinDistance = 25f;

		public bool autoModeDefaultOn;

		public bool enableFireModeForNPC = true;

		public Dictionary<string, VFM_WeaponProfile> CustomWeaponProfiles = new Dictionary<string, VFM_WeaponProfile>();

		public override void ExposeData()
		{
			//IL_0406: Unknown result type (might be due to invalid IL or missing references)
			//IL_040c: Invalid comparison between Unknown and I4
			((ModSettings)this).ExposeData();
			Scribe_Values.Look<float>(ref defaultAccuracy, "defaultAccuracy", 1f, false);
			Scribe_Values.Look<float>(ref defaultWarmup, "defaultWarmup", 1f, false);
			Scribe_Values.Look<float>(ref defaultCooldown, "defaultCooldown", 1f, false);
			Scribe_Values.Look<BurstShotOption>(ref defaultBurstOption, "defaultBurstOption", BurstShotOption.Linear, false);
			Scribe_Values.Look<float>(ref defaultBurstLinearMultiplier, "defaultBurstLinearMultiplier", 1f, false);
			Scribe_Values.Look<int>(ref defaultBurstAdditiveBonus, "defaultBurstAdditiveBonus", 1, false);
			Scribe_Values.Look<float>(ref defaultBurstTentMaxMultiplier, "defaultBurstTentMaxMultiplier", 1f, false);
			Scribe_Values.Look<float>(ref defaultBurstTentSlopeK, "defaultBurstTentSlopeK", 0f, false);
			Scribe_Values.Look<int>(ref defaultBurstTentPeakOffset, "defaultBurstTentPeakOffset", 3, false);
			Scribe_Values.Look<int>(ref defaultBurstAdaptiveBonus, "defaultBurstAdaptiveBonus", 1, false);
			Scribe_Values.Look<int>(ref defaultBurstAdaptivePeakOffset, "defaultBurstAdaptivePeakOffset", 2, false);
			Scribe_Values.Look<float>(ref precisionAccuracy, "precisionAccuracy", 1.5f, false);
			Scribe_Values.Look<float>(ref precisionWarmup, "precisionWarmup", 1.2f, false);
			Scribe_Values.Look<float>(ref precisionCooldown, "precisionCooldown", 0.5f, false);
			Scribe_Values.Look<BurstShotOption>(ref precisionBurstOption, "precisionBurstOption", BurstShotOption.Linear, false);
			Scribe_Values.Look<float>(ref precisionBurstLinearMultiplier, "precisionBurstLinearMultiplier", 0.8f, false);
			Scribe_Values.Look<int>(ref precisionBurstAdditiveBonus, "precisionBurstAdditiveBonus", 1, false);
			Scribe_Values.Look<float>(ref precisionBurstTentMaxMultiplier, "precisionBurstTentMaxMultiplier", 1f, false);
			Scribe_Values.Look<float>(ref precisionBurstTentSlopeK, "precisionBurstTentSlopeK", 0.05f, false);
			Scribe_Values.Look<int>(ref precisionBurstTentPeakOffset, "precisionBurstTentPeakOffset", 3, false);
			Scribe_Values.Look<int>(ref precisionBurstAdaptiveBonus, "precisionBurstAdaptiveBonus", 1, false);
			Scribe_Values.Look<int>(ref precisionBurstAdaptivePeakOffset, "precisionBurstAdaptivePeakOffset", 2, false);
			Scribe_Values.Look<float>(ref burstAccuracy, "burstAccuracy", 0.8f, false);
			Scribe_Values.Look<float>(ref burstWarmup, "burstWarmup", 0.8f, false);
			Scribe_Values.Look<float>(ref burstCooldown, "burstCooldown", 0.8f, false);
			Scribe_Values.Look<BurstShotOption>(ref burstBurstOption, "burstBurstOption", BurstShotOption.Tent, false);
			Scribe_Values.Look<float>(ref burstBurstLinearMultiplier, "burstBurstLinearMultiplier", 1f, false);
			Scribe_Values.Look<int>(ref burstBurstAdditiveBonus, "burstBurstAdditiveBonus", 3, false);
			Scribe_Values.Look<float>(ref burstBurstTentMaxMultiplier, "burstBurstTentMaxMultiplier", 1.75f, false);
			Scribe_Values.Look<float>(ref burstBurstTentSlopeK, "burstBurstTentSlopeK", 0.1f, false);
			Scribe_Values.Look<int>(ref burstBurstTentPeakOffset, "burstBurstTentPeakOffset", 4, false);
			Scribe_Values.Look<int>(ref burstBurstAdaptiveBonus, "burstBurstAdaptiveBonus", 5, false);
			Scribe_Values.Look<int>(ref burstBurstAdaptivePeakOffset, "burstBurstAdaptivePeakOffset", 4, false);
			Scribe_Values.Look<float>(ref suppressionAccuracy, "suppressionAccuracy", 0.5f, false);
			Scribe_Values.Look<float>(ref suppressionWarmup, "suppressionWarmup", 0.5f, false);
			Scribe_Values.Look<float>(ref suppressionCooldown, "suppressionCooldown", 1.2f, false);
			Scribe_Values.Look<BurstShotOption>(ref suppressionBurstOption, "suppressionBurstOption", BurstShotOption.Adaptive, false);
			Scribe_Values.Look<float>(ref suppressionBurstLinearMultiplier, "suppressionBurstLinearMultiplier", 1f, false);
			Scribe_Values.Look<int>(ref suppressionBurstAdditiveBonus, "suppressionBurstAdditiveBonus", 10, false);
			Scribe_Values.Look<float>(ref suppressionBurstTentMaxMultiplier, "suppressionBurstTentMaxMultiplier", 2f, false);
			Scribe_Values.Look<float>(ref suppressionBurstTentSlopeK, "suppressionBurstTentSlopeK", 0.05f, false);
			Scribe_Values.Look<int>(ref suppressionBurstTentPeakOffset, "suppressionBurstTentPeakOffset", 5, false);
			Scribe_Values.Look<int>(ref suppressionBurstAdaptiveBonus, "suppressionBurstAdaptiveBonus", 10, false);
			Scribe_Values.Look<int>(ref suppressionBurstAdaptivePeakOffset, "suppressionBurstAdaptivePeakOffset", 5, false);
			Scribe_Values.Look<bool>(ref alwaysDisplayGizmo, "alwaysDisplayGizmo", false, false);
			Scribe_Values.Look<bool>(ref enableAutoSelectionForPlayer, "enableAutoSelectionForPlayer", true, false);
			Scribe_Values.Look<float>(ref burstMinDistance, "burstMinDistance", 12f, false);
			Scribe_Values.Look<float>(ref precisionMinDistance, "precisionMinDistance", 25f, false);
			Scribe_Values.Look<bool>(ref autoModeDefaultOn, "autoModeDefaultOn", false, false);
			Scribe_Values.Look<bool>(ref enableFireModeForNPC, "enableFireModeForNPC", true, false);
			Scribe_Collections.Look<string, VFM_WeaponProfile>(ref CustomWeaponProfiles, "CustomWeaponProfiles", (LookMode)1, (LookMode)0);
			if ((int)Scribe.mode == 4 && CustomWeaponProfiles == null)
			{
				CustomWeaponProfiles = new Dictionary<string, VFM_WeaponProfile>();
			}
		}

		public void ResetSetting()
		{
			defaultAccuracy = 1f;
			defaultWarmup = 1f;
			defaultCooldown = 1f;
			defaultBurstOption = BurstShotOption.Linear;
			defaultBurstLinearMultiplier = 1f;
			defaultBurstAdditiveBonus = 1;
			defaultBurstTentMaxMultiplier = 1f;
			defaultBurstTentSlopeK = 0f;
			defaultBurstTentPeakOffset = 3;
			defaultBurstAdaptiveBonus = 1;
			defaultBurstAdaptivePeakOffset = 2;
			precisionAccuracy = 1.5f;
			precisionWarmup = 1.2f;
			precisionCooldown = 0.5f;
			precisionBurstOption = BurstShotOption.Linear;
			precisionBurstLinearMultiplier = 0.8f;
			precisionBurstAdditiveBonus = 1;
			precisionBurstTentMaxMultiplier = 1f;
			precisionBurstTentSlopeK = 0.05f;
			precisionBurstTentPeakOffset = 3;
			precisionBurstAdaptiveBonus = 1;
			precisionBurstAdaptivePeakOffset = 2;
			burstAccuracy = 0.8f;
			burstWarmup = 0.8f;
			burstCooldown = 0.8f;
			burstBurstOption = BurstShotOption.Tent;
			burstBurstLinearMultiplier = 1f;
			burstBurstAdditiveBonus = 3;
			burstBurstTentMaxMultiplier = 1.75f;
			burstBurstTentSlopeK = 0.1f;
			burstBurstTentPeakOffset = 4;
			burstBurstAdaptiveBonus = 5;
			burstBurstAdaptivePeakOffset = 4;
			suppressionAccuracy = 0.5f;
			suppressionWarmup = 0.5f;
			suppressionCooldown = 1.2f;
			suppressionBurstOption = BurstShotOption.Adaptive;
			suppressionBurstLinearMultiplier = 1f;
			suppressionBurstAdditiveBonus = 10;
			suppressionBurstTentMaxMultiplier = 2f;
			suppressionBurstTentSlopeK = 0.05f;
			suppressionBurstTentPeakOffset = 5;
			suppressionBurstAdaptiveBonus = 10;
			suppressionBurstAdaptivePeakOffset = 5;
			alwaysDisplayGizmo = false;
			enableAutoSelectionForPlayer = true;
			burstMinDistance = 12f;
			precisionMinDistance = 25f;
			autoModeDefaultOn = false;
			enableFireModeForNPC = true;
		}

		public void ResetWeaponProfileSetting()
		{
			if (CustomWeaponProfiles != null)
			{
				CustomWeaponProfiles.Clear();
			}
			if (CustomWeaponProfiles == null)
			{
				CustomWeaponProfiles = new Dictionary<string, VFM_WeaponProfile>();
			}
		}
	}
	public enum VFM_FireMode
	{
		Default,
		Precision,
		Burst,
		Suppression
	}
}
namespace VFM_VanillaFireModes.Settings.CustomWeaponProfile
{
	public class VFM_FireModeProfile : IExposable
	{
		public float accuracyMultiplier = 1f;

		public float warmupMultiplier = 1f;

		public float cooldownMultiplier = 1f;

		public int burstShotCount = 1;

		public VFM_FireModeProfile()
		{
		}

		public VFM_FireModeProfile(float accuracyMultiplier, float warmupMultiplier, float cooldownMultiplier, int burstShotCount)
		{
			this.accuracyMultiplier = accuracyMultiplier;
			this.warmupMultiplier = warmupMultiplier;
			this.cooldownMultiplier = cooldownMultiplier;
			this.burstShotCount = burstShotCount;
		}

		public void ExposeData()
		{
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Invalid comparison between Unknown and I4
			Scribe_Values.Look<float>(ref accuracyMultiplier, "accuracyMultiplier", 1f, false);
			Scribe_Values.Look<float>(ref warmupMultiplier, "warmupMultiplier", 1f, false);
			Scribe_Values.Look<float>(ref cooldownMultiplier, "cooldownMultiplier", 1f, false);
			Scribe_Values.Look<int>(ref burstShotCount, "burstShotCount", 1, false);
			if ((int)Scribe.mode == 4)
			{
				if (accuracyMultiplier <= 0f)
				{
					accuracyMultiplier = 1f;
				}
				if (warmupMultiplier <= 0f)
				{
					warmupMultiplier = 1f;
				}
				if (cooldownMultiplier <= 0f)
				{
					cooldownMultiplier = 1f;
				}
				if (burstShotCount < 1)
				{
					burstShotCount = 1;
				}
			}
		}

		public static VFM_FireModeProfile CreateDefault(int baseBurstShotCount)
		{
			return new VFM_FireModeProfile(VanillaFireModes.settings.defaultAccuracy, VanillaFireModes.settings.defaultWarmup, VanillaFireModes.settings.defaultCooldown, FireModeDB.GetBurstCount_Default(baseBurstShotCount));
		}

		public static VFM_FireModeProfile CreatePrecision(int baseBurstShotCount)
		{
			return new VFM_FireModeProfile(VanillaFireModes.settings.precisionAccuracy, VanillaFireModes.settings.precisionWarmup, VanillaFireModes.settings.precisionCooldown, FireModeDB.GetBurstCount_Precision(baseBurstShotCount));
		}

		public static VFM_FireModeProfile CreateBurst(int baseBurstShotCount)
		{
			return new VFM_FireModeProfile(VanillaFireModes.settings.burstAccuracy, VanillaFireModes.settings.burstWarmup, VanillaFireModes.settings.burstCooldown, FireModeDB.GetBurstCount_Burst(baseBurstShotCount));
		}

		public static VFM_FireModeProfile CreateSuppression(int baseBurstShotCount)
		{
			return new VFM_FireModeProfile(VanillaFireModes.settings.suppressionAccuracy, VanillaFireModes.settings.suppressionWarmup, VanillaFireModes.settings.suppressionCooldown, FireModeDB.GetBurstCount_Suppression(baseBurstShotCount));
		}
	}
	public class VFM_WeaponProfile : IExposable
	{
		public string defName;

		public VFM_FireModeProfile Default;

		public VFM_FireModeProfile Precision;

		public VFM_FireModeProfile Burst;

		public VFM_FireModeProfile Suppression;

		public VFM_WeaponProfile()
		{
		}

		public VFM_WeaponProfile(string defName, VFM_FireModeProfile Default, VFM_FireModeProfile Precision, VFM_FireModeProfile Burst, VFM_FireModeProfile Suppression)
		{
			this.defName = defName;
			this.Default = Default;
			this.Precision = Precision;
			this.Burst = Burst;
			this.Suppression = Suppression;
		}

		public bool defIsValid()
		{
			if (!GenText.NullOrEmpty(defName))
			{
				return DefDatabase<ThingDef>.GetNamedSilentFail(defName) != null;
			}
			return false;
		}

		public void ExposeData()
		{
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Invalid comparison between Unknown and I4
			Scribe_Values.Look<string>(ref defName, "defName", (string)null, false);
			Scribe_Deep.Look<VFM_FireModeProfile>(ref Default, "Default", Array.Empty<object>());
			Scribe_Deep.Look<VFM_FireModeProfile>(ref Precision, "Precision", Array.Empty<object>());
			Scribe_Deep.Look<VFM_FireModeProfile>(ref Burst, "Burst", Array.Empty<object>());
			Scribe_Deep.Look<VFM_FireModeProfile>(ref Suppression, "Suppression", Array.Empty<object>());
			if ((int)Scribe.mode == 4)
			{
				if (Default == null)
				{
					Default = new VFM_FireModeProfile();
				}
				if (Precision == null)
				{
					Precision = new VFM_FireModeProfile();
				}
				if (Burst == null)
				{
					Burst = new VFM_FireModeProfile();
				}
				if (Suppression == null)
				{
					Suppression = new VFM_FireModeProfile();
				}
			}
		}
	}
}
namespace VFM_VanillaFireModes.Patches
{
	[HarmonyPatch]
	public static class Patch_BurstShotCount
	{
		private static readonly Dictionary<string, int> _burstCache = new Dictionary<string, int>();

		[HarmonyPatch(typeof(Verb), "WarmupComplete")]
		[HarmonyPrefix]
		public static void LockCount(Verb __instance)
		{
			if (__instance.loadID != null && ShouldModify(__instance, out Pawn pawn, out ThingWithComps weapon))
			{
				int num = FireModeDB.GetBurstCount(pawn.VFM_GetFireMode(), weaponDefName: ((Def)((Thing)weapon).def).defName, baseBurstCount: __instance.BurstShotCount);
				_burstCache[__instance.loadID] = Mathf.Max(1, num);
			}
		}

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		[HarmonyPostfix]
		public static void BurstShotCountPostFix(Verb __instance, ref int __result)
		{
			if (__instance.loadID != null && ShouldModify(__instance, out Pawn _, out ThingWithComps _) && _burstCache.TryGetValue(__instance.loadID, out var value))
			{
				__result = Mathf.Max(1, value);
			}
		}

		[HarmonyPatch(typeof(Verb), "VerbTick")]
		[HarmonyPostfix]
		public static void Postfix_Cleanup(Verb __instance)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Invalid comparison between Unknown and I4
			if (__instance.loadID != null && _burstCache.ContainsKey(__instance.loadID) && (int)__instance.state != 1)
			{
				_burstCache.Remove(__instance.loadID);
			}
		}

		private static bool ShouldModify(Verb verb, out Pawn pawn, out ThingWithComps weapon)
		{
			pawn = null;
			weapon = null;
			Pawn casterPawn = verb.CasterPawn;
			if (casterPawn == null)
			{
				return false;
			}
			if (verb.verbProps == null)
			{
				return false;
			}
			if (verb is Verb_ShootOneUse)
			{
				return false;
			}
			if (verb.verbProps.IsMeleeAttack)
			{
				return false;
			}
			ThingWithComps equipmentSource = verb.EquipmentSource;
			if (equipmentSource == null || ((Thing)equipmentSource).def == null || !((Thing)equipmentSource).def.IsRangedWeapon || GenText.NullOrEmpty(((Def)((Thing)equipmentSource).def).defName))
			{
				return false;
			}
			pawn = casterPawn;
			weapon = equipmentSource;
			return true;
		}
	}
	[HarmonyPatch(typeof(Verb))]
	[HarmonyPatch("TryStartCastOn", new Type[]
	{
		typeof(LocalTargetInfo),
		typeof(LocalTargetInfo),
		typeof(bool),
		typeof(bool),
		typeof(bool),
		typeof(bool)
	})]
	public static class Patch_TryStartCastOn
	{
		private static void Prefix(Verb __instance, LocalTargetInfo castTarg)
		{
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			Pawn casterPawn = __instance.CasterPawn;
			if (casterPawn != null && casterPawn != null && ((LocalTargetInfo)(ref castTarg)).IsValid && __instance.verbProps != null && !__instance.verbProps.IsMeleeAttack && __instance.EquipmentSource != null && ((Thing)__instance.EquipmentSource).def.IsRangedWeapon)
			{
				VanillaFireModesModSetting settings = FireModeDB.Settings;
				bool enableAutoSelectionForPlayer = settings.enableAutoSelectionForPlayer;
				bool enableFireModeForNPC = settings.enableFireModeForNPC;
				if ((casterPawn.IsColonistPlayerControlled || casterPawn.IsColonyMechPlayerControlled) && enableAutoSelectionForPlayer && casterPawn.VFM_enableAutoSelection())
				{
					VFM_FireMode fireMode = Utils.EvaluateByDistance(IntVec3Utility.DistanceTo(((Thing)casterPawn).Position, ((LocalTargetInfo)(ref castTarg)).Cell), settings);
					casterPawn.VFM_SetFireMode(fireMode);
				}
				if (!casterPawn.IsColonistPlayerControlled && !casterPawn.IsColonyMechPlayerControlled && enableFireModeForNPC)
				{
					VFM_FireMode fireMode2 = Utils.EvaluateByDistance(IntVec3Utility.DistanceTo(((Thing)casterPawn).Position, ((LocalTargetInfo)(ref castTarg)).Cell), settings);
					casterPawn.VFM_SetFireMode(fireMode2);
				}
			}
		}
	}
}
namespace VFM_VanillaFireModes.ModSettingUI
{
	internal static class VFM_SettingsWindowContents
	{
		private enum TacticTab
		{
			AutoSelectionTab,
			PrecisionTab,
			BurstTab,
			SuppressionTab,
			DefaultTab
		}

		private static TacticTab currentTab = TacticTab.AutoSelectionTab;

		private static Vector2 scrollPos;

		private static float lastCalculatedHeight = 1000f;

		public static void SettingsWindowContents(Rect inRect, ref VanillaFireModesModSetting settings)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected O, but got Unknown
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Expected O, but got Unknown
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Expected O, but got Unknown
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Expected O, but got Unknown
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Expected O, but got Unknown
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01db: Expected O, but got Unknown
			//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0212: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_033d: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_0409: Unknown result type (might be due to invalid IL or missing references)
			//IL_0413: Unknown result type (might be due to invalid IL or missing references)
			//IL_047b: Unknown result type (might be due to invalid IL or missing references)
			//IL_049f: Unknown result type (might be due to invalid IL or missing references)
			//IL_04cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0507: Unknown result type (might be due to invalid IL or missing references)
			GUI.BeginGroup(inRect);
			TabDrawer.DrawTabs<TabRecord>(new Rect(0f, 32f, ((Rect)(ref inRect)).width, 0f), new List<TabRecord>
			{
				new TabRecord(TaggedString.op_Implicit(Translator.Translate("VFM_GeneralSettings_Label")), (Action)delegate
				{
					currentTab = TacticTab.AutoSelectionTab;
				}, currentTab == TacticTab.AutoSelectionTab),
				new TabRecord(TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode")), (Action)delegate
				{
					currentTab = TacticTab.PrecisionTab;
				}, currentTab == TacticTab.PrecisionTab),
				new TabRecord(TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode")), (Action)delegate
				{
					currentTab = TacticTab.BurstTab;
				}, currentTab == TacticTab.BurstTab),
				new TabRecord(TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode")), (Action)delegate
				{
					currentTab = TacticTab.SuppressionTab;
				}, currentTab == TacticTab.SuppressionTab),
				new TabRecord(TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode")), (Action)delegate
				{
					currentTab = TacticTab.DefaultTab;
				}, currentTab == TacticTab.DefaultTab)
			}, 200f);
			float num = 45f;
			float num2 = 42f;
			Rect val = default(Rect);
			((Rect)(ref val))..ctor(0f, num2, ((Rect)(ref inRect)).width, ((Rect)(ref inRect)).height - num2 - num);
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(0f, 0f, ((Rect)(ref inRect)).width - 24f, lastCalculatedHeight);
			Widgets.BeginScrollView(val, ref scrollPos, val2, true);
			Listing_Standard val3 = new Listing_Standard();
			((Listing)val3).Begin(val2);
			switch (currentTab)
			{
			case TacticTab.AutoSelectionTab:
				VFM_UI_SettingGroup.DrawGeneralGroup(val3, TaggedString.op_Implicit(Translator.Translate("VFM_GeneralSettings_Label")), ref settings.alwaysDisplayGizmo, ref settings.enableAutoSelectionForPlayer, ref settings.burstMinDistance, ref settings.precisionMinDistance, ref settings.enableFireModeForNPC, ref settings.autoModeDefaultOn);
				break;
			case TacticTab.PrecisionTab:
				VFM_UI_SettingGroup.DrawGroup(val3, TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode")), ref settings.precisionAccuracy, ref settings.precisionWarmup, ref settings.precisionCooldown, ref settings.precisionBurstOption, ref settings.precisionBurstLinearMultiplier, ref settings.precisionBurstAdditiveBonus, ref settings.precisionBurstTentMaxMultiplier, ref settings.precisionBurstTentSlopeK, ref settings.precisionBurstTentPeakOffset, ref settings.precisionBurstAdaptiveBonus, ref settings.precisionBurstAdaptivePeakOffset);
				break;
			case TacticTab.BurstTab:
				VFM_UI_SettingGroup.DrawGroup(val3, TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode")), ref settings.burstAccuracy, ref settings.burstWarmup, ref settings.burstCooldown, ref settings.burstBurstOption, ref settings.burstBurstLinearMultiplier, ref settings.burstBurstAdditiveBonus, ref settings.burstBurstTentMaxMultiplier, ref settings.burstBurstTentSlopeK, ref settings.burstBurstTentPeakOffset, ref settings.burstBurstAdaptiveBonus, ref settings.burstBurstAdaptivePeakOffset);
				break;
			case TacticTab.SuppressionTab:
				VFM_UI_SettingGroup.DrawGroup(val3, TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode")), ref settings.suppressionAccuracy, ref settings.suppressionWarmup, ref settings.suppressionCooldown, ref settings.suppressionBurstOption, ref settings.suppressionBurstLinearMultiplier, ref settings.suppressionBurstAdditiveBonus, ref settings.suppressionBurstTentMaxMultiplier, ref settings.suppressionBurstTentSlopeK, ref settings.suppressionBurstTentPeakOffset, ref settings.suppressionBurstAdaptiveBonus, ref settings.suppressionBurstAdaptivePeakOffset);
				break;
			case TacticTab.DefaultTab:
				VFM_UI_SettingGroup.DrawGroup(val3, TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode")), ref settings.defaultAccuracy, ref settings.defaultWarmup, ref settings.defaultCooldown, ref settings.defaultBurstOption, ref settings.defaultBurstLinearMultiplier, ref settings.defaultBurstAdditiveBonus, ref settings.defaultBurstTentMaxMultiplier, ref settings.defaultBurstTentSlopeK, ref settings.defaultBurstTentPeakOffset, ref settings.defaultBurstAdaptiveBonus, ref settings.defaultBurstAdaptivePeakOffset, TaggedString.op_Implicit(Translator.Translate("VFM_Default_Warning_Label")), Color.yellow);
				break;
			}
			lastCalculatedHeight = ((Listing)val3).CurHeight + 20f;
			((Listing)val3).End();
			Widgets.EndScrollView();
			Rect val4 = default(Rect);
			((Rect)(ref val4))..ctor(0f, ((Rect)(ref val)).yMax + 10f, ((Rect)(ref inRect)).width, num - 15f);
			GUI.color = new Color(1f, 1f, 1f, 0.3f);
			Widgets.DrawLineHorizontal(((Rect)(ref val4)).x, ((Rect)(ref val4)).y, ((Rect)(ref val4)).width);
			GUI.color = Color.white;
			if (Widgets.ButtonText(new Rect(((Rect)(ref val4)).xMax - 240f, ((Rect)(ref val4)).y + 5f, 240f, 30f), TaggedString.op_Implicit(Translator.Translate("VFM_ResetButton_Label")), true, true, true, (TextAnchor?)null))
			{
				settings.ResetSetting();
				SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, (Map)null);
			}
			GUI.color = Color.white;
			GUI.EndGroup();
		}
	}
	internal static class VFM_UI_BurstSection
	{
		public static void DrawBurstSection(Listing_Standard ls, ref BurstShotOption option, ref float linearMult, ref int addBonus, ref float tentMaxMult, ref float tentSlopeK, ref int tentPeak, ref int adaptBonus, ref int adaptPeak)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
			//IL_025c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0266: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_028e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0299: Unknown result type (might be due to invalid IL or missing references)
			//IL_029f: Unknown result type (might be due to invalid IL or missing references)
			ls.Label(Translator.Translate("VFM_Burst_Calculation_Mode_Label"), -1f, (string)null);
			((Listing)ls).GapLine(6f);
			if (ls.RadioButton(TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Linear_Label")), option == BurstShotOption.Linear, 0f, (string)null, (float?)null))
			{
				option = BurstShotOption.Linear;
			}
			if (ls.RadioButton(TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Additive_Label")), option == BurstShotOption.Additive, 0f, (string)null, (float?)null))
			{
				option = BurstShotOption.Additive;
			}
			if (ls.RadioButton(TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Tent_Label")), option == BurstShotOption.Tent, 0f, (string)null, (float?)null))
			{
				option = BurstShotOption.Tent;
			}
			if (ls.RadioButton(TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Adaptive_Label")), option == BurstShotOption.Adaptive, 0f, (string)null, (float?)null))
			{
				option = BurstShotOption.Adaptive;
			}
			((Listing)ls).Gap(6f);
			switch (option)
			{
			case BurstShotOption.Linear:
				VFM_UI_SliderWithInput.DrawSliderWithInput_Float(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Linear_input_Label")), ref linearMult, 0.1f, 5f);
				break;
			case BurstShotOption.Additive:
				VFM_UI_SliderWithInput.DrawSliderWithInput_Int(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Additive_input_Label")), ref addBonus, 0, 50);
				break;
			case BurstShotOption.Tent:
				VFM_UI_SliderWithInput.DrawSliderWithInput_Float(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Tent_MaxMult_input_Label")), ref tentMaxMult, 1f, 5f);
				VFM_UI_SliderWithInput.DrawSliderWithInput_Float(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Tent_SlopeK_input_Label")), ref tentSlopeK, 0f, 5f);
				VFM_UI_SliderWithInput.DrawSliderWithInput_Int(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Tent_PeakOffset_input_Label")), ref tentPeak, 2, 30);
				((Listing)ls).Gap(4f);
				VFM_UI_Graph.DrawTentFunctionGraph(GenUI.ContractedBy(((Listing)ls).GetRect(150f, 1f), 2f), tentMaxMult, tentSlopeK, tentPeak);
				Text.Font = (GameFont)0;
				GUI.color = Color.gray;
				ls.Label(Translator.Translate("VFM_Burst_Tent_Graph_Label"), -1f, (string)null);
				GUI.color = Color.white;
				Text.Font = (GameFont)1;
				break;
			case BurstShotOption.Adaptive:
				VFM_UI_SliderWithInput.DrawSliderWithInput_Int(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Adaptive_ExtraBonus_input_Label")), ref adaptBonus, 0, 50);
				VFM_UI_SliderWithInput.DrawSliderWithInput_Int(ls, TaggedString.op_Implicit(Translator.Translate("VFM_Burst_Adaptive_PeakOffset_input_Label")), ref adaptPeak, 2, 30);
				((Listing)ls).Gap(4f);
				VFM_UI_Graph.DrawModFunctionGraph(GenUI.ContractedBy(((Listing)ls).GetRect(150f, 1f), 2f), adaptBonus, adaptPeak);
				Text.Font = (GameFont)0;
				GUI.color = Color.gray;
				ls.Label(Translator.Translate("VFM_Burst_Adaptive_Graph_Label"), -1f, (string)null);
				GUI.color = Color.white;
				Text.Font = (GameFont)1;
				break;
			}
			((Listing)ls).GapLine(6f);
		}
	}
	internal static class VFM_UI_Graph
	{
		public static void DrawModFunctionGraph(Rect rect, float extraShot, float peakOffset)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0371: Unknown result type (might be due to invalid IL or missing references)
			//IL_0384: Unknown result type (might be due to invalid IL or missing references)
			//IL_0392: Unknown result type (might be due to invalid IL or missing references)
			//IL_0397: Unknown result type (might be due to invalid IL or missing references)
			//IL_0312: Unknown result type (might be due to invalid IL or missing references)
			//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0301: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.6f));
			Widgets.DrawBox(rect, 1, (Texture2D)null);
			Rect val = GenUI.ContractedBy(rect, 18f, 10f);
			((Rect)(ref val)).x = ((Rect)(ref val)).x + 10f;
			((Rect)(ref val)).width = ((Rect)(ref val)).width - 15f;
			((Rect)(ref val)).height = ((Rect)(ref val)).height - 10f;
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMin), new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMax), Color.gray, 1f);
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMax), new Vector2(((Rect)(ref val)).xMax, ((Rect)(ref val)).yMax), Color.gray, 1f);
			Text.Font = (GameFont)0;
			GUI.color = Color.gray;
			float num = ((Rect)(ref val)).yMax - 0.9090909f * ((Rect)(ref val)).height;
			Widgets.Label(new Rect(((Rect)(ref rect)).x + 2f, num - 7f, 25f, 15f), "1.0");
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, num), new Vector2(((Rect)(ref val)).xMax, num), new Color(1f, 1f, 1f, 0.1f), 1f);
			float num2 = 30f;
			float num3 = 1.1f;
			for (int i = 0; i <= 30; i += 5)
			{
				int num4 = ((i == 0) ? 1 : i);
				float num5 = ((Rect)(ref val)).x + (float)(num4 - 1) / (num2 - 1f) * ((Rect)(ref val)).width;
				Widgets.DrawLine(new Vector2(num5, ((Rect)(ref val)).yMax), new Vector2(num5, ((Rect)(ref val)).yMax + 3f), Color.gray, 1f);
				Rect val2 = new Rect(num5 - 10f, ((Rect)(ref val)).yMax + 3f, 20f, 15f);
				Text.Anchor = (TextAnchor)1;
				Widgets.Label(val2, num4.ToString());
				Text.Anchor = (TextAnchor)0;
			}
			GUI.color = Color.white;
			Vector2? val3 = null;
			Vector2 val4 = default(Vector2);
			for (float num6 = 0f; num6 <= ((Rect)(ref val)).width; num6 += 2f)
			{
				float num7 = 1f + num6 / ((Rect)(ref val)).width * (num2 - 1f);
				float num8 = 0f;
				float num9 = peakOffset - 1f;
				if (num9 > 0f)
				{
					float num10 = (num7 - 1f) / num9;
					if (num10 > 0f)
					{
						num8 = num10 * (float)Math.Exp(1f - num10);
					}
				}
				float num11 = ((Rect)(ref val)).yMax - num8 / num3 * ((Rect)(ref val)).height;
				num11 = Mathf.Clamp(num11, ((Rect)(ref val)).yMin, ((Rect)(ref val)).yMax);
				((Vector2)(ref val4))..ctor(((Rect)(ref val)).x + num6, num11);
				if (val3.HasValue)
				{
					Widgets.DrawLine(val3.Value, val4, new Color(0.3f, 1f, 0.3f), 1.5f);
				}
				val3 = val4;
			}
			float num12 = ((Rect)(ref val)).x + (peakOffset - 1f) / (num2 - 1f) * ((Rect)(ref val)).width;
			if (num12 <= ((Rect)(ref val)).xMax)
			{
				GUI.color = new Color(0.2f, 0.6f, 1f, 0.4f);
				Widgets.DrawLine(new Vector2(num12, ((Rect)(ref val)).yMin), new Vector2(num12, ((Rect)(ref val)).yMax), GUI.color, 1f);
			}
			Text.Font = (GameFont)1;
		}

		public static void DrawTentFunctionGraph(Rect rect, float maxMultiplierSetting, float slopeK, float peakOffset)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_014f: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_0206: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0250: Unknown result type (might be due to invalid IL or missing references)
			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_0297: Unknown result type (might be due to invalid IL or missing references)
			//IL_036f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_035c: Unknown result type (might be due to invalid IL or missing references)
			//IL_035e: Unknown result type (might be due to invalid IL or missing references)
			//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03db: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ee: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.6f));
			Widgets.DrawBox(rect, 1, (Texture2D)null);
			Rect val = GenUI.ContractedBy(rect, 18f, 10f);
			((Rect)(ref val)).x = ((Rect)(ref val)).x + 10f;
			((Rect)(ref val)).width = ((Rect)(ref val)).width - 15f;
			((Rect)(ref val)).height = ((Rect)(ref val)).height - 10f;
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMin), new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMax), Color.gray, 1f);
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, ((Rect)(ref val)).yMax), new Vector2(((Rect)(ref val)).xMax, ((Rect)(ref val)).yMax), Color.gray, 1f);
			Text.Font = (GameFont)0;
			GUI.color = Color.gray;
			float num = 30f;
			float num2 = 5.1f;
			for (int i = 1; i <= 5; i++)
			{
				float num3 = ((Rect)(ref val)).yMax - (float)i / num2 * ((Rect)(ref val)).height;
				Widgets.Label(new Rect(((Rect)(ref rect)).x + 2f, num3 - 7f, 25f, 15f), i.ToString());
				Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, num3), new Vector2(((Rect)(ref val)).xMax, num3), new Color(1f, 1f, 1f, 0.1f), 1f);
			}
			float num4 = ((Rect)(ref val)).yMax - 0.5f / num2 * ((Rect)(ref val)).height;
			Widgets.Label(new Rect(((Rect)(ref rect)).x + 2f, num4 - 7f, 25f, 15f), "0.5");
			Widgets.DrawLine(new Vector2(((Rect)(ref val)).x, num4), new Vector2(((Rect)(ref val)).xMax, num4), new Color(1f, 1f, 1f, 0.1f), 1f);
			for (int j = 0; j <= 30; j += 5)
			{
				int num5 = ((j == 0) ? 1 : j);
				float num6 = ((Rect)(ref val)).x + (float)(num5 - 1) / (num - 1f) * ((Rect)(ref val)).width;
				Widgets.DrawLine(new Vector2(num6, ((Rect)(ref val)).yMax), new Vector2(num6, ((Rect)(ref val)).yMax + 3f), Color.gray, 1f);
				Rect val2 = new Rect(num6 - 10f, ((Rect)(ref val)).yMax + 3f, 20f, 15f);
				Text.Anchor = (TextAnchor)1;
				Widgets.Label(val2, num5.ToString());
				Text.Anchor = (TextAnchor)0;
			}
			GUI.color = new Color(0.2f, 0.8f, 0.2f);
			Vector2? val3 = null;
			Vector2 val4 = default(Vector2);
			for (float num7 = 1f; num7 <= num; num7 += 1f)
			{
				float num8 = Mathf.Max(0.5f, maxMultiplierSetting - slopeK * Mathf.Abs(num7 - peakOffset));
				float num9 = ((Rect)(ref val)).x + (num7 - 1f) / (num - 1f) * ((Rect)(ref val)).width;
				float num10 = ((Rect)(ref val)).yMax - num8 / num2 * ((Rect)(ref val)).height;
				((Vector2)(ref val4))..ctor(num9, num10);
				if (val3.HasValue)
				{
					Widgets.DrawLine(val3.Value, val4, GUI.color, 1.5f);
				}
				val3 = val4;
			}
			float num11 = ((Rect)(ref val)).x + (peakOffset - 1f) / (num - 1f) * ((Rect)(ref val)).width;
			if (num11 <= ((Rect)(ref val)).xMax)
			{
				GUI.color = new Color(0.2f, 0.6f, 1f, 0.4f);
				Widgets.DrawLine(new Vector2(num11, ((Rect)(ref val)).yMin), new Vector2(num11, ((Rect)(ref val)).yMax), GUI.color, 1f);
			}
			GUI.color = Color.white;
		}
	}
	internal static class VFM_UI_SettingGroup
	{
		private const float MinDistance = 1f;

		private const float MaxDistance = 100f;

		public static void DrawGeneralGroup(Listing_Standard ls, string title, ref bool displayGizmoWhileUndrafted, ref bool enableAutoSelectionForPlayer, ref float burstMinDistance, ref float precisionMinDistance, ref bool enableFireModeForNPC, ref bool autoModeDefaultOn)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Expected O, but got Unknown
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0210: Unknown result type (might be due to invalid IL or missing references)
			//IL_0236: Unknown result type (might be due to invalid IL or missing references)
			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
			//IL_0289: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			float curHeight = ((Listing)ls).CurHeight;
			Listing_Standard val = new Listing_Standard();
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(0f, curHeight, ((Listing)ls).ColumnWidth, 10000f);
			((Listing)val).Begin(GenUI.ContractedBy(val2, 10f));
			Text.Font = (GameFont)2;
			val.Label(title, -1f, (TipSignal?)null);
			Text.Font = (GameFont)1;
			((Listing)val).Gap(6f);
			val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("VFM_AlwaysDisplayGizmo_Label")), ref displayGizmoWhileUndrafted, (string)null, 0f, 1f);
			val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("VFM_EnableAutoSelection_Player_Label")), ref enableAutoSelectionForPlayer, TaggedString.op_Implicit(Translator.Translate("VFM_EnableAutoSelection_Player_Desc")), 0f, 1f);
			if (enableAutoSelectionForPlayer)
			{
				val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("VFM_AutoModeDefaultOn_Label")), ref autoModeDefaultOn, TaggedString.op_Implicit(Translator.Translate("VFM_AutoModeDefaultOn_Desc")), 0f, 1f);
			}
			val.CheckboxLabeled(TaggedString.op_Implicit(Translator.Translate("VFM_EnableAutoSelection_NPC_Label")), ref enableFireModeForNPC, TaggedString.op_Implicit(Translator.Translate("VFM_EnableAutoSelection_NPC_Desc")), 0f, 1f);
			((Listing)val).GapLine(6f);
			((Listing)val).Gap(6f);
			Text.Font = (GameFont)2;
			val.Label(Translator.Translate("VFM_CustomWeaponProfile_Label"), -1f, (string)null);
			Text.Font = (GameFont)1;
			((Listing)val).Gap(6f);
			val.Label(Translator.Translate("VFM_CustomWeaponProfile_Desc"), -1f, (string)null);
			((Listing)val).Gap(6f);
			if (val.ButtonText(TaggedString.op_Implicit(Translator.Translate("VFM_CustomWeaponProfile_Button_Label")), (string)null, 1f))
			{
				Find.WindowStack.Add((Window)(object)new VFM_UI_CustomWeaponWindow());
			}
			((Listing)val).GapLine(6f);
			((Listing)val).Gap(6f);
			Text.Font = (GameFont)2;
			val.Label(Translator.Translate("VFM_AutoSelection_Label"), -1f, (string)null);
			Text.Font = (GameFont)1;
			((Listing)val).Gap(6f);
			val.Label(Translator.Translate("VFM_AutoSelectionThresholds_Label"), -1f, (string)null);
			val.Label(string.Format("{0}: {1:F1}", Translator.Translate("VFM_Burst_Min_Distance"), burstMinDistance), -1f, (TipSignal?)null);
			float num = val.Slider(burstMinDistance, 1f, precisionMinDistance - 1f);
			burstMinDistance = num;
			val.Label(string.Format("{0}: {1:F1}", Translator.Translate("VFM_Precision_Min_Distance"), precisionMinDistance), -1f, (TipSignal?)null);
			float num2 = val.Slider(precisionMinDistance, burstMinDistance + 1f, 100f);
			precisionMinDistance = num2;
			((Listing)val).GapLine(12f);
			DrawPreview(val, burstMinDistance, precisionMinDistance);
			float curHeight2 = ((Listing)val).CurHeight;
			((Listing)val).End();
			float num3 = curHeight2 + 20f;
			Widgets.DrawBox(new Rect(0f, curHeight, ((Listing)ls).ColumnWidth, num3), 1, (Texture2D)null);
			((Listing)ls).Gap(num3 + 15f);
		}

		private static void DrawPreview(Listing_Standard listing, float burstMinDistance, float precisionMinDistance)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			listing.Label(Translator.Translate("VFM_AutoSelection_Range_Preview") + ":", -1f, (string)null);
			listing.Label(string.Format("{0}: 0  ~ {1:F1}", Translator.Translate("VFM_SuppressionMode"), burstMinDistance), -1f, (TipSignal?)null);
			listing.Label(string.Format("{0}: {1:F1} ~ {2:F1}", Translator.Translate("VFM_ShortBurstMode"), burstMinDistance, precisionMinDistance), -1f, (TipSignal?)null);
			listing.Label(string.Format("{0}: {1:F1} +", Translator.Translate("VFM_PrecisionMode"), precisionMinDistance), -1f, (TipSignal?)null);
		}

		public static void DrawGroup(Listing_Standard ls, string title, ref float accuracy, ref float warmup, ref float cooldown, ref BurstShotOption option, ref float linearMult, ref int addBonus, ref float tentMaxMult, ref float tentSlopeK, ref int tentPeak, ref int adaptBonus, ref int adaptPeak, string? description = null, Color? descColor = null)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			float curHeight = ((Listing)ls).CurHeight;
			Rect val = default(Rect);
			((Rect)(ref val))..ctor(0f, curHeight, ((Listing)ls).ColumnWidth, 10000f);
			Listing_Standard val2 = new Listing_Standard();
			((Listing)val2).Begin(GenUI.ContractedBy(val, 10f));
			Text.Font = (GameFont)2;
			val2.Label(title, -1f, (TipSignal?)null);
			Text.Font = (GameFont)1;
			if (description != null)
			{
				GUI.color = (Color)(((??)descColor) ?? Color.white);
				val2.Label(description, -1f, (TipSignal?)null);
				GUI.color = Color.white;
			}
			((Listing)val2).Gap(6f);
			VFM_UI_SliderWithInput.DrawSliderWithInput_Float(val2, TaggedString.op_Implicit(Translator.Translate("VFM_Accuracy_Label")), ref accuracy);
			VFM_UI_SliderWithInput.DrawSliderWithInput_Float(val2, TaggedString.op_Implicit(Translator.Translate("VFM_Warmup_Label")), ref warmup);
			VFM_UI_SliderWithInput.DrawSliderWithInput_Float(val2, TaggedString.op_Implicit(Translator.Translate("VFM_Cooldown_Label")), ref cooldown);
			((Listing)val2).Gap(10f);
			VFM_UI_BurstSection.DrawBurstSection(val2, ref option, ref linearMult, ref addBonus, ref tentMaxMult, ref tentSlopeK, ref tentPeak, ref adaptBonus, ref adaptPeak);
			float curHeight2 = ((Listing)val2).CurHeight;
			((Listing)val2).End();
			float num = curHeight2 + 20f;
			Widgets.DrawBox(new Rect(0f, curHeight, ((Listing)ls).ColumnWidth, num), 1, (Texture2D)null);
			((Listing)ls).Gap(num + 15f);
		}
	}
	internal static class VFM_UI_SliderWithInput
	{
		public static void DrawSliderWithInput_Float(Listing_Standard ls, string label, ref float value, float min = 0.1f, float max = 3f)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			float num = 120f;
			float num2 = 60f;
			float num3 = 10f;
			float num4 = Math.Max(Text.CalcHeight(label, num), 30f);
			Rect rect = ((Listing)ls).GetRect(num4, 1f);
			Rect val = default(Rect);
			((Rect)(ref val))..ctor(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, num, num4);
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(((Rect)(ref val)).xMax + num3, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width - num - num2 - num3 * 2f, num4);
			Rect val3 = new Rect(((Rect)(ref val2)).xMax + num3, ((Rect)(ref rect)).y, num2, 30f);
			Color color = GUI.color;
			GUI.color = Color.white;
			Widgets.Label(val, label);
			GUI.color = color;
			value = Widgets.HorizontalSlider(val2, value, min, max, true, value.ToString("0.00"), (string)null, (string)null, -1f);
			string text = value.ToString("0.00");
			Widgets.TextFieldNumeric<float>(val3, ref value, ref text, min, max);
		}

		public static void DrawSliderWithInput_Int(Listing_Standard ls, string label, ref int value, int min = 0, int max = 20)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			float num = 120f;
			float num2 = 60f;
			float num3 = 10f;
			float num4 = Math.Max(Text.CalcHeight(label, num), 30f);
			Rect rect = ((Listing)ls).GetRect(num4, 1f);
			Rect val = default(Rect);
			((Rect)(ref val))..ctor(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, num, num4);
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(((Rect)(ref val)).xMax + num3, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width - num - num2 - num3 * 2f, num4);
			Rect val3 = new Rect(((Rect)(ref val2)).xMax + num3, ((Rect)(ref rect)).y, num2, 30f);
			Color color = GUI.color;
			GUI.color = Color.white;
			Widgets.Label(val, label);
			GUI.color = color;
			float num5 = value;
			num5 = Widgets.HorizontalSlider(val2, num5, (float)min, (float)max, true, value.ToString("0"), (string)null, (string)null, -1f);
			value = (int)num5;
			string text = value.ToString("0");
			Widgets.TextFieldNumeric<int>(val3, ref value, ref text, (float)min, (float)max);
		}
	}
}
namespace VFM_VanillaFireModes.ModSettingUI.WeaponProfileSetting
{
	public class VFM_UI_AddAllWeaponsConfirmDialog : Window
	{
		private const float ButtonWidth = 120f;

		private const float ButtonHeight = 35f;

		private const float Spacing = 20f;

		private const float BottomPadding = 10f;

		public override Vector2 InitialSize => new Vector2(400f, 300f);

		public VFM_UI_AddAllWeaponsConfirmDialog()
			: base((IWindowDrawing)null)
		{
			base.doCloseX = true;
			base.closeOnClickedOutside = false;
		}

		public override void DoWindowContents(Rect inRect)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			Rect val = default(Rect);
			((Rect)(ref val))..ctor(((Rect)(ref inRect)).x, ((Rect)(ref inRect)).y, ((Rect)(ref inRect)).width, ((Rect)(ref inRect)).height - 35f - 10f);
			Listing_Standard val2 = new Listing_Standard();
			((Listing)val2).Begin(val);
			val2.Label(Translator.Translate("VFM_AddAllWeapons_Dialog_1"), -1f, (string)null);
			((Listing)val2).Gap(10f);
			Color color = GUI.color;
			GUI.color = Color.yellow;
			val2.Label(Translator.Translate("VFM_AddAllWeapons_Dialog_2"), -1f, (string)null);
			GUI.color = color;
			((Listing)val2).End();
			float num = 260f;
			float num2 = ((Rect)(ref inRect)).x + (((Rect)(ref inRect)).width - num) / 2f;
			float num3 = ((Rect)(ref inRect)).yMax - 35f - 10f;
			Rect val3 = new Rect(num2, num3, 120f, 35f);
			Rect val4 = default(Rect);
			((Rect)(ref val4))..ctor(num2 + 120f + 20f, num3, 120f, 35f);
			if (Widgets.ButtonText(val3, TaggedString.op_Implicit(Translator.Translate("VFM_Confirm_Button_Label")), true, true, true, (TextAnchor?)null))
			{
				DoAddAllWeapons();
				((Window)this).Close(true);
			}
			if (Widgets.ButtonText(val4, TaggedString.op_Implicit(Translator.Translate("VFM_Cancel_Button_Label")), true, true, true, (TextAnchor?)null))
			{
				((Window)this).Close(true);
			}
		}

		private void DoAddAllWeapons()
		{
			foreach (ThingDef allRangedWeapon in WeaponProfileUtils.GetAllRangedWeapons())
			{
				if (!VanillaFireModes.settings.CustomWeaponProfiles.ContainsKey(((Def)allRangedWeapon).defName))
				{
					VerbProperties primaryVerb = WeaponProfileUtils.GetPrimaryVerb(allRangedWeapon);
					if (primaryVerb != null)
					{
						WeaponProfileUtils.AddSingleWeapon(((Def)allRangedWeapon).defName, primaryVerb.burstShotCount);
					}
				}
			}
			((ModSettings)VanillaFireModes.settings).Write();
		}
	}
	public class VFM_UI_CustomWeaponWindow : Window
	{
		private const float LeftWidthRatio = 0.33f;

		private const float Padding = 10f;

		private const float LeftRowHeight = 50f;

		private const float RightRowHeight = 100f;

		private const float iconSize = 40f;

		private const float iconTotalWidth = 45f;

		private Vector2 leftScrollPos;

		private Vector2 rightScrollPos;

		private string leftSearch = "";

		private string rightSearch = "";

		private const float rightRowPadding = 5f;

		private const float ModeLabelWidth = 60f;

		private const float ModeValueWidth = 90f;

		private const float InfoBlockWidth = 210f;

		public override Vector2 InitialSize => new Vector2(1200f, 700f);

		private static VanillaFireModesModSetting Settings => VanillaFireModes.settings;

		public VFM_UI_CustomWeaponWindow()
			: base((IWindowDrawing)null)
		{
			base.doCloseX = true;
			base.absorbInputAroundWindow = true;
			base.closeOnClickedOutside = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			GUI.BeginGroup(inRect);
			Rect rect = default(Rect);
			((Rect)(ref rect))..ctor(((Rect)(ref inRect)).x, ((Rect)(ref inRect)).y, ((Rect)(ref inRect)).width * 0.33f - 10f, ((Rect)(ref inRect)).height);
			Rect rect2 = default(Rect);
			((Rect)(ref rect2))..ctor(((Rect)(ref rect)).xMax + 10f, ((Rect)(ref inRect)).y, ((Rect)(ref inRect)).width * 0.66999996f - 10f, ((Rect)(ref inRect)).height);
			DrawLeftBlock(rect);
			DrawRightBlock(rect2);
			GUI.EndGroup();
		}

		private IEnumerable<KeyValuePair<string, VFM_WeaponProfile>> GetProfiles()
		{
			return Settings.CustomWeaponProfiles.Where<KeyValuePair<string, VFM_WeaponProfile>>(delegate(KeyValuePair<string, VFM_WeaponProfile> kv)
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(kv.Value.defName);
				return namedSilentFail != null && (GenText.NullOrEmpty(rightSearch) || ((Def)namedSilentFail).label.ToLower().Contains(rightSearch.ToLower()));
			});
		}

		private void DrawLeftBlock(Rect rect)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Expected O, but got Unknown
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawMenuSection(rect);
			Rect val = GenUI.ContractedBy(rect, 10f);
			float y = ((Rect)(ref val)).y;
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 30f);
			leftSearch = Widgets.TextField(val2, leftSearch);
			y += 35f;
			if (Widgets.ButtonText(new Rect(((Rect)(ref val)).x + ((Rect)(ref val)).width - 120f, y, 120f, 30f), TaggedString.op_Implicit(Translator.Translate("VFM_Profile_AddAllWeapons_Label")), true, true, true, (TextAnchor?)null))
			{
				Find.WindowStack.Add((Window)(object)new VFM_UI_AddAllWeaponsConfirmDialog());
			}
			y += 35f;
			Rect val3 = default(Rect);
			((Rect)(ref val3))..ctor(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, ((Rect)(ref val)).height - (y - ((Rect)(ref val)).y));
			Rect val4 = default(Rect);
			((Rect)(ref val4))..ctor(0f, 0f, ((Rect)(ref val3)).width - 16f, GetLeftListHeight());
			Widgets.BeginScrollView(val3, ref leftScrollPos, val4, true);
			Listing_Standard val5 = new Listing_Standard();
			((Listing)val5).Begin(val4);
			foreach (ThingDef item in WeaponProfileUtils.GetAllRangedWeaponsWithSearch(leftSearch))
			{
				DrawLeftItem(((Listing)val5).GetRect(50f, 1f), item);
			}
			((Listing)val5).End();
			Widgets.EndScrollView();
		}

		private void DrawLeftItem(Rect rect, ThingDef def)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawHighlightIfMouseover(rect);
			TooltipHandler.TipRegion(rect, TipSignal.op_Implicit(((Def)def).defName));
			float x = ((Rect)(ref rect)).x;
			Widgets.DrawTextureFitted(new Rect(x, ((Rect)(ref rect)).y + 5f, 40f, 40f), (Texture)(object)((BuildableDef)def).uiIcon, 1f, 1f);
			x += 45f;
			Rect val = new Rect(x, ((Rect)(ref rect)).y, 200f, ((Rect)(ref rect)).height);
			Text.Anchor = (TextAnchor)3;
			Widgets.Label(val, ((Def)def).LabelCap);
			Text.Anchor = (TextAnchor)0;
			x += 210f;
			if (Widgets.ButtonImage(new Rect(((Rect)(ref rect)).xMax - 30f, ((Rect)(ref rect)).y + 10f, 24f, 24f), TexButton.Reveal, true, (string)null) && !Settings.CustomWeaponProfiles.ContainsKey(((Def)def).defName))
			{
				VerbProperties primaryVerb = WeaponProfileUtils.GetPrimaryVerb(def);
				if (primaryVerb != null)
				{
					WeaponProfileUtils.AddSingleWeapon(((Def)def).defName, primaryVerb.burstShotCount);
				}
			}
		}

		private float GetLeftListHeight()
		{
			return (float)WeaponProfileUtils.GetAllRangedWeaponsWithSearch(leftSearch).Count() * 50f;
		}

		private void DrawRightBlock(Rect rect)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawMenuSection(rect);
			Rect val = GenUI.ContractedBy(rect, 10f);
			float y = ((Rect)(ref val)).y;
			Rect val2 = default(Rect);
			((Rect)(ref val2))..ctor(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 30f);
			rightSearch = Widgets.TextField(val2, rightSearch);
			y += 35f;
			if (Widgets.ButtonText(new Rect(((Rect)(ref val)).x + ((Rect)(ref val)).width - 120f, y, 120f, 30f), TaggedString.op_Implicit(Translator.Translate("VFM_Profile_RemoveAllProfile_Label")), true, true, true, (TextAnchor?)null))
			{
				Settings.ResetWeaponProfileSetting();
			}
			y += 35f;
			Rect rect2 = default(Rect);
			((Rect)(ref rect2))..ctor(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 30f);
			DrawRightHeader(rect2);
			y += 35f;
			Rect val3 = default(Rect);
			((Rect)(ref val3))..ctor(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, ((Rect)(ref val)).height - (y - ((Rect)(ref val)).y));
			Rect val4 = default(Rect);
			((Rect)(ref val4))..ctor(0f, 0f, ((Rect)(ref val3)).width - 16f, GetRightListHeight());
			Widgets.BeginScrollView(val3, ref rightScrollPos, val4, true);
			float num = 0f;
			List<string> list = new List<string>();
			Rect rect3 = default(Rect);
			foreach (KeyValuePair<string, VFM_WeaponProfile> profile in GetProfiles())
			{
				ThingDef namedSilentFail = DefDatabase<ThingDef>.GetNamedSilentFail(profile.Value.defName);
				if (namedSilentFail != null)
				{
					num += 5f;
					((Rect)(ref rect3))..ctor(0f, num, ((Rect)(ref val4)).width, 100f);
					DrawRightItem(rect3, profile.Key, profile.Value, namedSilentFail, list);
					num += 100f;
				}
			}
			foreach (string item in list)
			{
				if (item != null)
				{
					Settings.CustomWeaponProfiles.Remove(item);
				}
			}
			Widgets.EndScrollView();
		}

		private void DrawRightHeader(Rect rect)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			float num = ((Rect)(ref rect)).x + 5f + 45f + 210f + 60f;
			DrawHeaderCell(new Rect(num, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), TaggedString.op_Implicit(Translator.Translate("VFM_Accuracy_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_Accuracy_Label")));
			num += 90f;
			DrawHeaderCell(new Rect(num, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), TaggedString.op_Implicit(Translator.Translate("VFM_Warmup_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_Warmup_Label")));
			num += 90f;
			DrawHeaderCell(new Rect(num, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), TaggedString.op_Implicit(Translator.Translate("VFM_Cooldown_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_Cooldown_Label")));
			num += 90f;
			DrawHeaderCell(new Rect(num, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), TaggedString.op_Implicit(Translator.Translate("VFM_BurstCount_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_BurstCount_Label")));
		}

		private void DrawHeaderCell(Rect rect, string label, string tooltip)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			Text.Anchor = (TextAnchor)4;
			Widgets.Label(rect, label);
			Text.Anchor = (TextAnchor)0;
			TooltipHandler.TipRegion(rect, TipSignal.op_Implicit(tooltip));
		}

		private void DrawRightItem(Rect rect, string key, VFM_WeaponProfile profile, ThingDef def, List<string> keysToDelete)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_017a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0217: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0246: Unknown result type (might be due to invalid IL or missing references)
			//IL_024b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawHighlightIfMouseover(rect);
			float num = ((Rect)(ref rect)).x + 5f;
			Widgets.ThingIcon(new Rect(num, ((Rect)(ref rect)).y + (((Rect)(ref rect)).height - 40f) / 2f, 40f, 40f), def, (ThingDef)null, (ThingStyleDef)null, 1f, (Color?)null, (int?)null, 1f);
			num += 45f;
			Text.Anchor = (TextAnchor)3;
			Rect val = new Rect(num, ((Rect)(ref rect)).y, 200f, ((Rect)(ref rect)).height);
			Widgets.Label(val, ((Def)def).LabelCap);
			TooltipHandler.TipRegion(val, TipSignal.op_Implicit(profile.defName));
			Text.Anchor = (TextAnchor)0;
			num += 210f;
			float num2 = ((Rect)(ref rect)).height / 4f;
			float num3 = 420f;
			DrawModeRow(new Rect(num, ((Rect)(ref rect)).y + num2 * 0f, num3, num2), TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode")), profile.Default);
			DrawModeRow(new Rect(num, ((Rect)(ref rect)).y + num2 * 1f, num3, num2), TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode")), profile.Precision);
			DrawModeRow(new Rect(num, ((Rect)(ref rect)).y + num2 * 2f, num3, num2), TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode")), profile.Burst);
			DrawModeRow(new Rect(num, ((Rect)(ref rect)).y + num2 * 3f, num3, num2), TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode_Abbr")), TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode")), profile.Suppression);
			float num4 = ((Rect)(ref rect)).xMax - 40f;
			Rect val2 = new Rect(num4, ((Rect)(ref rect)).y + 5f, 24f, 24f);
			if (Widgets.ButtonImage(val2, TexButton.Delete, true, (string)null))
			{
				keysToDelete.Add(key);
			}
			TooltipHandler.TipRegion(val2, TipSignal.op_Implicit(Translator.Translate("VFM_Delete_Button_Label")));
			Rect val3 = new Rect(num4, ((Rect)(ref rect)).y + ((Rect)(ref rect)).height - 30f, 24f, 24f);
			if (Widgets.ButtonImage(val3, TexButton.Rename, true, (string)null))
			{
				VerbProperties primaryVerb = WeaponProfileUtils.GetPrimaryVerb(def);
				if (primaryVerb != null)
				{
					Find.WindowStack.Add((Window)(object)new VFM_UI_EditWeaponProfileDialog(profile, primaryVerb.burstShotCount));
				}
			}
			TooltipHandler.TipRegion(val3, TipSignal.op_Implicit(Translator.Translate("VFM_Edit_Button_Label")));
			Widgets.DrawBox(rect, 1, (Texture2D)null);
		}

		private void DrawModeRow(Rect rect, string label, string tooltipStr, VFM_FireModeProfile data)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			float x = ((Rect)(ref rect)).x;
			Rect val = new Rect(x, ((Rect)(ref rect)).y, 60f, ((Rect)(ref rect)).height);
			Text.Anchor = (TextAnchor)3;
			Widgets.Label(val, label);
			Text.Anchor = (TextAnchor)0;
			TooltipHandler.TipRegion(val, TipSignal.op_Implicit(tooltipStr));
			x += 60f;
			float num = data?.accuracyMultiplier ?? 1f;
			float num2 = data?.warmupMultiplier ?? 1f;
			float num3 = data?.cooldownMultiplier ?? 1f;
			int num4 = data?.burstShotCount ?? 1;
			DrawValueCell(new Rect(x, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), num.ToString("0.##"));
			x += 90f;
			DrawValueCell(new Rect(x, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), num2.ToString("0.##"));
			x += 90f;
			DrawValueCell(new Rect(x, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), num3.ToString("0.##"));
			x += 90f;
			DrawValueCell(new Rect(x, ((Rect)(ref rect)).y, 90f, ((Rect)(ref rect)).height), num4.ToString());
		}

		private void DrawValueCell(Rect rect, string text)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			Text.Anchor = (TextAnchor)4;
			Widgets.Label(rect, text);
			Text.Anchor = (TextAnchor)0;
		}

		private float GetRightListHeight()
		{
			return (float)GetProfiles().Count() * 105f;
		}
	}
	public class VFM_UI_EditWeaponProfileDialog : Window
	{
		private readonly VFM_WeaponProfile profile;

		private readonly int baseBurstShotCount;

		private const float labelHeight = 25f;

		private const float fieldHeight = 50f;

		private const float blockHeight = 120f;

		private const float floatInputMin = 0.1f;

		private const float floatInputMax = 100f;

		private const int intInputMin = 1;

		private const int intInputMax = 100;

		public override Vector2 InitialSize => new Vector2(700f, 600f);

		public VFM_UI_EditWeaponProfileDialog(VFM_WeaponProfile profile, int baseBurstShotCount)
			: base((IWindowDrawing)null)
		{
			this.profile = profile;
			this.baseBurstShotCount = baseBurstShotCount;
			base.doCloseX = true;
			base.draggable = true;
			base.closeOnClickedOutside = false;
			base.doCloseButton = true;
		}

		public override void DoWindowContents(Rect inRect)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			float y = ((Rect)(ref inRect)).y;
			DrawModeEditor(new Rect(((Rect)(ref inRect)).x, y, ((Rect)(ref inRect)).width, 120f), TaggedString.op_Implicit(Translator.Translate("VFM_DefaultMode")), profile.Default);
			y += 130f;
			DrawModeEditor(new Rect(((Rect)(ref inRect)).x, y, ((Rect)(ref inRect)).width, 120f), TaggedString.op_Implicit(Translator.Translate("VFM_PrecisionMode")), profile.Precision);
			y += 130f;
			DrawModeEditor(new Rect(((Rect)(ref inRect)).x, y, ((Rect)(ref inRect)).width, 120f), TaggedString.op_Implicit(Translator.Translate("VFM_ShortBurstMode")), profile.Burst);
			y += 130f;
			DrawModeEditor(new Rect(((Rect)(ref inRect)).x, y, ((Rect)(ref inRect)).width, 120f), TaggedString.op_Implicit(Translator.Translate("VFM_SuppressionMode")), profile.Suppression);
			y += 130f;
			if (Widgets.ButtonText(new Rect(((Rect)(ref inRect)).x + ((Rect)(ref inRect)).width - 130f, y, 120f, 35f), TaggedString.op_Implicit(Translator.Translate("VFM_ResetButton_Label")), true, true, true, (TextAnchor?)null))
			{
				profile.Default = VFM_FireModeProfile.CreateDefault(baseBurstShotCount);
				profile.Precision = VFM_FireModeProfile.CreatePrecision(baseBurstShotCount);
				profile.Burst = VFM_FireModeProfile.CreateBurst(baseBurstShotCount);
				profile.Suppression = VFM_FireModeProfile.CreateSuppression(baseBurstShotCount);
			}
		}

		public override void PreClose()
		{
			((Window)this).PreClose();
			((ModSettings)VanillaFireModes.settings).Write();
		}

		private void DrawModeEditor(Rect rect, string label, VFM_FireModeProfile data)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			Widgets.DrawMenuSection(rect);
			Rect val = GenUI.ContractedBy(rect, 8f);
			float y = ((Rect)(ref val)).y;
			Text.Anchor = (TextAnchor)3;
			Widgets.Label(new Rect(((Rect)(ref val)).x, y, ((Rect)(ref val)).width, 30f), label);
			Text.Anchor = (TextAnchor)0;
			y += 35f;
			float num = ((Rect)(ref val)).width / 4f;
			DrawFloatField(new Rect(((Rect)(ref val)).x + num * 0f, y, num, 50f), TaggedString.op_Implicit(Translator.Translate("VFM_Accuracy_Label")), ref data.accuracyMultiplier, 0.1f, 100f);
			DrawFloatField(new Rect(((Rect)(ref val)).x + num * 1f, y, num, 50f), TaggedString.op_Implicit(Translator.Translate("VFM_Warmup_Label")), ref data.warmupMultiplier, 0.1f, 100f);
			DrawFloatField(new Rect(((Rect)(ref val)).x + num * 2f, y, num, 50f), TaggedString.op_Implicit(Translator.Translate("VFM_Cooldown_Label")), ref data.cooldownMultiplier, 0.1f, 100f);
			DrawIntField(new Rect(((Rect)(ref val)).x + num * 3f, y, num, 50f), TaggedString.op_Implicit(Translator.Translate("VFM_BurstCount_Label")), ref data.burstShotCount, 1, 100);
		}

		private void DrawFloatField(Rect rect, string label, ref float value, float min, float max)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			Widgets.Label(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width, 25f), label);
			Rect val = new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + 25f, ((Rect)(ref rect)).width, ((Rect)(ref rect)).height - 25f);
			string text = value.ToString("0.##");
			if (float.TryParse(Widgets.TextField(val, text), out var result))
			{
				value = Mathf.Clamp(result, min, max);
			}
		}

		private void DrawIntField(Rect rect, string label, ref int value, int min, int max)
		{
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			Widgets.Label(new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y, ((Rect)(ref rect)).width, 25f), label);
			Rect val = new Rect(((Rect)(ref rect)).x, ((Rect)(ref rect)).y + 25f, ((Rect)(ref rect)).width, ((Rect)(ref rect)).height - 25f);
			string text = value.ToString();
			if (int.TryParse(Widgets.TextField(val, text), out var result))
			{
				value = Mathf.Clamp(result, min, max);
			}
		}
	}
}
namespace VFM_VanillaFireModes.Comps
{
	public class VFM_CompProperties_FireMode : CompProperties
	{
		public VFM_CompProperties_FireMode()
		{
			base.compClass = typeof(VFM_PawnCompFireMode);
		}
	}
	[StaticConstructorOnStartup]
	public static class VFM_CompsPatcher
	{
		static VFM_CompsPatcher()
		{
			foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
			{
				if (allDef.race != null && (allDef.race.Humanlike || allDef.race.ToolUser))
				{
					if (allDef.comps == null)
					{
						allDef.comps = new List<CompProperties>();
					}
					if (!GenCollection.Any<CompProperties>(allDef.comps, (Predicate<CompProperties>)((CompProperties c) => c is VFM_CompProperties_FireMode)))
					{
						allDef.comps.Add((CompProperties)(object)new VFM_CompProperties_FireMode());
					}
				}
			}
		}
	}
	public class VFM_PawnCompFireMode : ThingComp
	{
		private VFM_FireMode mode;

		private bool enableAutoSelection = FireModeDB.Settings?.autoModeDefaultOn ?? false;

		public VFM_FireMode curMode
		{
			get
			{
				return mode;
			}
			set
			{
				mode = value;
			}
		}

		public bool curEnableAutoSelection
		{
			get
			{
				return enableAutoSelection;
			}
			set
			{
				enableAutoSelection = value;
			}
		}

		public override void PostExposeData()
		{
			Scribe_Values.Look<VFM_FireMode>(ref mode, "VFM_fireMode", VFM_FireMode.Default, false);
			Scribe_Values.Look<bool>(ref enableAutoSelection, "VFM_autoSelection", false, false);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			ThingWithComps parent = base.parent;
			Pawn val = (Pawn)(object)((parent is Pawn) ? parent : null);
			if (val == null || (!val.IsColonistPlayerControlled && !val.IsColonyMechPlayerControlled) || (!val.Drafted && !FireModeDB.Settings.alwaysDisplayGizmo) || !HasRemoteWeapon(val))
			{
				yield break;
			}
			if (!curEnableAutoSelection || !FireModeDB.Settings.enableAutoSelectionForPlayer)
			{
				yield return (Gizmo)new Command_Action
				{
					icon = (Texture)(object)GetIconFor(curMode),
					defaultLabel = Utils.GetFireModeLabelFor(curMode),
					defaultDesc = TaggedString.op_Implicit(Translator.Translate("VFM_SwitchGizmoDesc")),
					action = delegate
					{
						curMode = (VFM_FireMode)((int)(curMode + 1) % 4);
						SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, (Map)null);
					}
				};
			}
			if (FireModeDB.Settings.enableAutoSelectionForPlayer)
			{
				yield return (Gizmo)new Command_Toggle
				{
					icon = (Texture)(object)VFM_IconTexture.VFM_Auto_Icon,
					defaultLabel = TaggedString.op_Implicit(Translator.Translate("VFM_AutoSelection")),
					defaultDesc = TaggedString.op_Implicit(Translator.Translate("VFM_AutoSelection_Desc")),
					isActive = () => curEnableAutoSelection,
					toggleAction = delegate
					{
						curEnableAutoSelection = !curEnableAutoSelection;
						curMode = VFM_FireMode.Default;
						SoundStarter.PlayOneShotOnCamera(SoundDefOf.Tick_High, (Map)null);
					}
				};
			}
		}

		private bool HasRemoteWeapon(Pawn pawn)
		{
			Pawn_EquipmentTracker equipment = pawn.equipment;
			bool? obj;
			if (equipment == null)
			{
				obj = null;
			}
			else
			{
				ThingWithComps primary = equipment.Primary;
				obj = ((primary != null) ? new bool?(((Thing)primary).def.IsRangedWeapon) : ((bool?)null));
			}
			bool? flag = obj;
			return flag == true;
		}

		private Texture2D GetIconFor(VFM_FireMode mode)
		{
			return (Texture2D)(mode switch
			{
				VFM_FireMode.Precision => VFM_IconTexture.VFM_Precision_Icon, 
				VFM_FireMode.Burst => VFM_IconTexture.VFM_Burst_Icon, 
				VFM_FireMode.Suppression => VFM_IconTexture.VFM_Suppression_Icon, 
				_ => VFM_IconTexture.VFM_Default_Icon, 
			});
		}
	}
}
