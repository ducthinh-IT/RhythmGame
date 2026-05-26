# DANH SÁCH THẺ TEST THANH TOÁN (Môi trường Sandbox)

Tất cả thông tin dưới đây chỉ dùng để thử nghiệm và không mất tiền thật.

---

## 1. CỔNG VNPAY (Thẻ ATM nội địa)
Khi giao diện chuyển sang VNPay, hãy chọn logo ngân hàng **NCB**.

- **Số thẻ:** `9704198526191432198`
- **Tên chủ thẻ:** `NGUYEN VAN A`
- **Ngày phát hành:** `07/15`
- **Mã OTP:** `123456`

---

## 2. CỔNG MOMO (Thẻ ATM qua NAPAS)
Khi chọn hình thức "Thẻ ATM qua MoMo".

- **Số thẻ:** `9704000000000018`
- **Tên chủ thẻ:** `NGUYEN VAN A`
- **Ngày phát hành:** `03/07`
- **Mã OTP:** `000000` (Nếu sai thử: `123456` hoặc chữ `OTP`)

---

## 3. CỔNG MOMO (Ví điện tử - Đăng nhập web)
Khi chọn hình thức "Ví MoMo" và chọn đăng nhập thay vì quét mã.

- **Số điện thoại:** `0900000001` (Hoặc bất kỳ số nào từ `0900000000` đến `0900000099`)
- **Mật khẩu (Password):** `000000`
- **Mã xác thực (OTP):** `000000`

---

### ⚠️ Lưu ý quan trọng khi gặp lỗi:
1. **Lỗi "Khóa trang web không hợp lệ":** Đây là lỗi cấu hình reCAPTCHA của chính MoMo/VNPay. Bạn hãy **mặc kệ nó**, cứ điền thông tin thẻ và nhấn Thanh toán.
2. **Lỗi thẻ không hợp lệ:** Đảm bảo bạn đang chạy Backend ở môi trường Sandbox (đã cấu hình trong file `.env`).
3. **Thử lại:** Nếu một cổng bị nghẽn (do máy chủ test bảo trì), hãy chuyển sang cổng còn lại để test luồng cộng tiền của code.
