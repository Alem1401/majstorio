import 'package:flutter/material.dart';
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/user/user_profile.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/screens/account/profile_edit_screen.dart';
import 'package:majstorio_mobile/screens/appointment/appointments_screen.dart';
import 'package:majstorio_mobile/screens/news_post/news_post_screen.dart';
import 'package:majstorio_mobile/screens/password/change_password_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_requests_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/widgets/common.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The "Profil" tab: who is signed in (photo and name), profile editing, history, news, password change and sign out.
class AccountScreen extends StatefulWidget {
  const AccountScreen({super.key});

  @override
  State<AccountScreen> createState() => _AccountScreenState();
}

class _AccountScreenState extends State<AccountScreen> {
  late AccountProvider _accountProvider;
  UserProfile? profile;

  @override
  void initState() {
    _accountProvider = context.read<AccountProvider>();
    // A saved profile or a new photo shows here right away.
    _accountProvider.addListener(loadProfile);
    loadProfile();
    super.initState();
  }

  @override
  void dispose() {
    _accountProvider.removeListener(loadProfile);
    super.dispose();
  }

  // The header falls back to the user name from the token when the profile cannot be loaded.
  Future<void> loadProfile() async {
    try {
      var result = await _accountProvider.getMine();
      if (!mounted) return;
      setState(() {
        profile = result;
      });
    } catch (e) {
      // Keep the fallback header.
    }
  }

  String get roleName => AuthSession.isTradesperson ? "Majstor" : "Korisnik";

  void open(BuildContext context, Widget screen) {
    Navigator.push(context, MaterialPageRoute(builder: (context) => screen));
  }

  void signOut(BuildContext context) async {
    var confirmed = await showConfirmDialog(context, "Da li se želite odjaviti?");
    if (!confirmed || !context.mounted) return;

    var navigator = Navigator.of(context);
    var messenger = ScaffoldMessenger.of(context);
    var serverConfirmed = await context.read<AuthProvider>().logout();
    if (!serverConfirmed) {
      // Signed out on the phone, but the server could not be told.
      messenger.showSnackBar(
        const SnackBar(content: Text("Odjavljeni ste na uređaju, ali server nije potvrdio odjavu.")),
      );
    }
    // Removes every open screen, so "back" cannot return to them.
    navigator.pushNamedAndRemoveUntil(loginRoute, (route) => false);
  }

  Widget tile(IconData icon, String title, VoidCallback onTap, {Color? color}) {
    return ListTile(
      minTileHeight: 56,
      leading: Icon(icon, color: color ?? AppTheme.ink),
      title: Text(title, style: TextStyle(color: color)),
      trailing: const Icon(Icons.chevron_right),
      onTap: onTap,
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Profil",
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Row(
                children: [
                  NetworkPicture(url: profile?.profileImageUrl, size: 64),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          profile?.fullName ?? AuthSession.userName ?? "",
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppTheme.ink),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          "${profile?.userName ?? AuthSession.userName ?? ""} · $roleName",
                          style: const TextStyle(color: AppTheme.textMuted),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: Column(
              children: [
                tile(Icons.edit_outlined, "Uredi profil", () {
                  open(context, const ProfileEditScreen());
                }),
                const Divider(),
                if (AuthSession.isCustomer) ...[
                  tile(Icons.history, "Historija zahtjeva", () {
                    open(context, const ServiceRequestsScreen(history: true));
                  }),
                  const Divider(),
                  tile(Icons.event_outlined, "Moji termini", () {
                    open(context, const AppointmentsScreen());
                  }),
                ],
                if (AuthSession.isTradesperson)
                  tile(Icons.history, "Historija poslova", () {
                    open(context, const AppointmentsScreen(history: true));
                  }),
                const Divider(),
                tile(Icons.article_outlined, "Novosti", () {
                  open(context, const NewsPostScreen());
                }),
                const Divider(),
                tile(Icons.lock_outline, "Promjena lozinke", () {
                  open(context, const ChangePasswordScreen());
                }),
              ],
            ),
          ),
          const SizedBox(height: 16),
          Card(
            child: tile(Icons.logout, "Odjava", () => signOut(context), color: AppTheme.danger),
          ),
        ],
      ),
    );
  }
}
