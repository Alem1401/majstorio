import 'package:flutter/material.dart';

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
    return Container(
      margin: const EdgeInsets.all(40),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            "Welcome, Administrator",
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Text(formattedDate, style: TextStyle(color: Colors.grey.shade600)),
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
              SizedBox(width: 16),
              Expanded(
                child: _StatCard(
                  value: 12,
                  label: "Active Requests",
                  icon: Icons.assignment,
                ),
              ),
              SizedBox(width: 16),
              Expanded(
                child: _StatCard(
                  value: 25,
                  label: "Completed Services",
                  icon: Icons.check_circle_outline,
                ),
              ),
              SizedBox(width: 16),
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
      color: Colors.white,
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: Colors.purple, size: 28),
            const SizedBox(height: 16),
            Text(
              value.toString(),
              style: const TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 4),
            Text(label, style: TextStyle(color: Colors.grey.shade700)),
          ],
        ),
      ),
    );
  }
}
