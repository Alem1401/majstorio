// The signed-in user's own account (GET /Account). Mirrors UserResponse on the backend.
class UserProfile {
  final int id;
  final String userName;
  final String email;
  final String? phoneNumber;
  final String firstName;
  final String lastName;
  // Relative to the API ("Account/photos/...") for an uploaded photo; null when there is none.
  final String? profileImageUrl;
  final String role;

  UserProfile({
    required this.id,
    required this.userName,
    required this.email,
    this.phoneNumber,
    required this.firstName,
    required this.lastName,
    this.profileImageUrl,
    required this.role,
  });

  String get fullName => "$firstName $lastName".trim();

  factory UserProfile.fromJson(Map<String, dynamic> json) {
    return UserProfile(
      id: json['id'],
      userName: json['userName'] ?? '',
      email: json['email'] ?? '',
      phoneNumber: json['phoneNumber'],
      firstName: json['firstName'] ?? '',
      lastName: json['lastName'] ?? '',
      profileImageUrl: json['profileImageUrl'],
      role: json['role'] ?? '',
    );
  }
}
