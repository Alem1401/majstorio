import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/constants.dart';
import 'package:majstorio_desktop/models/auth/auth_session.dart';
import 'package:majstorio_desktop/models/auth/login_response.dart';

class AuthProvider extends ChangeNotifier {
  // Returns null when the sign-in worked, otherwise the message to show.
  Future<String?> login(String userName, String password) async {
    http.Response response;
    try {
      response = await http.post(
        Uri.parse("$baseUrl/Auth/login"),
        headers: {"content-type": "application/json"},
        body: json.encode({"userName": userName, "password": password}),
      );
    } catch (e) {
      return "The server could not be reached. Check that the API is running.";
    }

    if (response.statusCode != 200) {
      // 401 = wrong user name or password, 403 = deactivated account.
      try {
        return json.decode(response.body)["message"] ?? "Sign in failed.";
      } catch (e) {
        return "Sign in failed.";
      }
    }

    var login = LoginResponse.fromJson(json.decode(response.body));
    if (login.role != "Administrator") {
      return "Only administrators can use the desktop application.";
    }

    AuthSession.token = login.accessToken;
    AuthSession.role = login.role;
    return null;
  }

  // Tells the API to invalidate the token, then forgets it. The local sign-out happens even when the call fails.
  Future<void> logout() async {
    try {
      await http.post(Uri.parse("$baseUrl/Auth/logout"), headers: authHeaders());
    } catch (e) {
      // Nothing to do: the token is dropped below either way.
    }
    AuthSession.clear();
  }
}
