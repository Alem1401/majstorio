// A kind of work, for example "Vodoinstalaterski radovi". Mirrors ServiceCategoryResponse on the backend.
class Category {
  final int id;
  final String name;
  final bool isActive;

  Category({required this.id, required this.name, this.isActive = true});

  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(id: json['id'], name: json['name'], isActive: json['isActive'] ?? true);
  }
}
