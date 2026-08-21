# 05 — Kiến trúc kỹ thuật

> Target: RimWorld 1.6. DLC: Royalty, Ideology, Biotech, Anomaly (đủ).
> Harmony là dependency duy nhất bắt buộc ngoài DLC.

---

## 0. Kỷ luật kiến trúc (K1–K3)

Ba luật này đến từ `00-vision.md` mục 4b. Chúng không phải để phát hành framework —
chúng để chính tác giả thêm hero/trục sau này mà không phải sửa code lõi.

### K1 — Namespace tách đôi

```
RimwardExiles.Core       ← engine, def types, ITab, debug, loader
RimwardExiles.Odyssey    ← campaign: hero, quest, decision cụ thể
```

packageId: `william.rimwardexiles` — **không đổi được sau khi có save**.
Def prefix: `RWX_` — cũng không đổi được.

**`Core` không được `using RimwardExiles.Odyssey`.** Một assembly, hai namespace —
compiler ép giữ ranh giới. Nếu sau này muốn tách assembly thì chỉ là việc cơ học.

Kiểm tra nhanh: grep `using RimwardExiles.Odyssey` trong thư mục `Source/Core/` phải ra rỗng.

### K2 — Zero hardcode trong Core

`Core` không được chứa tên trục hay tên hero dưới dạng chuỗi hoặc nhánh điều kiện.
Tác động của trục khai báo trong XML; `Core` chỉ chạy engine tra bảng:

```xml
<RimwardExiles.HeroValueEffectDef>
  <defName>RWX_Effect_PurityBody</defName>
  <axis>RWX_Purity</axis>
  <effects>
    <li><source>BodyPart_Steel</source><perUnit>-8</perUnit></li>
    <li><source>BodyPart_Flesh</source><perUnit>-11</perUnit></li>
    <li><source>BodyPart_Intact</source><perUnit>5</perUnit></li>
    <li><source>ColonyEnhancement</source><perUnit>-5</perUnit><cap>-25</cap></li>
  </effects>
</RimwardExiles.HeroValueEffectDef>
```

`Core` định nghĩa tập `source` hợp lệ (một enum hoặc `HeroSourceDef`), rồi tra bảng.
Thêm trục mới = thêm XML, không recompile.

### K3 — Reaction map là Def

Bảng "incident vanilla → delta vector" **không** được là `static Dictionary` trong code.

```xml
<RimwardExiles.HeroReactionDef>
  <defName>RWX_React_SlaveTrade</defName>
  <triggers>
    <li><incident>TraderCaravanArrival</incident><condition>SlaveTraded</condition></li>
  </triggers>
  <delta>
    <li><axis>RWX_Mercy</axis><amount>-15</amount></li>
    <li><axis>RWX_Order</axis><amount>5</amount></li>
  </delta>
  <memoryHalfLifeDays>20</memoryHalfLifeDays>
</RimwardExiles.HeroReactionDef>
```

Tinh chỉnh cân bằng bằng cách sửa XML rồi reload — không recompile, không restart.
Đây là kỷ luật tiết kiệm nhiều thời gian nhất trong giai đoạn cân bằng.

### Nạp pawn

Dự án dùng **snapshot-based duy nhất** (`HeroPawnLoader`), chấp nhận khoá vào modlist cá nhân (R6).
Không có lớp trừu tượng provider.

---

## 1. Cấu trúc thư mục

```
RimwardExiles/
├── About/
│   ├── About.xml
│   └── Preview.png
├── Assemblies/
│   └── RimwardExiles.dll
├── Defs/
│   ├── HeroValueDefs/
│   ├── HeroValueEffectDefs/   ← K2: tác động của trục, không hardcode
│   ├── HeroCreedDefs/
│   ├── HeroDecisionDefs/
│   ├── HeroReactionDefs/      ← K3: incident vanilla → delta
│   ├── HeroBodyPathDefs/      ← override phân loại ModPath
│   ├── IncidentDefs/
│   ├── QuestScriptDefs/
│   ├── PawnRelationDefs/
│   ├── HediffDefs/
│   └── ThoughtDefs/
├── Languages/vi/
├── Patches/                    ← XML patch cho mod ngoài
└── Presets/                    ← snapshot pawn (.xml)
    └── manifest.xml            ← bảng phụ thuộc mod
```

Presets nằm **trong mod**, không nằm ở `Config/`. Lý do: version cùng với code,
dễ backup, và không bị mất khi reset config.

---

## 2. Danh sách class

### Lõi trạng thái

| Class | Trách nhiệm |
|---|---|
| `GameComponent_Exiles` | Giữ toàn bộ state. Tick chậm (2500). ExposeData. Điểm vào duy nhất của hệ thống |
| `HeroState` | Per-hero: loyalty, creed ref, memory list, cờ trạng thái, ngưỡng |
| `HeroMemory` | Một ký ức: eventDef, targetPawnID, tick, weight, decayable |
| `LoyaltyFactor` | struct: label + delta. Đơn vị hiển thị của ITab |

### Tính toán

| Class | Trách nhiệm |
|---|---|
| `LoyaltyCalculator` | static. Gom factor từ mọi nguồn, trả `List<LoyaltyFactor>` |
| `BodyPathClassifier` | static. Phân loại HediffDef → ModPath. Cache lúc startup |
| `CreedEvaluator` | static. Tích vô hướng delta vector × creed weight. Phát hiện tension |

### Nội dung

| Class | Trách nhiệm |
|---|---|
| `HeroDecisionDef` | Def. Letter text, options, delta vector mỗi option, cost |
| `HeroDecisionWorker` | Chọn decision theo weight, bắn letter, áp dụng kết quả |
| `IncidentWorker_HeroDecision` | Cầu nối storyteller → decision |
| `HeroReactionDef` | **Def** (K3). Incident vanilla → delta vector |
| `ReactionResolver` | Tra `HeroReactionDef` khi P1 bắn. Không chứa dữ liệu |
| `HeroValueEffectDef` | **Def** (K2). Trục → tác động theo source |
| `EffectResolver` | Áp `HeroValueEffectDef`. `Core` không biết trục nào tồn tại |

### Pawn

| Class | Trách nhiệm |
|---|---|
| `HeroPawnLoader` | static. Load preset qua Scribe, Sanitize, validate def |
| `QuestNode_LoadUniquePawn` | QuestNode. Load preset, PassToWorld, set slate |
| `Hediff_Conflicted` | Hediff mâu thuẫn nội tâm |
| `HediffComp_Aura` | (v2) Aura cho Purity path |

### UI & Debug

| Class | Trách nhiệm |
|---|---|
| `ITab_Pawn_Loyalty` | Hiển thị loyalty, xu hướng, factor list, memory list |
| `ExilesDebugActions` | Dev Mode actions |

**Tổng: 17 class.** Không class nào chạy trong hot path.

Phân bổ namespace theo K1: **toàn bộ 17 class nằm trong `Core`.** `Odyssey` chỉ chứa XML
(hero, quest, decision, reaction). Khi `Odyssey` phát sinh nhu cầu cần class riêng, xử lý bằng
cách thêm điểm mở rộng vào `Core`.

---

## 3. Def schema mới

### `HeroDecisionDef`

```xml
<RimwardExiles.HeroDecisionDef>
  <defName>RWX_Decision_Example</defName>
  <letterLabel>...</letterLabel>
  <letterText>...</letterText>
  <letterDef>NeutralEvent</letterDef>

  <baseWeight>1.0</baseWeight>
  <minRefireDays>12</minRefireDays>

  <!-- chỉ xuất hiện khi đủ điều kiện -->
  <requiredHeroes><li>RWX_Hero_A</li></requiredHeroes>
  <requiresAllHeroes>false</requiresAllHeroes>

  <!-- weight nhân lên khi hero liên quan đang bất mãn -->
  <escalationAxes><li>RWX_Loyalty</li></escalationAxes>
  <escalationMultiplier>2.5</escalationMultiplier>

  <options>
    <li>
      <label>...</label>
      <description>...</description>
      <delta>
        <li><axis>RWX_Loyalty</axis><amount>35</amount></li>
        <li><axis>RWX_Mercy</axis><amount>-15</amount></li>
      </delta>
      <silverCost>0</silverCost>
      <factionGoodwill>
        <li><faction>...</faction><amount>-30</amount></li>
      </factionGoodwill>
      <createsMemory>true</createsMemory>
      <memoryDecayable>true</memoryDecayable>
      <requeueDays>0</requeueDays>
    </li>
  </options>
</RimwardExiles.HeroDecisionDef>
```

**Bắt buộc:** mỗi decision phải có ít nhất một option với `silverCost > 0` hoặc chi phí
tương đương, và delta không âm với bất kỳ hero nào — đó là option "giữ tất cả" theo nguyên tắc N2.

**Ngoại lệ:** decision đánh dấu `<formativeDecision>true</formativeDecision>` được miễn yêu cầu trên.
Đây là loại decision không hỏi "anh giữ ai" mà hỏi "anh là loại người nào" — mọi option đều
hợp lệ, và hệ quả nằm ở `HeroDispositionDef` chứ không ở chi phí. Dùng tối đa **một lần mỗi hero**,
ở tầng 1.

### `HeroDispositionDef`

```xml
<RimwardExiles.HeroDispositionDef>
  <defName>RWX_Disp_Transactional</defName>
  <label>Quan hệ giao dịch</label>
  <reason>Anh nhìn người của tôi như hàng hoá.</reason>   <!-- giọng của hero, hiện trong ITab -->
  <gainMultiplier>0.55</gainMultiplier>
  <lossMultiplier>1.15</lossMultiplier>
  <gatedOptions>
    <li>RWX_Opt_Trust_Tier2</li>
    <li>RWX_Opt_Trust_Tier3</li>
  </gatedOptions>
  <bonusOptions />
  <replaceableBy>
    <li>RWX_Decision_SecondChance</li>   <!-- decision muộn có thể gỡ -->
  </replaceableBy>
</RimwardExiles.HeroDispositionDef>
```

`gainMultiplier` không được xuống dưới 0.3 (`02` mục 6b).

### `HeroBodyPathDef` — override phân loại

```xml
<RimwardExiles.HeroBodyPathDef>
  <defName>RWX_BodyPathOverrides</defName>
  <entries>
    <li><hediff>SomeModHediff</hediff><path>Flesh</path></li>
  </entries>
  <packageIdRules>
    <li><packageIdContains>evolvedorgans</packageIdContains><path>Flesh</path></li>
  </packageIdRules>
</RimwardExiles.HeroBodyPathDef>
```

Không recompile khi phân loại sai. Sửa XML là xong.

---

## 3b. Tập `source` hợp lệ

`Core` định nghĩa tập này (enum hoặc `HeroSourceDef`); `HeroValueEffectDef` chỉ được tham chiếu
tới các giá trị dưới đây. Thêm `source` mới là mở rộng engine — cân nhắc kỹ, không thêm bừa.

### Trạng thái cơ thể

| Source | Đọc từ |
|---|---|
| `BodyPart_Steel` | Đếm hediff phân loại Steel |
| `BodyPart_Flesh` | Đếm hediff phân loại Flesh |
| `BodyPart_Intact` | Số bộ phận tự nhiên còn nguyên |
| `BodyPart_Missing` | `Hediff_MissingPart` chưa thay |
| `AvgPartEfficiency` | Trung bình `addedPartProps.partEfficiency` |

### Biotech

| Source | Đọc từ |
|---|---|
| `Gene_Inherited` | Số endogene |
| `Gene_Implanted` | Số xenogene đã cấy |
| `ColonyXenotypeDiversity` | Số xenotype khác nhau trong colony |
| `MechanitorLevel` | Bandwidth / cấp mechanitor |
| `MechCount` | Số mech thuộc colony |
| `ChildrenInColony` | Số pawn dưới tuổi trưởng thành |
| `GrowthVat_InUse` | Số growth vat đang hoạt động |
| `Sanguophage_Present` | Có sanguophage trong colony |

### Royalty

| Source | Đọc từ |
|---|---|
| `PsylinkLevel` | `Hediff_Psylink` severity |
| `RoyalTitle` | Cấp title cao nhất trong colony |

### Colony & xã hội

| Source | Đọc từ |
|---|---|
| `ColonyEnhancement` | Trung bình (Steel + Flesh + Gene) của mọi colonist |
| `MentalBreakRate` | Số mental break trong 10 ngày gần nhất |
| `PrisonerCount` | Số tù binh |
| `SlaveCount` | Số nô lệ |
| `ColonyWealth` | `WealthWatcher.WealthTotal` |

### Hành động (chỉ dùng trong `HeroReactionDef`, không dùng trong `HeroValueEffectDef`)

`GeneExtraction_Performed`, `XenogermImplant_Performed`, `BionicInstall_Performed`,
`OrganHarvest_Performed`, `Execution_Performed`, `PrisonerReleased`, `RefugeeAccepted`,
`ColonistDied`, `RaidRepelled`.

**Phân biệt:** `HeroValueEffectDef` đọc **trạng thái** (đang có bao nhiêu), `HeroReactionDef`
bắt **sự kiện** (vừa xảy ra) và tạo memory. Không trộn hai loại.

---

## 4. Harmony patch — giữ ngắn nhất có thể

| # | Target | Loại | Lý do |
|---|---|---|---|
| P1 | `IncidentWorker.TryExecute` | Postfix | Bắt mọi incident vanilla để hero phản ứng |
| P2 | `Recipe_InstallArtificialBodyPart.ApplyOnPawn` | Postfix | Bắt sự kiện lắp implant ngay lúc xảy ra |
| P3 | `Pawn.Kill` | Postfix | Colonist chết → memory + loyalty toàn colony |
| P4 | `Recipe_ExtractXenogerm` / `Recipe_ImplantXenogerm` `.ApplyOnPawn` | Postfix | Bắt hành động gene — nguồn memory nặng nhất của Biotech |
| P5 | `InteractionWorker_Social...` (nếu cần) | Postfix | (cân nhắc — có thể bỏ, dùng polling) |

**Năm patch, toàn bộ là postfix.** Không patch nào huỷ hành vi gốc. Tuân thủ R4.

P1 chạy mỗi incident — tần suất thấp, không phải hot path.
P3 chạy mỗi pawn chết — cần guard `pawn.Faction == Faction.OfPlayer` ngay đầu để thoát sớm.

---

## 5. Quy trình snapshot pawn

### Load

```
LoadFromFile(name)
  → Scribe.loader.InitLoading(path)
  → ScribeMetaHeaderUtility.LoadGameDataHeader(None, false)
  → Scribe_Deep.Look(ref pawn, "pawn")
  → Scribe.loader.FinalizeLoading()
  → [catch] Scribe.ForceStop()   ← BẮT BUỘC
```

`Scribe.ForceStop()` trong catch là bắt buộc tuyệt đối. Scribe kẹt ở trạng thái loading
sẽ làm hỏng **mọi** save/load sau đó trong session, kể cả autosave của game.

### Sanitize

```
SetFactionDirect(null)
ThingID = "RWX_Hero_" + Find.UniqueIDsManager.GetNextThingID()   ← chống trùng ID
relations.ClearAllRelations()
jobs.StopAll()
health.surgeryBills.Clear()
if (Map != null) DeSpawn()
Notify_DisabledWorkTypesChanged()
```

Trùng `ThingID` là bug âm thầm nhất: save load được, nhưng hai pawn tranh nhau một ID
và một trong hai biến mất sau vài lần save/load.

### Preload & validate

Chạy trong `GameComponent.FinalizeInit`, **không** chạy lúc quest fire.

Lý do: nếu def bị thiếu do đổi modlist, phải biết ngay lúc load game, không phải giữa ván chơi.

Validate report ghi ra log:
```
[RimwardExiles] Preset 'hero_a': OK (47 hediffs, 12 apparel)
[RimwardExiles] Preset 'hero_b': WARNING — 3 def missing:
    - Moyo_Apparel_Coat (mod: Moyo Race)
    - CE_Ammo_762x39mm (mod: Combat Extended)
```

### manifest.xml — bảng phụ thuộc

```xml
<PresetManifest>
  <preset>
    <fileName>hero_a</fileName>
    <createdWith>
      <li>alance.cosmicodyssey @ 1.6.2</li>
      <li>author.moyo @ 2024.11</li>
    </createdWith>
    <criticalDefs>
      <li>CO_GaussRifle</li>
    </criticalDefs>
  </preset>
</PresetManifest>
```

Đây là bảo hiểm cho quyết định snapshot-based. Sáu tháng nữa update mod thì tra bảng này.

---

## 6. Save-compat & migration

`GameComponent_Exiles` giữ một `int saveVersion`.

```csharp
public override void ExposeData()
{
    Scribe_Values.Look(ref saveVersion, "saveVersion", 0);
    Scribe_Collections.Look(ref heroStates, "heroStates", LookMode.Deep);
    // ...
    if (Scribe.mode == LoadSaveMode.PostLoadInit)
        Migrate();
}
```

### Quy tắc migration

| Thay đổi | Xử lý |
|---|---|
| Thêm trục mới | Weight mặc định 0 cho mọi creed cũ. Không cần migrate |
| Xoá trục | Bỏ qua khi load, log warning. **Không** xoá dữ liệu memory tham chiếu tới nó |
| Đổi tên trục | Bảng ánh xạ `oldDefName → newDefName` trong `Migrate()` |
| Đổi cấu trúc `HeroMemory` | Bump saveVersion, viết hàm chuyển đổi. Đây là thứ đau nhất — thiết kế kỹ từ đầu |

**Nguyên tắc:** không bao giờ đổi tên field đã ship. Thêm field mới thay vì sửa field cũ.

---

## 7. Debug surface

Có ngay từ đầu, không phải "làm sau". Không có nó thì test bằng cách chơi 30 ngày thật.

Dev Mode → Actions → `Mod - Rimward Exiles`:

| Action | Tác dụng |
|---|---|
| `Spawn hero...` | Chọn preset, spawn thẳng vào colony |
| `Force decision...` | Chọn `HeroDecisionDef`, bắn ngay |
| `Set loyalty...` | Chọn hero, đặt giá trị |
| `Dump loyalty factors` | In toàn bộ factor list + delta ra log |
| `Dump body profile` | In phân loại ModPath của mọi hediff trên pawn |
| `Validate all presets` | Chạy lại validate, in report |
| `Clear all memories` | Xoá memory của một hero |
| `Advance decision timer` | Nhảy tới decision tiếp theo trong queue |

---

## 8. Hiệu năng

| Thành phần | Tần suất | Chi phí |
|---|---|---|
| Loyalty tick | 2500 ticks / hero | Vòng qua ~10 factor, không alloc nếu dùng list tái sử dụng |
| BodyPathClassifier | 1 lần lúc startup | Cache vào Dictionary |
| P1 incident postfix | mỗi incident | Không đáng kể |
| Aura tick (v2) | 60 ticks / aura | 1 vòng radial ~10 ô |

Không có gì chạy mỗi tick. Không có gì chạy trong pathfinding hay job scan. Tuân thủ R2.

---

## 9. Thứ tự triển khai đề xuất

0. Dựng khung namespace `Core` / `Odyssey` **trước khi viết class đầu tiên**. Đổi sau thì phải sửa mọi file.
1. `HeroValueDef` + `HeroValueEffectDef` + `HeroCreedDef` + `CreedEvaluator` + `EffectResolver` — không cần gì khác, test bằng unit-ish
2. `GameComponent_Exiles` + `HeroState` + ExposeData — test save/load rỗng
3. `BodyPathClassifier` + `HeroBodyPathDef` — test bằng debug dump
4. `LoyaltyCalculator` + `ITab` — giờ nhìn thấy được số, mọi thứ sau dễ hơn nhiều
5. `HeroPawnLoader` + validate + manifest
6. `HeroDecisionDef` + worker + `IncidentDef`
7. `IncidentReactionMap` + P1
8. `HeroMemory` + P2 + P3
9. Tension + `Hediff_Conflicted`
10. QuestScriptDef + chuỗi quest

Bước 4 là mốc quan trọng: từ đó trở đi anh debug bằng mắt thay vì bằng log.

---

## 10. Checklist chốt tài liệu này

- [ ] Đã dựng khung namespace `Core` / `Odyssey` trước khi viết class đầu tiên (K1)
- [ ] Đã kiểm tra `Core` không hardcode tên trục / tên hero nào (K2) — grep định kỳ
- [ ] Đã chuyển toàn bộ reaction map sang Def (K3)
- [ ] Đã chốt danh sách class (hiện tại: 17)
- [ ] Đã chốt tập `source` hợp lệ (mục 3b)
- [ ] Đã chốt danh sách Harmony patch (hiện tại: 4 chắc chắn + 1 cân nhắc)
- [ ] Đã chốt cấu trúc `ExposeData` — **đọc lại kỹ, đổi sau khi có save là đau nhất**
- [ ] Đã chốt vị trí thư mục Presets và format manifest
- [ ] Đã liệt kê đủ debug action cần thiết
