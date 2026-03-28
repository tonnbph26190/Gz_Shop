# Ship & Loyalty Test Checklist

## Unit-like business cases

- Loyalty earn:
  - `SoTienTrenMotDiemTich = 10000`, order after discounts = `99,999` -> earn `9` points.
  - `SoTienTrenMotDiemTich = 10000`, order after discounts = `100,000` -> earn `10` points.
- Loyalty redeem:
  - Customer has `120` points, `SoTienGiamTrenMotDiem = 1000`, order amount = `50,000`, use point = true -> used points must be `50`, discount `50,000`.
  - `DiemToiDaSuDungMoiDon = 30` -> points used must not exceed `30` even when customer has more.
- Tier shipping discount:
  - Tier percent is `0%` -> shipping unchanged.
  - Tier percent is `25%`, shipping original `40,000` -> final `30,000`.
  - Tier percent is `100%` -> final `0`.

## Integration checks (API + POS + online)

- Config:
  - GET `api/CauHinhBanHang` returns config and tier list.
  - PUT `api/CauHinhBanHang` persists new point/shipping ratios.
  - Tier CRUD (`POST/PUT/DELETE api/CauHinhBanHang/tiers/*`) works and visible on admin screen.
- POS flow (`BanHangTaiQuay`):
  - with customer + `UsePoint = true`: invoice stores `DiemDaDung`, `SoTienGiamTuDiem`, ledger row `LoaiBienDong = Tru`.
  - after checkout: invoice stores `DiemCong`, ledger row `LoaiBienDong = Cong`, customer point balance updates.
  - shipping on checkout stores `PhiVanChuyenGoc`, `PhiVanChuyen`, `SoTienGiamPhiVanChuyen`.
- Online flow (`HoaDons` create):
  - with customer + `UsePoint = true`: point redeem and point earn fields update correctly.
  - tier shipping discount applies to incoming shipping fee.

## UAT checklist (manual)

- Admin updates:
  - change `SoTienTrenMotDiemTich`, create one order, verify earned points follow new ratio.
  - change `SoTienGiamTrenMotDiem`, use points on next order, verify money discount.
  - change tier `% giảm ship`, calculate ship in POS for customer in that tier, verify fee.
- Tier transitions:
  - place order to cross a tier threshold, verify customer tier and next shipping discount change.
- Edge cases:
  - customer without account (`guest`) checkout must not create point ledger.
  - customer with low points cannot redeem more than available.
  - shipping fee never goes below `0`.
