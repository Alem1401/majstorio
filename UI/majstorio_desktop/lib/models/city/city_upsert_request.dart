// What the app sends to create (POST /Cities) or update (PUT /Cities/{id}) a city.
// No id: on update it goes in the URL. Mirrors CityCreateRequest / CityUpdateRequest on the backend.
class CityUpsertRequest {
  final String name;
  final bool isActive;

  CityUpsertRequest({required this.name, this.isActive = true});

  Map<String, dynamic> toJson() => {
    'name': name,
    'isActive': isActive,
  };
}
