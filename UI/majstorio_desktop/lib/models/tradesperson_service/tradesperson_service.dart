// A service a tradesperson offers. Mirrors TradespersonServiceResponse on the backend.
class TradespersonService {
  final int id;
  final int serviceCategoryId;
  final String serviceCategoryName;
  final String name;
  final String description;
  final double minimumPrice;
  final double maximumPrice;
  final int estimatedDurationMinutes;
  final bool isActive;

  TradespersonService({
    required this.id,
    required this.serviceCategoryId,
    required this.serviceCategoryName,
    required this.name,
    required this.description,
    required this.minimumPrice,
    required this.maximumPrice,
    required this.estimatedDurationMinutes,
    required this.isActive,
  });

  factory TradespersonService.fromJson(Map<String, dynamic> json) {
    return TradespersonService(
      id: json['id'],
      serviceCategoryId: json['serviceCategoryId'],
      serviceCategoryName: json['serviceCategoryName'],
      name: json['name'],
      description: json['description'],
      minimumPrice: (json['minimumPrice'] as num).toDouble(),
      maximumPrice: (json['maximumPrice'] as num).toDouble(),
      estimatedDurationMinutes: json['estimatedDurationMinutes'],
      isActive: json['isActive'],
    );
  }
}
