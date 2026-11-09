class Category {
  int? id;
  final String name;
  final bool isActive;

  Category({this.id, required this.name, this.isActive = true});

  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(id: json['id'], name: json['name'], isActive: json['isActive']);
  }
  static Map<String, dynamic> toJson(Category category) {
    return {'name': category.name, 'isActive': category.isActive};
  }
}
