// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'review.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Review _$ReviewFromJson(Map<String, dynamic> json) => Review(
  id: (json['id'] as num).toInt(),
  appointmentId: (json['appointmentId'] as num).toInt(),
  customerName: json['customerName'] as String,
  tradespersonName: json['tradespersonName'] as String,
  rating: (json['rating'] as num).toInt(),
  comment: json['comment'] as String?,
  createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
);
