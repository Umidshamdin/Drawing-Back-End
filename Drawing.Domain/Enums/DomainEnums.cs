
using System.ComponentModel;

namespace Drawing.Domain.Enums;

public enum ArtistStatus
{
    [Description("Təsdiq gözləyir")]
    Pending = 1,

    [Description("Təsdiqlənib")]
    Approved = 2,

    [Description("Rədd edilib")]
    Rejected = 3,

    [Description("Fəaliyyəti dayandırılıb")]
    Suspended = 4
}

public enum ArtworkModerationStatus
{
    [Description("Qaralama")]
    Draft = 1,

    [Description("Yoxlanış gözləyir")]
    PendingReview = 2,

    [Description("Təsdiqlənib")]
    Approved = 3,

    [Description("Rədd edilib")]
    Rejected = 4,

    [Description("Düzəliş tələb olunur")]
    ChangesRequested = 5,

    [Description("Gizlədilib")]
    Hidden = 6
}

public enum ArtworkSaleStatus
{
    [Description("Satışdadır")]
    Available = 1,

    [Description("Rezerv edilib")]
    Reserved = 2,

    [Description("Satılıb")]
    Sold = 3
}

public enum OrderStatus
{
    [Description("Təsdiq gözləyir")]
    Pending = 1,

    [Description("Təsdiqlənib")]
    Confirmed = 2,

    [Description("Ödəniş gözlənilir")]
    AwaitingPayment = 3,

    [Description("Ödəniş edilib")]
    Paid = 4,

    [Description("Çatdırılmaya hazırlanır")]
    PreparingDelivery = 5,

    [Description("Göndərilib")]
    Shipped = 6,

    [Description("Tamamlanıb")]
    Completed = 7,

    [Description("Ləğv edilib")]
    Cancelled = 8,

    [Description("Rədd edilib")]
    Rejected = 9
}

public enum ReservationStatus
{
    [Description("Aktivdir")]
    Active = 1,

    [Description("Müddəti bitib")]
    Expired = 2,

    [Description("Ləğv edilib")]
    Cancelled = 3,

    [Description("Satışa çevrilib")]
    ConvertedToSale = 4
}

public enum PaymentStatus
{
    [Description("Ödəniş gözlənilir")]
    Pending = 1,

    [Description("Ödəniş təsdiqlənib")]
    Confirmed = 2,

    [Description("Ödəniş uğursuz olub")]
    Failed = 3,

    [Description("Ödəniş geri qaytarılıb")]
    Refunded = 4
}
