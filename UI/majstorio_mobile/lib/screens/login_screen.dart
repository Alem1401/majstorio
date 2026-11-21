import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/main_navigation.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/screens/password/forgot_password_screen.dart';
import 'package:majstorio_mobile/screens/register_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:provider/provider.dart';

// The first screen of the app. It has no menu, because nothing else is available before signing in.
class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _userName = TextEditingController();
  final _password = TextEditingController();
  final key = GlobalKey<FormState>();
  late AuthProvider _authProvider;

  // True while the sign-in request is running, so the button cannot be tapped twice.
  var isLoading = false;
  var hidePassword = true;
  String? error;

  @override
  void initState() {
    _authProvider = context.read<AuthProvider>();
    super.initState();
  }

  @override
  void dispose() {
    _userName.dispose();
    _password.dispose();
    super.dispose();
  }

  void signIn() async {
    var isValid = key.currentState?.validate();
    if (isValid != true) return;

    setState(() {
      isLoading = true;
      error = null;
    });

    var message = await _authProvider.login(_userName.text.trim(), _password.text);
    if (!mounted) return;

    if (message != null) {
      // A failed sign-in stays on the form, with the reason under it.
      setState(() {
        isLoading = false;
        error = message;
      });
      return;
    }

    // Replaces the login screen, so "back" cannot return to it.
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (context) => const MainNavigation()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Form(
                  key: key,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: AppTheme.gold,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(Icons.handyman, color: AppTheme.inkDeep),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  "Majstorio",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  "Prijavite se na svoj račun.",
                                  style: TextStyle(color: AppTheme.textMuted),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 24),
                      const Divider(),
                      const SizedBox(height: 24),
                      TextFormField(
                        controller: _userName,
                        textInputAction: TextInputAction.next,
                        autofillHints: const [AutofillHints.username],
                        decoration: const InputDecoration(
                          labelText: "Korisničko ime",
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "Korisničko ime je obavezno";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _password,
                        obscureText: hidePassword,
                        autofillHints: const [AutofillHints.password],
                        decoration: InputDecoration(
                          labelText: "Lozinka",
                          prefixIcon: const Icon(Icons.lock_outline),
                          suffixIcon: IconButton(
                            tooltip: hidePassword ? "Prikaži lozinku" : "Sakrij lozinku",
                            icon: Icon(hidePassword ? Icons.visibility_outlined : Icons.visibility_off_outlined),
                            onPressed: () {
                              setState(() {
                                hidePassword = !hidePassword;
                              });
                            },
                          ),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return "Lozinka je obavezna";
                          }
                          return null;
                        },
                        // "Done" on the keyboard signs in.
                        onFieldSubmitted: (value) {
                          if (!isLoading) signIn();
                        },
                      ),
                      if (error != null) ...[
                        const SizedBox(height: 16),
                        Text(error!, style: const TextStyle(color: AppTheme.danger)),
                      ],
                      const SizedBox(height: 24),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: isLoading ? null : signIn,
                          icon: const Icon(Icons.login),
                          label: Text(isLoading ? "Prijava..." : "Prijavi se"),
                        ),
                      ),
                      const SizedBox(height: 8),
                      Center(
                        child: TextButton(
                          onPressed: () async {
                            // The new account's user name comes back, so it is filled in.
                            var userName = await Navigator.push<String>(
                              context,
                              MaterialPageRoute(builder: (context) => const RegisterScreen()),
                            );
                            if (userName != null) {
                              _userName.text = userName;
                              _password.clear();
                            }
                          },
                          child: const Text("Nemate račun? Registrujte se"),
                        ),
                      ),
                      Center(
                        child: TextButton(
                          onPressed: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute(builder: (context) => const ForgotPasswordScreen()),
                            );
                          },
                          child: const Text("Zaboravljena lozinka?"),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
