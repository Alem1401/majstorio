// A city a request or a tradesperson belongs to. Mirrors CityResponse on the backend.
class City {
  final int id;
  final String name;
  final bool isActive;

  City({required this.id, required this.name, this.isActive = true});

  factory City.fromJson(Map<String, dynamic> json) {
    return City(id: json['id'], name: json['name'], isActive: json['isActive'] ?? true);
  }
}
