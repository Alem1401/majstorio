import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_desktop/models/auth/auth_session.dart';

// The API address. Override it when starting the app:
//   flutter run -d windows --dart-define=API_BASE_URL=http://localhost:5000
const String baseUrl = String.fromEnvironment(
  "API_BASE_URL",
  defaultValue: "http://localhost:5000",
);


final navigatorKey = GlobalKey<NavigatorState>();

const String loginRoute = "/login";

Map<String, String> authHeaders() {
  return {
    "content-type": "application/json",
    if (AuthSession.token != null) "Authorization": "Bearer ${AuthSession.token}",
  };
}


void checkUnauthorized(http.Response response) {
  if (response.statusCode != 401) return;

  AuthSession.clear();
  navigatorKey.currentState?.pushNamedAndRemoveUntil(loginRoute, (route) => false);
  throw Exception("The session has expired. Sign in again.");
}
