import 'package:flutter/material.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/screens/account/account_screen.dart';
import 'package:majstorio_mobile/screens/appointment/appointments_screen.dart';
import 'package:majstorio_mobile/screens/home_screen.dart';
import 'package:majstorio_mobile/screens/offer/offers_screen.dart';
import 'package:majstorio_mobile/screens/service_request/open_requests_screen.dart';
import 'package:majstorio_mobile/screens/service_request/service_requests_screen.dart';
import 'package:majstorio_mobile/screens/tradesperson_profile/tradesperson_profiles_screen.dart';

class _Tab {
  const _Tab(this.label, this.icon, this.selectedIcon, this.screen);

  final String label;
  final IconData icon;
  final IconData selectedIcon;
  final Widget screen;
}

// The menu of the mobile app: a bottom bar with the main screens of the signed-in role.
// The desktop app has a drawer instead; on a phone the bottom bar is the expected menu.
// The tabs stay alive while switching, so each keeps its scroll position and filters.
class MainNavigation extends StatefulWidget {
  const MainNavigation({super.key, this.initialIndex = 0});

  // Which tab is open first, for example 2 ("Zahtjevi") after sending a request from a profile.
  final int initialIndex;

  @override
  State<MainNavigation> createState() => _MainNavigationState();
}

class _MainNavigationState extends State<MainNavigation> {
  late var index = widget.initialIndex;

  final _customerTabs = const [
    _Tab("Početna", Icons.home_outlined, Icons.home, HomeScreen()),
    _Tab("Pretraga", Icons.search, Icons.search, TradespersonProfilesScreen()),
    _Tab("Zahtjevi", Icons.assignment_outlined, Icons.assignment, ServiceRequestsScreen()),
    _Tab("Profil", Icons.person_outline, Icons.person, AccountScreen()),
  ];

  final _tradespersonTabs = const [
    _Tab("Radni prostor", Icons.work_outline, Icons.work, OpenRequestsScreen()),
    _Tab("Ponude", Icons.request_quote_outlined, Icons.request_quote, OffersScreen()),
    _Tab("Termini", Icons.event_outlined, Icons.event, AppointmentsScreen()),
    _Tab("Profil", Icons.person_outline, Icons.person, AccountScreen()),
  ];

  @override
  Widget build(BuildContext context) {
    var tabs = AuthSession.isTradesperson ? _tradespersonTabs : _customerTabs;

    return Scaffold(
      body: IndexedStack(
        index: index,
        children: [for (var tab in tabs) tab.screen],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) {
          setState(() {
            index = value;
          });
        },
        destinations: [
          for (var tab in tabs)
            NavigationDestination(
              icon: Icon(tab.icon),
              selectedIcon: Icon(tab.selectedIcon),
              label: tab.label,
            ),
        ],
      ),
    );
  }
}
