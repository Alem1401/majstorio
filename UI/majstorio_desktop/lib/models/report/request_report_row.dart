import 'package:json_annotation/json_annotation.dart';

part 'request_report_row.g.dart';

// One line of the service requests report. Mirrors RequestReportRowResponse on the backend.
@JsonSerializable(createToJson: false)
class RequestReportRow {
  // The status numbers the API uses for a service request.
  static const Map<int, String> statusNames = {
    0: 'Created',
    1: 'Offer accepted',
    2: 'Appointment confirmed',
    3: 'Completed',
    4: 'Cancelled',
  };

  final String cityName;
  final String serviceCategoryName;
  final int status;
  final int requestCount;

  RequestReportRow({
    required this.cityName,
    required this.serviceCategoryName,
    required this.status,
    required this.requestCount,
  });

  String get statusName => statusNames[status] ?? 'Unknown';

  factory RequestReportRow.fromJson(Map<String, dynamic> json) =>
      _$RequestReportRowFromJson(json);
}
