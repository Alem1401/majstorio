import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/utils/error_message.dart';

// The API address. Override it when starting the app:
//   flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5100
// 10.0.2.2 is how the Android emulator reaches "localhost" of the computer it runs on.
const String baseUrl = String.fromEnvironment(
  "API_BASE_URL",
  defaultValue: "http://10.0.2.2:5100",
);

// How long a call may take before the app gives up and shows an error.
const Duration requestTimeout = Duration(seconds: 20);

final navigatorKey = GlobalKey<NavigatorState>();

const String loginRoute = "/login";

Map<String, String> authHeaders() {
  return {
    "content-type": "application/json",
    if (AuthSession.token != null) "Authorization": "Bearer ${AuthSession.token}",
  };
}

// A 401 means the token is no longer valid (expired, or signed out on the server).
// The session is cleared and the app returns to the login screen once.
void checkUnauthorized(http.Response response) {
  if (response.statusCode != 401) return;

  // Only the session that made the call is cleared. If another user has signed in meanwhile, keep theirs.
  var sentToken = response.request?.headers["Authorization"];
  if (sentToken != null && sentToken != "Bearer ${AuthSession.token}") {
    throw ApiException(401, "Sesija je istekla. Prijavite se ponovo.");
  }

  var wasSignedIn = AuthSession.isLoggedIn;
  AuthSession.clear();
  if (wasSignedIn) {
    navigatorKey.currentState?.pushNamedAndRemoveUntil(loginRoute, (route) => false);
  }
  throw ApiException(401, "Sesija je istekla. Prijavite se ponovo.");
}

// Every answer goes through here before its body is read.
// 401 returns to login; any other status that is not a success becomes an Exception with the message to show.
void checkResponse(http.Response response) {
  checkUnauthorized(response);
  if (response.statusCode < 200 || response.statusCode > 299) {
    throw ApiException(
      response.statusCode,
      errorMessage(response),
      fieldErrors: fieldErrors(response),
    );
  }
}
