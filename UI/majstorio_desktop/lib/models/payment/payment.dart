// Mirrors PaymentResponse on the backend.
class Payment {
  // The status numbers the API uses for a payment.
  static const int paid = 1;
  static const Map<int, String> statusNames = {
    0: 'Pending',
    1: 'Paid',
    2: 'Failed',
    3: 'Cancelled',
  };

  final int id;
  final String customerName;
  final String tradespersonName;
  final double amount;
  final int status;
  final DateTime? paidAtUtc;
  final bool isRefunded;

  Payment({
    required this.id,
    required this.customerName,
    required this.tradespersonName,
    required this.amount,
    required this.status,
    this.paidAtUtc,
    required this.isRefunded,
  });

  String get statusName => statusNames[status] ?? 'Unknown';
  bool get canBeRefunded => status == paid && !isRefunded;

  factory Payment.fromJson(Map<String, dynamic> json) {
    return Payment(
      id: json['id'],
      customerName: json['customerName'] ?? '',
      tradespersonName: json['tradespersonName'] ?? '',
      // The refund is always the amount PayPal captured, so that is the amount shown once it is known.
      amount: ((json['capturedAmount'] ?? json['amount']) as num).toDouble(),
      status: json['status'],
      paidAtUtc: json['paidAtUtc'] == null ? null : DateTime.parse(json['paidAtUtc']),
      isRefunded: json['isRefunded'] ?? false,
    );
  }
}
