import 'package:flutter/material.dart';
import 'package:majstorio_desktop/layouts/master_screen.dart';
import 'package:majstorio_desktop/models/dashboard/dashboard.dart';
import 'package:majstorio_desktop/providers/dashboard/dashboard_provider.dart';
import 'package:majstorio_desktop/theme/app_theme.dart';
import 'package:provider/provider.dart';

class MyWidget extends StatefulWidget {
  const MyWidget({super.key});

  @override
  State<MyWidget> createState() => _MyWidgetState();
}

class _MyWidgetState extends State<MyWidget> {
  late DashboardProvider _provider;
  DateTime now = DateTime.now();

  // Null while the numbers are loading.
  Dashboard? dashboard;
  String? error;

  String get formattedDate {
    String twoDigits(int n) => n.toString().padLeft(2, '0');
    return "${twoDigits(now.day)}.${twoDigits(now.month)}.${now.year}.";
  }

  @override
  void initState() {
    _provider = context.read<DashboardProvider>();
    loadDashboard();
    super.initState();
  }

  void loadDashboard() async {
    try {
      var response = await _provider.get();
      if (!mounted) return;
      setState(() {
        dashboard = response;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        error = "The dashboard could not be loaded. Check that the API is running.";
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: 'Home',
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(40),
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
            if (error != null)
              Text(error!, style: const TextStyle(color: AppTheme.danger))
            else if (dashboard == null)
              const Center(child: CircularProgressIndicator())
            else
              _buildContent(dashboard!),
          ],
        ),
      ),
    );
  }

  Widget _buildContent(Dashboard data) {
    return Column(
      children: [
        Row(
          children: [
            Expanded(
              child: _StatCard(
                value: data.pendingTradespersonCount,
                label: "Pending Professionals",
                icon: Icons.hourglass_empty,
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              child: _StatCard(
                value: data.activeRequestCount,
                label: "Active Requests",
                icon: Icons.assignment_outlined,
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              child: _StatCard(
                value: data.completedServiceCount,
                label: "Completed Services",
                icon: Icons.check_circle_outline,
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              child: _StatCard(
                value: data.activeUserCount,
                label: "Active Users",
                icon: Icons.people_outline,
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),
        Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: _BarChartCard(
                title: "Requests by category",
                items: data.requestsByCategory,
              ),
            ),
            const SizedBox(width: 20),
            Expanded(
              child: _BarChartCard(
                title: "Requests by city",
                items: data.requestsByCity,
              ),
            ),
          ],
        ),
      ],
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

// A simple horizontal bar chart: one row per item, the longest bar is the largest count.
class _BarChartCard extends StatelessWidget {
  const _BarChartCard({required this.title, required this.items});

  final String title;
  final List<NameCount> items;

  @override
  Widget build(BuildContext context) {
    // The API sends the items sorted, so the first one has the largest count.
    final max = items.isEmpty ? 1 : items.first.count;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: const TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.w700,
                color: AppTheme.ink,
              ),
            ),
            const SizedBox(height: 16),
            if (items.isEmpty)
              const Text(
                "There are no service requests yet.",
                style: TextStyle(color: AppTheme.textMuted),
              ),
            for (final item in items)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Row(
                  children: [
                    SizedBox(
                      width: 190,
                      child: Text(item.name, overflow: TextOverflow.ellipsis),
                    ),
                    Expanded(
                      child: LinearProgressIndicator(
                        value: item.count / max,
                        minHeight: 14,
                        borderRadius: BorderRadius.circular(7),
                        color: AppTheme.gold,
                        backgroundColor: AppTheme.goldSoft,
                      ),
                    ),
                    SizedBox(
                      width: 40,
                      child: Text(
                        item.count.toString(),
                        textAlign: TextAlign.right,
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
