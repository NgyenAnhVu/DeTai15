# Đề tài 15: Hệ thống Nộp bài và Chấm bài Tự động (Event-Driven Architecture)

Hệ thống cho phép sinh viên nộp bài tập trực tuyến, xử lý chấm điểm bất đồng bộ thông qua Message Broker (RabbitMQ) và gửi thông báo kết quả theo thời gian thực (SignalR).

## 1. Kiến trúc hệ thống

Student Client (index.html) --> SubmissionAPI --> RabbitMQ (submission_queue)
                                     |                        |
                                     v                        v
                                SQLite DB <----------- GradingWorker
                                     |                        |
                                     v                        v
Student Client (SignalR) <-- NotificationService <-- RabbitMQ (graded_queue)

## 2. Cấu trúc dự án (.NET 10)
- **SubmissionAPI** (Port `5158`): Cung cấp RESTful API quản lý bài tập, tiếp nhận bài nộp (`Submitted`), lưu vào SQLite và đẩy sự kiện vào `submission_queue`.
- **GradingWorker**: Background Worker lắng nghe `submission_queue`, thực hiện chấm điểm tự động giả lập, cập nhật điểm/nhận xét (`Graded`) vào SQLite và phát sự kiện sang `graded_queue`.
- **NotificationService** (Port `5200`): Lắng nghe `graded_queue` và đẩy thông báo kết quả realtime tới trình duyệt qua SignalR Hub (`/notificationHub`).
- **index.html**: Giao diện Web Client tương tác đầy đủ với hệ thống.

## 3. Danh sách API chính
- `GET /assignments`: Lấy danh sách bài tập.
- `POST /assignments/{id}/submissions`: Nộp bài cho một bài tập.
- `GET /submissions/{id}`: Tra cứu chi tiết một bài nộp.
- `GET /students/{studentId}/submissions`: Tra cứu lịch sử bài nộp của sinh viên.

## 4. Hướng dẫn chạy hệ thống
1. Cấu hình chuỗi kết nối RabbitMQ (CloudAMQP) trong `appsettings.json` của cả 3 service.
2. Mở 3 cửa sổ Terminal và chạy lần lượt từng service:
   - `cd SubmissionAPI && dotnet run`
   - `cd GradingWorker && dotnet run`
   - `cd NotificationService && dotnet run`
3. Mở file `index.html` trên trình duyệt để trải nghiệm nộp bài và nhận kết quả realtime.