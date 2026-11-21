import 'package:flutter/material.dart';
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The signed-in user's own password: the old one is required.
// The API signs the user out everywhere afterwards, so the app returns to the login screen.
class ChangePasswordScreen extends StatefulWidget {
  const ChangePasswordScreen({super.key});

  @override
  State<ChangePasswordScreen> createState() => _ChangePasswordScreenState();
}

class _ChangePasswordScreenState extends State<ChangePasswordScreen> {
  final key = GlobalKey<FormState>();
  final _oldPassword = TextEditingController();
  final _newPassword = TextEditingController();
  final _confirmPassword = TextEditingController();
  late AuthProvider _authProvider;

  var isSaving = false;
  String? oldPasswordError;

  @override
  void initState() {
    _authProvider = context.read<AuthProvider>();
    super.initState();
  }

  @override
  void dispose() {
    _oldPassword.dispose();
    _newPassword.dispose();
    _confirmPassword.dispose();
    super.dispose();
  }

  void save() async {
    oldPasswordError = null;
    if (key.currentState?.validate() != true) return;
    setState(() {
      isSaving = true;
    });
    try {
      await _authProvider.changePassword(_oldPassword.text, _newPassword.text);
      if (!mounted) return;
      showMessage(context, "Lozinka je promijenjena. Prijavite se novom lozinkom.");
      Navigator.pushNamedAndRemoveUntil(context, loginRoute, (route) => false);
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
        // 400 here means the old password is wrong: show it under that field.
        if (e.statusCode == 400) oldPasswordError = e.message;
      });
      key.currentState?.validate();
      showMessage(context, e.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Promjena lozinke",
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
                    icon: Icons.lock_outline,
                    title: "Promjena lozinke",
                    subtitle: "Nakon promjene ponovo se prijavljujete.",
                  ),
                  const SizedBox(height: 24),
                  const Divider(),
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _oldPassword,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: "Stara lozinka", prefixIcon: Icon(Icons.lock_outline)),
                    validator: (value) {
                      if (value == null || value.isEmpty) return "Stara lozinka je obavezna";
                      return oldPasswordError;
                    },
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _newPassword,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: "Nova lozinka", prefixIcon: Icon(Icons.lock_reset)),
                    validator: (value) {
                      var error = validate.password(value);
                      if (error != null) return error;
                      if (value == _oldPassword.text) return "Nova lozinka mora biti različita od stare";
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),
                  TextFormField(
                    controller: _confirmPassword,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: "Ponovite novu lozinku", prefixIcon: Icon(Icons.lock_reset)),
                    validator: (value) => value != _newPassword.text ? "Lozinke se ne podudaraju" : null,
                  ),
                  const SizedBox(height: 24),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: isSaving ? null : save,
                      icon: const Icon(Icons.check),
                      label: Text(isSaving ? "Spremanje..." : "Spremi"),
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
