// The signed-in user. Kept only in memory, so closing the app signs out.
// The API decides what the user may do from the token; the app uses role and userId only to choose what to show.
class AuthSession {
  static String? token;
  static String? role;
  // The user's id from the token ("sub"), used to recognise the user's own records.
  static int? userId;
  static String? userName;
  static DateTime? expiresAtUtc;

  static const String customer = "Customer";
  static const String tradesperson = "Tradesperson";
  static const String administrator = "Administrator";

  static bool get isLoggedIn => token != null;
  static bool get isCustomer => role == customer;
  static bool get isTradesperson => role == tradesperson;

  static void clear() {
    token = null;
    role = null;
    userId = null;
    userName = null;
    expiresAtUtc = null;
  }
}
