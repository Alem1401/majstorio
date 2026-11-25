// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'offer.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Offer _$OfferFromJson(Map<String, dynamic> json) => Offer(
  id: (json['id'] as num).toInt(),
  customerName: json['customerName'] as String,
  tradespersonName: json['tradespersonName'] as String,
  serviceCategoryName: json['serviceCategoryName'] as String,
  price: (json['price'] as num).toDouble(),
  proposedStartUtc: DateTime.parse(json['proposedStartUtc'] as String),
  durationMinutes: (json['durationMinutes'] as num).toInt(),
  status: (json['status'] as num).toInt(),
);
