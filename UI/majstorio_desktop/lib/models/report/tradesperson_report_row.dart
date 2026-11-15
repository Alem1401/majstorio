import 'package:json_annotation/json_annotation.dart';

part 'tradesperson_report_row.g.dart';

// One line of the tradesperson performance report. Mirrors TradespersonReportRowResponse on the backend.
@JsonSerializable(createToJson: false)
class TradespersonReportRow {
  final String tradespersonName;
  final String cityName;
  final int offerCount;
  final int completedJobCount;
  // Null when the tradesperson has no reviews in the period.
  final double? averageRating;

  TradespersonReportRow({
    required this.tradespersonName,
    required this.cityName,
    required this.offerCount,
    required this.completedJobCount,
    this.averageRating,
  });

  factory TradespersonReportRow.fromJson(Map<String, dynamic> json) =>
      _$TradespersonReportRowFromJson(json);
}
