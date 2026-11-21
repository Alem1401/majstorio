// One tradesperson in the search results. Mirrors TradespersonProfileResponse on the backend.
class TradespersonProfile {
  final int id;
  // The user who owns the profile; compared with AuthSession.userId to recognise the user's own profile.
  final int userId;
  final String fullName;
  final String? profileImageUrl;
  final int cityId;
  final String cityName;
  final String description;
  final bool isApproved;
  final bool isActive;
  // Null while the tradesperson has no reviews.
  final double? averageRating;
  final int reviewCount;
  final int completedJobs;
  // Price range in EUR over the active services; null when there are none.
  final double? minimumPrice;
  final double? maximumPrice;
  final List<String> categories;

  TradespersonProfile({
    required this.id,
    required this.userId,
    required this.fullName,
    this.profileImageUrl,
    required this.cityId,
    required this.cityName,
    required this.description,
    required this.isApproved,
    required this.isActive,
    this.averageRating,
    required this.reviewCount,
    required this.completedJobs,
    this.minimumPrice,
    this.maximumPrice,
    required this.categories,
  });

  factory TradespersonProfile.fromJson(Map<String, dynamic> json) {
    return TradespersonProfile(
      id: json['id'],
      userId: json['userId'] ?? 0,
      fullName: json['fullName'] ?? '',
      profileImageUrl: json['profileImageUrl'],
      cityId: json['cityId'] ?? 0,
      cityName: json['cityName'] ?? '',
      description: json['description'] ?? '',
      isApproved: json['isApproved'] ?? false,
      isActive: json['isActive'] ?? false,
      averageRating: (json['averageRating'] as num?)?.toDouble(),
      reviewCount: json['reviewCount'] ?? 0,
      completedJobs: json['completedJobs'] ?? 0,
      minimumPrice: (json['minimumPrice'] as num?)?.toDouble(),
      maximumPrice: (json['maximumPrice'] as num?)?.toDouble(),
      categories: ((json['categories'] ?? []) as List).cast<String>(),
    );
  }
}

// One service a tradesperson offers, with its price range. Mirrors TradespersonServiceResponse.
class TradespersonService {
  final int id;
  final int serviceCategoryId;
  final String serviceCategoryName;
  final String name;
  final String description;
  final double minimumPrice;
  final double maximumPrice;
  final int estimatedDurationMinutes;

  TradespersonService({
    required this.id,
    required this.serviceCategoryId,
    required this.serviceCategoryName,
    required this.name,
    required this.description,
    required this.minimumPrice,
    required this.maximumPrice,
    required this.estimatedDurationMinutes,
  });

  factory TradespersonService.fromJson(Map<String, dynamic> json) {
    return TradespersonService(
      id: json['id'],
      serviceCategoryId: json['serviceCategoryId'],
      serviceCategoryName: json['serviceCategoryName'] ?? '',
      name: json['name'] ?? '',
      description: json['description'] ?? '',
      minimumPrice: (json['minimumPrice'] as num).toDouble(),
      maximumPrice: (json['maximumPrice'] as num).toDouble(),
      estimatedDurationMinutes: json['estimatedDurationMinutes'] ?? 0,
    );
  }
}

// A review shown on the profile. Mirrors ReviewResponse (only what the mobile app shows).
class ProfileReview {
  final int id;
  final String customerName;
  final int rating;
  final String? comment;

  ProfileReview({required this.id, required this.customerName, required this.rating, this.comment});

  factory ProfileReview.fromJson(Map<String, dynamic> json) {
    return ProfileReview(
      id: json['id'],
      customerName: json['customerName'] ?? '',
      rating: json['rating'] ?? 0,
      comment: json['comment'],
    );
  }
}

// Everything the profile screen shows. Mirrors TradespersonProfileDetailResponse on the backend.
class TradespersonProfileDetail {
  final TradespersonProfile profile;
  final List<TradespersonService> services;
  final List<ProfileReview> latestReviews;

  TradespersonProfileDetail({required this.profile, required this.services, required this.latestReviews});

  // The categories the customer can choose from when sending this tradesperson a request.
  Map<int, String> get serviceCategories => {
    for (var service in services) service.serviceCategoryId: service.serviceCategoryName,
  };

  factory TradespersonProfileDetail.fromJson(Map<String, dynamic> json) {
    return TradespersonProfileDetail(
      profile: TradespersonProfile.fromJson(json),
      services: ((json['services'] ?? []) as List)
          .map((service) => TradespersonService.fromJson(service))
          .toList(),
      latestReviews: ((json['latestReviews'] ?? []) as List)
          .map((review) => ProfileReview.fromJson(review))
          .toList(),
    );
  }
}
