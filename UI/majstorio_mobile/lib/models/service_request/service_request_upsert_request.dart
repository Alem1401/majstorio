import 'package:majstorio_mobile/utils/formatters.dart';

// What the customer sends when creating a request (POST /ServiceRequests).
// The customer is taken from the token, never sent. A request is never edited; it can only be cancelled.
class ServiceRequestUpsertRequest {
  final int serviceCategoryId;
  final int cityId;
  final String description;
  final String? address;
  final DateTime desiredStart;
  final double maximumBudget;
  final int? targetTradespersonProfileId;

  ServiceRequestUpsertRequest({
    required this.serviceCategoryId,
    required this.cityId,
    required this.description,
    this.address,
    required this.desiredStart,
    required this.maximumBudget,
    this.targetTradespersonProfileId,
  });

  Map<String, dynamic> toJson() => {
    'serviceCategoryId': serviceCategoryId,
    'cityId': cityId,
    'description': description,
    if (address != null && address!.isNotEmpty) 'address': address,
    'desiredStartUtc': toApiDate(desiredStart),
    'maximumBudget': maximumBudget,
    if (targetTradespersonProfileId != null) 'targetTradespersonProfileId': targetTradespersonProfileId,
  };
}
