import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for the tradesperson search. Mirrors TradespersonProfileSearchRequest on the backend.
// The API already limits non-admin users to approved, active profiles (plus their own).
class TradespersonProfileSearchRequest extends BaseSearchRequest {
  final String? name;
  final int? cityId;
  final int? serviceCategoryId;
  // 1 to 5; null means any rating.
  final int? minimumRating;

  TradespersonProfileSearchRequest({
    super.page,
    super.pageSize,
    this.name,
    this.cityId,
    this.serviceCategoryId,
    this.minimumRating,
  });

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.isNotEmpty) 'name': name!,
    if (cityId != null) 'cityId': cityId.toString(),
    if (serviceCategoryId != null) 'serviceCategoryId': serviceCategoryId.toString(),
    if (minimumRating != null) 'minimumRating': minimumRating.toString(),
  };
}
