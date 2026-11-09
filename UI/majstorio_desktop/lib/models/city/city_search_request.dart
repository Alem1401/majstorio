import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the city list. Mirrors CitySearchRequest on the backend.
class CitySearchRequest extends BaseSearchRequest {
  final String? name;
  final bool? isActive;

  CitySearchRequest({super.page, super.pageSize, this.name, this.isActive});

  // Adds the city filters to the page values; a filter that is not set is left out.
  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.trim().isNotEmpty) 'name': name!.trim(),
    if (isActive != null) 'isActive': isActive.toString(),
  };
}
