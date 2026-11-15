// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'appointment.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Appointment _$AppointmentFromJson(Map<String, dynamic> json) => Appointment(
  id: (json['id'] as num?)?.toInt(),
  offerId: (json['offerId'] as num).toInt(),
  customerName: json['customerName'] as String,
  tradespersonName: json['tradespersonName'] as String,
  serviceCategoryName: json['serviceCategoryName'] as String,
  startUtc: DateTime.parse(json['startUtc'] as String),
  endUtc: DateTime.parse(json['endUtc'] as String),
  status: (json['status'] as num).toInt(),
);

Map<String, dynamic> _$AppointmentToJson(Appointment instance) =>
    <String, dynamic>{
      'id': instance.id,
      'offerId': instance.offerId,
      'customerName': instance.customerName,
      'tradespersonName': instance.tradespersonName,
      'serviceCategoryName': instance.serviceCategoryName,
      'startUtc': instance.startUtc.toIso8601String(),
      'endUtc': instance.endUtc.toIso8601String(),
      'status': instance.status,
    };
