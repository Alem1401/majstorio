import 'package:json_annotation/json_annotation.dart';

part 'dashboard.g.dart';

// The numbers and the two charts of the home screen. Mirrors DashboardResponse on the backend.
@JsonSerializable(createToJson: false)
class Dashboard {
  final int activeUserCount;
  final int pendingTradespersonCount;
  final int activeRequestCount;
  final int completedServiceCount;
  final List<NameCount> requestsByCategory;
  final List<NameCount> requestsByCity;

  Dashboard({
    required this.activeUserCount,
    required this.pendingTradespersonCount,
    required this.activeRequestCount,
    required this.completedServiceCount,
    required this.requestsByCategory,
    required this.requestsByCity,
  });

  factory Dashboard.fromJson(Map<String, dynamic> json) =>
      _$DashboardFromJson(json);
}

// One bar of a chart: a name and how many service requests it has.
@JsonSerializable(createToJson: false)
class NameCount {
  final String name;
  final int count;

  NameCount({required this.name, required this.count});

  factory NameCount.fromJson(Map<String, dynamic> json) =>
      _$NameCountFromJson(json);
}
