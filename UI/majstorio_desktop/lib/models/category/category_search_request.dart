import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the category list. Mirrors ServiceCategorySearchRequest on the backend.
class CategorySearchRequest extends BaseSearchRequest {
  final String? name;
  final bool? isActive;

  CategorySearchRequest({super.page, super.pageSize, this.name, this.isActive});

  // Adds the category filters to the page values; a filter that is not set is left out.
  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.trim().isNotEmpty) 'name': name!.trim(),
    if (isActive != null) 'isActive': isActive.toString(),
  };
}
