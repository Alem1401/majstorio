import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for the city list. The mobile app only offers active cities.
class CitySearchRequest extends BaseSearchRequest {
  final String? name;
  final bool? isActive;

  CitySearchRequest({super.page, super.pageSize = 100, this.name, this.isActive = true});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.isNotEmpty) 'name': name!,
    if (isActive != null) 'isActive': isActive.toString(),
  };
}
