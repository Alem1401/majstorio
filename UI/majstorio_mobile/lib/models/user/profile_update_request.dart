// What PUT /Account accepts: only personal details. The user name and the role cannot be changed here.
class ProfileUpdateRequest {
  final String email;
  final String? phoneNumber;
  final String firstName;
  final String lastName;

  ProfileUpdateRequest({required this.email, this.phoneNumber, required this.firstName, required this.lastName});

  Map<String, dynamic> toJson() => {
    'email': email,
    'phoneNumber': phoneNumber == null || phoneNumber!.isEmpty ? null : phoneNumber,
    'firstName': firstName,
    'lastName': lastName,
  };
}
