import 'package:flutter/material.dart';
import 'package:majstorio_desktop/screens/categories_screen.dart';
import 'package:majstorio_desktop/screens/cities_screen.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

// The frame shared by every admin screen: top bar, menu and the content of the screen.
class MasterScreen extends StatelessWidget {
  const MasterScreen({super.key, required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      drawer: Drawer(
        child: ListView(
          padding: EdgeInsets.zero,
          children: [
            const _DrawerHeader(),
            const SizedBox(height: 12),
            _DrawerButton(
              icon: Icons.home_outlined,
              label: "Home",
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => MyWidget()),
                );
              },
            ),
            _DrawerButton(
              icon: Icons.people_outline,
              label: "Users and Professionals",
              onPressed: () {},
            ),
            _DrawerButton(
              icon: Icons.assignment_outlined,
              label: "Requests, Offers and Appointments",
              onPressed: () {},
            ),
            _DrawerButton(
              icon: Icons.category_outlined,
              label: "Service Categories",
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => CategoriesScreen()),
                );
              },
            ),
            _DrawerButton(
              icon: Icons.location_city_outlined,
              label: "Cities",
              onPressed: () {
                Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => CitiesScreen()),
                );
              },
            ),
            _DrawerButton(
              icon: Icons.picture_as_pdf_outlined,
              label: "Reports",
              onPressed: () {},
            ),
          ],
        ),
      ),
      appBar: AppBar(
        title: Text(title),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(2),
          child: Container(height: 2, color: AppTheme.gold),
        ),
      ),
      body: child,
    );
  }
}

class _DrawerHeader extends StatelessWidget {
  const _DrawerHeader();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 170,
      padding: const EdgeInsets.fromLTRB(24, 24, 24, 20),
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppTheme.ink, AppTheme.inkDeep],
        ),
        border: Border(bottom: BorderSide(color: AppTheme.gold, width: 2)),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.end,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: AppTheme.gold,
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Icon(Icons.handyman, color: AppTheme.inkDeep),
          ),
          const SizedBox(height: 16),
          const Text(
            "Majstorio",
            style: TextStyle(
              color: Colors.white,
              fontSize: 24,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.5,
            ),
          ),
          const SizedBox(height: 2),
          const Text(
            "ADMINISTRATION",
            style: TextStyle(
              color: AppTheme.gold,
              fontSize: 11,
              fontWeight: FontWeight.w600,
              letterSpacing: 2.5,
            ),
          ),
        ],
      ),
    );
  }
}

class _DrawerButton extends StatelessWidget {
  const _DrawerButton({
    required this.icon,
    required this.label,
    required this.onPressed,
  });

  final IconData icon;
  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 2),
      child: TextButton.icon(
        onPressed: onPressed,
        icon: Icon(icon, color: AppTheme.gold),
        label: Text(
          label,
          style: const TextStyle(
            color: Colors.white70,
            fontWeight: FontWeight.w500,
          ),
        ),
        style: TextButton.styleFrom(
          alignment: Alignment.centerLeft,
          overlayColor: Colors.white,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(10),
          ),
        ),
      ),
    );
  }
}
