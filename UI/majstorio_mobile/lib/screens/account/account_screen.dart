import 'package:flutter/material.dart';
import 'package:majstorio_mobile/constants.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/providers/auth/auth_provider.dart';
import 'package:majstorio_mobile/screens/appointment/appointments_screen.dart';
import 'package:majstorio_mobile/screens/news_post/news_post_screen.dart';
import 'package:majstorio_mobile/screens/password/change_password_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_requests_screen.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:provider/provider.dart';

// The "Profil" tab: who is signed in, history, news, password change and sign out.
class AccountScreen extends StatelessWidget {
  const AccountScreen({super.key});

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
                  Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: AppTheme.goldSoft,
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: const Icon(Icons.person, color: AppTheme.ink, size: 32),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          AuthSession.userName ?? "",
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700, color: AppTheme.ink),
                        ),
                        const SizedBox(height: 2),
                        Text(roleName, style: const TextStyle(color: AppTheme.textMuted)),
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
