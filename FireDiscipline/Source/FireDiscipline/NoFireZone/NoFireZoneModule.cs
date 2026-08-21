using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FireDiscipline.Core;
using HarmonyLib;
using RimWorld;
using Verse;

namespace FireDiscipline.NoFireZone
{
    /// <summary>
    /// [TÍNH NĂNG / FEATURE]: Vùng cấm bắn - người chơi khoanh vùng mà pháo tự động không được nhắm vào.
    /// [TẠI SAO LÀM THẾ / RATIONALE]: Bật mặc định, khác với các module đổi cân bằng. Module này không
    ///     sửa một con số nào của game: nó chỉ thêm một công cụ để người chơi nói rõ ý định. Không có
    ///     vùng cấm nào được vẽ thì hành vi giống hệt vanilla.
    /// [Ý NGHĨA & CƠ CHẾ / MECHANICS]: Giai đoạn 1 chưa có Harmony patch nào - chỉ có lưới, designator
    ///     và hiển thị. Việc chặn tự động ngắm sẽ vào ApplyPatches ở giai đoạn 2.
    /// </summary>
    public class NoFireZoneModule : IModule
    {
        public const string Id = "NoFireZone";

        public string ModuleId => Id;
        public string DisplayName => "No-Fire Zone";
        public string Description => "Adds a zone tool marking cells that player turrets must not auto-target. Manual force-target always overrides it.";
        public bool DefaultEnabled => true;
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Đọc thẳng từ danh sách module trong settings thay vì một cờ bool riêng. Module này không có
        /// tuỳ chỉnh số nào để cần một mục riêng trong cửa sổ settings, nên checkbox ở "Core Active
        /// Modules" là nút bật/tắt duy nhất - không có hai chỗ điều khiển cùng một thứ.
        /// </summary>
        public bool ShouldEnable()
        {
            return FireDisciplineMod.Settings?.IsModuleEnabled(this) ?? DefaultEnabled;
        }

        /// <summary>
        /// Tiêm comp "tuân thủ vùng cấm" vào mọi ThingDef dùng Building_TurretGun hoặc lớp con của nó.
        ///
        /// Làm bằng code chứ không phải PatchOperation XML vì XML chỉ khớp được thingClass ghi đúng chữ
        /// "Building_TurretGun"; turret của mod thường dùng lớp con riêng và sẽ bị bỏ sót. IsAssignableFrom
        /// bắt hết cả hai.
        /// </summary>
        public void OnStartup()
        {
            int injected = 0;

            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.thingClass == null || !typeof(Building_TurretGun).IsAssignableFrom(def.thingClass)) continue;

                if (def.comps == null)
                {
                    def.comps = new List<CompProperties>();
                }
                else if (def.comps.Any(c => c.compClass == typeof(CompNoFireZoneObedience)))
                {
                    // Def hot-reload có thể chạy lại hàm này; đừng chồng comp thứ hai lên cùng một def.
                    continue;
                }

                def.comps.Add(new CompProperties { compClass = typeof(CompNoFireZoneObedience) });
                injected++;
            }

            Log.Message($"[Fire Discipline] No-Fire Zone: obedience comp injected into {injected} turret defs.");
        }

        public void ApplyPatches(Harmony harmony)
        {
            // IsValidTarget là private - AccessTools tìm được, nhưng nếu một bản RimWorld sau này đổi
            // tên nó thì phải im lặng bỏ qua chứ không được ném ra: giai đoạn 1 (lưới + designator)
            // vẫn dùng được bình thường khi không có patch này.
            var isValidTarget = AccessTools.Method(typeof(Building_TurretGun), "IsValidTarget");
            if (isValidTarget == null)
            {
                Log.Warning("[Fire Discipline] No-Fire Zone: Building_TurretGun.IsValidTarget not found. "
                    + "The zone can still be drawn, but turrets will not respect it.");
                return;
            }

            var postfix = typeof(Patch_TurretAutoTarget).GetMethod(
                nameof(Patch_TurretAutoTarget.Postfix_IsValidTarget),
                BindingFlags.Static | BindingFlags.Public);

            harmony.Patch(isValidTarget, postfix: new HarmonyMethod(postfix));
            Log.Message("[Fire Discipline] Patched Building_TurretGun.IsValidTarget for No-Fire Zone.");
        }
    }
}
