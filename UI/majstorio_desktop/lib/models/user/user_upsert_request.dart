// What the app sends to update a user (PUT /Users/{id}). No id: it goes in the URL.
// Accounts are created by registering, so the desktop app only updates. Mirrors UserUpdateRequest.
class UserUpsertRequest {
  final String userName;
  final String email;
  final String? phoneNumber;
  final String firstName;
  final String lastName;
  final String? profileImageUrl;
  final bool isActive;
  // Customer, Tradesperson or Administrator.
  final String? role;

  UserUpsertRequest({
    required this.userName,
    required this.email,
    this.phoneNumber,
    required this.firstName,
    required this.lastName,
    this.profileImageUrl,
    this.isActive = true,
    this.role,
  });

  Map<String, dynamic> toJson() => {
    'userName': userName,
    'email': email,
    'phoneNumber': phoneNumber,
    'firstName': firstName,
    'lastName': lastName,
    'profileImageUrl': profileImageUrl,
    'isActive': isActive,
    'role': role,
  };
}
