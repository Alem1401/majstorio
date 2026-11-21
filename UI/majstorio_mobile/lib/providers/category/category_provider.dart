import 'package:majstorio_mobile/models/category/category.dart';
import 'package:majstorio_mobile/models/category/category_search_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// Service categories for the home screen and the pickers. Read only on mobile.
class CategoryProvider extends BaseProvider<Category> {
  CategoryProvider() : super("ServiceCategories", Category.fromJson);

  // All active categories, sorted by name, read page by page (the server sends at most 100 per page).
  Future<List<Category>> getActive() async {
    var categories = <Category>[];
    var page = 1;
    while (true) {
      var result = await get(CategorySearchRequest(page: page, pageSize: 100));
      categories.addAll(result.items);
      if (result.items.isEmpty || categories.length >= result.totalCount) break;
      page++;
    }
    categories.sort((a, b) => a.name.compareTo(b.name));
    return categories;
  }
}
