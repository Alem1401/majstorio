// What the app sends to create (POST /ServiceCategories) or update (PUT /ServiceCategories/{id}) a category.
// No id: on update it goes in the URL. Mirrors ServiceCategoryCreateRequest / ServiceCategoryUpdateRequest.
class CategoryUpsertRequest {
  final String name;
  final bool isActive;

  CategoryUpsertRequest({required this.name, this.isActive = true});

  Map<String, dynamic> toJson() => {
    'name': name,
    'isActive': isActive,
  };
}
