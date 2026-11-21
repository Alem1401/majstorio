import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/register_request.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/validators.dart' as validate;
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// Creates a customer account. Tradesperson accounts are approved by the administrator on the desktop.
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final key = GlobalKey<FormState>();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _userName = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();
  late AuthProvider _authProvider;

  var isSaving = false;
  // Errors the API returned for single fields, shown under those fields.
  var serverErrors = <String, String>{};

  @override
  void initState() {
    _authProvider = context.read<AuthProvider>();
    super.initState();
  }

  @override
  void dispose() {
    for (var controller in [_firstName, _lastName, _userName, _email, _phone, _password, _confirmPassword]) {
      controller.dispose();
    }
    super.dispose();
  }

  void register() async {
    setState(() {
      serverErrors = {};
    });
    if (key.currentState?.validate() != true) return;

    setState(() {
      isSaving = true;
    });
    try {
      await _authProvider.register(
        RegisterRequest(
          userName: _userName.text.trim(),
          email: _email.text.trim(),
          password: _password.text,
          firstName: _firstName.text.trim(),
          lastName: _lastName.text.trim(),
          phoneNumber: _phone.text.trim(),
        ),
      );
      if (!mounted) return;
      showMessage(context, "Račun je kreiran. Prijavite se.");
      Navigator.pop(context, _userName.text.trim());
    } on ApiException catch (e) {
      if (!mounted) return;
      setState(() {
        isSaving = false;
        serverErrors = e.fieldErrors;
      });
      key.currentState?.validate();
      showMessage(context, e.message);
    }
  }

  Widget field(
    TextEditingController controller,
    String label,
    IconData icon,
    String apiName, {
    String? Function(String?)? validator,
    TextInputType? keyboardType,
    bool obscure = false,
  }) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: TextFormField(
        controller: controller,
        obscureText: obscure,
        keyboardType: keyboardType,
        textInputAction: TextInputAction.next,
        decoration: InputDecoration(labelText: label, prefixIcon: Icon(icon)),
        validator: (value) => validator?.call(value) ?? serverErrors[apiName],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Registracija",
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
                    icon: Icons.person_add_alt_1_outlined,
                    title: "Novi račun",
                    subtitle: "Unesite podatke za korisnički račun.",
                  ),
                  const SizedBox(height: 24),
                  const Divider(),
                  const SizedBox(height: 24),
                  field(_firstName, "Ime", Icons.badge_outlined, "firstName",
                      validator: (value) => validate.requiredText(value, "Ime je obavezno")),
                  field(_lastName, "Prezime", Icons.badge_outlined, "lastName",
                      validator: (value) => validate.requiredText(value, "Prezime je obavezno")),
                  field(_userName, "Korisničko ime", Icons.person_outline, "userName",
                      validator: (value) => validate.requiredText(value, "Korisničko ime je obavezno")),
                  field(_email, "E-pošta", Icons.email_outlined, "email",
                      validator: validate.email, keyboardType: TextInputType.emailAddress),
                  field(_phone, "Telefon (nije obavezno)", Icons.phone_outlined, "phoneNumber",
                      validator: validate.phone, keyboardType: TextInputType.phone),
                  field(_password, "Lozinka", Icons.lock_outline, "password",
                      validator: validate.password, obscure: true),
                  field(_confirmPassword, "Ponovite lozinku", Icons.lock_outline, "confirmPassword",
                      obscure: true, validator: (value) {
                    if (value != _password.text) return "Lozinke se ne podudaraju";
                    return null;
                  }),
                  const SizedBox(height: 8),
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: isSaving ? null : register,
                      icon: const Icon(Icons.check),
                      label: Text(isSaving ? "Spremanje..." : "Registruj se"),
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
