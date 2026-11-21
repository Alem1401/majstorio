import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/screens/password/reset_password_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:provider/provider.dart';

// Step 1 of resetting a forgotten password: ask for a one-time token.
// The answer does not say whether the e-mail exists. No e-mail is sent yet; in Development
// the API returns the token, which is shown here so it can be used on the next screen.
class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final key = GlobalKey<FormState>();
  final _email = TextEditingController();
  late AuthProvider _authProvider;

  var isSending = false;
  var isSent = false;
  String? developmentToken;
  String? error;

  @override
  void initState() {
    _authProvider = context.read<AuthProvider>();
    super.initState();
  }

  @override
  void dispose() {
    _email.dispose();
    super.dispose();
  }

  void requestToken() async {
    if (key.currentState?.validate() != true) return;
    setState(() {
      isSending = true;
      error = null;
    });
    try {
      var token = await _authProvider.forgotPassword(_email.text.trim());
      if (!mounted) return;
      setState(() {
        isSending = false;
        isSent = true;
        developmentToken = token;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        isSending = false;
        error = exceptionText(e);
      });
    }
  }

  void openReset() {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ResetPasswordScreen(
          email: _email.text.trim(),
          token: developmentToken,
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Zaboravljena lozinka",
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Card(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Form(
              key: key,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const FormHeader(
                    icon: Icons.key_outlined,
                    title: "Reset lozinke",
                    subtitle: "Unesite e-poštu svog računa da dobijete token za reset.",
                  ),
                  const SizedBox(height: 24),
                  const Divider(),
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(
                      labelText: "E-pošta",
                      prefixIcon: Icon(Icons.email_outlined),
                    ),
                    validator: validate.email,
                  ),
                  if (error != null) ...[
                    const SizedBox(height: 16),
                    Text(error!, style: const TextStyle(color: AppTheme.danger)),
                  ],
                  if (isSent) ...[
                    const SizedBox(height: 16),
                    const Text(
                      "Ako račun sa ovom e-poštom postoji, izdat je token za reset lozinke. "
                      "Token vrijedi 15 minuta i može se iskoristiti samo jednom.",
                    ),
                    if (developmentToken != null) ...[
                      const SizedBox(height: 12),
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: AppTheme.goldSoft,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: SelectableText(
                          "Razvojni token: $developmentToken",
                          style: const TextStyle(color: AppTheme.ink),
                        ),
                      ),
                    ],
                  ],
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: isSending ? null : requestToken,
                      icon: const Icon(Icons.send_outlined),
                      label: Text(isSending ? "Slanje..." : "Zatraži token"),
                    ),
                  ),
                  const SizedBox(height: 8),
                  Center(
                    child: TextButton(
                      onPressed: openReset,
                      child: const Text("Već imam token"),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
