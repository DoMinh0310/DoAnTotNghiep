# NỘI DUNG SLIDE ĐỒ ÁN TỐT NGHIỆP
## Xây dựng trò chơi chiến thuật thẻ bài Roguelike sử dụng Unity Engine

---

## SLIDE 1: TRANG BÌA

**XÂY DỰNG TRÒ CHƠI CHIẾN THUẬT THẺ BÀI ROGUELIKE SỬ DỤNG UNITY ENGINE**

- **Sinh viên thực hiện:** Đỗ Vũ Thế Minh
- **Lớp:** 64CNTT1 — **Mã sinh viên:** 2251061835
- **Ngành:** Công nghệ thông tin — **Khoa:** Công nghệ Thông tin
- **Người hướng dẫn:** TS. Cù Việt Dũng
- **Trường Đại học Thủy Lợi — Hà Nội, năm 2026**

---

## SLIDE 2: GIỚI THIỆU

### Lý do chọn đề tài *(Mục 1.1)*
- Phát triển game là một trong những bài toán phức tạp và mang tính tổng hợp cao nhất trong Kỹ thuật phần mềm: xử lý tương tác thời gian thực, quản lý đồng thời nhiều thực thể, tối ưu hiệu năng phần cứng
- Unity Engine + C# là nền tảng phù hợp để ứng dụng OOP và Design Patterns; cho phép đối mặt trực tiếp với các bài toán như Object Pooling, ScriptableObject
- Đề tài là cơ hội củng cố kiến thức chuyên sâu và vận dụng tư duy giải thuật, logic lập trình vào bài toán thực tiễn

### Mục tiêu tổng quát *(Mục 1.6.1)*
Nghiên cứu quy trình thiết kế, phát triển và hoàn thiện sản phẩm trò chơi 2D thuộc thể loại **Roguelike Deck-builder** trên Unity, vận hành trên Windows; vận dụng kiến thức chuyên ngành để giải quyết các bài toán về thiết kế kiến trúc hệ thống, xây dựng gameplay chiến thuật, quản lý dữ liệu và tối ưu hóa hiệu năng

### Phạm vi đề tài *(Mục 1.7)*
- **Về nội dung:** bản đồ Roguelike sinh ngẫu nhiên; chiến đấu theo lượt trên bàn cờ; Deck-building + Relic + Trinket + nguyên tố; Permadeath
- **Về kỹ thuật:** Unity + C#; Data-Driven Architecture + ScriptableObject; hệ thống Save/Load
- **Về giới hạn:** Single-player Windows; **không có** Online Multiplayer, Cloud Save, bảng xếp hạng, cửa hàng thương mại, Live Service

---

## SLIDE 3: TỔNG QUAN ĐỀ TÀI

### Giới thiệu trò chơi *(Mục 1.2)*
Trò chơi kết hợp lối chơi **Deck-builder** với đặc trưng của **Roguelike**. Các hệ thống cốt lõi:
- **Roguelike và tiến trình màn chơi:** bản đồ sinh ngẫu nhiên theo từng lần bắt đầu; cuộn dọc nhiều tuyến đường; các nút sự kiện đa dạng (chiến đấu, cửa hàng, nâng cấp thẻ, Relic, đánh đổi tài nguyên)
- **Deck-building:** bắt đầu bộ bài cơ bản; thu thập, nâng cấp, loại bỏ thẻ; hệ thống Cổ vật (Relic) và Trang bị (Trinket) với hiệu ứng chủ động và nội tại
- **Procedural Generation:** bản đồ, sự kiện, thẻ bài, Relic sinh ngẫu nhiên hoàn toàn
- **Permadeath:** thua → mất toàn bộ bộ bài, trang bị, tiến trình; bắt đầu lại với bản đồ mới
- **Chiến đấu theo lượt + chiến thuật vị trí:** sàn đấu 2 hàng × 3 cột; ưu tiên tấn công mục tiêu trực diện; dồn hàng khi có đơn vị bị tiêu diệt
- **Hệ thống nguyên tố:** Bleed, Frost, Decay, Chain — cộng dồn Stack, kích hoạt ở thời điểm khác nhau

### Khảo sát trò chơi tham khảo *(Mục 1.4)*
| | Wildfrost | Slay the Spire |
|---|---|---|
| **Điểm mạnh** | Grid Combat, Countdown, nội tại đơn vị độc đáo | Deck-building, cân bằng Relic, bản đồ phân nhánh |
| **Điểm yếu** | Deck-building chưa đa dạng | Không có chiến thuật vị trí không gian |
| **Tham khảo** | Hệ thống chiến đấu theo hàng, xây dựng đội hình | Bản đồ Roguelike, sự kiện, quản lý bộ bài, Relic |

**Định hướng đề tài:** kế thừa bản đồ + Deck-building của Slay the Spire, phát triển chiến đấu bàn cờ từ Wildfrost, bổ sung hệ thống phản ứng nguyên tố và kỹ năng tác động theo khu vực riêng

---

## SLIDE 4: CƠ SỞ LÝ THUYẾT

### Unity Engine *(Mục 1.10.1)*
- Game Engine đa nền tảng do Unity Technologies phát triển, ra mắt 2005
- Hỗ trợ 2D, 3D, VR, AR; tích hợp Scene Editor, Animation System, UI System, Physics Engine, Particle System
- Khả năng đa nền tảng (Windows, macOS, Android, iOS, WebGL) từ cùng một mã nguồn

### Ngôn ngữ C# *(Mục 1.10.2)*
- Ngôn ngữ lập trình hướng đối tượng do Microsoft phát triển, là ngôn ngữ chính của Unity
- Hỗ trợ đầy đủ: Encapsulation, Inheritance, Polymorphism, Abstraction
- Trong Unity: các lớp kế thừa từ **MonoBehaviour** để quản lý hành vi của GameObject

### Mô hình Component-Based *(Mục 1.10.3)*
- Mỗi GameObject cấu thành từ nhiều Component độc lập (Transform, SpriteRenderer, Animator, Collider, Script...)
- Tách chức năng thành Component riêng biệt → giảm phụ thuộc, tăng tái sử dụng

### Các cơ chế Unity được áp dụng *(Mục 2.1)*
| Cơ chế | Vai trò trong đề tài |
|---|---|
| Unity UI (uGUI) + RectTransform | Xây dựng toàn bộ giao diện người dùng |
| Graphic Raycaster + Event System | Kéo thả thẻ bài, chọn mục tiêu, tương tác UI |
| ScriptableObject | Lưu trữ dữ liệu tĩnh của thẻ bài, kỹ năng, Relic, Trinket, bản đồ |
| Coroutine (IEnumerator) | Điều khiển hiệu ứng di chuyển, độ trễ lượt tấn công, phản ứng nguyên tố |
| JSONUtility | Lưu/tải tiến trình người chơi (bản đồ, bộ bài, HP, Gold, Relic) |
| Particle System + Custom Shader | Hiệu ứng hình ảnh: nổi bật mục tiêu, Dissolve khi đơn vị bị tiêu diệt |

---

## SLIDE 5: MỤC TIÊU VÀ YÊU CẦU

### Mục tiêu cụ thể — Công nghệ & thuật toán *(Mục 1.6.2.1)*
- Nghiên cứu thuật toán sinh bản đồ ngẫu nhiên dựa trên **DAG** (Directed Acyclic Graph), đảm bảo tính cân bằng qua các ràng buộc thuật toán
- Áp dụng Design Patterns: **Singleton**, **Strategy Pattern**, **State Machine** — mở rộng cao, dễ bảo trì
- Khai thác các công cụ Unity: UI Toolkit, Animation, BattleGrid, ScriptableObject, quản lý Scene

### Mục tiêu cụ thể — Gameplay & dữ liệu *(Mục 1.6.2.2)*
- Hệ thống chiến đấu theo lượt trên **bàn cờ chiến thuật**: triển khai đội hình, sử dụng thẻ bài, kích hoạt kỹ năng, tương tác vị trí
- Hệ thống **Deck-building**: thu thập, nâng cấp, loại bỏ thẻ; Relic, Trinket, hiệu ứng nguyên tố
- Kiến trúc **Data-Driven** qua ScriptableObject: tách biệt dữ liệu và logic xử lý
- Vòng lặp Roguelike: sinh bản đồ, lựa chọn tuyến đường, Boss, **Permadeath**

### Yêu cầu chức năng *(Mục 2.3.1)*
- **UI Interaction:** Drag & Drop thẻ bài, lựa chọn tuyến đường, xem thông tin chi tiết khi hover
- **Map Progression:** sinh bản đồ theo Seed dạng DAG; khóa tuyến không được chọn
- **Combat System:** triển khai thẻ, tính sát thương, xác định mục tiêu ưu tiên, cập nhật trạng thái
- **Skill System:** đơn mục tiêu, theo hàng, toàn bộ kẻ địch, áp dụng hiệu ứng nguyên tố
- **Target Selection:** dựa trên vị trí quân bài trên bàn cờ
- **Deck Management:** thêm, loại bỏ thẻ bài; nhận phần thưởng sau trận
- **Save/Load:** lưu tự động sau sự kiện quan trọng; khôi phục đầy đủ dữ liệu

### Yêu cầu phi chức năng *(Mục 2.3.2)*
- **Performance:** ~60 FPS trên cấu hình phổ thông
- **Usability:** giao diện trực quan, phản hồi hình ảnh và âm thanh đồng bộ
- **Maintainability & Scalability:** thêm nội dung qua cấu hình dữ liệu, không sửa mã nguồn
- **Data Integrity:** không mất dữ liệu khi thoát đột ngột
- **Compatibility:** Microsoft Windows 64-bit

---

## SLIDE 6: PHƯƠNG PHÁP NGHIÊN CỨU

### Phương pháp nghiên cứu lý thuyết *(Mục 1.8.1)*
- Nghiên cứu tài liệu chính thức Unity Technologies: uGUI, ScriptableObject, Animation, quản lý Scene, C#
- Nghiên cứu lý thuyết DAG và thuật toán PCG (Procedural Content Generation)
- Nghiên cứu nguyên lý xác suất trong cơ chế rút bài, phân phối phần thưởng, cân bằng độ khó
- Phân tích gameplay, hệ thống Deck-building, cân bằng trò chơi của **Slay the Spire** và **Wildfrost**

### Phương pháp thực nghiệm *(Mục 1.8.2)*
- Kiến trúc **Data-Driven** với ScriptableObject: tách biệt dữ liệu và logic
- Áp dụng Singleton, Strategy Pattern, State Machine
- Phát triển từng module độc lập theo giai đoạn
- Kiểm thử thường xuyên trên Windows; đánh giá hiệu năng qua Unity Profiler

### Kế hoạch thực hiện *(8 giai đoạn)*
| TT | Thời gian | Nội dung | Kết quả dự kiến |
|---|---|---|---|
| 1 | 16/03–30/03 | Nghiên cứu Unity, xây dựng kịch bản | Nắm vững kiến trúc Unity, hoàn thành kịch bản |
| 2 | 31/03–13/04 | Module Thẻ bài, hoạt ảnh Kéo-Thả | Thẻ bài rút, đặt, kéo thả mượt trên UI |
| 3 | 14/04–27/04 | Luật chơi: lượt, máu, giáp, năng lượng | Vòng lặp chiến đấu hoạt động đúng logic |
| 4 | 28/04–11/05 | AI kẻ địch (Intent System), hoàn thiện thư viện thẻ | Kẻ địch đúng thiết kế, thẻ tương tác được |
| 5 | 12/05–25/05 | Thuật toán sinh Bản đồ ngẫu nhiên (Node Map), hệ thống sự kiện | Di chuyển màn chơi và sự kiện ổn định |
| 6 | 26/05–08/06 | Save/Load, UI/UX, âm thanh, Screen shake, Hit flash | Game lưu/tải được tại các thời điểm xác định |
| 7 | 09/06–20/06 | Playtest, cân bằng chỉ số, sửa lỗi | Bản Build hoàn chỉnh không crash |
| 8 | Tuần cuối | Hoàn thiện slide, chuẩn bị bảo vệ | Sẵn sàng bảo vệ trước hội đồng |

---

## SLIDE 7: THIẾT KẾ HỆ THỐNG — MÔ HÌNH KIẾN TRÚC

### Singleton Design Pattern *(Mục 2.2.1)*
- Đảm bảo một lớp chỉ tồn tại **duy nhất một đối tượng** trong suốt quá trình chạy
- Áp dụng cho: **GameManager**, **BattleManager**, **MapManager**, **AudioManager**
- Mỗi lớp cung cấp điểm truy cập toàn cục qua thuộc tính `Instance`
- Giảm phụ thuộc (Low Coupling); không cần `FindObjectOfType()` hay truyền tham chiếu qua nhiều lớp

### State Machine (FSM) *(Mục 2.2.2)*
- Tại một thời điểm đối tượng chỉ ở **một trạng thái xác định**; chuyển trạng thái khi thỏa điều kiện
- Áp dụng cho vòng lặp chiến đấu và trạng thái thẻ bài
- Thẻ bài: `Spawn → Idle → Attack → Hit → Death`
- Tránh lỗi: thẻ đã chết không thể tiếp tục tấn công hoặc trở thành mục tiêu

### Data-Driven Architecture *(Mục 2.2.3)*
- Dữ liệu và logic xử lý **tách biệt hoàn toàn**
- Toàn bộ dữ liệu tĩnh lưu dưới dạng ScriptableObject Asset: **CardData, SkillData, RelicData, TrinketData**
- Các lớp xử lý (CardDisplay, SkillExecutor, BattleManager) chỉ đọc dữ liệu từ ScriptableObject
- Thêm/sửa thẻ bài, kỹ năng → chỉnh sửa Asset, **không cần thay đổi mã nguồn**

### Strategy Design Pattern *(Mục 2.2.6)*
- Đóng gói hành vi thành các lớp độc lập, có thể thay thế nhau
- Interface `IElementalEffect` ← `FrostEffect`, `BleedEffect`, `DecayEffect`, `ChainEffect`
- `ElementalHandler` gọi phương thức chung của Interface mà không cần biết nguyên tố cụ thể
- Tuân thủ Open/Closed Principle: thêm nguyên tố mới không sửa code hiện có

### Các mô hình khác
- **DAG** *(Mục 2.2.4)*: bản đồ hành trình dạng đồ thị có hướng không chu trình — chỉ tiến về phía trước
- **Grid-based Spatial Model** *(Mục 2.2.5)*: sàn đấu 2 hàng × 3 cột mỗi bên; kỹ năng tác động theo hàng/khu vực

---

## SLIDE 8: THIẾT KẾ HỆ THỐNG — SƠ ĐỒ PHÂN TÍCH

### Biểu đồ hoạt động *(Mục 2.4.1)*
**Luồng tổng quan (Hình 2.4):** Khởi tạo nhân vật + bộ bài cơ bản → Hiển thị bản đồ → Chọn điểm đến → (Sự kiện thường | Chiến đấu) → Phần thưởng → Quay lại bản đồ → Lặp lại → Đánh Boss → Kết thúc / Game Over (máu về 0)

**Luồng chọn mục tiêu và kích hoạt thẻ (Hình 2.5):** Chọn thẻ bài → Chế độ nhắm mục tiêu (mũi tên định hướng cập nhật theo chuột) → Kiểm tra mục tiêu hợp lệ → Xác nhận: thực thi kỹ năng + hoạt ảnh + áp dụng hiệu ứng | Hủy: giữ thẻ trên tay

**Luồng xử lý nguyên tố (Hình 2.6):** Đầu lượt: Frost (làm chậm/ngăn), Chain — Sau hành động NCC: Bleed (tích lũy sát thương vật lý) — Cuối lượt: Decay (sát thương tăng dần)

**Luồng xử lý sự kiện (Hình 2.7):** Di chuyển đến node → Khóa bản đồ → Xác định loại sự kiện → Shop/Relic/Sacrifice/Card Reward/Upgrade → Cập nhật dữ liệu → Mở khóa bản đồ

### Biểu đồ tuần tự *(Mục 2.4.2)*
**Khởi tạo chiến đấu (Hình 2.9):** Chọn node chiến đấu → Lưu dữ liệu → Chuyển sang Battle Scene → BattleManager khởi tạo đội hình + sinh quái → Giai đoạn bố trí → Phát thẻ kỹ năng ban đầu → Mở khóa thao tác chiến đấu

**Xử lý sát thương từ kỹ năng (Hình 2.10):** Chọn mục tiêu hợp lệ → Thoát chế độ nhắm → Hoạt ảnh tấn công → Tính sát thương (chỉ số kỹ năng + hiệu ứng bổ trợ) → ElementalHandler ghi nhận (Bleed...) → Cập nhật UI → Xác định sống/chết → Chuyển thẻ sang discard

**Vòng lặp lượt đánh (Hình 2.11):** Kết thúc lượt NCC → Kiểm tra sinh quái → Lượt kẻ địch: (nguyên tố đầu lượt → tấn công nếu không bị khống chế → nguyên tố cuối lượt) → Lượt tướng NCC → Bleed hậu kỳ → Trả quyền cho người chơi

### Biểu đồ lớp *(Mục 2.4.3)*
- **Dữ liệu lõi (Hình 2.12):** `ScriptableObject` ← `CardData` ← `ChampionData`, `EnemyData`, `SkillData`; `ScriptableObject` ← `RelicData`, `TrinketData`
- **Thực thể sàn đấu (Hình 2.13):** `CardBattle` (MonoBehaviour) kết hợp `RelicHandler` + `TrinketHandler` + `ElementalHandler`
- **Hệ thống nguyên tố (Hình 2.14):** `IElementalEffect` ← `FrostEffect`, `BleedEffect`, `DecayEffect`, `ChainEffect`; `ElementalHandler` lưu danh sách hiệu ứng + gọi đúng thời điểm
- **Bản đồ (Hình 2.15):** `MapManager` quản lý sinh bản đồ + tiến trình; `MapRunData` lưu Seed, Depth, vị trí, lộ trình; `MapSlotDefinition` = loại sự kiện + vị trí + danh sách node kế tiếp (DAG); `MapNode` hiển thị + tương tác; `UIMapLine` vẽ đường nối

---

## SLIDE 9: KẾT QUẢ THỰC HIỆN — CÁC MODULE CHỨC NĂNG CỐT LÕI

### Thiết kế bản đồ và tiến trình *(Mục 2.5.1 & 3.2.2)*
Các loại sự kiện trên bản đồ: **Battle**, **Boss**, **Card Reward**, **Shop** (dùng Gold mua thẻ/Relic/vật phẩm), **Relic**, **Resource**, **Smith** (nâng cấp thẻ), **Sacrifice** (đánh đổi tài nguyên)

Thuật toán sinh bản đồ có ràng buộc (Hình 3.3):
- **Thuật toán phân tách chiều sâu (Depth Calculation):** duyệt 20 Node, gán giá trị Depth; các node cùng Depth = cùng hàng ngang
- **Sinh sự kiện ngẫu nhiên từ Seed** với 3 ràng buộc:
  - *Luật Cha-Con:* Node con không trùng loại với Node cha kết nối trực tiếp
  - *Luật Anh-Em:* Các Node cùng Depth không được trùng loại sự kiện
  - *Luật Cửa hàng:* Đảm bảo 2–3 Shop, cách nhau ít nhất 2 hàng
- **Nội suy không gian (Spatial Lerping):** ScrollRect tự động cuộn dọc; token di chuyển theo quỹ đạo đường cong Sin (Arc)

### Hệ thống quản lý bộ bài *(Mục 3.2.3)*
- Lớp `SkillHandManager` quản lý 3 nhóm: **drawPile** (chồng rút), **hand** (trên tay), **discardPile** (chồng bỏ)
- Đầu trận: xáo trộn ngẫu nhiên; sau khi dùng → sang discardPile; hết drawPile → thu hồi toàn bộ discard → xáo → tiếp tục (Hình 3.4)

### Hệ thống chiến đấu trên sân đấu *(Mục 2.5.3 & 3.2.4)*
- `BattleGrid`: 2 hàng × 3 cột mỗi bên (6 ô/bên)
- `GetAttackTarget()`: ưu tiên mục tiêu trực diện cùng hàng (toán tử `??`), chuyển hàng còn lại nếu trống
- `ShiftRow()`: vòng lặp từ trung tâm ra mép, phát hiện ô rỗng → `MoveCardToSlot()` tiến lên — mô phỏng cơ chế tiền tuyến (Hình 3.5)

### Hệ thống phản ứng nguyên tố *(Mục 2.5.4 & 3.2.6)*
- Strategy Pattern qua `IElementalEffect`; `ElementalHandler` lưu số Stack từng nguyên tố, kích hoạt đúng thời điểm

| Nguyên tố | Thời điểm kích hoạt | Cơ chế |
|---|---|---|
| **Bleed (Chảy máu)** | Sau lượt NCC kết thúc | Tích lũy sát thương vật lý nhận được → phát nổ |
| **Frost (Đóng băng)** | Đầu lượt mục tiêu | Làm chậm / ngăn hành động tương ứng số Stack |
| **Decay (Phân rã)** | Cuối lượt | Sát thương theo thời gian, tăng dần nếu mục tiêu sống |
| **Chain (Dây chuyền)** | Sau đòn tấn công chính | Sát thương lan sang mục tiêu lân cận cùng hàng |

### Cơ chế kéo thả và chọn mục tiêu *(Mục 3.2.7)*
- Áp dụng **Click-to-Target** (không kéo thẻ trực tiếp đến mục tiêu) → hạn chế che khuất màn hình, tăng chính xác
- Hiển thị mũi tên định hướng vẽ bằng **đường cong Bezier** từ thẻ đến con trỏ chuột
- `SkillDragHandler` dùng `EventSystem.RaycastAll` (UI Raycasting) thay vì va chạm vật lý (Collider) → giảm chi phí xử lý (Hình 3.8)

### Hệ thống quản lý kẻ địch *(Mục 3.2.8)*
- **Dynamic Fill Logic:** theo dõi ô trống qua bộ đếm lượt; ô trống sau N lượt → lấy quái từ đợt kế tiếp bổ sung
- DOTween tạo hiệu ứng quái xuất hiện từ ngoài màn hình vào đúng vị trí chiến đấu (Hình 3.9)

---

## SLIDE 10: KẾT QUẢ THỰC HIỆN — GIAO DIỆN & KỸ THUẬT

### Thiết kế chi tiết các sự kiện trên bản đồ *(Mục 2.5.2)*
- **Shop UI:** mua Thẻ kỹ năng, Cổ vật (Relic), Phụ kiện (Trinket) bằng Gold tích lũy được
- **Relic UI:** hiển thị thông tin chi tiết (hiệu ứng, chỉ số hỗ trợ); chọn trang bị ngay hoặc lưu vào túi đồ
- **Sacrifice UI:** kéo thả thẻ không cần thiết vào khu vực hiến tế → xóa khỏi bộ bài, giảm thẻ dư thừa
- **Card Reward UI:** sinh ngẫu nhiên 3 thẻ từ cơ sở dữ liệu; người chơi chọn 1 bổ sung vào bộ bài
- **Upgrade UI:** tăng HP, Attack hoặc hiệu quả kỹ năng của thẻ/tướng đang sở hữu
- **Battle UI:** giao diện trung tâm — bàn cờ nửa trên; thẻ trên tay + tài nguyên nửa dưới; hỗ trợ toàn bộ thao tác kéo thả và kỹ năng

### Quản lý dữ liệu và lưu tiến trình *(Mục 3.3.2)*
- **ScriptableObject:** nhiều đối tượng dùng chung 1 nguồn dữ liệu → giảm RAM, tách biệt data/logic; cân bằng trò chơi qua Unity Inspector mà không cần sửa mã nguồn
- **MapRunData** đóng gói: bộ bài hiện tại, HP còn lại, Gold, Relic đã thu thập, Node đang đứng, tiến trình lượt chơi
- `JsonUtility.ToJson()` → ghi file cục bộ tự động sau mỗi trận đấu hoặc di chuyển Node (Hình 3.13)
- Khởi động: đọc JSON → Deserialize → tái tạo đầy đủ trạng thái; hạn chế mất tiến trình khi đóng đột ngột

### Áp dụng Design Pattern vào code *(Mục 3.3.1)*
- **Singleton (Hình 3.10):** `GameManager`, `MapManager`, `BattleManager`, `AudioManager` — loại bỏ `FindObjectOfType()`, đồng bộ dữ liệu xuyên Scene
- **Strategy (Hình 3.11):** `IElementalEffect` — thêm nguyên tố mới chỉ cần tạo lớp kế thừa Interface, không sửa code hiện có; tuân thủ Open/Closed Principle
- **FSM (Hình 3.12):** `Spawn → Idle → Attack → Hit → Death` cho thẻ bài trên bàn cờ — tránh xung đột giữa hoạt ảnh và logic chiến đấu

### Tối ưu hiệu năng *(Mục 3.3.3)*
- **DOTween** thay Animator cho hoạt ảnh UI (phóng to hover, kéo thả, bay vào bộ bài, xuất hiện, di chuyển bản đồ) — nội suy trực tiếp bằng code, giảm số State Machine cần quản lý, tiết kiệm CPU
- Tái sử dụng đối tượng: cửa sổ phần thưởng, giao diện bản đồ, hiệu ứng UI — không khởi tạo mới mỗi lần
- ScriptableObject chia sẻ dữ liệu chung → hạn chế tạo bản sao không cần thiết trong bộ nhớ

### Hệ thống âm thanh *(Mục 2.5.6)*
- `AudioManager` (Singleton) — nhạc nền duy trì liên tục khi chuyển Scene
- 3 kênh: **Menu/Map Music** (tạo không khí thư giãn), **Battle Music** (tiết tấu nhanh khi vào trận), **SFX** (kéo thả, tấn công, nhận sát thương, tương tác UI)
- Cơ chế lặp không ngắt quãng: Coroutine theo dõi thời gian → fade out bằng `Mathf.Lerp` → phát lại → khôi phục âm lượng
- Đồng bộ âm thanh với hoạt ảnh qua DOTween tweening

---

## SLIDE 11: ĐÁNH GIÁ KẾT QUẢ

### Đóng gói và triển khai *(Mục 3.4.1)*
- Build thành công file `.exe` standalone Windows, kiến trúc **IL2CPP** (C# → C++ → biên dịch) — cải thiện hiệu năng thực thi, tăng tương thích, hạn chế dịch ngược mã nguồn
- Toàn bộ tài nguyên (hình ảnh, âm thanh, ScriptableObject, Scene) đóng gói đồng bộ — không cần cài thêm môi trường phát triển
- Kiểm tra sau Build: Save/Load, chuyển Scene, chiến đấu, bản đồ, phần thưởng — **hoạt động chính xác** so với Unity Editor

### Kết quả thực nghiệm *(Mục 3.4.2)*
- **Đồ họa:** phong cách vẽ tay (hand-drawn), màu sắc tươi sáng đồng nhất giữa nhân vật, quái, thẻ bài, bối cảnh
- **Giao diện chiến đấu:** bàn cờ nửa trên, thẻ tay + tài nguyên nửa dưới — dễ quan sát thao tác
- **Hiệu ứng pop-out:** nhân vật/quái vật vượt khung thẻ → tạo chiều sâu, sinh động hơn cách truyền thống (Hình 3.16)
- **Unity Profiler (Hình 3.17):** frame time thường **1–2ms**, dưới ngưỡng 16ms cần thiết cho 60FPS; đỉnh ngắn khi khởi tạo đối tượng nhưng không kéo dài, không ảnh hưởng trải nghiệm; Rendering & Memory ổn định

### Ưu điểm *(Kết luận)*
- Kiến trúc Data-Driven → thêm thẻ bài, Relic, kỹ năng **hầu như không cần sửa mã nguồn**
- Bản đồ ngẫu nhiên → nhiều hướng đi, tăng khả năng chơi lại
- Chiến đấu kết hợp Deck-building + vị trí lưới → chiều sâu chiến thuật, khuyến khích nhiều chiến lược
- Mã nguồn theo Singleton, Strategy, FSM → dễ bảo trì và mở rộng
- Hệ thống lưu tiến trình ổn định
- Giao diện và âm thanh thiết kế đồng bộ

---

## SLIDE 12: ỨNG DỤNG VÀ ĐÓNG GÓP

### Tính thực tiễn của đề tài *(Mục 1.5.2)*

**Đối với xu hướng phát triển trò chơi:**
Roguelike Deck-builder đang được nhiều người chơi quan tâm trên PC và thiết bị di động nhờ tính chơi lại cao; xây dựng theo hướng này giúp tiếp cận xu hướng thiết kế hiện đại, tạo tiền đề phát triển thành sản phẩm có khả năng thương mại hóa trong tương lai

**Đối với quá trình phát triển và mở rộng sản phẩm:**
Kiến trúc Data-Driven → các thành phần (thẻ bài, kỹ năng, Relic, bản đồ, hiệu ứng mới) bổ sung qua cấu hình dữ liệu, không thay đổi mã nguồn → giảm thời gian phát triển, thuận lợi bảo trì lâu dài

**Đối với trải nghiệm người chơi:**
Giao diện trực quan — cơ chế kéo thả có mũi tên định hướng, bố trí đội hình trên sàn đấu, highlight mục tiêu, phản hồi hình ảnh trong chiến đấu → dễ thao tác, tăng tính trực quan

**Đối với quản lý dữ liệu:**
Cơ chế Save/Load đảm bảo tính ổn định, hạn chế mất dữ liệu khi ứng dụng bị đóng ngoài ý muốn; tạo nền tảng cho việc phát triển thêm tính năng lưu trữ và đồng bộ dữ liệu trong tương lai

---

## SLIDE 13: HẠN CHẾ VÀ HƯỚNG PHÁT TRIỂN

### Hạn chế hiện tại *(Kết luận — Hạn chế)*
- **AI kẻ địch:** chủ yếu hoạt động dựa trên các quy tắc được xây dựng sẵn; chưa áp dụng các mô hình hành vi phức tạp như **Behavior Tree** hoặc **Utility AI** để đưa ra quyết định linh hoạt hơn
- **Tối ưu bộ nhớ:** một số hệ thống hiệu ứng hình ảnh và đối tượng giao diện vẫn được khởi tạo trực tiếp trong quá trình chơi — còn tiềm năng tối ưu bằng **Object Pooling** để giảm Garbage Collection
- **Nội dung:** số lượng thẻ bài, Relic, sự kiện và Boss còn ở quy mô thử nghiệm; chưa đủ phong phú để tạo ra nhiều hướng xây dựng chiến thuật trong thời gian dài

### Hướng phát triển tiếp theo *(Kết luận — Hướng phát triển)*
- Phát triển thêm nhiều **thẻ bài, Relic, Trinket** và hiệu ứng mới → đa dạng hóa chiến thuật, tăng chiều sâu
- Vẽ và nâng cấp tài nguyên game → tăng tính hiển thị và trải nghiệm người chơi
- Áp dụng **Object Pooling** cho hiệu ứng chiến đấu, Damage Popup và Particle → giảm chi phí bộ nhớ
- Bổ sung nhiều **loại sự kiện**, kẻ địch với cơ chế mới, Boss và cấu trúc bản đồ mới → tăng khả năng chơi lại
- Hoàn thiện hệ thống cài đặt, lưu nhiều hồ sơ người chơi, bổ sung **Achievement**

---

## SLIDE 14: KẾT LUẬN

### Tóm tắt nội dung đề tài

Đề tài đã nghiên cứu và xây dựng thành công trò chơi chiến thuật thẻ bài Roguelike 2D trên Unity, đáp ứng hầu hết các mục tiêu ban đầu đề ra:

**Chương 1 — Cơ sở lý thuyết (20%):**
Tổng quan Roguelike Deck-builder; phân tích Wildfrost và Slay the Spire; cơ sở khoa học (DAG, PCG, Design Patterns, Data-Driven, lý thuyết xác suất); Unity Engine, C#, Component-Based Architecture

**Chương 2 — Phân tích và Thiết kế hệ thống (30%):**
Các mẫu thiết kế Singleton, Strategy, FSM, Data-Driven; phân tích yêu cầu chức năng và phi chức năng; biểu đồ hoạt động, tuần tự, lớp; thiết kế bản đồ, sự kiện, sàn đấu, thẻ bài, nguyên tố, kỹ năng, âm thanh

**Chương 3 — Xây dựng, Cài đặt và Đánh giá (50%):**
Môi trường phát triển (Unity + C# + Git); xây dựng các module cốt lõi (bản đồ, bộ bài, Grid Combat, nguyên tố, kéo thả, kẻ địch); giải pháp kỹ thuật (Design Pattern, Save/Load JSON, tối ưu DOTween); kết quả thực nghiệm (Unity Profiler: 1–2ms/frame, Build .exe IL2CPP ổn định)

---

## SLIDE 15: CẢM ƠN

*Lời cảm ơn — Lời mở đầu*

Em xin gửi lời cảm ơn đến:

- **TS. Cù Việt Dũng** — đã tận tình hướng dẫn, hỗ trợ, cung cấp kiến thức quý giá và nhiệt tình giải đáp thắc mắc trong suốt quá trình nghiên cứu và thực hiện đề tài; giúp em vượt qua những khó khăn trong quá trình thực hiện
- **Quý Thầy Cô Khoa Công nghệ Thông tin — Trường Đại học Thủy Lợi** — đã truyền đạt không chỉ kiến thức từ cơ bản đến chuyên sâu mà còn là động lực, đam mê tìm tòi và tinh thần học hỏi trong suốt bốn năm học

Em rất mong nhận được sự đánh giá, nhận xét và góp ý quý báu từ **Hội đồng bảo vệ** và quý Thầy Cô để đề tài được hoàn thiện hơn, đồng thời giúp em rút ra bài học kinh nghiệm sâu sắc cho con đường sự nghiệp phía trước.

---

*Kính mời Hội đồng đặt câu hỏi.*
