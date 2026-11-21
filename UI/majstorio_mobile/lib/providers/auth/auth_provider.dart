import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/auth/login_response.dart';
import 'package:majstorio_mobile/models/auth/register_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';

// Sign in, register, sign out and the password calls (everything under /Auth).
class AuthProvider extends BaseProvider<void> {
  AuthProvider() : super("Auth", (json) {});

  // Returns null when the sign-in worked, otherwise the message to show.
  // An administrator is not let in: the mobile app is for customers and tradespeople.
  Future<String?> login(String userName, String password) async {
    http.Response response;
    try {
      response = await send(
        BaseProvider.client.post(
          url("Auth/login"),
          headers: {"content-type": "application/json"},
          body: json.encode({"userName": userName, "password": password}),
        ),
      );
    } on ApiException catch (e) {
      return e.message;
    }

    if (response.statusCode != 200) {
      // 401 = wrong user name or password, 403 = deactivated account.
      if (response.statusCode == 401) return "Pogrešno korisničko ime ili lozinka.";
      return errorMessage(response);
    }

    var login = LoginResponse.fromJson(json.decode(response.body));
    if (login.role == AuthSession.administrator) {
      return "Administrator koristi desktop aplikaciju Majstorio.";
    }
    if (login.role != AuthSession.customer && login.role != AuthSession.tradesperson) {
      return "Ovaj korisnički račun ne može koristiti mobilnu aplikaciju.";
    }

    var claims = _readToken(login.accessToken);
    AuthSession.token = login.accessToken;
    AuthSession.role = login.role;
    AuthSession.expiresAtUtc = login.expiresAtUtc;
    AuthSession.userId = int.tryParse("${claims["sub"] ?? ""}");
    AuthSession.userName = "${claims["unique_name"] ?? claims["name"] ?? userName}";
    return null;
  }

  // The API creates a Customer account. Throws an ApiException with the reason when it fails.
  Future<void> register(RegisterRequest request) async {
    await command("POST", "Auth/register", request.toJson());
  }

  // Tells the API to invalidate the token, then forgets it.
  // Returns false when the server could not be reached: the user is signed out on the phone anyway,
  // but the token was not confirmed invalid on the server.
  Future<bool> logout() async {
    var confirmed = true;
    try {
      await command("POST", "Auth/logout");
    } on ApiException catch (e) {
      // 401: the token was already invalid on the server, which is what signing out wants.
      confirmed = e.statusCode == 401;
    }
    AuthSession.clear();
    return confirmed;
  }

  // The API invalidates every token of the user after a password change, so the user signs in again.
  Future<void> changePassword(String oldPassword, String newPassword) async {
    await command("POST", "Auth/change-password", {
      "oldPassword": oldPassword,
      "newPassword": newPassword,
    });
    AuthSession.clear();
  }

  // Asks for a reset token. The answer is the same whether the e-mail exists or not.
  // No e-mail is sent yet: in Development the API returns the token itself, so it can be entered by hand.
  Future<String?> forgotPassword(String email) async {
    var response = await send(
      BaseProvider.client.post(
        url("Auth/forgot-password"),
        headers: {"content-type": "application/json"},
        body: json.encode({"email": email}),
      ),
      isCommand: true,
    );
    if (response.statusCode != 200) {
      throw ApiException(response.statusCode, errorMessage(response), fieldErrors: fieldErrors(response));
    }
    if (response.body.isEmpty) return null;
    return json.decode(response.body)["resetToken"];
  }

  // The token is valid once, for 15 minutes.
  Future<void> resetPassword(String email, String token, String newPassword) async {
    await command("POST", "Auth/reset-password", {
      "email": email,
      "token": token,
      "newPassword": newPassword,
    });
  }

  // The middle part of the token holds the user's id ("sub") and name. Only read to decide what to show.
  Map<String, dynamic> _readToken(String token) {
    try {
      var parts = token.split(".");
      var payload = utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
      return json.decode(payload);
    } catch (e) {
      return {};
    }
  }
}
