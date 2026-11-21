import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for the category list. The mobile app only offers active categories.
class CategorySearchRequest extends BaseSearchRequest {
  final String? name;
  final bool? isActive;

  CategorySearchRequest({super.page, super.pageSize = 100, this.name, this.isActive = true});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (name != null && name!.isNotEmpty) 'name': name!,
    if (isActive != null) 'isActive': isActive.toString(),
  };
}
