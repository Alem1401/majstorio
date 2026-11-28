// One row of the tradesperson list. Mirrors TradespersonProfileResponse on the backend.
class TradespersonProfile {
  final int id;
  final String fullName;
  final int cityId;
  final String cityName;
  final String description;
  final bool isApproved;
  final bool isActive;
  // Null while the tradesperson has no reviews.
  final double? averageRating;
  final int reviewCount;
  final int completedJobs;
  final List<String> categories;

  TradespersonProfile({
    required this.id,
    required this.fullName,
    required this.cityId,
    required this.cityName,
    required this.description,
    required this.isApproved,
    required this.isActive,
    this.averageRating,
    required this.reviewCount,
    required this.completedJobs,
    required this.categories,
  });

  factory TradespersonProfile.fromJson(Map<String, dynamic> json) {
    return TradespersonProfile(
      id: json['id'],
      fullName: json['fullName'],
      cityId: json['cityId'],
      cityName: json['cityName'],
      description: json['description'] ?? '',
      isApproved: json['isApproved'],
      isActive: json['isActive'],
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      reviewCount: json['reviewCount'],
      completedJobs: json['completedJobs'],
      categories: (json['categories'] as List).map((name) => name.toString()).toList(),
    );
  }
}
