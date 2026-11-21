// What POST /Auth/register expects. The API always creates a Customer; the role is never sent.
class RegisterRequest {
  final String userName;
  final String email;
  final String password;
  final String firstName;
  final String lastName;
  final String? phoneNumber;

  RegisterRequest({
    required this.userName,
    required this.email,
    required this.password,
    required this.firstName,
    required this.lastName,
    this.phoneNumber,
  });

  Map<String, dynamic> toJson() => {
    'userName': userName,
    'email': email,
    'password': password,
    'firstName': firstName,
    'lastName': lastName,
    if (phoneNumber != null && phoneNumber!.isNotEmpty) 'phoneNumber': phoneNumber,
  };
}
