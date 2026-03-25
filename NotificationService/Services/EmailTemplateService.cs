namespace NotificationService.Services;

public sealed class EmailTemplateService
{
    public string GetOrderCreatedHtml(Guid orderId, string customerId, decimal amount)
    {
        return WrapInLayout("Xác nhận đơn hàng", $@"
            <h2 style='color: #2d3748;'>Cảm ơn bạn đã đặt hàng!</h2>
            <p>Đơn hàng của bạn đã được tiếp nhận và đang trong quá trình xử lý.</p>
            <div style='background-color: #f7fafc; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                <p style='margin: 5px 0;'><strong>Mã đơn hàng:</strong> <span style='color: #4a5568;'>{orderId}</span></p>
                <p style='margin: 5px 0;'><strong>Khách hàng:</strong> {customerId}</p>
                <p style='margin: 5px 0;'><strong>Tổng tiền:</strong> <span style='color: #2b6cb0; font-size: 18px; font-weight: bold;'>{amount:N0} VND</span></p>
            </div>
            <p style='font-size: 14px; color: #718096;'>Chúng tôi sẽ thông báo cho bạn ngay khi đơn hàng được chuẩn bị xong.</p>
        ");
    }

    public string GetOrderCompletedHtml(Guid orderId)
    {
        return WrapInLayout("Đơn hàng hoàn tất", $@"
            <div style='text-align: center;'>
                <div style='background-color: #c6f6d5; color: #22543d; padding: 10px; border-radius: 50%; width: 60px; height: 60px; line-height: 60px; font-size: 30px; margin: 0 auto 20px;'>✓</div>
                <h2 style='color: #22543d;'>Thanh toán thành công!</h2>
                <p>Chúc mừng! Đơn hàng <strong>{orderId}</strong> của bạn đã hoàn tất thanh toán và đang được chuẩn bị để giao đến bạn.</p>
                <div style='margin-top: 30px;'>
                    <a href='#' style='background-color: #38a169; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Theo dõi đơn hàng</a>
                </div>
            </div>
        ");
    }

    public string GetOrderCancelledHtml(Guid orderId, string reason)
    {
        var reasonText = reason switch
        {
            "InventoryUnavailable" => "Sản phẩm trong kho đã hết đột xuất.",
            "PaymentGatewayTimeout" => "Cổng thanh toán không phản hồi.",
            _ => reason
        };

        return WrapInLayout("Thông báo hủy đơn hàng", $@"
            <h2 style='color: #c53030;'>Đơn hàng đã bị hủy</h2>
            <p>Chúng tôi rất tiếc phải thông báo rằng đơn hàng <strong>{orderId}</strong> của bạn đã bị hủy.</p>
            <div style='background-color: #fff5f5; border-left: 4px solid #c53030; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0; color: #9b2c2c;'><strong>Lý do:</strong> {reasonText}</p>
            </div>
            <p style='font-size: 14px; color: #718096;'>Nếu bạn đã bị trừ tiền, hệ thống sẽ tự động hoàn tiền trong vòng 24-48 giờ làm việc.</p>
        ");
    }

    private string WrapInLayout(string title, string content)
    {
        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #4a5568; background-color: #edf2f7; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 40px auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
                .header {{ background-color: #2d3748; color: white; padding: 20px; text-align: center; }}
                .content {{ padding: 30px; }}
                .footer {{ background-color: #f7fafc; color: #a0aec0; padding: 20px; text-align: center; font-size: 12px; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h1 style='margin: 0; font-size: 24px;'>{title}</h1>
                </div>
                <div class='content'>
                    {content}
                </div>
                <div class='footer'>
                    <p>&copy; 2026 EventSourcing Demo System. All rights reserved.</p>
                    <p>Đây là email tự động, vui lòng không phản hồi email này.</p>
                </div>
            </div>
        </body>
        </html>";
    }
}
