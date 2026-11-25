import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for the tradesperson search. Mirrors TradespersonProfileSearchRequest on the backend.
// The API already limits non-admin users to approved, active profiles (plus their own).
class TradespersonProfileSearchRequest extends BaseSearchRequest {
  final String? name;
  final int? cityId;
  final int? serviceCategoryId;
  // 1 to 5; null means any rating.
  final int? minimumRating;
  // Both or neither; hides tradespeople who already have a job in this interval.
  final DateTime? availableFromUtc;
  final DateTime? availableToUtc;

  TradespersonProfileSearchRequest({
    super.page,
    super.pageSize,
    this.name,
    this.cityId,
    this.serviceCategoryId,
    this.minimumRating,
    this.availableFromUtc,
    this.availableToUtc,
  });

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.isNotEmpty) 'name': name!,
    if (cityId != null) 'cityId': cityId.toString(),
    if (serviceCategoryId != null) 'serviceCategoryId': serviceCategoryId.toString(),
    if (minimumRating != null) 'minimumRating': minimumRating.toString(),
    if (availableFromUtc != null) 'availableFromUtc': availableFromUtc!.toUtc().toIso8601String(),
    if (availableToUtc != null) 'availableToUtc': availableToUtc!.toUtc().toIso8601String(),
  };
}
