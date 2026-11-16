// The signed-in administrator. Kept only in memory, so closing the app signs out.
class AuthSession {
  static String? token;
  static String? role;

  static bool get isLoggedIn => token != null;

  static void clear() {
    token = null;
    role = null;
  }
}
