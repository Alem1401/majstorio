import 'package:flutter/material.dart';
import 'package:majstorio_desktop/providers/auth/auth_provider.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
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

  // True while the sign-in request is running, so the button cannot be clicked twice.
  var isLoading = false;
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
      setState(() {
        isLoading = false;
        error = message;
      });
      return;
    }

    // Replaces the login screen, so "back" cannot return to it.
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (context) => const MyWidget()),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(32),
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
                            child: const Icon(
                              Icons.handyman,
                              color: AppTheme.inkDeep,
                            ),
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
                                  "Sign in to the administration.",
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
                        autofocus: true,
                        decoration: const InputDecoration(
                          labelText: "User name",
                          prefixIcon: Icon(Icons.person_outline),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "The user name is required";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _password,
                        obscureText: true,
                        decoration: const InputDecoration(
                          labelText: "Password",
                          prefixIcon: Icon(Icons.lock_outline),
                        ),
                        validator: (value) {
                          if (value == null || value.isEmpty) {
                            return "The password is required";
                          }
                          return null;
                        },
                        // Enter in the password field signs in.
                        onFieldSubmitted: (value) {
                          if (!isLoading) signIn();
                        },
                      ),
                      if (error != null) ...[
                        const SizedBox(height: 16),
                        Text(
                          error!,
                          style: const TextStyle(color: AppTheme.danger),
                        ),
                      ],
                      const SizedBox(height: 24),
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: isLoading ? null : signIn,
                          icon: const Icon(Icons.login),
                          label: Text(isLoading ? "Signing in..." : "Sign in"),
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
