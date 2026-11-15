import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/user/user.dart';
import 'package:majstorio_desktop/providers/user/user_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

import 'package:provider/provider.dart';

class UserDetailsScreen extends StatefulWidget {
  const UserDetailsScreen({super.key, required this.toUpdate});

  // Users are only edited here; creating one will be the register endpoint.
  final User toUpdate;

  @override
  State<UserDetailsScreen> createState() => _UserDetailsScreenState();
}

class _UserDetailsScreenState extends State<UserDetailsScreen> {
  final _userName = TextEditingController();
  final _firstName = TextEditingController();
  final _lastName = TextEditingController();
  final _email = TextEditingController();
  final _phoneNumber = TextEditingController();
  late UserProvider _userProvider;

  @override
  void initState() {
    _userProvider = context.read<UserProvider>();
    isActive = widget.toUpdate.isActive;
    _userName.text = widget.toUpdate.userName;
    _firstName.text = widget.toUpdate.firstName;
    _lastName.text = widget.toUpdate.lastName;
    _email.text = widget.toUpdate.email;
    _phoneNumber.text = widget.toUpdate.phoneNumber ?? "";
    super.initState();
  }

  // TODO: User has no role field yet; the selection is not sent on update.
  String userRole = "User";

  var isActive = true;
  final key = GlobalKey<FormState>();

  @override
  void dispose() {
    _userName.dispose();
    _firstName.dispose();
    _lastName.dispose();
    _email.dispose();
    _phoneNumber.dispose();
    super.dispose();
  }

  String? _required(String? value) {
    if (value == null || value.trim().isEmpty) {
      return "The text is required";
    }
    return null;
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Edit user",
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
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
                              color: AppTheme.goldSoft,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: const Icon(
                              Icons.person_outline,
                              color: AppTheme.ink,
                            ),
                          ),
                          const SizedBox(width: 16),
                          const Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  "User details",
                                  style: TextStyle(
                                    fontSize: 20,
                                    fontWeight: FontWeight.w700,
                                    color: AppTheme.ink,
                                  ),
                                ),
                                SizedBox(height: 2),
                                Text(
                                  "Edit the account information and status of the user.",
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
                        decoration: const InputDecoration(
                          labelText: "Username",
                          prefixIcon: Icon(Icons.alternate_email),
                        ),
                        validator: _required,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _firstName,
                        decoration: const InputDecoration(
                          labelText: "First name",
                          prefixIcon: Icon(Icons.badge_outlined),
                        ),
                        validator: _required,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _lastName,
                        decoration: const InputDecoration(
                          labelText: "Last name",
                          prefixIcon: Icon(Icons.badge_outlined),
                        ),
                        validator: _required,
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _email,
                        decoration: const InputDecoration(
                          labelText: "Email",
                          prefixIcon: Icon(Icons.email_outlined),
                        ),
                        validator: (value) {
                          if (value == null || value.trim().isEmpty) {
                            return "The text is required";
                          }
                          if (!value.contains("@")) {
                            return "Enter a valid email";
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      TextFormField(
                        controller: _phoneNumber,
                        decoration: const InputDecoration(
                          labelText: "Phone number",
                          prefixIcon: Icon(Icons.phone_outlined),
                        ),
                      ),
                      const SizedBox(height: 16),
                      DropdownButtonFormField<String>(
                        initialValue: userRole,
                        decoration: const InputDecoration(
                          labelText: "Role",
                          prefixIcon: Icon(Icons.admin_panel_settings_outlined),
                        ),
                        items: const [
                          DropdownMenuItem(
                            value: "Administrator",
                            child: Text("Administrator"),
                          ),
                          DropdownMenuItem(value: "User", child: Text("User")),
                        ],
                        onChanged: (value) {
                          if (value == null) return;
                          setState(() => userRole = value);
                        },
                      ),
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          Checkbox(
                            value: isActive,
                            semanticLabel: "Is active",
                            onChanged: (value) {
                              setState(() {
                                isActive = value ?? false;
                              });
                            },
                          ),
                          const Text("Active"),
                        ],
                      ),
                      const SizedBox(height: 24),
                      Align(
                        alignment: Alignment.centerRight,
                        child: ElevatedButton.icon(
                          onPressed: () async {
                            var isValid = key.currentState?.validate();
                            if (isValid != null && isValid == true) {
                              var updateDto = User(
                                id: widget.toUpdate.id,
                                userName: _userName.text,
                                email: _email.text,
                                phoneNumber: _phoneNumber.text.trim().isEmpty
                                    ? null
                                    : _phoneNumber.text,
                                firstName: _firstName.text,
                                lastName: _lastName.text,
                                profileImageUrl:
                                    widget.toUpdate.profileImageUrl,
                                isActive: isActive,
                              );
                              var statusCode = await _userProvider.update(
                                updateDto,
                              );
                              if (statusCode == 200) {
                                Navigator.pop(context, true);
                              }
                            }
                          },
                          icon: const Icon(Icons.save_outlined),
                          label: const Text("Update"),
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
