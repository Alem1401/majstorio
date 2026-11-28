// What the app sends to update a tradesperson profile (PUT /TradespersonProfiles/{id}).
// No id (it goes in the URL). Mirrors TradespersonProfileUpdateRequest on the backend.
class TradespersonProfileUpsertRequest {
  final int cityId;
  final String description;

  TradespersonProfileUpsertRequest({required this.cityId, required this.description});

  Map<String, dynamic> toJson() => {
    'cityId': cityId,
    'description': description,
  };
}
