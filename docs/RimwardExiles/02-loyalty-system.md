# 02 — Hệ thống Loyalty

> Phụ thuộc: `01-value-axes.md` (bộ trục đã chốt).
> Chặn: `04-content.md` (decision cần biết delta được diễn giải thế nào).
> Nguyên tắc chi phối: **N3** (random khuếch đại lỗi) và **N4** (minh bạch tuyệt đối).

---

## 1. Loyalty là gì

Một số thực **0 .. 100** cho mỗi hero, mô tả mức độ họ còn muốn ở lại colony.

Không phải mood. Không phải opinion. Loyalty là **kết luận** mà hero rút ra từ mood, quan hệ,
ký ức, và những quyết định người chơi đã ra. Mood tụt một ngày không làm loyalty tụt;
mood tụt ba tuần thì có.

Khởi điểm khi gia nhập: **65**.

---

## 2. Phân vai — ai chịu trách nhiệm cho cái gì

Đây là bảng quan trọng nhất tài liệu này. Vi phạm nó = tính hai lần, hoặc viết lại thứ vanilla đã có.

| Câu hỏi | Ai trả lời | Mod đọc gì |
|---|---|---|
| Phòng có đẹp không, ăn có ngon không, mặc có ấm không | Needs + Ideology vanilla | `needs.mood.CurLevelPercentage` |
| Colony có vi phạm chuẩn mực chung không | Ideology (precept) | Đã gộp trong mood |
| Hero nghĩ gì về pawn X | Social vanilla | `relations.OpinionOf(X)` |
| Hero có đánh nhau với ai không | Social vanilla | Không đọc — để vanilla tự xử |
| **Hero nghĩ gì về quyết định người chơi vừa ra** | **HeroCreed** | Delta vector × weight |
| **Hero nhớ gì, ghét ai vì chuyện gì** | **HeroMemory** | — |
| **Hero có đang giằng xé không** | **Tensions** | — |
| **Hero có bỏ đi không** | **Loyalty** | Tổng hợp tất cả |

**Nguyên tắc:** mod không bao giờ tự đo phòng ốc, apparel, hay thức ăn. Vanilla đã tính rồi,
và tính tốt hơn. Mod chỉ đọc kết quả cuối cùng.

---

## 3. Công thức

Loyalty không được tính lại từ đầu mỗi tick. Nó **trôi dần** về một mục tiêu.

```
target  = 50 + Σ factors
loyalty += clamp( (target - loyalty) × 0.08 , -maxDropPerTick , +maxRisePerTick )
```

Trôi dần thay vì gán thẳng, vì hai lý do:

1. Một ngày tồi tệ không làm hero bỏ đi ngay — người chơi có thời gian phản ứng
2. Người chơi thấy được **xu hướng** (mũi tên lên/xuống), không chỉ con số

Tick mỗi **2500 ticks** (~1 giờ in-game). Với hệ số 0.08, loyalty cần khoảng 12 giờ in-game
để đi được ~60% quãng đường tới target. Đủ chậm để cảm thấy có quán tính, đủ nhanh để
người chơi thấy hành động của mình có tác dụng trong ngày.

### Nguồn factor

| Nguồn | Khoảng đóng góp | Ghi chú |
|---|---|---|
| Mood tổng hợp | −25 .. +20 | Từ vanilla, đã gồm Ideology/phòng ốc/thức ăn |
| Opinion trung bình với colony | −15 .. +15 | Từ social vanilla |
| Trạng thái cơ thể × trục thân thể | −30 .. +25 | Ma trận ở tài liệu 01 |
| Memory đang hoạt động | −40 .. +30 | Tổng có trần, xem mục 5 |
| Trạng thái colony × trục xã hội | −15 .. +15 | Ví dụ: Order thấp khi mental break nhiều |
| Tension đang mở | −10 .. 0 | Chỉ âm, giải quyết được thì hết |

### Công thức mood → factor

```csharp
float moodPct = hero.needs.mood.CurLevelPercentage;   // 0..1
float moodFactor = (moodPct - 0.55f) * 45f;           // 0.55 là điểm trung tính
moodFactor = Mathf.Clamp(moodFactor, -25f, 20f);
```

0.55 làm điểm trung tính (thay vì 0.5) vì pawn RimWorld bình thường sống quanh 0.6–0.7.
Trần dương thấp hơn trần âm: hero vui không bù được ký ức xấu.

---

## 4. Trạng thái cơ thể → factor

Đọc profile hiện tại, không cần lịch sử.

```
profile.steel      = số hediff phân loại Steel
profile.flesh      = số hediff phân loại Flesh
profile.geneImpl   = số xenogene đã cấy
profile.geneInher  = số endogene
profile.missing    = số Hediff_MissingPart chưa thay
profile.avgEff     = hiệu suất trung bình phần đã thay

colonyEnhancement  = trung bình (steel + flesh + geneImpl) của mọi colonist
```

Áp ma trận ở tài liệu 01, nhân với weight của hero trên bốn trục thân thể.

Hai chi tiết giữ nguyên khi tinh chỉnh:

- **SteelPath/FleshPath cao → `missing` là factor âm.** Cơ thể dở dang là vấn đề cần sửa.
- **Purity cao → `missing` không phạt.** Mất tay là số phận, không phải ô uế.
- **`geneInher` (endogene) không tính vào Purity.** Sinh ra đã có thì không phải lựa chọn — chỉ `geneImpl` mới bị phạt. Đây là chỗ phân biệt nạn nhân với tín đồ.

Và `colonyEnhancement` tác động ở mức nhẹ hơn cơ thể bản thân (~40%), có trần cứng.
Colony 20 người toàn bionic không được phép một mình đẩy Purity hero xuống 0.

---

## 5. HeroMemory

Thứ Ideology hoàn toàn không có: ký ức về **hành động cụ thể**, gắn với **người cụ thể**.

```csharp
class HeroMemory
{
    public string sourceDefName;   // decision hoặc incident nào tạo ra
    public int    targetPawnID;    // ai là nạn nhân/người hưởng lợi (-1 nếu không có)
    public int    tickOccurred;
    public float  initialWeight;   // đã nhân creed weight sẵn
    public bool   decayable;
    public float  halfLifeDays;
    public string label;           // hiển thị trong ITab — bắt buộc, N4
}
```

### Decay

```csharp
float Current(int now)
{
    if (!decayable) return initialWeight;
    float days = (now - tickOccurred) / 60000f;
    return initialWeight * Mathf.Pow(0.5f, days / halfLifeDays);
}
```

| Loại | halfLife | Ví dụ |
|---|---|---|
| Thoáng qua | 5 ngày | Từ chối một yêu cầu nhỏ |
| Thường | 20 ngày | Quyết định có ý nghĩa |
| Sâu | 60 ngày | Phản bội, mất người thân |
| **Không phai** | ∞ | Món nợ máu, giao nộp đồng đội |

**Memory không phai phải hiếm.** Đề xuất: tối đa 2 loại decision trong toàn bộ dự án
được phép tạo memory không phai. Nếu nhiều hơn, hero sẽ tích luỹ oán hận không thể cứu vãn
và người chơi mất quyền tự sửa sai — vi phạm tinh thần N2.

### Trần tổng

Tổng memory âm có trần **−40**, tổng dương có trần **+30**. Không có trần thì 20 quyết định
nhỏ cộng lại thành án tử.

### Memory có mục tiêu → nối vào social vanilla

Khi `targetPawnID >= 0`, ngoài việc tính vào loyalty, **cấp thêm** một `Thought_MemorySocial`
lên hero nhắm vào pawn đó. Từ đó hệ social vanilla tự lo: opinion tụt, social fight,
mood ảnh hưởng. Mod không viết thêm gì.

Đây là cách "hero ghét *anh* vì anh đã bán em trai tôi" hoạt động mà không cần hệ quan hệ riêng.

---

## 6. Tension

Kích hoạt khi **một sự kiện** tạo delta ngược chiều trên hai trục được đánh dấu `tension`
trong creed của hero, và **cả hai** vượt ngưỡng `|delta × weight| >= 8`.

Không cộng dồn thành 0. Thay vào đó:

1. Cấp `Hediff_Conflicted` — mood −6, kéo dài 6 ngày, không stack
2. Loyalty factor −10 trong khi hediff còn
3. Sau 1 ngày, mở khoá decision cá nhân: *"[Hero] xin nói chuyện riêng"*
4. Decision này có 2 option, mỗi option nghiêng về một trục
5. Người chơi chọn → trục đó **+0.15 weight vĩnh viễn** (cap 1.0), trục kia **−0.10**
6. Hediff biến mất, loyalty +8

Nếu người chơi bỏ qua decision quá 10 ngày: hediff hết, không có thay đổi weight,
loyalty −5 và một memory decayable ("bị bỏ mặc trong lúc giằng xé").

**Đây là cơ chế duy nhất khiến creed thay đổi.** Giữ nó hiếm — mỗi hero nên gặp
tension khoảng 2–4 lần trong toàn bộ playthrough.

---

## 6b. Disposition — modifier vĩnh viễn

Một số quyết định không chỉ cộng/trừ loyalty một lần. Chúng thay đổi **cách hero diễn giải
mọi thứ về sau** — ấn tượng đầu tiên, và nó dính lại.

```csharp
class HeroDisposition
{
    public string defName;        // HeroDispositionDef
    public float  gainMultiplier; // nhân với mọi delta DƯƠNG
    public float  lossMultiplier; // nhân với mọi delta ÂM
    public List<string> gatedOptions;   // option bị khoá ở decision sau
    public List<string> bonusOptions;   // option được thưởng thêm
    public string label;          // hiển thị trong ITab — bắt buộc
}
```

Áp dụng sau khi tính delta, trước khi cộng vào target:

```
if (delta > 0) delta *= disposition.gainMultiplier;
else           delta *= disposition.lossMultiplier;
```

### Quy tắc

- **Mỗi hero tối đa 1 disposition tại một thời điểm.** Chồng nhiều cái là không debug nổi.
- **Gán bởi decision, không bởi trạng thái.** Nó là hệ quả của một lựa chọn cụ thể.
- **`gainMultiplier` không được xuống dưới 0.3.** Dưới ngưỡng đó hero trở thành không thể cứu, vi phạm N2.
- **Hiển thị rõ trong ITab** kèm lý do — người chơi phải hiểu vì sao quan hệ tiến chậm.
- **Có thể đổi được** ở một decision muộn trong chuỗi, nhưng phải đắt. Không có disposition nào là án chung thân.

### Gated / bonus options

`gatedOptions` khoá một số option ở decision về sau — chúng vẫn hiện trong letter nhưng bị
xám và có tooltip giải thích lý do (N4). `bonusOptions` cho option đó hiệu quả cao hơn
(thường +50% delta dương).

Đây là cách một quyết định sớm tạo hệ quả **cấu trúc** thay vì chỉ hệ quả số học.

---

## 7. Ngưỡng và cách ra đi

| Loyalty | Trạng thái | Biểu hiện |
|---|---|---|
| 70–100 | Tận tuỵ | (không có gì đặc biệt) |
| 40–69 | Bình thường | — |
| 25–39 | **Bất mãn** | Letter cảnh báo (1 lần). Hediff "Bất mãn" hiện trong Health tab |
| 10–24 | **Chuẩn bị đi** | Letter thứ hai. Mở decision khẩn cấp để cứu vãn (đắt) |
| < 10 trong 3 ngày liên tiếp | **Rời đi** | Xem dưới |

### Khi rời đi

```
pawn.SetFaction(null)
→ đi bộ ra khỏi map (không teleport, không biến mất)
→ ThoughtDef "[Hero] đã rời đi" cho toàn colony
→ ghi cờ trong GameComponent: đã rời, lý do chính là trục nào
```

**Không xoá pawn.** Giữ trong world pawn pool để v2 có thể cho họ quay lại.

Nếu nguyên nhân chính là quan hệ với một hero khác (memory có target là hero đó
chiếm > 50% tổng memory âm), cho họ đi **cùng nhau** hoặc gia nhập faction thù địch.

### Decision cứu vãn

Ở mức 10–24, mở một `HeroDecisionDef` đặc biệt. Đặc điểm bắt buộc:

- Có ít nhất một option **luôn thành công**, chi phí cao (theo N1 và N2)
- Chi phí phải cụ thể và cảm nhận được: bạc, tài nguyên hiếm, hoặc mất goodwill faction
- Hiệu quả: loyalty +30, xoá 50% memory decayable
- Cooldown 30 ngày — không được dùng làm nút reset

---

## 8. Phanh an toàn

Ba cái. Thiếu bất kỳ cái nào thì hệ thống rơi vào death spiral: raid → hero thương →
mood tụt → loyalty tụt → hero đi → phòng thủ yếu → raid.

### P1 — Trần tốc độ

```
maxDropPerTick = 0.8    // ~19 điểm/ngày tối đa
maxRisePerTick = 0.5
```

Rơi nhanh hơn hồi phục (thực tế tâm lý), nhưng có trần cứng.

### P2 — Cửa sổ miễn nhiễm sau thảm hoạ

Sau một sự kiện lớn (colonist chết, raid phá hơn 30% base, toxic fallout bắt đầu):
**5 ngày** trong đó hero **không được rời đi**, dù loyalty < 10. Loyalty vẫn tụt và hiển thị,
chỉ là ngưỡng ra đi bị đóng băng.

Cho người chơi cửa sổ để cứu vãn thay vì mất hai hero trong cùng một đêm.

### P3 — Chống nhiễu

Chỉ phản ứng với sự kiện **có ý nghĩa với trục của hero đó**. Mỗi phản ứng phải đủ lớn
để người chơi nhận ra (|delta| >= 3 sau khi nhân weight, nếu không thì bỏ qua hoàn toàn).

Ít mà rõ hơn nhiều mà mờ.

### Trường hợp đặc biệt: hormone regulator

EvolvedOrgansRedux có bộ điều hoà hormone cho mood cao vĩnh viễn. Nếu để nguyên,
nó vô hiệu hoá toàn bộ hệ thống qua đường mood.

Xử lý: hero mang hediff loại đó thì **trọng số mood giảm còn 20%**, phần còn lại
dồn sang creed + memory. Và biến nó thành nội dung:

> *"[Hero] đau khổ. Anh có thể cấy bộ điều hoà hormone: cô ấy sẽ không bao giờ bất mãn nữa,
> nhưng cũng không bao giờ thật lòng nữa."*

Đánh đổi một nhân vật sống lấy một công cụ ổn định. Đây là decision hay nhất mà mod đó tặng miễn phí.

---

## 9. ITab — bắt buộc trong v1

Nguyên tắc N4 nằm ở đây. Không có tab này thì toàn bộ hệ thống là hộp đen.

```
┌─ Lòng trung thành ────────────────────────┐
│  [Hero name]                              │
│                                           │
│  ██████████░░░░░░░░  52  ↓                │
│  Bình thường                              │
│                                           │
│  ── Vì sao ──────────────────────         │
│  Tâm trạng                        +8      │
│  Quan hệ với thuộc địa            −4      │
│  Thân thể vẹn nguyên             +12      │
│  Thuộc địa đầy kẻ nửa máy        −18      │
│  Bị bỏ mặc trong lúc giằng xé     −6      │
│                                           │
│  ── Ấn tượng ────────────────────         │
│  "Anh đã phí phạm để làm dáng"            │
│  Quan hệ tiến chậm (×0.6)                 │
│                                           │
│  ── Ký ức ───────────────────────         │
│  Anh đã giao nộp Kaeso      −22  (32 ngày)│
│  Anh đã cứu tôi khỏi Alance +15  (phai)   │
│                                           │
│  ── Niềm tin ────────────────────         │
│  Thuần khiết          ███████░░  0.9      │
│  Xa hoa               █████░░░░  0.7      │
│  Nhân từ              ░░░██░░░░ −0.3      │
└───────────────────────────────────────────┘
```

Yêu cầu cứng:

- Mọi factor phải có **label tiếng Việt dễ hiểu**. Factor không viết được label thì không được tồn tại
- Hiển thị **xu hướng** (mũi tên), không chỉ giá trị
- Memory hiển thị cả tuổi và trạng thái phai/không phai
- Creed weight hiển thị để người chơi biết hero này quan tâm gì mà chuẩn bị trước
- Disposition hiển thị kèm hệ số và câu lý do bằng giọng của hero

---

## 10. Những gì KHÔNG làm

- **Không** cho loyalty ảnh hưởng ngược lại mood. Một chiều: mood → loyalty. Hai chiều tạo vòng lặp khuếch đại không kiểm soát được.
- **Không** cho loyalty ảnh hưởng work speed hay combat stat. Nó là thước đo quan hệ, không phải buff.
- **Không** cho hero rời đi trong lúc đang bị downed, đang bị bắt, hoặc đang trong caravan.
- **Không** tính loyalty cho colonist thường. Chỉ hero. (Trục xã hội vẫn áp dụng cho colonist thường qua Ideology vanilla nếu họ có meme tương ứng — nhưng không có loyalty state.)

---

## 11. Checklist chốt tài liệu này

- [ ] Đã chốt hệ số trôi (0.08) và chu kỳ tick (2500)
- [ ] Đã chốt khoảng đóng góp của từng nguồn factor
- [ ] Đã chốt ngưỡng và hành vi ở mỗi mức
- [ ] Đã chốt danh sách loại memory và halfLife
- [ ] Đã chốt tối đa bao nhiêu decision được tạo memory không phai (đề xuất: 2)
- [ ] Đã chốt ba phanh an toàn
- [ ] Đã chốt sàn `gainMultiplier` cho disposition (đề xuất: 0.3)
- [ ] Đã phác layout ITab
