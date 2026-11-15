// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'request_report_row.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

RequestReportRow _$RequestReportRowFromJson(Map<String, dynamic> json) =>
    RequestReportRow(
      cityName: json['cityName'] as String,
      serviceCategoryName: json['serviceCategoryName'] as String,
      status: (json['status'] as num).toInt(),
      requestCount: (json['requestCount'] as num).toInt(),
    );
