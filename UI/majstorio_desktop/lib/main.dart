import 'package:flutter/material.dart';
import 'package:majstorio_desktop/screens/dashboard_screen.dart';
import 'package:majstorio_desktop/screens/cities_screen.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return const MaterialApp(
      home: MasterScreen(title: "Welcome", child: MyWidget()),
    );
  }
}

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
            const DrawerHeader(
              decoration: BoxDecoration(color: Colors.purple),
              child: Align(
                alignment: Alignment.bottomLeft,
                child: Text(
                  "Majstorio",
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 24,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            _DrawerButton(
              icon: Icons.home_outlined,
              label: "Home",
              onPressed: () {},
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
              onPressed: () {},
            ),
            _DrawerButton(
              icon: Icons.location_city_outlined,
              label: "Cities",
              onPressed: () {},
            ),
            _DrawerButton(
              icon: Icons.picture_as_pdf_outlined,
              label: "Reports",
              onPressed: () {},
            ),
          ],
        ),
      ),
      backgroundColor: Colors.grey.shade100,
      appBar: AppBar(
        backgroundColor: Colors.purple,
        centerTitle: true,
        title: Text(title, style: const TextStyle(color: Colors.white)),
      ),
      body: CitiesScreen(),
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
        icon: Icon(icon, color: Colors.purple),
        label: Text(label, style: TextStyle(color: Colors.grey.shade800)),
        style: TextButton.styleFrom(
          alignment: Alignment.centerLeft,
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
    );
  }
}
