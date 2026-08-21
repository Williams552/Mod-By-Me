# 01 — Bộ trục giá trị (Value Axes)

> **Tài liệu chặn.** Mọi hero là một vector trên bộ trục này. Mọi decision là một vector delta.
> Sửa bộ trục sau khi đã viết content = viết lại toàn bộ content.
> Chốt kỹ trước khi sang tài liệu 02.

---

## 1. Mô hình

Mỗi hero có một `HeroCreedDef` chứa danh sách `(HeroValueDef, weight)`.

- `weight` là số thực trong khoảng **-1.0 .. +1.0**
- **Dương** = coi trọng giá trị đó. **Âm** = coi trọng điều ngược lại. **0 / không khai báo** = thờ ơ
- Độ lớn quyết định cường độ phản ứng, dấu quyết định chiều

Mỗi sự kiện (decision option, incident vanilla, trạng thái cơ thể) sinh ra một **delta vector**
trên bộ trục. Loyalty delta của một hero = tích vô hướng của hai vector:

```
loyaltyDelta = Σ ( event.delta[axis] × creed.weight[axis] )
```

Hệ quả tự nhiên: cùng một sự kiện, hero A giận nhẹ, hero B giận dữ dội, hero C vui —
mà không cần viết reaction riêng cho từng người.

### Quy ước độ lớn của delta

| Mức | Giá trị | Dùng khi |
|---|---|---|
| Nhỏ | ±5 | Sự kiện nền, incident vanilla thường |
| Vừa | ±15 | Quyết định có ý nghĩa nhưng không định mệnh |
| Lớn | ±35 | Quyết định lớn trong chuỗi truyện |
| Cực lớn | ±60 | Phản bội trực tiếp, chạm vào giá trị cốt lõi |

Nhân với weight (tối đa 1.0) → delta thực tế không bao giờ vượt quá độ lớn danh nghĩa.

---

## 2. Bộ trục xã hội (6 trục)

### `RWX_Loyalty` — Trung thành

Giữ lời hứa, không bỏ rơi đồng đội, đứng về phía người của mình.

- **Tăng:** cứu người, từ chối giao nộp ai đó, chấp nhận thiệt hại để giữ cam kết
- **Giảm:** phản bội, bán đồng minh, đổi phe vì lợi ích
- **Weight âm nghĩa là:** cơ hội chủ nghĩa — coi trọng việc luôn chọn bên có lợi

### `RWX_Mercy` — Nhân từ

Đối xử tử tế với kẻ yếu, tù binh, người ngoài.

- **Tăng:** thả tù binh, chữa trị kẻ địch, nhận refugee, từ chối xử tử
- **Giảm:** xử tử, mua bán nô lệ, thu hoạch nội tạng, bỏ mặc người bị nạn
- **Weight âm nghĩa là:** tàn nhẫn — coi sự mềm yếu là khuyết điểm

### `RWX_Order` — Trật tự

Luật lệ, thứ bậc, kỷ luật, sự đoán trước được.

- **Tăng:** thiết lập quy tắc, tuân thủ hiệp ước, trừng phạt kẻ vi phạm
- **Giảm:** vô chính phủ, phá vỡ cam kết, để colony hỗn loạn (mental break nhiều)
- **Weight âm nghĩa là:** tự do — ghét bị ràng buộc bởi luật

### `RWX_Ambition` — Tham vọng

Vươn lên, mở rộng, không chấp nhận hiện trạng.

- **Tăng:** mở rộng lãnh thổ, nâng cấp công nghệ, chấp nhận rủi ro để lớn mạnh
- **Giảm:** thu mình, từ chối cơ hội, colony trì trệ nhiều ngày
- **Weight âm nghĩa là:** an phận — coi trọng sự ổn định hơn phát triển

### `RWX_Kinship` — Thân tộc

Gắn bó với nhóm của mình: chủng tộc, faction cũ, người cùng xuất thân.

- **Tăng:** ưu ái pawn cùng xenotype/faction, bảo vệ người của mình
- **Giảm:** ưu ái người ngoài, colony đa chủng tộc lộn xộn (với weight dương cao)
- **Weight âm nghĩa là:** hoà nhập — coi trọng sự đa dạng, ghét chủ nghĩa bè phái

> **Ghi chú:** đây là trục mang "Godwoken ghét chủng tộc khác". Godwoken có Kinship cao,
> không phải một tag `purist` riêng.

### `RWX_Splendor` — Xa hoa

Cái đẹp, sự tinh tế, tiện nghi, nghi lễ trang trọng.

- **Tăng:** phòng ốc đẹp (Impressiveness cao), apparel chất lượng, bữa ăn thịnh soạn, tác phẩm nghệ thuật
- **Giảm:** sống kham khổ, đồ tồi tàn, base bẩn
- **Weight âm nghĩa là:** khổ hạnh — coi xa hoa là sự suy đồi

> **Ghi chú:** trục này đọc trực tiếp từ số vanilla (`RoomStatDefOf.Impressiveness`,
> apparel quality, thought ăn uống). Không tự đo lại.

---

## 3. Bộ trục thân thể (4 trục)

Bốn trục này đặc biệt: chúng **loại trừ nhau**, tạo thành tứ giác.

Ba trục Steel / Flesh / Blood đều là *con người tự cải tạo con người*, nhưng khác nhau về
tính hoàn tác và tính áp đặt — đó là điều làm chúng xung đột thay vì bổ sung cho nhau.

### `RWX_SteelPath` — Con đường Thép

Siêu việt qua máy móc. Bionic, archotech, cấy ghép công nghiệp.

- **Tăng:** lắp implant Steel cho bản thân hoặc colony, nghiên cứu công nghệ bionic
- **Giảm:** colony toàn người nguyên vẹn, từ chối nâng cấp, mất implant

### `RWX_FleshPath` — Con đường Thịt

Siêu việt qua sinh học. Organ mod, DNA splice, mọc thêm chi, cải tạo cơ thể sống.

- **Tăng:** lắp implant Flesh, xây organ vat, nghiên cứu sinh học
- **Giảm:** colony từ chối con đường sinh học

### `RWX_BloodPath` — Con đường Huyết thống

Siêu việt qua gene. Xenotype, xenogerm, germline, growth vat.

- **Tăng:** cấy xenogerm, thiết kế xenotype, nuôi trẻ có gene chọn lọc, research gene
- **Giảm:** colony thuần baseliner, từ chối can thiệp gene

Ba tính chất đạo đức khiến nó tách khỏi `FleshPath`:

| | Steel | Flesh | **Blood** |
|---|---|---|---|
| Hoàn tác được | Có | Có | **Không** |
| Áp đặt lên người khác được | Khó | Khó | **Dễ** (tù binh, trẻ trong vat) |
| Truyền cho đời sau | Không | Không | **Có** |

Flesh cải tạo cá thể đang sống. Blood viết lại thứ được truyền đi. Cùng là sinh học,
đối lập về phương pháp — và đó là nguồn xung đột giữa hai phái.

### `RWX_Purity` — Thuần khiết

Cơ thể không bị sửa đổi. Sức mạnh đến từ tâm trí, không từ can thiệp vật lý.

- **Tăng:** giữ cơ thể nguyên vẹn, phát triển psylink, colony tôn trọng con đường này
- **Giảm:** mỗi implant trên bản thân (nặng), mỗi implant trong colony (nhẹ)

### Ma trận khinh–trọng

Đọc theo hàng: hero có trục này ở weight cao thì phản ứng thế nào với hành động thuộc cột.

| Hero ↓ / Hành động → | Lắp Steel | Lắp Flesh | Cấy gene | Giữ nguyên vẹn |
|---|---|---|---|---|
| **SteelPath cao** | **+6** | **−3** | −2 | −2 |
| **FleshPath cao** | **−4** | **+6** | **+2** | −2 |
| **BloodPath cao** | −2 | **+2** | **+7** | −3 |
| **Purity cao** | **−8** | **−11** | **−14** | **+5** |

Bốn điểm cần giữ nguyên khi tinh chỉnh số:

1. **Steel và Flesh khinh nhau**, không chỉ cùng chống Purity. Đây là thứ làm mô hình thành tứ giác thay vì trục nhị phân.
2. **Purity phạt theo mức không thể hoàn tác:** Blood > Flesh > Steel. Tay giả là công cụ; mọc thêm tay là báng bổ; sửa germline là tội với người chưa sinh ra.
3. **Flesh và Blood hơi thích nhau** (+2 chéo) — cùng phe sinh học, khác phương pháp. Xung đột giữa họ đến từ decision, không từ ma trận.
4. **Ba phái cải tạo phạt sự nguyên vẹn ở mức nhẹ.** Họ thấy tiếc, không thấy ghê tởm.

### Nguồn riêng của BloodPath

Ba hành động dưới đây **không** tính vào bảng trên vì chúng là hành động, không phải trạng thái —
chúng tạo memory qua `HeroReactionDef`:

| Hành động | Trục bị chạm |
|---|---|
| Rút gene từ tù binh (`GeneExtraction`) | **Mercy −20**, Order −5, Blood +4 |
| Cấy xenogerm cho tù binh | **Mercy −12**, Blood +6 |
| Nuôi trẻ trong growth vat | Blood +5, Mercy −6, Kinship −4 |

`GeneExtraction` là hành động tàn ác rõ ràng nhất trong Biotech — làm tù binh hôn mê để rút
thứ không tái tạo được. Đây là ứng viên cho memory không phai.

### Ba nền kinh tế song song

Mỗi phái đòi một loại tài nguyên khan hiếm khác nhau. Đây là cơ chế khiến "giữ tất cả thì đắt"
hoạt động ở tầng vật chất, không chỉ tầng cảm xúc.

| Phái | Chi phí giữ họ vui |
|---|---|
| Steel | Component, plasteel, research bionic, bàn phẫu thuật tốt |
| Flesh | Organ vat, nguyên liệu sinh học, research sinh học |
| Blood | Gene processor, gene bank, xenogerm, growth vat, archite capsule |
| Purity | Neuroformer / anima grass, psychic ritual, đất trống quanh anima tree |

Colony không thể tối đa hoá cả bốn trong early-mid game. Blood đắt nhất về research,
Purity đắt nhất về không gian và thời gian.

---

## 4. Tensions — mâu thuẫn nội tâm

Hai trục được đánh dấu `tension` trong cùng một creed: khi một sự kiện tạo delta **ngược chiều**
trên cả hai và cả hai đều vượt ngưỡng, **không cộng dồn thành 0**. Thay vào đó:

- Bắn `Hediff_Conflicted` — mood penalty vừa phải, kéo dài vài ngày
- Mở khoá một decision cá nhân: *"[Hero] xin nói chuyện riêng"*
- Người chơi chọn giúp họ nghiêng về một phía → trục đó **tăng weight vĩnh viễn** (+0.15, cap 1.0),
  trục kia giảm tương ứng

Đây là cơ chế duy nhất khiến hero **thay đổi theo playthrough**. Giữ nó hiếm và có trọng lượng.

### Các cặp tension đáng dùng

| Cặp | Câu chuyện |
|---|---|
| `Purity` ↔ `Splendor` | Công nghệ của kẻ khác đẹp đẽ nhưng ô uế |
| `Loyalty` ↔ `Mercy` | Đồng đội của tôi đã làm điều tàn ác |
| `Kinship` ↔ `Order` | Luật đòi trừng phạt người của tôi |
| `Ambition` ↔ `Purity` | Muốn mạnh hơn nhưng không được đánh đổi thân thể |
| `Mercy` ↔ `Order` | Tha thứ hay giữ kỷ luật |
| `Loyalty` ↔ `Ambition` | Ở lại với người cũ hay đi theo cơ hội lớn |

Mỗi hero nên có **1–2 tension**, không nhiều hơn. Nhiều hơn thì hero lúc nào cũng giằng xé
và cơ chế mất ý nghĩa.

---

## 5. Schema XML

```xml
<HeroValueDef>
  <defName>RWX_Purity</defName>
  <label>thuần khiết</label>
  <description>Thân thể con người không nên bị sửa đổi.</description>
  <!-- label hiển thị trong ITab khi trục này là nguyên nhân thay đổi loyalty -->
  <positiveLabel>Thân thể vẹn nguyên</positiveLabel>
  <negativeLabel>Thân thể bị xâm phạm</negativeLabel>
</HeroValueDef>
```

Tác động của trục **không** nằm trong C# (kỷ luật K2). Mỗi trục có một `HeroValueEffectDef`
khai báo nó phản ứng với cái gì:

```xml
<HeroValueEffectDef>
  <defName>RWX_Effect_Purity</defName>
  <axis>RWX_Purity</axis>
  <effects>
    <li><source>BodyPart_Steel</source>       <perUnit>-8</perUnit></li>
    <li><source>BodyPart_Flesh</source>       <perUnit>-11</perUnit></li>
    <li><source>Gene_Implanted</source>       <perUnit>-14</perUnit></li>
    <li><source>BodyPart_Intact</source>      <perUnit>5</perUnit></li>
    <li><source>ColonyEnhancement</source>    <perUnit>-5</perUnit>  <cap>-25</cap></li>
    <li><source>PsylinkLevel</source>         <perUnit>5</perUnit></li>
  </effects>
</HeroValueEffectDef>
```

Thêm một trục mới = thêm hai file XML, không đụng C#. Đây là điều kiện để bảng ma trận
ở mục 3 tinh chỉnh được mà không recompile.

```xml
<HeroCreedDef>
  <defName>RWX_Creed_Example</defName>
  <label>Đạo ...</label>

  <values>
    <li><value>RWX_Purity</value>   <weight>0.9</weight></li>
    <li><value>RWX_Splendor</value> <weight>0.7</weight></li>
    <li><value>RWX_Kinship</value>  <weight>0.6</weight></li>
    <li><value>RWX_Mercy</value>    <weight>-0.3</weight></li>
  </values>

  <tensions>
    <li>
      <between>RWX_Purity</between>
      <and>RWX_Splendor</and>
      <note>Công nghệ Alance đẹp đẽ nhưng ô uế</note>
    </li>
  </tensions>
</HeroCreedDef>
```

---

## 6. Quy tắc khi thêm trục mới

Trước khi thêm một `HeroValueDef`, kiểm tra cả bốn điều:

1. **Có ít nhất 3 sự kiện khác nhau tác động lên nó không?** Ít hơn thì nó là một sự kiện, không phải một trục.
2. **Có ít nhất 2 hero có weight khác dấu trên nó không?** Nếu mọi hero cùng dấu, trục không tạo xung đột → vô dụng.
3. **Có viết được `positiveLabel`/`negativeLabel` dễ hiểu không?** Không viết được = vi phạm nguyên tắc N4.
4. **Có trùng với trục sẵn có không?** Trục gần nhau làm loãng tín hiệu; gộp lại tốt hơn tách ra.
5. **Viết được `HeroValueEffectDef` cho nó bằng các `source` sẵn có không?** Nếu phải thêm `source` mới vào `Core`, cân nhắc kỹ — đó là điểm mở rộng của engine, không nên thêm bừa.

Trần đề xuất: **12 trục**. Vượt quá thì người chơi không đọc nổi ITab và anh không cân bằng nổi.

---

## 7. Checklist chốt tài liệu này

- [ ] Đã chốt danh sách trục cuối cùng (hiện tại: 10 trục — 6 xã hội + 4 thân thể)
- [ ] Đã kiểm tra mỗi trục qua 4 câu hỏi ở mục 6
- [ ] Đã chốt số trong ma trận khinh–trọng 4×4
- [ ] Đã chốt danh sách `source` Biotech cần đọc (xem `05-technical.md` mục 3b)
- [ ] Đã chốt quy ước độ lớn delta
- [ ] Đã chọn tension cho từng hero dự kiến (chuyển sang tài liệu 03)
