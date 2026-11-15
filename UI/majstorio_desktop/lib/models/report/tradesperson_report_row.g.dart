// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'tradesperson_report_row.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TradespersonReportRow _$TradespersonReportRowFromJson(
  Map<String, dynamic> json,
) => TradespersonReportRow(
  tradespersonName: json['tradespersonName'] as String,
  cityName: json['cityName'] as String,
  offerCount: (json['offerCount'] as num).toInt(),
  completedJobCount: (json['completedJobCount'] as num).toInt(),
  averageRating: (json['averageRating'] as num?)?.toDouble(),
);
