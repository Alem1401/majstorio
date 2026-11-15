// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'review.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Review _$ReviewFromJson(Map<String, dynamic> json) => Review(
  id: (json['id'] as num?)?.toInt(),
  appointmentId: (json['appointmentId'] as num).toInt(),
  customerName: json['customerName'] as String,
  tradespersonName: json['tradespersonName'] as String,
  rating: (json['rating'] as num).toInt(),
  comment: json['comment'] as String?,
  createdAtUtc: DateTime.parse(json['createdAtUtc'] as String),
);

Map<String, dynamic> _$ReviewToJson(Review instance) => <String, dynamic>{
  'id': instance.id,
  'appointmentId': instance.appointmentId,
  'customerName': instance.customerName,
  'tradespersonName': instance.tradespersonName,
  'rating': instance.rating,
  'comment': instance.comment,
  'createdAtUtc': instance.createdAtUtc.toIso8601String(),
};
