// Mirrors PaymentCheckoutResponse on the backend: the PayPal page, and the addresses where it ends.
class PaymentCheckout {
  final int paymentId;
  final String approvalUrl;
  final String returnUrl;
  final String cancelUrl;

  PaymentCheckout({required this.paymentId, required this.approvalUrl, required this.returnUrl, required this.cancelUrl});

  factory PaymentCheckout.fromJson(Map<String, dynamic> json) {
    return PaymentCheckout(
      paymentId: json['paymentId'],
      approvalUrl: json['approvalUrl'] ?? '',
      returnUrl: json['returnUrl'] ?? '',
      cancelUrl: json['cancelUrl'] ?? '',
    );
  }
}
