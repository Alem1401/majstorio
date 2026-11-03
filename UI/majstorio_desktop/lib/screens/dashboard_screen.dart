import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';

class MyWidget extends StatefulWidget {
  const MyWidget({super.key});

  @override
  State<MyWidget> createState() => _MyWidgetState();
}

class _MyWidgetState extends State<MyWidget> {
  DateTime now = DateTime.now();

  String get formattedDate {
    String twoDigits(int n) => n.toString().padLeft(2, '0');
    return "${twoDigits(now.day)}.${twoDigits(now.month)}.${now.year}.";
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: 'Home',
      child: Container(
        margin: const EdgeInsets.all(40),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              "Welcome, Administrator",
              style: TextStyle(
                fontSize: 28,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
                letterSpacing: 0.2,
              ),
            ),
            const SizedBox(height: 6),
            Row(
              children: [
                const Icon(
                  Icons.calendar_today_outlined,
                  size: 14,
                  color: AppTheme.textMuted,
                ),
                const SizedBox(width: 8),
                Text(
                  formattedDate,
                  style: const TextStyle(color: AppTheme.textMuted),
                ),
              ],
            ),
            const SizedBox(height: 32),
            const Row(
              children: [
                Expanded(
                  child: _StatCard(
                    value: 3,
                    label: "Pending Professionals",
                    icon: Icons.hourglass_empty,
                  ),
                ),
                SizedBox(width: 20),
                Expanded(
                  child: _StatCard(
                    value: 12,
                    label: "Active Requests",
                    icon: Icons.assignment_outlined,
                  ),
                ),
                SizedBox(width: 20),
                Expanded(
                  child: _StatCard(
                    value: 25,
                    label: "Completed Services",
                    icon: Icons.check_circle_outline,
                  ),
                ),
                SizedBox(width: 20),
                Expanded(
                  child: _StatCard(
                    value: 8,
                    label: "Active Users",
                    icon: Icons.people_outline,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.value,
    required this.label,
    required this.icon,
  });

  final int value;
  final String label;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppTheme.goldSoft,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: AppTheme.ink, size: 24),
            ),
            const SizedBox(height: 20),
            Text(
              value.toString(),
              style: const TextStyle(
                fontSize: 36,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
                height: 1,
              ),
            ),
            const SizedBox(height: 8),
            Text(label, style: const TextStyle(color: AppTheme.textMuted)),
          ],
        ),
      ),
    );
  }
}
