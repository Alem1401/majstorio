import 'package:json_annotation/json_annotation.dart';

part 'appointment.g.dart';

// Read only: what is sent is a separate *UpsertRequest.
@JsonSerializable(createToJson: false)
class Appointment {
  // The status numbers the API uses for an appointment.
  static const Map<int, String> statusNames = {
    0: 'Pending',
    1: 'Confirmed',
    2: 'Cancelled',
    3: 'Completed',
  };

  final int id;
  final int offerId;
  final String customerName;
  final String tradespersonName;
  final String serviceCategoryName;
  final DateTime startUtc;
  final DateTime endUtc;
  final int status;

  Appointment({
    required this.id,
    required this.offerId,
    required this.customerName,
    required this.tradespersonName,
    required this.serviceCategoryName,
    required this.startUtc,
    required this.endUtc,
    required this.status,
  });

  String get statusName => statusNames[status] ?? 'Unknown';

  factory Appointment.fromJson(Map<String, dynamic> json) =>
      _$AppointmentFromJson(json);
}
