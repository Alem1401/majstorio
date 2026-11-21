import 'package:majstorio_mobile/utils/formatters.dart';

// What POST /Auth/login returns.
class LoginResponse {
  final String accessToken;
  final DateTime expiresAtUtc;
  final String role;

  LoginResponse({required this.accessToken, required this.expiresAtUtc, required this.role});

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      accessToken: json['accessToken'],
      expiresAtUtc: parseUtc(json['expiresAtUtc']),
      role: json['role'],
    );
  }
}
