// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'dashboard.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Dashboard _$DashboardFromJson(Map<String, dynamic> json) => Dashboard(
  activeUserCount: (json['activeUserCount'] as num).toInt(),
  pendingTradespersonCount: (json['pendingTradespersonCount'] as num).toInt(),
  activeRequestCount: (json['activeRequestCount'] as num).toInt(),
  completedServiceCount: (json['completedServiceCount'] as num).toInt(),
  requestsByCategory: (json['requestsByCategory'] as List<dynamic>)
      .map((e) => NameCount.fromJson(e as Map<String, dynamic>))
      .toList(),
  requestsByCity: (json['requestsByCity'] as List<dynamic>)
      .map((e) => NameCount.fromJson(e as Map<String, dynamic>))
      .toList(),
);

NameCount _$NameCountFromJson(Map<String, dynamic> json) => NameCount(
  name: json['name'] as String,
  count: (json['count'] as num).toInt(),
);
