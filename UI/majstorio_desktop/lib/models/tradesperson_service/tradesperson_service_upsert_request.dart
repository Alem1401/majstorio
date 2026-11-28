// What the app sends to create (POST /TradespersonServices) or update (PUT /TradespersonServices/{id}).
// No id (it goes in the URL). The profile is sent only on create and isActive is used only on update.
class TradespersonServiceUpsertRequest {
  final int? tradespersonProfileId;
  final int serviceCategoryId;
  final String name;
  final String description;
  final double minimumPrice;
  final double maximumPrice;
  final int estimatedDurationMinutes;
  final bool? isActive;

  TradespersonServiceUpsertRequest({
    this.tradespersonProfileId,
    required this.serviceCategoryId,
    required this.name,
    required this.description,
    required this.minimumPrice,
    required this.maximumPrice,
    required this.estimatedDurationMinutes,
    this.isActive,
  });

  Map<String, dynamic> toJson() => {
    if (tradespersonProfileId != null) 'tradespersonProfileId': tradespersonProfileId,
    'serviceCategoryId': serviceCategoryId,
    'name': name,
    'description': description,
    'minimumPrice': minimumPrice,
    'maximumPrice': maximumPrice,
    'estimatedDurationMinutes': estimatedDurationMinutes,
    if (isActive != null) 'isActive': isActive,
  };
}
