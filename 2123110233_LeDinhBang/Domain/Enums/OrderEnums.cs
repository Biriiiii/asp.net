using System.Text.Json.Serialization;

namespace BookStore.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Pending,        // Vừa tạo, chờ thanh toán (online)
    Confirmed,      // Đã xác nhận / đã thanh toán
    Processing,     // Kho đang đóng gói
    Shipping,       // Đã bàn giao vận chuyển
    Delivered,      // Khách đã nhận hàng
    Cancelled,      // Đã hủy
    Refunded        // Đã hoàn tiền
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    COD,            // Thanh toán khi nhận hàng
    BankTransfer,   // Chuyển khoản thủ công
    VNPay,
    MoMo,
    ZaloPay,
    CreditCard      // Visa / Mastercard
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Unpaid,
    Paid,
    Refunding,
    Refunded,
    Failed
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShippingMethod
{
    Standard,       // 3-5 ngày
    Express,        // 1-2 ngày
    SameDay         // Giao trong ngày (nội thành)
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShipmentStatus
{
    Pending,        // Chờ lấy hàng
    PickedUp,       // Đã lấy hàng
    InTransit,      // Đang vận chuyển
    OutForDelivery, // Đang giao
    Delivered,      // Đã giao thành công
    Failed,         // Giao thất bại
    Returned        // Hoàn hàng về kho
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RefundStatus
{
    Pending,
    Processing,
    Completed,
    Rejected
}
