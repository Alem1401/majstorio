import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// Step 2 of resetting a forgotten password: e-mail, the token from step 1 and the new password.
class ResetPasswordScreen extends StatefulWidget {
  const ResetPasswordScreen({super.key, this.email, this.token});

  final String? email;
  final String? token;

  @override
  State<ResetPasswordScreen> createState() => _ResetPasswordScreenState();
}

class _ResetPasswordScreenState extends State<ResetPasswordScreen> {
  final key = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _token = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();
  late AuthProvider _authProvider;

  var isSaving = false;

  @override
  void initState() {
    _authProvider = context.read<AuthProvider>();
    _email.text = widget.email ?? "";
    _token.text = widget.token ?? "";
    super.initState();
  }

  @override
  void dispose() {
    _email.dispose();
    _token.dispose();
    _password.dispose();
    _confirmPassword.dispose();
    super.dispose();
  }

  void save() async {
    if (key.currentState?.validate() != true) return;
    setState(() {
      isSaving = true;
    });
    try {
      await _authProvider.resetPassword(_email.text.trim(), _token.text.trim(), _password.text);
      if (!mounted) return;
      showMessage(context, "Lozinka je promijenjena. Prijavite se novom lozinkom.");
      // Back to the login screen, which is the first screen.
      Navigator.popUntil(context, (route) => route.isFirst);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
      });
      showMessage(context, exceptionText(e));
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Nova lozinka",
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
                    icon: Icons.lock_reset,
                    title: "Nova lozinka",
                    subtitle: "Unesite token i novu lozinku.",
                  ),
                  const SizedBox(height: 24),
                  const Divider(),
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: "E-pošta", prefixIcon: Icon(Icons.email_outlined)),
                    validator: validate.email,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _token,
                    decoration: const InputDecoration(labelText: "Token", prefixIcon: Icon(Icons.key_outlined)),
                    validator: (value) => validate.requiredText(value, "Token je obavezan"),
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _password,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: "Nova lozinka", prefixIcon: Icon(Icons.lock_outline)),
                    validator: validate.password,
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _confirmPassword,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: "Ponovite novu lozinku", prefixIcon: Icon(Icons.lock_outline)),
                    validator: (value) => value != _password.text ? "Lozinke se ne podudaraju" : null,
                  ),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: isSaving ? null : save,
                      icon: const Icon(Icons.check),
                      label: Text(isSaving ? "Spremanje..." : "Promijeni lozinku"),
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
