import 'package:majstorio_mobile/utils/formatters.dart';

// The agreed time of the work, created by the API when an offer is accepted. Mirrors AppointmentResponse.
class Appointment {
  // The status numbers the API uses for an appointment.
  static const int pending = 0;
  static const int confirmed = 1;
  static const int cancelled = 2;
  static const int completed = 3;
  static const Map<int, String> statusNames = {
    0: 'Na čekanju',
    1: 'Potvrđen',
    2: 'Otkazan',
    3: 'Završen',
  };

  final int id;
  // The accepted offer. The appointment has no request id, so the app finds the request through this offer.
  final int offerId;
  final String customerName;
  final String tradespersonName;
  final String serviceCategoryName;
  final DateTime startUtc;
  final DateTime endUtc;
  final int status;
  // Set by the API once PayPal confirmed the payment; the pay button is hidden then.
  final bool isPaid;
  final bool isRefunded;

  Appointment({
    required this.id,
    required this.offerId,
    required this.customerName,
    required this.tradespersonName,
    required this.serviceCategoryName,
    required this.startUtc,
    required this.endUtc,
    required this.status,
    this.isPaid = false,
    this.isRefunded = false,
  });

  String get statusName => statusNames[status] ?? 'Nepoznato';
  bool get isFinished => status == cancelled || status == completed;

  // The tradesperson can mark the work done once it is confirmed and has started. The API checks this again.
  bool canBeCompleted(DateTime now) => status == confirmed && !startUtc.isAfter(now.toUtc());

  factory Appointment.fromJson(Map<String, dynamic> json) {
    return Appointment(
      id: json['id'],
      offerId: json['offerId'],
      customerName: json['customerName'] ?? '',
      tradespersonName: json['tradespersonName'] ?? '',
      serviceCategoryName: json['serviceCategoryName'] ?? '',
      startUtc: parseUtc(json['startUtc']),
      endUtc: parseUtc(json['endUtc']),
      status: json['status'],
      isPaid: json['isPaid'] ?? false,
      isRefunded: json['isRefunded'] ?? false,
    );
  }
}
